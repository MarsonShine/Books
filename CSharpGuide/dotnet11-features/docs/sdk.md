# .NET 11 SDK：从几个命令看它实际改了什么

这个示例会创建一套很小的临时解决方案，依次运行筛选器、启动配置和文件式应用命令。它只使用 SDK 自带框架，不需要下载第三方包，也不会修改仓库里的解决方案或项目。

需要 .NET SDK 11.0.100 或兼容版本，以及 PowerShell 7。在仓库根目录执行：

```powershell
dotnet --version
pwsh -NoProfile -File CSharpGuide/dotnet11-features/sdk-examples/cli-demo.ps1
```

脚本每次在系统临时目录下创建一个名字不同的 `dotnet11-sdk-demo-*` 目录，并在最后打印完整路径。中途命令失败会停止，并保留已经生成的文件，便于排查。脚本不会自动删除这些目录；看完后可以自行删除输出中对应的目录。若希望放到其他地方，先准备一个存在的目录，再传 `-WorkspaceRoot`。

## 解决方案筛选器：从大解决方案里选出当前要处理的项目

假设一个解决方案有很多项目，当前只想处理应用项目。可以新建一个 `.slnf` 筛选器，让工具只加载或构建选中的那部分；原来的解决方案仍然保留完整项目列表。

脚本先建一个 `Demo.slnx`，加入 `App` 和 `Core`。再建一个引用它的 `Focus.slnf`，把两个项目都加入筛选器，然后从筛选器里移除 `Core`：

```powershell
dotnet new sln --name Demo --format slnx
dotnet sln Demo.slnx add App/App.csproj Core/Core.csproj
dotnet new slnf --name Focus --parent-solution Demo.slnx
dotnet sln Focus.slnf add App/App.csproj Core/Core.csproj
dotnet sln Focus.slnf list
dotnet sln Focus.slnf remove Core/Core.csproj
dotnet sln Focus.slnf list
dotnet sln Demo.slnx list
dotnet build Focus.slnf
```

观察两次筛选器列表：第一次有两个项目，移除后只剩 `App`。再看父解决方案列表，`Core` 依然在里面。`remove` 操作修改的是它所指向的 `.slnf`，不会删除 `Core` 项目文件。

脚本中两个项目互不依赖，所以最后只构建 `App`。如果选中的项目引用了其他项目，构建仍可能为了满足项目依赖而构建它们；筛选器不能替代对项目引用关系的检查。关于本版命令支持，参见 [SDK 发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#solution-filter-cli-support)。

## 启动参数里的项目属性，以及只给这次运行传环境变量

示例项目把程序集名称设为 `MyDemoApp`，启动配置中则写着：

```json
"commandLineArgs": "--name $(AssemblyName)"
```

执行下面的命令后，程序会打印 `--name MyDemoApp`：

```powershell
dotnet run --project App/App.csproj --launch-profile Demo -e SDK_DEMO_MESSAGE=HelloDotNet11
```

这里不需要把程序集名称重复硬编码到启动配置里。`dotnet run` 先对项目求值，再把 `$(AssemblyName)` 展开成实际值。若在项目中改名，下次运行使用的参数也会改变。

同一条命令中的 `-e` 给这次启动的应用设置环境变量，程序会打印 `HelloDotNet11`。这比先修改当前 shell 的环境更方便；它的作用范围是启动的进程及其子进程，不会永久修改用户或系统环境变量。命令行参数与环境变量是两种独立的传值方式，示例将两者分别打印出来。

相关说明：[启动参数中的 MSBuild 属性](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#msbuild-properties-in-launch-profile-arguments)、[`dotnet run -e`](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#pass-environment-variables-with-dotnet-run)。

## 文件式应用可以用命令添加项目引用

文件式应用不一定只能使用单个文件。项目已有的 [file-apps/main.cs](../file-apps/main.cs) 用 `#:include` 引入另一个 C# 文件；本示例进一步让一个临时 `app.cs` 调用 `Core` 项目中的方法。

运行：

```powershell
dotnet reference add --file app.cs Core/Core.csproj
```

命令会**直接修改 `app.cs`**，加入 `#:project Core/Core.csproj`。之后执行 `dotnet run app.cs` 时，SDK 会处理这个项目引用，程序输出“来自被引用的 Core 项目”。脚本用临时文件演示，所以不会修改仓库里的源文件。

`dotnet reference list --file app.cs` 能查看引用；`remove` 会从文件中移除相应指令。脚本打印添加后的文件，运行一次，再移除并列出引用。脚本还在 `App` 目录里使用 `dotnet reference add ../Core/Core.csproj`，演示当前目录只有一个项目时可以省略 `--project`。

最后，脚本先编译 `Core`，再生成另一个文件式应用 `app-dll.cs`：

```csharp
#:property PublishAot=false
#:property AssemblyTitle="SDK Demo Title"
#:include Core/bin/Debug/net11.0/Core.dll
```

它直接引用已经编译好的 DLL，调用和前面相同的 `Message.SayHello()`。其中标题包含空格，写在带引号的属性值里；程序通过反射打印程序集标题，能看到属性确实生效。DLL 路径必须指向真实的编译产物，因此脚本先执行了 `dotnet build Core/Core.csproj`。

文件式示例设置了 `#:property PublishAot=false`，以便仅用已安装的运行时运行，无须安装 Native AOT 工具链。本例不使用需要联网恢复的 `#:package`；这些指令及其元数据规则见[文件式应用参考](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps)。

## 其他 SDK 更新放在什么情况下看

`dotnet watch` 的崩溃恢复需要启动一个长期运行的应用，再修改文件观察重启；移动设备选择需要相应工作负载和设备。容器发布需要 Podman、Docker 或受支持的平台容器运行时。Native AOT 的构建结果复用需要先安装对应工具链并完成实际发布。本地离线脚本没有假装执行这些功能；使用时分别查看 [watch](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#dotnet-watch-improvements)、[AOT 输出复用](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#file-based-apps-reuse-native-aot-build-outputs) 和 [容器发布](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)的官方说明。
