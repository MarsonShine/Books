# .NET 11 正式版学习示例

这个项目把 .NET 11 GA 和 C# 15 的知识点拆成可直接运行的小例子。先看屏幕上的输入和结果，再打开对应代码与文档，理解运行时或类库背后做了什么。项目没有单元测试；它是供动手修改的学习项目。

## 环境和运行

需要 .NET SDK 11.0.100 或更新的兼容 SDK：

```powershell
dotnet --list-sdks
dotnet build CSharpGuide/dotnet11-features -c Release
dotnet run --project CSharpGuide/dotnet11-features -c Release --no-build -- all
```

也可以只运行一组，把最后的 `all` 改成 `language`、`runtime`、`libraries`、`extra` 或 `advanced`。命令在仓库根目录执行；在本目录运行时，用 `dotnet run -c Release -- runtime` 即可。默认分组不访问外网；进程示例会运行本机 `dotnet --version`，归档示例会创建并清理自己的一份临时文件。

主控制台项目只使用共享框架，不下载额外 NuGet 包。目录里的 `NuGet.Config` 清空包源，避免继承本机的私有或不安全 HTTP 源。下面的 CBOR 和 EF Core 独立项目各自配置了 nuget.org HTTPS 源，第一次运行需要还原依赖。

`CSharpGuide/Directory.build.props` 将语言版本固定为 10，所以项目显式设置 C# 15；这不是 C# 15 GA 本身需要 preview。项目默认通过 `<Features>runtime-async=on</Features>` 编译自己的异步方法。对照传统状态机时用：

```powershell
dotnet build CSharpGuide/dotnet11-features -c Release -t:Rebuild -p:EnableRuntimeAsyncDemo=false
dotnet run --project CSharpGuide/dotnet11-features -c Release --no-build -- runtime
```

切换这个编译开关时必须使用 `-t:Rebuild`：单独改变属性不一定让增量构建重新编译。恢复默认版本也运行 `dotnet build CSharpGuide/dotnet11-features -c Release -t:Rebuild`。运行输出中的 `AsyncStateMachineAttribute` 用来观察本项目示例方法的编译结果，不是测速。框架库和未重编译的依赖各自有自己的编译方式。

## 从哪里开始

| 组 | 代码 | 讲解 |
| --- | --- | --- |
| C# 15 | [LanguageDemos.cs](LanguageDemos.cs)：联合类型、closed、集合表达式参数、扩展索引器、带标签跳转 | [C# 15 说明](docs/csharp15.md) |
| 运行时 | [RuntimeDemos.cs](RuntimeDemos.cs)：异步完成与挂起、异常、JIT、边界检查、GC、向量和尾部 | [运行时与 SIMD](docs/runtime.md) |
| 基础库 | [LibraryDemos.cs](LibraryDemos.cs)：集合、数值、文本、JSON、流、压缩、HTTP、进程 | [基础库说明](docs/libraries.md) |
| 更多基础库 | [ExtraLibraryDemos.cs](ExtraLibraryDemos.cs)：内存流、ZIP 校验、Zstandard、加密与管道 | [补充基础库](docs/extra-libraries.md) |
| JSON 与验证 | [AdvancedDemos.cs](AdvancedDemos.cs)：对象 union 分类、源生成与异步验证 | [JSON 与异步验证](docs/advanced.md) |
| 网络与进程 | [EnvironmentDemos.cs](EnvironmentDemos.cs)：真实 DNS、回环 HTTP 指标、进程状态；须显式选择 | [环境与运行条件](docs/environment.md) |
| ASP.NET Core | [独立 Web 项目](web/Program.cs)：union、异步验证、端点过滤器 | [启动本机服务](docs/web.md) |
| EF Core 11 | [SQLite 内存数据库](ef-sqlite/Program.cs)：TPT、JSON 复杂类型和查询翻译 | [EF Core 示例](docs/ef.md) |
| 增量 CBOR | [独立项目](cbor/Program.cs)：分段读取 | [CBOR 示例](docs/cbor.md) |
| Windows Forms | [独立 Windows 项目](winforms/Program.cs)：视觉样式和批量更新 | [WinForms 示例](docs/winforms.md) |
| SDK | [文件式应用](file-apps/main.cs)：`#:include` 拆分文件、`dotnet run -e` 传入环境变量 | 本节下方的运行命令 |
| SDK 更多命令 | [cli-demo.ps1](sdk-examples/cli-demo.ps1)：筛选解决方案、文件式应用引用等 | [SDK 示例](docs/sdk.md) |
| 发布范围 | 示例之外的平台、SDK、Web、桌面及预览功能 | [正式版特性地图](docs/release-map.md) |

建议先运行一组，读对应方法，再修改输入观察变化。例如把 SIMD 输入从 130 项改为 128、129 或 3；把 `cacheHit` 改为 false；给 `FullJoin` 增加重复键。JIT 的边界检查消除、去虚拟化、GC 写屏障等内部优化不能只从正确结果推断，运行时文档说明了如何观察及其局限。

文件式应用不依赖本项目的 `Program.cs`。在仓库根目录运行：

```powershell
dotnet run CSharpGuide/dotnet11-features/file-apps/main.cs -e DOTNET11_DEMO_NAME=DotNet11
```

`main.cs` 用 `#:include Greeting.cs` 引入同目录的辅助文件，`-e` 只为本次启动设置环境变量。文件中设置 `PublishAot=false`，使示例使用已安装的运行时运行，不要求安装 Native AOT 工具链或下载相关包。

需要真实 DNS、指定 DNS 服务器或在本机启动 HTTP 监听器的示例通过 `environment` 参数显式运行，具体命令见[环境说明](docs/environment.md)。Web、EF、CBOR 和 Windows Forms 是独立项目，不会由主程序的 `all` 自动启动。它们的平台、包恢复和端口条件见各自文档。

官方发布还包含硬件、平台修复和预览功能；特性地图逐项区分已有演示与仍需资料或特定平台的项目，不把“仅介绍”标为已经实现。

## 官方资料

- [.NET 11 GA 总览](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)：用户提供的目录 URL 目前返回 404，正式总览位于此地址。
- [.NET 11 Runtime](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime)、[基础库](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries)、[SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)。
- [C# 15 新特性](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)。其中 memory safety 仍是预览功能，不在默认项目中启用。
- [性能改进文章的仓库中文笔记](../../blogs/2026/performance-improvements-in-net-11.md)。
