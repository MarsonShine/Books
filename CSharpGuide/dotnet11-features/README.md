# .NET 11 学习示例

这是一组可以边运行边修改的 .NET 11 示例。你可以先从感兴趣的主题开始：查看 C# 15 怎么表达联合类型，观察异步方法何时挂起，也可以跟着 130 个整数相加的例子理解 SIMD 的尾部处理。每组代码旁边都有一篇说明，介绍示例的运行过程和使用时要留意的条件。

## 运行控制台示例

需要 .NET SDK 11.0.100 或兼容版本。在仓库根目录执行：

```powershell
dotnet --list-sdks
dotnet build CSharpGuide/dotnet11-features -c Release
dotnet run --project CSharpGuide/dotnet11-features -c Release --no-build -- all
```

`all` 会运行语言、运行时和基础库示例。想一次只看一组，可以换成下面的参数：

| 参数 | 内容 | 代码与说明 |
| --- | --- | --- |
| `language` | C# 15 联合类型、封闭类型层次、集合表达式和扩展索引器 | [代码](LanguageDemos.cs) · [讲解](docs/csharp15.md) |
| `runtime` | Runtime Async、JIT、GC、SIMD 和尾部处理 | [代码](RuntimeDemos.cs) · [讲解](docs/runtime.md) |
| `libraries` | 文本、LINQ、数值、JSON、压缩、HTTP 和进程 | [代码](LibraryDemos.cs) · [讲解](docs/libraries.md) |
| `extra` | 内存流、归档、加密和管道 | [代码](ExtraLibraryDemos.cs) · [讲解](docs/extra-libraries.md) |
| `advanced` | JSON 对象联合类型、源生成和异步验证 | [代码](AdvancedDemos.cs) · [讲解](docs/advanced.md) |
| `environment` | 进程、真实 DNS 和本地 HTTP 指标 | [代码](EnvironmentDemos.cs) · [运行条件](docs/environment.md) |

例如，只看运行时示例：

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- runtime
```

如果当前目录已经是 `CSharpGuide/dotnet11-features`，也可以简写成 `dotnet run -c Release -- runtime`。`all` 不会查询外部 DNS 或启动 HTTP 监听器；进程示例会执行本机的 `dotnet --version`，归档示例会创建并清理自己的临时文件。

主控制台项目只依赖 SDK 自带框架。目录中的 `NuGet.Config` 清空了包源，防止示例受到机器上其他包源的影响。CBOR 和 EF Core 是单独项目，各自配置了 nuget.org HTTPS 源，首次运行需要下载依赖。

## 对照两种异步编译方式

项目默认开启 Runtime Async。想看传统编译器状态机，可以先强制重新编译，再运行 `runtime` 组：

```powershell
dotnet build CSharpGuide/dotnet11-features -c Release -t:Rebuild -p:EnableRuntimeAsyncDemo=false
dotnet run --project CSharpGuide/dotnet11-features -c Release --no-build -- runtime
```

留意输出中的 `AsyncStateMachineAttribute`：示例方法采用传统方式编译时为 `True`，开启 Runtime Async 后为 `False`。它只帮助观察这个方法的编译结果，不能用来判断性能或推断其他程序集的编译方式。

切换属性后要使用 `-t:Rebuild`；单独改属性不一定触发增量重新编译。恢复默认版本也需要执行：

```powershell
dotnet build CSharpGuide/dotnet11-features -c Release -t:Rebuild
```

项目显式设置了 C# 15，因为上层 `CSharpGuide/Directory.build.props` 固定了 C# 10。C# 15 的这些示例不需要预览语言版本；内存安全规则仍是单独的预览功能，本项目没有启用。

## 独立项目和环境示例

下面这些示例各有自己的运行条件，不会由 `all` 自动启动：

| 示例 | 需要什么 | 说明 |
| --- | --- | --- |
| [ASP.NET Core](docs/web.md) | ASP.NET Core 11 共享框架；本机回环端口 5087 可用 | 启动本机服务，观察联合类型响应、验证和过滤器 |
| [EF Core SQLite](docs/ef.md) | 首次运行可访问 nuget.org；支持 SQLite 的平台 | 使用内存数据库并打印 SQL，无需数据库服务 |
| [增量 CBOR](docs/cbor.md) | 首次运行可访问 nuget.org | 分两段读取一个数组 |
| [Windows Forms](docs/winforms.md) | Windows 和 .NET 11 Windows Desktop 运行时 | 打开窗口查看新控件样式，也可用 `--inspect` 只检查属性 |
| [网络与进程](docs/environment.md) | 各命令分别需要本机进程、回环网络或 DNS | 真实 DNS 查询需要显式指定域名 |
| [SDK 命令](docs/sdk.md) | PowerShell 7、.NET 11 SDK | 在系统临时目录创建解决方案并保留结果 |

文件式应用也可以直接运行：

```powershell
dotnet run CSharpGuide/dotnet11-features/file-apps/main.cs -e DOTNET11_DEMO_NAME=DotNet11
```

它用 `#:include Greeting.cs` 引入另一个源文件，`-e` 为这次启动传入环境变量。示例设置了 `PublishAot=false`，不需要 Native AOT 工具链。

## 阅读顺序和资料

可以先运行一组，再打开对应代码，修改输入观察结果。比如把向量数组长度从 130 改成 128、129 或 3，或者给 `FullJoin` 增加重复键。JIT 是否消除了边界检查、GC 是否使用了某条写屏障，不能单靠程序结果判断；[运行时文档](docs/runtime.md)介绍了如何查看生成的代码。

[特性地图](docs/release-map.md)列出官方发布内容，并区分已有示例、需要环境和仅提供资料的部分。项目没有为所有平台修复或实验性 API 构造示例。

官方资料：[.NET 11 总览](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)、[运行时](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime)、[基础库](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries)、[SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)和 [C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)。另可参考仓库中的 [.NET 11 性能改进笔记](../../blogs/2026/performance-improvements-in-net-11.md)。
