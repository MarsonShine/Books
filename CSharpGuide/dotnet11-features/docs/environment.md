# 进程、DNS 与 HTTP 指标：需要操作系统或网络的示例

这里的三个例子在 `EnvironmentDemos.cs`。从仓库根目录运行（主项目入口为 `environment`）：

```powershell
# 不访问外网；查找当前进程，再启动 dotnet --version
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment process

# 只在本机 127.0.0.1 启动一次 HTTP 服务，不发送外网请求
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment http

# 真实 DNS 请求：明确填写需要查询的域名
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment dns example.com

# 可选：明确指定 DNS 服务器的 IP（服务器需要允许该机器发起查询）
dotnet run --project CSharpGuide/dotnet11-features -c Release -- environment dns example.com 1.1.1.1
```

`process` 需要本机 PATH 中有 `dotnet`；`http` 需要系统允许绑定回环地址和一个临时端口；`dns` 需要系统 DNS 或指定 DNS 服务器可达，示例仅在 Windows、Linux 和 macOS 上执行。DNS 的记录、TTL 和网络条件会随时间变化，不要把示例输出当作固定答案。查询会暴露给所使用的 DNS 服务器，请使用愿意公开查询的域名。

## 进程：逐行收输出，再拿退出状态

`Process.TryGetProcessById` 返回 `bool`，找不到正整数 PID 时可返回 `false`，调用方可以不靠异常表达“进程不存在”。例子只查询自己的 PID，不探测其他人的进程。`Process` 实例用完要释放；查询成功也不代表另一个进程今后仍然存在，实际获取后仍可能退出。

接着程序启动自己的 `dotnet --version` 子进程，用 `ReadAllLinesAsync` 按行读取输出。每个 `ProcessOutputLine` 有 `Content` 和 `StandardError`，后者说明这行来自标准错误还是标准输出。两个管道异步收集的行不应拿来推断子进程在两个管道之间的严格写入顺序。

读取结束后，`WaitForExitStatusAsync` 给出 `ProcessExitStatus`：正常情况看 `ExitCode`；在 Unix 上若由信号终止，`Signal` 可提供终止信号。这个 API 在 Unix 上只能取得子进程的退出状态。示例设置十秒取消期限，并在超时后只请求结束它自己启动的子进程。

注意，取消 `WaitForExitStatusAsync` 只结束等待，本身不终止子进程。是否结束子进程应根据程序的所有权和用途决定，不要对从外部取得的 PID 随意执行终止。另见 [进程 API 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#diagnostics-and-process-execution)。

## DNS：一次查询返回的不只有记录

以邮件域名为例，MX 记录告诉邮件发送方应联系哪些邮件服务器及其优先级。示例用 `Dns.ResolveMxAsync` 查询系统 DNS，再读取：

- `ResponseCode`：DNS 服务器的响应码。
- `Records`：MX 列表；每条记录包含 `Preference`、`Exchange` 和 `Ttl`。优先级数值较小的服务器一般优先。
- `NegativeCacheTtl`：适用时建议缓存“不存在／没有该类型记录”结果的时间；值为零可能表示不适用或当前平台无法取得，不等于一定发生错误。

一次返回的记录数为零时，要先看响应码：域名不存在、该域名没有 MX 记录、查询发生错误是不同情况。仅凭 `Records.Count == 0` 不能断言域名不存在。某些域名返回交换主机 `.`，这是声明不接收邮件的特殊记录，也不代表查询出错。

如果传入可选服务器 IP，代码通过 `DnsResolverOptions.Servers` 指定该服务器的 53 端口，再用 `DnsResolver.ResolveMxAsync` 查询；不改变机器的全局 DNS 设置。实例捕获选项时的服务器列表副本，后续修改选项不会改变已创建实例。公司网络可能拦截自定义 DNS，个别平台对服务器配置也有限制。示例传入取消令牌，最多等待约五秒。

相同思路可用 `ResolveTxtAsync`、`ResolveSrvAsync`、`ResolveNsAsync`、`ResolveCNameAsync` 等查询不同记录。选择记录类型由业务协议决定；MX 不是网站 IP，不能直接代替 A/AAAA 查询。[.NET 11 DNS 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#dns-record-resolution-apis)、[DnsResolver](https://learn.microsoft.com/en-us/dotnet/api/system.net.dnsresolver?view=net-11.0)。

## HTTP 客户端指标：为什么要主动采集

.NET 11 的 `System.Net.Http` 指标中，`http.client.active_requests` 和 `http.client.open_connections` 是可观察指标。它们不是每次变化就主动向你的 `MeterListener` 推一个值；使用 `MeterListener` 自行采集时，需要调用 `RecordObservableInstruments()`。

示例建立的只是本机临时 TCP 服务。过程是：

1. 启动监听器，订阅 `System.Net.Http` 中这两个指标。
2. `HttpClient` 向回环地址发起 GET；服务端收齐请求头，但暂时不发送响应。
3. 此时调用 `RecordObservableInstruments()`，可以观察仍在进行的请求和活动连接及其标签。
4. 放行服务端发送 `OK`，请求结束后再采集。服务端以 `Connection: close` 返回，回收完成时可能没有这两个活动时间序列，因此第二次没有数据不应一概判定为失败或直接推断为零。

监听器的回调收到的标签包含 URL scheme、服务器地址等。实际仪表可能按协议、请求方式和连接状态等维度分别报告多条记录；这些记录是快照，不能直接当作吞吐量。请求完成速度、连接池以及并发流量都会影响输出。示例有十秒超时，并释放 listener、客户端、服务端和连接。

`SocketsHttpHandler.ShouldEvictConnection` 在 .NET 11 API 标记为实验性（`SYSLIB5008`），因此没有将它伪装成正式版默认示例。若日后采用，需要单独启用实验诊断并处理 API 可能变化。[HTTP 指标发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#http-metrics-use-observable-instruments)、[连接回收 API](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.shouldevictconnection?view=net-11.0)。
