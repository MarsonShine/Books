# 进程、DNS 和 HTTP 指标

[EnvironmentDemos.cs](../EnvironmentDemos.cs) 有三组需要和操作系统或网络打交道的示例。它们不会随 `all` 自动执行，需要在仓库根目录明确指定：

```powershell
# 查询当前进程，再启动 dotnet --version；不访问外网
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment process

# 在本机回环地址启动临时 HTTP 服务，并采集客户端指标
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment http

# 通过系统 DNS 查询域名的 MX 记录
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment dns example.com

# 可选：指定 DNS 服务器 IP
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment dns example.com 1.1.1.1
```

`process` 需要 PATH 中能找到 `dotnet`；`http` 需要允许绑定 `127.0.0.1` 的临时端口；`dns` 需要能访问系统 DNS 或指定服务器，代码只在 Windows、Linux 和 macOS 上执行查询。DNS 服务器能看到所查的域名，运行时请填写愿意交给该服务器查询的名称。

## 查找进程、读取输出、等待退出

示例先用 `Process.TryGetProcessById(Environment.ProcessId, out ...)` 查找自己。方法返回 `bool`，调用方可以把“没有找到进程”当作普通情况处理；查找成功后得到的 `Process` 用完也要释放。

接下来，程序启动 `dotnet --version`，使用 `ReadAllLinesAsync` 读取输出。每一行的 `ProcessOutputLine` 都有 `Content` 和 `StandardError`，可以区分标准输出与标准错误。两个管道分别收集，不能根据收到的行推断子进程跨两个管道写入的严格先后顺序。

进程可以把正常结果写到标准输出（stdout），把错误信息写到标准错误（stderr）。这两路数据各有自己的管道。逐行读取时，`Content` 是文字，`StandardError` 告诉我们文字来自哪一路；读取结束只说明输出已读完，还要继续等待进程退出，才能知道退出状态。

读取结束后，`WaitForExitStatusAsync` 返回退出状态：正常退出时可以查看 `ExitCode`，Unix 上由信号终止时可以查看 `Signal`。在 Unix 上，这个 API 只能取得子进程的退出状态。示例设置了十秒期限，超时后只请求结束它自己启动的子进程。

取消等待并不会自动终止进程。是否需要终止，要看调用方是否拥有这个进程以及业务希望它怎样结束。[.NET 11 进程 API](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#diagnostics-and-process-execution)还列出了句柄、挂起启动和信号相关的方法。

## MX 记录里有哪些信息

邮件发送方可以通过 MX 记录找到负责接收某个域名邮件的服务器。示例调用 `Dns.ResolveMxAsync`，然后打印：

例如，给 `alice@example.com` 发送邮件时，发送方会先查询 `example.com` 的 MX 记录，找到应当联系的邮件服务器。MX 记录中的名称还需要进一步解析为 IP 地址，才能建立连接；它不表示收件人账号一定存在。

- `ResponseCode`：DNS 响应码。
- `Records`：返回的 MX 记录，每条包含优先级 `Preference`、服务器 `Exchange` 和 `Ttl`。优先级数值越小，通常越先尝试。
- `NegativeCacheTtl`：适用时，对“不存在”或“没有该类型记录”的结果建议缓存多久。零可能表示不适用或平台没有提供该值。

TTL 是“这条结果可以缓存多久”。例如记录的 TTL 为 300 秒，查询方通常可以在这段时间内复用结果，而不必每次都重新查询。`NegativeCacheTtl` 则针对没有找到的结果；它和记录上各自的 TTL 不是同一个字段。

没有返回记录时，应结合响应码判断是域名不存在、没有 MX 记录，还是查询失败。有些域名返回交换主机 `.`，表示它声明不接收邮件，这也是一种有效结果。

不指定服务器时，程序使用系统 DNS；指定 IP 时，会创建带 `DnsResolverOptions.Servers` 的 `DnsResolver`，把查询发往该地址的 53 端口。它不会修改系统全局 DNS 设置。公司网络可能阻止访问外部 DNS，示例最多等待约五秒。记录和 TTL 会变化，输出不必与别人的结果相同。

类似的 `ResolveTxtAsync`、`ResolveSrvAsync`、`ResolveNsAsync` 等方法用于查询其他类型；MX 记录不是网站的 A/AAAA 地址。[DNS 更新说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#dns-record-resolution-apis)和 [DnsResolver API](https://learn.microsoft.com/en-us/dotnet/api/system.net.dnsresolver?view=net-11.0)列出了具体类型。

## 在请求进行时查看 HTTP 指标

示例先用 `MeterListener` 订阅 `System.Net.Http` 的 `http.client.active_requests` 和 `http.client.open_connections`，然后在本机创建一个只处理一次请求的 TCP 服务。

`HttpClient` 发出 GET 后，服务端读完请求头，却暂时不发响应。此时请求仍在进行，程序调用 `RecordObservableInstruments()`，便能采集活动请求和连接的当前值。随后服务端返回 `OK`，程序再采集一次。

之所以先让服务端等一下，是为了给采集留出时间：如果服务端马上响应，请求可能在第一次采集前就结束，看不到“正在处理的请求”。在本机的一次运行中，等待响应时 `http.client.active_requests` 显示为 1；放行之后它不再处于进行状态。

这两个指标是可观察指标：使用 `MeterListener` 时，需要主动调用 `RecordObservableInstruments()` 才会取得当前读数。回调还会收到服务器地址、协议、连接状态等标签；实际可能按不同标签组合返回多条数据。它们是某一时刻的状态，不能直接当作每秒请求数。

例子返回 `Connection: close`。请求结束、连接回收后，第二次采集可能没有对应的活动时间序列；没有输出不一定代表监听器失败，也不能直接当作数值零。整个示例有十秒超时，结束时会释放客户端和监听器。

`SocketsHttpHandler.ShouldEvictConnection` 目前标记为实验性 API（`SYSLIB5008`），本例没有启用。相关资料见 [HTTP 指标说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#http-metrics-use-observable-instruments)与[连接回收 API](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.shouldevictconnection?view=net-11.0)。
