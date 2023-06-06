# Dumps

转储（Dumps）是一个包含创建转储时的进程快照的文件，对于检查你的应用程序的状态非常有用。转储可以用来调试你的 .NET 应用程序，当它很难附加调试器时，如生产或 CI 环境。使用转储允许你捕捉有问题的进程的状态，并检查它而不必停止应用程序。

## 收集 Dumps

转储可以通过各种方式收集，这取决于你的应用程序在哪个平台上运行。

> 注意：
>
> 转储可能包含敏感信息，因为它们可能包含运行进程的全部内存。在处理它们时要考虑到任何安全限制和准则。

> 提示：
>
> 关于转储收集、分析和其他注意事项的常见问题，见转储：[常见问题](dumps-faq.md)。

- 你可以使用环境变量来配置你的应用程序，以便在[崩溃时收集转储](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash)。

- 你可能想在应用程序还没有崩溃的时候收集转储。例如，如果你想检查一个似乎处于死锁状态的应用程序的状态，将环境变量配置为在崩溃时收集转储将没有帮助，因为该应用程序仍在运行。
- `dotnet-dump` 是一个简单的跨平台命令行工具，用于收集转储。其他几个调试器工具如 [Visual Studio](https://learn.microsoft.com/en-us/visualstudio/debugger/using-dump-files) 或 [windbg](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/-dump--create-dump-file-) 也有转储收集功能。
- 如果你在生产中运行你的应用程序，或者你正在以分布式方式运行它（几个服务，副本），`dotnet-monitor` 为许多常见情况和临时诊断调查提供支持，包括转储收集和出口。它使转储能被远程收集或有触发条件。

## 分析 Dumps

Linux：[Linux Dumps 调试](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-linux-dumps)

Windows：[Windows Dumps 调试](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-windows-dumps)

## 内存分析

如果你的应用程序的内存持续增长，但你不确定为什么会这样，你可以对你的应用程序进行内存分析。[调试内存泄漏教程](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-memory-leak)显示了如何使用 `dotnet-sos` 的命令 `dumpheap` 和 `gcroot` 的 dotnet CLI 工具来调试内存泄漏。

[Visual Studio 内存分析](https://learn.microsoft.com/en-us/visualstudio/profiling/analyze-memory-usage)可以用来诊断 Windows 上的内存泄漏。

## 收集 Linux dumps

这里推荐两个用于在 Linux 上的收集 dumps 工具：

- [dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump) CLI 工具
- 收集崩溃时转储的[环境变量](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash)

## 分析 Linux dumps

在收集到转储后，可以使用 [dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump) 工具和 `dotnet-dump analyze` 命令来分析它。这个分析步骤需要在一台机器上运行，该机器的架构和 Linux 发行版与转储的环境相同。`dotnet-dump` 工具支持显示 .NET 代码的信息，但对于了解其他语言（如 C 和 C++）的代码问题没有用。

另外，[LLDB](https://lldb.llvm.org/) 可以用来分析 Linux 上的转储，它允许分析托管和本地代码。LLDB 使用 SOS 扩展来调试托管代码。[dotnet-sos](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos) CLI 工具可以用来安装 SOS，它有许多调试托管代码的有用命令。为了分析 .NET Core 转储，LLDB 和 SOS 需要转储创建环境中的以下.NET Core二进制文件：

- libmscordaccore.so
- libcoreclr.so
- dotnet (用于启动应用程序的主机)

在大多数情况下，这些二进制文件可以使用 [dotnet-symbol](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-symbol) 工具下载。如果不能用 [dotnet-symbol](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-symbol) 下载必要的二进制文件（例如，如果正在使用从源代码构建的私有版本的 .NET Core），可能需要从创建转储的环境中复制上面列出的文件。如果这些文件不在转储文件旁边，你可以使用 `LLDB/SOS` 命令 `setclrpath <path>` 来设置它们应该被加载的路径，以及 `setymbolserver -directory <path>`w 来设置寻找符号文件的路径。

一旦有了必要的文件，就可以通过指定 dotnet 主机作为调试的可执行文件在 LLDB 中加载转储：

```
lldb --core <dump-file> <host-program>
```

在前面的命令中，`<dump-file>` 是要分析的转储路径，`<host-program>` 是启动 .NETCore 应用程序的本地程序。这通常是 dotnet 二进制文件，除非该应用程序是独立的，在这种情况下，它是应用程序的名称，没有 .dll 扩展。

一旦 LLDB 启动，可能需要使用 `setymbolserver` 命令来指向正确的符号位置（`setymbolserver -ms` 用于使用微软的符号服务器，或者 `setymbolserver -directory <path>` 用于指定本地路径）。要加载本地符号，运行 `loadsymbols`。在这一点上，你可以使用 [SOS命令](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/sos-debugging-extension) 来分析转储。

> LLDB 可通过执行命令：`sudo apt-get install lldb` 安装。

## 分析 Windows dumps

从 Linux 机器上收集的转储信息也可以在 Windows 机器上使用 [Visual Studio](https://learn.microsoft.com/en-us/visualstudio/debugger/using-dump-files)、[Windbg](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/analyzing-a-user-mode-dump-file) 或 [dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump) 工具进行分析。**Visual Studio 和 Windbg 都可以分析本地和托管代码，而 dotnet-dump 只分析托管代码。**

> 注意：
>
> Visual Studio 16.8 及以后的版本允许你[打开和分析在 .NET Core 3.1.7 或更高版本上生成的 Linux 转储。](https://devblogs.microsoft.com/visualstudio/linux-managed-memory-dump-debugging/)

- Visual Studio - 参见 [Visual Studio 转储调试指南](https://learn.microsoft.com/en-us/visualstudio/debugger/using-dump-files)。
- Windbg - 你可以在 windbg 上调试 Linux 转储，使用与调试 Windows 用户模式转储[相同的指令](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/analyzing-a-user-mode-dump-file)。使用 x64 版本的 windbg 从 Linux x64 或 Arm64 环境收集转储，使用 x86 版本从 Linux x86 环境收集转储。
- dotnet-dump - 使用 [dotnet-dump 分析](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)命令查看转储。使用 dotnet-dump 的 x64 版本用于从 Linux x64 或 Arm64 环境中收集的转储，x86 版本用于从 Linux x86 环境中收集的转储。

## SOS 调试拓展

这里有关于使用 SOS 命令的各种解释：https://learn.microsoft.com/en-us/dotnet/core/diagnostics/sos-debugging-extension

## 程序崩溃时收集 dumps

通过设置特定的环境变量，可以将你的应用程序配置为在崩溃时收集转储。当你想了解崩溃发生的原因时，这很有帮助。例如，当一个异常被抛出时捕获转储，可以帮助你通过检查应用程序崩溃时的状态来确定一个问题。

下表显示了你可以为收集崩溃时的转储而配置的环境变量。

| Environment variable                                         | Description                                                  | Default value         |
| ------------------------------------------------------------ | ------------------------------------------------------------ | --------------------- |
| `COMPlus_DbgEnableMiniDump` or `DOTNET_DbgEnableMiniDump`    | 如果设置为 1，启用核心转储生成。                             | 0                     |
| `COMPlus_DbgMiniDumpType` or `DOTNET_DbgMiniDumpType`        | 要收集的转储的类型。更多信息，请参见[迷你转储的类型](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash#types-of-mini-dumps)。 | 2 (`Heap`)            |
| `COMPlus_DbgMiniDumpName` or `DOTNET_DbgMiniDumpName`        | 写入转储的文件的路径。确保 dotnet 进程所运行的用户对指定的目录有写入权限。 | `/tmp/coredump.<pid>` |
| `COMPlus_CreateDumpDiagnostics` or `DOTNET_CreateDumpDiagnostics` | 如果设置为 1，启用转储过程的诊断记录。                       | 0                     |
| `COMPlus_EnableCrashReport` or `DOTNET_EnableCrashReport`    | (需要.NET 6 或更高版本；在 Windows上 不支持。)<br/>如果设置为 1，运行时将生成一个 JSON 格式的崩溃报告，其中包括关于崩溃应用程序的线程和堆栈框架的信息。崩溃报告的名称是转储路径或名称，后面加上 .crashreport.json。 |                       |
| `COMPlus_CreateDumpVerboseDiagnostics` or `DOTNET_CreateDumpVerboseDiagnostics` | (需要.NET 7或更高版本。)<br/>如果设置为 1，则启用转储过程的粗略诊断日志。 | 0                     |
| `COMPlus_CreateDumpLogToFile` or `DOTNET_CreateDumpLogToFile` | (需要.NET 7或更高版本。)<br/>诊断信息应被写入的文件路径。如果不设置，诊断信息将被写到崩溃的应用程序的控制台。 |                       |

> 注意：
>
> .NET 7 对这些环境变量采用标准化的前缀 `DOTNET_`，而不是 `COMPUS_`。然而，`COMPUS_` 前缀将继续工作。如果你使用的是以前的 .NET 运行时版本，你仍然应该使用`COMPlus_` 前缀来表示环境变量。

### 文件路径模板

从 .NET 5 开始，`DOTNET_DbgMiniDumpName` 还可以包括格式化模板指定器，这些指定器将被动态地填入：

| Specifier | Value                                                        |
| --------- | ------------------------------------------------------------ |
| %%        | 一个单一的 % 字符                                            |
| %p        | 被转储进程的 PID                                             |
| %e        | 进程的可执行文件名                                           |
| %h        | 由 `gethostname()` 返回的主机名                              |
| %t        | 转储的时间，以 1970-01-01 00:00:00 +0000 (UTC) 的秒数来表示。 |

### 迷你 dumps 类型

下表显示了你可以为 `DOTNET_DbgMiniDumpType` 使用的所有数值。例如，将 `DOTNET_DbgMiniDumpType` 设置为 1 意味着崩溃时将收集 Mini 类型的转储。

| Value | Name     | Description                                                  |
| ----- | -------- | ------------------------------------------------------------ |
| 1     | `Mini`   | 一个小型转储，包含模块列表、线程列表、异常信息和所有堆栈。   |
| 2     | `Heap`   | 一个大型且相对全面的转储，包含模块列表、线程列表、所有堆栈、异常信息、句柄信息和所有内存，除了映射的图像。 |
| 3     | `Triage` | 与 `Mini` 相同，但删除了个人用户信息，如路径和密码。         |
| 4     | `Full`   | 最大的转储，包含所有内存，包括模块映像。                     |