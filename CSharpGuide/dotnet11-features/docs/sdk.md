# .NET 11 SDK：解决方案筛选器与文件式应用

这个例子在系统临时目录里创建两个小项目，通过几条命令体验 `.slnf`、启动参数、环境变量和文件式应用引用。脚本不会改动仓库中的解决方案。

需要 PowerShell 7 和 .NET SDK 11.0.100 或兼容版本。在仓库根目录运行：

```powershell
pwsh -NoProfile -File CSharpGuide/dotnet11-features/sdk-examples/cli-demo.ps1
```

脚本只使用 SDK 自带框架，并为临时目录配置空 NuGet 包源。完成后会打印 `dotnet11-sdk-demo-*` 目录的位置，文件不会自动删除，你可以打开查看，也可以自己删除。中途失败时，已有文件同样会保留。若要指定位置，可传入已经存在的目录：

```powershell
pwsh -NoProfile -File CSharpGuide/dotnet11-features/sdk-examples/cli-demo.ps1 -WorkspaceRoot C:/temp
```

## 用筛选器选出要处理的项目

假设一个解决方案有几十个项目，但当前只需打开其中两个。`.slnf` 记录要从父解决方案中选哪些项目，不需要复制或修改原解决方案。

这里的解决方案文件 `Demo.slnx` 保存项目清单，筛选器 `Focus.slnf` 则保存“从这份清单里选哪些项目”。它们都不是项目本身：项目的源代码和构建设置仍在各自目录中。开发环境可以加载筛选器，`dotnet build` 也可以使用它作为入口。

脚本先创建包含 `App` 和 `Core` 的 `Demo.slnx`，再创建指向它的 `Focus.slnf`：

```powershell
dotnet new sln --name Demo --format slnx
dotnet sln Demo.slnx add App/App.csproj Core/Core.csproj
dotnet new slnf --name Focus --parent-solution Demo.slnx
dotnet sln Focus.slnf add App/App.csproj Core/Core.csproj
```

接着列出筛选器，移除 `Core`，再列一次：

```powershell
dotnet sln Focus.slnf list
dotnet sln Focus.slnf remove Core/Core.csproj
dotnet sln Focus.slnf list
dotnet sln Demo.slnx list
dotnet build Focus.slnf
```

第一次列表有两个项目，第二次只剩 `App`；最后查看 `Demo.slnx`，`Core` 仍在父解决方案里。移除的是筛选器中的条目，不是磁盘上的项目。

例子中的两个项目互不依赖，所以构建筛选器时只构建 `App`。如果被选中的项目引用了其他项目，构建仍可能继续构建那些依赖。有关命令用法，参见 [SDK 发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#solution-filter-cli-support)。

## 把项目属性放进启动参数

脚本给 `App` 设置了程序集名称 `MyDemoApp`，启动配置中则写：

```json
"commandLineArgs": "--name $(AssemblyName)"
```

下面的命令读取这个启动配置，并给此次启动的程序传一个环境变量：

```powershell
dotnet run --project App/App.csproj --launch-profile Demo -e SDK_DEMO_MESSAGE=HelloDotNet11
```

程序会打印参数 `--name MyDemoApp` 和环境变量 `HelloDotNet11`。`dotnet run` 展开了启动配置中的 `$(AssemblyName)`；如果以后改程序集名，不必再去同步修改参数。`-e` 设置的是启动进程及其子进程的环境变量，不会永久修改用户或系统环境。

这些命令由脚本在临时目录内执行。若要自己逐条尝试，请先运行脚本，然后进入它打印的目录。参见[启动参数中的 MSBuild 属性](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#msbuild-properties-in-launch-profile-arguments)和 [`dotnet run -e`](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#pass-environment-variables-with-dotnet-run)。

## 文件式应用也能引用项目或 DLL

仓库里的 [main.cs](../file-apps/main.cs) 使用 `#:include Greeting.cs` 拆分源文件。临时脚本还创建一个 `app.cs`，让它调用 `Core` 项目的方法：

```powershell
dotnet reference add --file app.cs Core/Core.csproj
dotnet reference list --file app.cs
dotnet run app.cs
dotnet reference remove --file app.cs Core/Core.csproj
```

`add` 会修改 `app.cs`，写入 `#:project Core/Core.csproj`；运行时 SDK 根据指令处理引用，输出来自 `Core` 的消息。`remove` 再从文件中删掉这条引用。脚本还在 `App` 项目目录执行 `dotnet reference add ../Core/Core.csproj`，演示当前目录只有一个项目时可以省略 `--project`。

项目引用告诉 SDK：构建 `app.cs` 之前，需要先处理 `Core.csproj`，再让应用使用它的类型。命令修改的是文件开头的构建指令，不是把 `Core` 的源码复制进 `app.cs`。

另一种情况是手头已有编译好的 DLL。脚本先构建 `Core`，再创建含以下指令的 `app-dll.cs`：

```csharp
#:property PublishAot=false
#:property AssemblyTitle="SDK Demo Title"
#:include Core/bin/Debug/net11.0/Core.dll
```

程序调用 DLL 中的方法，并通过反射打印程序集标题。标题含空格，因此用引号括起来；DLL 路径必须指向已经存在的编译产物。`PublishAot=false` 让例子使用已安装的运行时执行，不要求安装 Native AOT 工具链。[文件式应用参考](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps)介绍了其他指令及其元数据。

这里的 DLL 是 `Core` 已经编译好的程序集。与前面的 `#:project` 不同，`#:include` 指向的这个 DLL 不会告诉 SDK 如何重新编译它；修改 `Core` 源码后，需要先重新构建 `Core`，再运行使用该 DLL 的应用。

## 其他工具功能

`dotnet watch` 的崩溃恢复需要一个长期运行的应用；移动设备选择需要相应工作负载和设备。容器发布需要可用的容器运行时，Native AOT 输出复用需要安装 AOT 工具链并完成发布。这些步骤没有放进当前脚本。需要时可查看 [watch](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#dotnet-watch-improvements)、[AOT 输出复用](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk#file-based-apps-reuse-native-aot-build-outputs)和[容器发布](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)的说明。
