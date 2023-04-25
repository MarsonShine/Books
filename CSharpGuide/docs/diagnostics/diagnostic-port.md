# 诊断端口（Diagnostic Port）

.NET Core 运行时暴露了一个服务终结点，允许其他进程通过[IPC通道](https://en.wikipedia.org/wiki/Inter-process_communication)发送诊断命令和接收响应。这个端点被称为诊断端口。可以向诊断端口发送命令以下命令：
- 捕获一个内存转储（memory dump）。
- 启动一个 [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) 跟踪。
- 请求用于启动应用程序的命令行。

诊断端口根据平台的不同，支持不同的传输方式。目前，CoreCLR 和 Mono 运行时的实现都在 Windows 上使用命名管道（Named Pipes），在 Linux 和 macOS 上使用 Unix Domain Sockets。Android、iOS 和 tvOS 上的 Mono 运行时实现使用 TCP/IP。该通道使用[自定义的二进制协议](https://github.com/dotnet/diagnostics/blob/main/documentation/design-docs/ipc-protocol.md)。大多数开发者不会直接与底层通道和协议互动，而是使用代表他们进行通信的 GUI 或 CLI 工具。例如，[dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump) 和 [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) 工具抽象地发送协议命令来捕获转储和启动跟踪。对于想要编写自定义工具的开发者，[Microsoft.Diagnostics.NETCore.Client NuGet 包](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostics-client-library)提供了底层传输和协议的 .NET API 抽象。

## 安全考虑

诊断端口暴露了运行中的应用程序的敏感信息。如果一个不受信任的用户获得了这个通道的访问权，他们可以观察详细的程序状态，包括内存中的所有秘密，并任意修改程序的执行。在CoreCLR 运行时，默认的诊断端口被配置为只能由启动应用程序的同一用户账户或具有超级用户权限的账户访问。如果你的安全模型不信任其他具有相同用户账户凭证的进程，可以通过设置环境变量 `DOTNET_EnableDiagnostics=0` 来禁用所有诊断端口。这样做将阻止你使用外部工具，如 .NET调试或任何 dotnet-* 诊断工具的能力。

> 注意：.NET 6 对配置 .NET 运行时行为的环境变量采用了标准化的前缀 `DOTNET_`，而不是 `COMPUS_`。然而，`COMPUS_`前缀将继续工作。如果你使用以前版本的 .NET 运行时，你仍然应该使用 `COMPUS_` 前缀来配置环境变量。

## 默认诊断端口

在 Windows、Linux 和 macOS 上，运行时默认在一个知名的终结点上打开一个诊断端口。这是 dotnet-* 诊断工具在没有明确配置为使用其他端口时自动连接的端口。该端点是：

- Windows - Named Pipe`\\.\pipe\dotnet-diagnostic-{pid}`。
- Linux 和 macOS - Unix Domain Socket `{temp}/dotnet-diagnostic-{pid}-{disambiguation_key}-socket`

`{pid}` 是以十进制书写的进程 ID，`{temp}` 是 `TMPDIR` 环境变量，如果 `TMPDIR` 未定义或为空，则为 `/tmp` 值，`{disambiguation_key}` 是以十进制书写的进程启动时间。在 macOS 和 NetBSD 上，进程开始时间是自 UNIX 纪元时间以来的秒数，在所有其他平台上，它是自启动时间以来的 jiffies。

> jiffies 是一个单位，通常用于计算计算机启动后一段时间内的时钟周期。它是指从系统启动到当前时间之间的时间间隔，以毫秒为单位。在计算机中，Jiffies 通常用于计算操作系统和应用程序之间的响应时间，因为操作系统和应用程序在启动后需要一些时间来加载和初始化，然后才能开始执行。Jiffies 越小，表示系统启动后越快，反之亦然。

## 启动时暂停运行时

默认情况下，运行时一启动就执行托管代码，不管是否有诊断工具连接到诊断端口。有时候，让运行时等待运行托管代码，直到诊断工具连接后，观察最初的程序行为，是很有用的。设置环境变量 `DOTNET_DefaultDiagnosticPortSuspend=1` 使运行时等待，直到有工具连接到默认端口。如果几秒钟后没有工具连接，运行时将向控制台打印一条警告信息，解释它仍在等待工具连接。

## 配置额外的诊断端口

> 该特性只用于 .NET5 及以上版本

Mono 和 CoreCLR 运行系统都可以使用自定义配置的诊断端口。这些端口是在保持可用的默认端口之外的。有几个常见的原因，这很有用：

- 在 Android、iOS 和 tvOS 上，没有默认的端口，所以配置一个端口对于使用诊断工具是必要的。
- 在有容器或防火墙的环境中，你可能想设置一个可预测的端点地址，它不会像默认端口那样根据进程 ID 而变化。然后，自定义端口可以被明确地添加到允许列表中，或通过一些安全边界进行代理。
- 对于监控工具来说，让工具监听一个端点是很有用的，而且运行时主动尝试连接到它。这就避免了监控工具需要不断轮询新的应用程序的启动。在默认诊断端口无法访问的环境中，这也避免了需要为每个被监控的应用程序配置一个自定义端点的监控器。

在诊断工具和 .NET 运行时之间的每个通信通道中，一方需要成为监听器并等待另一方的连接。运行时可以被配置为在每个端口的任意角色中执行。端口也可以被独立配置为在启动时暂停，等待诊断工具发出恢复命令。被配置为连接的端口将无限期地重复它们的连接尝试，如果远程端点没有监听或连接丢失，但应用程序在等待建立该连接时不会自动暂停托管代码。如果你想让应用程序等待连接的建立，请使用启动时暂停选项。

自定义端口是使用 `DOTNET_DiagnosticPorts` 环境变量配置的。这个变量应该被设置为一个以分号分隔的端口描述列表。每个端口描述由一个端点地址和可选的修饰词组成，这些修饰词控制运行时的连接或监听角色以及运行时是否应在启动时暂停。在 Windows 上，端点地址是一个命名管道(Named Pipe)的名称，没有 `\\.\pipe\` 前缀。在 Linux 和 macOS 上，它是一个 Unix Domain Socket 的完整路径。在 Android、iOS 和 tvOS 上，地址是一个 IP 和端口。例如：

1. `DOTNET_DiagnosticPorts=my_diag_port1` - (Windows) 运行时连接到命名管道`\\.pipe\my_diag_port1`。
2. `DOTNET_DiagnosticPorts=/foo/tool1.socket;foo/tool2.socket` - ( Linux 和 macOS) 运行时连接到 Unix域套接字 `/foo/tool1.socket`和 `/foo/tool2.socket`。
3. `DOTNET_DiagnosticPorts=127.0.0.1:9000` - (Android, iOS, and tvOS) 运行时连接到端口 9000 的IP 127.0.0.1。
4. `DOTNET_DiagnosticPorts=/foo/tool1.socket,listen,nosuspend` - ( Linux 和 macOS) 这个例子有 listen 和 nosuspend 修改器。运行时创建并监听 Unix域套接字 `/foo/tool1.socket` 而不是连接到它。额外的诊断端口通常会导致运行时在启动时暂停，等待恢复命令，但 `nosuspend` 导致运行时不等待。

端口的完整语法是 `address[,(listen|connect)][,(suspend|nosuspend)]`，如果没有指定 connect 或 listen，则默认为 connect；如果没有指定 suspend 或 nosuspend，则默认为 suspend。

## 原文链接

https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-port

