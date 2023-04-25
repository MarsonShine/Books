# 事件管道（EventPipe）

EventPipe 是一个运行时组件，可用于收集跟踪数据，类似于 ETW 或 LTng。EventPipe 的目标是让 .NET 开发人员能够轻松地追踪他们的 .NET 应用程序，而不必依赖特定平台的操作系统原生组件，如 ETW 或 LTng。

EventPipe 是许多诊断工具背后的机制，可用于消耗由运行时发出的事件以及用 [EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 编写的自定义事件。

本文是对 EventPipe 的高级概述。它描述了何时和如何使用 EventPipe，以及如何配置它以最佳方式满足您的需求。

## 事件管道的基础知识

EventPipe 聚合了由运行时组件（例如即时编译器或垃圾回收器）发出的事件以及由库和用户代码中的 [EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 实例编写的事件。

然后，这些事件被序列化为 `.nettrace` 文件格式，并可直接写入文件或通过[诊断端口](../../diagnostics/diagnostic-port.md)流转，用于进程外消费。

要了解有关 EventPipe 序列化格式的更多信息，请参阅 [EventPipe 格式文档](https://github.com/microsoft/perfview/blob/main/src/TraceEvent/EventPipe/EventPipeFormat.md)。

## EventPipe 与 ETW/LTTng 的比较

EventPipe 是 .NET 运行时（CoreCLR）的一部分，被设计为在所有 .NET Core 支持的平台上以同样的方式工作。这使得基于 EventPipe 的追踪工具，如 `dotnet-counters`、`dotnet-gcdump` 和 `dotnet-trace`，能够跨平台无缝工作。

然而，由于 EventPipe 是一个运行时内置组件，**其范围仅限于托管代码和运行时本身**。EventPipe 不能用于跟踪一些较低级别的事件，如解决本地代码堆栈或获得各种内核事件。如果你在你的应用程序中使用 C/C++ 互操作，或者你想跟踪运行时本身（它是用 C++ 编写的），或者想更深入地诊断需要内核事件的应用程序的行为（即本地线程上下文切换事件），你应该使用 ETW 或 [perf/LTTng](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/trace-perfcollect-lttng)。

EventPipe 和 ETW/LTTng 的另一个主要区别是 admin/root **权限要求**。要使用 ETW 或 LTTng 追踪一个应用程序，你需要成为 admin/root。使用 EventPipe，只要追踪器（例如 `dotnet-trace`）与启动应用程序的用户是同一个用户，你就可以追踪应用程序。

下表是对 EventPipe 和 ETW/LTTng 之间的区别的总结。

| Feature                   | EventPipe | ETW                  | LTTng                                |
| ------------------------- | --------- | -------------------- | ------------------------------------ |
| 跨平台                    | Yes       | No (only on Windows) | No (only on supported Linux distros) |
| 需要 admin/root 权限      | No        | Yes                  | Yes                                  |
| 可以获得操作系统/内核事件 | No        | Yes                  | Yes                                  |
| 可以解决本地调用堆栈问题  | No        | Yes                  | Yes                                  |

## 使用 EventPipe 追踪你的应用程序

你可以通过多种方式使用 EventPipe 来追踪你的 .NET 应用程序：

- 使用建立在 [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#tools-that-use-eventpipe) 之上的诊断工具之一。
- 使用 [Microsoft.Diagnostics.NETCore.Client](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostics-client-library) 库来编写您自己的工具，以配置和启动 EventPipe 会话。
- 使用[环境变量](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables)来启动 EventPipe。

在您制作了包含 EventPipe 事件的 `nettrace` 文件后，您可以在 [PerfView](https://github.com/Microsoft/perfview#perfview-overview) 或 Visual Studio 中查看该文件。在非 Windows 平台上，您可以通过使用 [dotnet-trace convert](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-convert) 命令将 `nettrace` 文件转换为 `speedscope` 或 `Chromium` 跟踪格式，并使用 `speedscope` 或 Chrome DevTools 查看。

您还可以用 [TraceEvent](https://github.com/Microsoft/perfview/blob/main/documentation/TraceEvent/TraceEventLibrary.md) 以编程方式分析 EventPipe 的跟踪。

### EventPipe 的使用工具

这是使用 EventPipe 追踪应用程序的最简单方法。要了解更多关于如何使用这些工具的信息，请参考每个工具的文档。

- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) 让你监控和收集由 .NET 运行时和核心库发出的各种指标，以及你可以编写的自定义指标。
- [dotnet-gcdump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump) 让你收集**实时进程**的 GC 堆转储，以分析应用程序的管理堆。
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) 能让您收集应用程序的痕迹，以分析其性能。

## 使用环境变量追踪

使用 EventPipe 的首选机制是使用 [dotnet-race](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) 或 [Microsoft.Diagnostics.NETCore.Client](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostics-client-library) 库。

然而，您可以使用以下环境变量在应用程序上设置 EventPipe 会话，让它直接将跟踪写入文件。要停止追踪，请退出应用程序。

- `DOTNET_EnableEventPipe`：将其设置为 1 以启动直接写入文件的 EventPipe 会话。默认值为 0。

- `DOTNET_EventPipeOutputPath`：当通过 `DOTNET_EnableEventPipe` 配置为运行(run)时，输出 EventPipe 跟踪文件的路径。默认值是 `trace.nettrace`，它将在应用程序运行的同一目录下创建。

  > 注意：
  >
  > 从 .NET 6 开始，`DOTNET_EventPipeOutputPath` 中的字符串 `{pid}` 的实例被替换为被追踪进程的 id。

- `DOTNET_EventPipeCircularMB`：一个十六进制的值，代表 EventPipe 内部缓冲区的大小，单位为兆字节。这个配置值只在通过 `DOTNET_EnableEventPipe` 配置为运行时使用。默认的缓冲区大小为 1024MB，这意味着此环境变量被设置为 `400`，因为 `0x400` == `1024`。

  > 注意：
  >
  > 如果目标进程写入事件的频率过高，它可能会溢出这个缓冲区，一些事件可能被丢弃。如果有太多的事件被丢弃，增加缓冲区的大小，看看丢弃的事件的数量是否减少。如果被丢弃的事件的数量没有随着缓冲区大小的增加而减少，可能是由于慢速读取器阻止了目标进程的缓冲区被刷新。

- `DOTNET_EventPipeProcNumbers`：将此设置为 1，以启用在 EventPipe 事件头中捕获处理器号码。默认值为 0。

- `DOTNET_EventPipeConfig`：当用 `DOTNET_EnableEventPipe` 启动一个 EventPipe 会话时，设置 EventPipe 会话配置。其语法如下：

  `<provider>:<keyword>:<level>`

  您也可以通过用逗号连接来指定多个提供者：

  `<provider1>:<keyword1>:<level1>,<provider2>:<keyword2>:<level2>`

  如果没有设置这个环境变量，但通过 `DOTNET_EnableEventPipe` 启用了 EventPipe，它将通过启用以下关键词和级别的提供者开始追踪：

  - `Microsoft-Windows-DotNETRuntime:4c14fccbd:5`
  - `Microsoft-Windows-DotNETRuntimePrivate:4002000b:5`
  - `Microsoft-DotNETCore-SampleProfiler:0:5`

要了解更多关于 .NET 中一些知名的提供者，请参考[知名的事件提供者](readme.md)。

> 注意：
>
> .NET 6 对配置 .NET 运行时行为的环境变量采用了标准化的前缀 `DOTNET_`，而不是 `COMPUS_`。然而，`COMPUS_`前缀将继续工作。如果你使用以前版本的 .NET 运行时，你仍然应该使用 `COMPUS_` 前缀来配置环境变量。