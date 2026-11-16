# ASP.NET Core 11：从请求走到响应

这个示例提供几个 Minimal API，用来观察 .NET 11 对联合类型、异步验证、端点过滤器和缓存指标的支持。代码在 [web/Program.cs](../web/Program.cs)。

需要 .NET 11 SDK 和 ASP.NET Core 共享框架，不需要数据库或额外 NuGet 包。在仓库根目录启动：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/web -c Release
```

服务只监听本机 `127.0.0.1:5087`。启动后保留这个终端，在另一个终端发送下面的请求；结束时回到服务终端按 Ctrl+C。如果 5087 端口已经被占用，可以修改 `web/Program.cs` 中的 `UseUrls`，并相应修改请求地址。

`127.0.0.1` 表示当前这台机器，`5087` 是程序监听的端口。浏览器或命令行向这个地址发 HTTP 请求后，ASP.NET Core 根据请求方法和路径选择处理代码。例如 `GET /reading/2` 会匹配代码中的 `/reading/{id:int}`，再把路径里的 `2` 交给参数 `id`。

下面带 `-SkipHttpErrorCheck` 的命令需要 PowerShell 7。Windows PowerShell 5.1 可以用 `curl.exe` 查看返回的 400 响应。

## 设备状态：同一个返回类型可以装数字或字符串

```powershell
Invoke-RestMethod http://127.0.0.1:5087/reading/2
Invoke-RestMethod http://127.0.0.1:5087/reading/3
```

路由参数是偶数时，示例把它作为读数返回；是奇数时返回“设备离线”。接口声明的返回类型是 `Reading`：

```csharp
public union Reading(int, string);
```

它可以存放 `int` 或 `string`。发送到 HTTP 客户端时，`System.Text.Json` 写出当前分支的值：第一个请求得到 JSON 数字 `2`，第二个得到 JSON 字符串 `"设备离线"`，没有额外包一层对象。

这是联合类型用于 JSON 响应的例子。请求中的 `id` 仍按路由约束绑定为 `int`；不能由此推断 union 已支持直接绑定路由、查询字符串或请求头。真实接口还需要和客户端约定稳定的响应含义。

## 会员编号：等待验证完成，再决定是否执行端点

`/members` 接收一个含 `code` 的 JSON 对象。示例中的 `[KnownCode]` 验证器继承 `AsyncValidationAttribute`，通过一段短暂延时模拟查询：`VIP-42` 通过，其他编号失败。

```powershell
Invoke-RestMethod http://127.0.0.1:5087/members -Method Post -ContentType application/json -Body '{"code":"VIP-42"}'
Invoke-WebRequest http://127.0.0.1:5087/members -Method Post -ContentType application/json -Body '{"code":"BAD"}' -SkipHttpErrorCheck
```

第一个请求返回 200 和 `accepted: true`；第二个返回 400，响应中的验证错误为“会员编号不存在”。`builder.Services.AddValidation()` 注册了验证，框架会等待异步规则完成，再决定是否执行处理函数。

代码里的 `Task.Delay` 不访问外部服务。实际应用可以通过依赖注入取得数据库或 HTTP 客户端，并把取消令牌传给查询。验证器的同步入口有意抛异常，避免调用者误以为同步验证已经执行了异步规则。预先查到编号可用，也不能替代真正写入时的数据库约束。

## count 不是整数，过滤器仍能返回自己的错误信息

```powershell
Invoke-WebRequest 'http://127.0.0.1:5087/items?count=abc' -SkipHttpErrorCheck
```

`/items` 要求 `int count`，所以 `abc` 无法绑定。这个端点有一个过滤器：它执行后续处理，看到响应状态是 400 时，返回 `{"error":"count 必须是整数"}`。

这里的参数绑定是框架把请求里的文字转换成方法参数的过程。`?count=3` 中的 `"3"` 能转成整数 3；`?count=abc` 不能。端点过滤器包在处理函数周围，示例中的 `next(context)` 表示继续后面的处理，返回后再检查本次处理留下的状态。

.NET 11 让配置了过滤器的 Minimal API 即使参数绑定失败，也能运行过滤器。示例还设置了 `RouteHandlerOptions.ThrowOnBadRequest = false`：在 Development 环境中，这样绑定失败会返回可供过滤器观察的 400，而不是先抛出 `BadHttpRequestException`。该过滤器只说明当前端点的处理，不是整个应用的统一错误处理中间件。

## 健康检查为什么能跳过后续中间件

```powershell
Invoke-RestMethod http://127.0.0.1:5087/health
```

端点标记了 `[ShortCircuit]`。路由选中它以后，可以直接执行并返回 `{"status":"healthy"}`，不再经过路由之后的其他中间件。

这只适用于确实不依赖那些中间件的端点。如果接口需要后续身份验证、授权、CORS 或其他安全处理，就不能为了省一点开销而直接短路。

## 看一次缓存命中和一次未命中

```powershell
Invoke-RestMethod http://127.0.0.1:5087/cache-metrics
```

这个端点新建 `MemoryCache`，开启 `TrackStatistics`，放入 `book`，然后分别查询 `book` 和一个不存在的键。`MeterListener` 订阅 `Microsoft.Extensions.Caching.Memory.MemoryCache`，调用 `RecordObservableInstruments()` 采集当前值。

可以把 `Meter` 看作发布一组指标的名称，`MeterListener` 是接收这些指标的监听器。示例先订阅缓存的 meter，再进行一次命中和一次未命中；因为这些指标需要主动读取，最后调用 `RecordObservableInstruments()` 才取得当前计数。

输出里的 `dotnet.cache.requests` 带有 `dotnet.cache.request.result` 标签：`hit` 和 `miss` 在本例中各为 1。`dotnet.cache.entries` 表示当前条目数；指标不会包含缓存中的值。

默认 meter 汇总进程内数据。实际服务如果要区分缓存实例，需要按照[官方说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#memorycache-opentelemetry-metrics)配置 `IMeterFactory` 和标签；采集器本身也可以换成 OpenTelemetry 等工具。

更多背景见 [ASP.NET Core 11 Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11#minimal-apis)。该页面目前仍含预览更新提示，部署前请对照实际 SDK 和 ASP.NET Core 共享框架版本。
