# ASP.NET Core：联合类型、验证和端点过滤器

示例是一个独立 Minimal API 项目，只监听本机 `127.0.0.1:5087`。需要安装 .NET 11 SDK 和 ASP.NET Core 共享框架，不需要数据库或 NuGet 外部包。若端口已被占用，请修改 `web/Program.cs` 的 `UseUrls` 后运行。

在仓库根目录启动：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/web -c Release
```

保留这个终端，在另一个 PowerShell 中发送请求：

```powershell
Invoke-RestMethod http://127.0.0.1:5087/reading/2
Invoke-RestMethod http://127.0.0.1:5087/reading/3
Invoke-RestMethod http://127.0.0.1:5087/members -Method Post -ContentType application/json -Body '{"code":"VIP-42"}'
Invoke-WebRequest http://127.0.0.1:5087/members -Method Post -ContentType application/json -Body '{"code":"BAD"}' -SkipHttpErrorCheck
Invoke-WebRequest 'http://127.0.0.1:5087/items?count=abc' -SkipHttpErrorCheck
Invoke-RestMethod http://127.0.0.1:5087/health
Invoke-RestMethod http://127.0.0.1:5087/cache-metrics
```

结束后回到服务器终端按 Ctrl+C。上面的 `-SkipHttpErrorCheck` 是 PowerShell 7 的选项；Windows PowerShell 5.1 可用 `curl.exe` 查看 400 响应。

## 一条接口可以返回数字或字符串

`Reading` 是 `union(int, string)`。设备正常时返回数字，异常时返回文本；JSON 输出分别是 `2` 和 `"设备离线"`，不额外套一个写着类型名称的对象。真实接口仍要设计客户端能理解的稳定协议。当前演示的是 JSON 响应，不能推断 union 可以直接作为路由参数或查询字符串绑定。

## 验证需要等待时，端点也会等待

`/members` 的 `Registration.Code` 贴了 `[KnownCode]`。它继承 `AsyncValidationAttribute`，使用一个短暂延时模拟外部查询：只有 `VIP-42` 通过。`AddValidation()` 注册 ASP.NET Core 的验证流程，规则失败时框架返回 HTTP 400，处理函数不会收到一个已经通过验证的对象。

`Task.Delay` 只是演示等待，实际应用应通过依赖注入访问真实服务，并继续传递取消令牌。一次“编号存在”的检查不取代数据库约束。同步验证入口有意抛异常，避免错误地把异步规则当成已经验证通过。

## 参数绑定失败，过滤器也能处理

`/items?count=abc` 不能绑定到 `int count`。带过滤器的 Minimal API 会让过滤器观察这个 400，示例将响应替换成更容易看懂的 JSON。开发环境中的绑定失败可能抛异常，因此项目明确设置 `ThrowOnBadRequest = false`；它不是修改所有请求的全局错误处理方案。

## ShortCircuit 不是普通的性能开关

`/health` 使用 `[ShortCircuit]`：路由选中端点后尽早执行，跳过后续中间件。它适合明确不需要后续授权、CORS 等处理的简单端点。实际服务中不要把它加到依赖身份验证或其他安全中间件的接口上。

## 采集 MemoryCache 的命中和未命中

`/cache-metrics` 新建一份 `MemoryCache`，开启 `TrackStatistics`，接着查询一个存在的键和一个不存在的键。示例用 `MeterListener` 订阅 `Microsoft.Extensions.Caching.Memory.MemoryCache`，最后主动采集可观察指标。

输出中 `dotnet.cache.requests` 带有 `dotnet.cache.request.result=hit` 或 `miss` 标签；本例中两者各为 1。它们表示发生了多少次查询，并不包含缓存中的值。`dotnet.cache.entries` 则表示当前条目数。默认 meter 汇总进程内数据，真实服务若需要区分实例，应按官方说明提供 `IMeterFactory` 并配置相应标签。

官方资料：[ASP.NET Core 11 Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11#minimal-apis)、[MemoryCache 指标](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#memorycache-opentelemetry-metrics)。ASP.NET 页面仍含预览更新提示，部署时请核对 SDK 和共享框架版本。
