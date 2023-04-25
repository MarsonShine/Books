# .NET 日志与追踪

代码可以通过工具来产生日志，作为程序运行时发生的有趣事件的记录。为了理解应用程序的行为，要审查日志。日志和追踪都封装了这种技术。.NET 在其历史上积累了几个不同的日志 API，本文将帮助你了解有哪些可用的选项。

## 日志 APIs 关键区分

### 结构化日志

日志 API 可以是结构化的，也可以是非结构化的：

- 非结构化：日志条目有任意格式的文本内容，旨在供人类查看。
- 日志条目有一个明确的模式，可以用不同的二进制和文本格式编码。这些日志被设计成可被机器翻译和查询的，这样人和系统都可以很容易地使用它们。

支持结构化日志的 API 对于非琐碎的使用来说是最好的。它们提供了更多的功能、灵活性和性能，而在可用性方面没有什么区别。

### 配置

对于简单的用例，你可能想使用直接向控制台或文件写入消息的 API。但大多数软件项目会发现，配置哪些日志事件被记录，以及它们如何被持久化是非常有用的。例如，当在本地开发环境中运行时，你可能想把纯文本输出到控制台，以方便阅读。然后，当应用程序被部署到生产环境时，你可能会切换到将日志存储在一个专门的数据库或一组滚动文件中。具有良好配置选项的 API 将使这些转换变得容易，而可配置性较差的选项则需要随处更新仪器代码来进行更改。

### Sinks

大多数日志 API 允许将日志信息发送到不同的目的地，称为 Sinks。一些 API 有大量的预制汇，而另一些只有少数几个。如果没有预制的汇，通常会有一个可扩展的 API，让你编写一个自定义的汇，尽管这需要编写更多的代码。

## .NET 日志 APIs

### ILogger

在大多数情况下，无论是向现有项目添加日志还是创建一个新项目，[ILogger](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) 接口都是一个不错的默认选择。ILogger 支持快速的结构化日志记录，灵活的配置，以及一系列的[通用 sinks](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-providers#built-in-logging-providers)。此外，ILogger 接口还可以作为许多第三方日志实现的门面，提供更多的功能和可扩展性。

### EventSource

[EventSource](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource) 是一个较早的高性能结构化日志 API。它最初被设计为与 [Event Tracing for Windows (ETW)](https://learn.microsoft.com/en-us/windows/win32/etw/event-tracing-portal) 很好地集成，但后来被扩展为支持 [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) 跨平台追踪和用于自定义 sinks 的 [EventListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventlistener)。与 ILogger 相比，`EventSource` 的预制日志 sinks 相对较少，而且没有内置支持通过单独的配置文件进行配置。如果你想对 ETW 或 `EventPipe` 集成进行更严格的控制，`EventSource` 是很好的选择，但对于一般用途的日志，`ILogger` 更灵活，更容易使用。

### Trace

[System.Diagnostics.Trace](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.trace) 和 [System.Diagnostics.Debug](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.debug) 是 .NET 最古老的日志记录 API。这些类有灵活的配置 API 和一个庞大的汇的生态系统，但只支持非结构化的日志记录。在 .NET Framework 上，它们可以通过 app.config 文件进行配置，但在 .NET Core 中，没有内置的、基于文件的配置机制。出于向后兼容的目的，.NET 团队将继续支持这些 API，但不会增加新的功能。对于已经在使用这些 API 的应用程序来说，这些 API 是一个不错的选择。对于那些还没有承诺使用记录 API 的较新的应用程序，`ILogger` 可能会提供更好的功能。

## 专门的日志 APIs

### Console

[System.Console](https://learn.microsoft.com/en-us/dotnet/api/system.console) 类有 [Write](https://learn.microsoft.com/en-us/dotnet/api/system.console.write) 和 [WriteLine](https://learn.microsoft.com/en-us/dotnet/api/system.console.writeline) 方法，可用于简单的日志方案。这些 API 非常容易上手，但解决方案不会像通用的日志 API 那样灵活。Console 只允许非结构化的日志记录，而且没有配置支持来选择启用哪些日志信息，或者重新定位到不同的 sinks。使用 `ILogger` 或 Trace APIs 与控制台水槽不需要太多的额外工作，并保持日志记录的可配置性。

### DiagnosticSource

[System.Diagnostics.DiagnosticSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource) 用于日志记录，日志消息将在过程中被同步分析，而不是被序列化到任何存储中。这允许源和监听器交换任意的 .NET 对象作为消息，而大多数日志 API 要求日志事件是可序列化的。这种技术也可以非常快，如果监听器被有效地实现，可以在几十纳秒内处理日志事件。使用这些 API 的工具通常更像进程中的剖析器，尽管 API 在这里没有施加任何约束。

### EventLog

[System.Diagnostics.EventLog](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.eventlog) 是一个**仅适用于 Windows** 的 API，它将消息写入 Windows 的 EventLog 中。在许多情况下，当在 Windows 上运行时，使用带有可选的 EventLog sinks 的 ILogger 可以提供类似的功能，而不需要将应用程序与 Windows 操作系统紧密结合。

## .NET 中著名的事件提供者

.NET 运行时和库通过一些不同的事件提供者编写诊断事件。根据你的诊断需要，你可以选择适当的提供者来启用。本文介绍了 .NET 运行时和库中一些最常用的事件提供者。

### CoreCLR

**"Microsoft-Windows-DotNETRuntime" 提供者**

这个提供者从 .NET 运行时发出各种事件，包括 GC、加载器、JIT、异常和其他事件。在[运行时提供者事件列表](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-events)中阅读更多关于该提供者的每个事件。

**"Microsoft-DotNETCore-SampleProfiler" 提供者**

该提供者是一个 .NET 运行时事件提供者，用于对管理的 callstacks 进行 CPU 采样。启用后，它每隔一毫秒捕获每个线程的托管调用栈的快照。要启用这种捕获，你必须指定一个 `Informational` 或更高的 [EventLevel](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventlevel)。

### Framework 库

**"Microsoft-Extensions-DependencyInjection" 提供者**

该提供者记录了来自 DependencyInjection 的信息。下表显示了由 `Microsoft-Extensions-DependencyInjection` 提供者记录的事件：

| Event name                   | Keyword                           | Level       | Description                                                  |
| ---------------------------- | --------------------------------- | ----------- | ------------------------------------------------------------ |
| `CallSiteBuilt`              |                                   | Verbose (5) | 一个调用点已建立。                                           |
| `ServiceResolved`            |                                   | Verbose (5) | 一个服务已解决。                                             |
| `ExpressionTreeGenerated`    |                                   | Verbose (5) | 一个表达式树已被生成。                                       |
| `DynamicMethodBuilt`         |                                   | Verbose (5) | A [DynamicMethod](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.dynamicmethod) 已经被建立. |
| `ScopeDisposed`              |                                   | Verbose (5) | 一个作用域已被处置。                                         |
| `ServiceRealizationFailed`   |                                   | Verbose (5) | 一个服务实现已经失败。                                       |
| `ServiceProviderBuilt`       | `ServiceProviderInitialized(0x1)` | Verbose (5) | 一个 [ServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovider) 已构建。 |
| `ServiceProviderDescriptors` | `ServiceProviderInitialized(0x1)` | Verbose (5) | 在 [ServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovider) 构建期间已使用的 [ServiceDescriptor](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicedescriptor) 列表。 |

**"System.Buffers.ArrayPoolEventSource" 提供者**

这个提供者记录来自 ArrayPool 的信息。下表显示由 `ArrayPoolEventSource` 记录的事件：

| Event name        | Level             | Description                                  |
| ----------------- | ----------------- | -------------------------------------------- |
| `BufferRented`    | Verbose (5)       | 一个缓冲区被成功租用。                       |
| `BufferAllocated` | Informational (4) | 一个缓冲区被池子分配。                       |
| `BufferReturned`  | Verbose (5)       | 一个缓冲区被返回到池中。                     |
| `BufferTrimmed`   | Informational (4) | 一个缓冲区由于内存压力或不活动而试图被释放。 |
| `BufferTrimPoll`  | Informational (4) | 正在检查修剪缓冲区。                         |

**"System.Net.Http" 提供者**

这个提供者记录了来自 HTTP 堆栈的信息。下表显示了 `System.Net.Http` 提供者所记录的事件：

| Event name            | Level             | Description                        |
| --------------------- | ----------------- | ---------------------------------- |
| RequestStart          | Informational (4) | 一个 HTTP 请求已经开始。           |
| RequestStop           | Informational (4) | 一个 HTTP 请求已经完成。           |
| RequestFailed         | Error (2)         | 一个 HTTP 请求已经失败。           |
| ConnectionEstablished | Informational (4) | 一个 HTTP 连接已经建立。           |
| ConnectionClosed      | Informational (4) | 一个 HTTP 连接已经关闭。           |
| RequestLeftQueue      | Informational (4) | 一个 HTTP 请求已经离开请求队列。   |
| RequestHeadersStart   | Informational (4) | 一个 HTTP 请求的标题已经开始。     |
| RequestHeaderStop     | Informational (4) | 一个 HTTP 请求的标头已经完成。     |
| RequestContentStart   | Informational (4) | 一个对内容的 HTTP 请求已经开始。   |
| RequestContentStop    | Informational (4) | 一个关于内容的 HTTP 请求已经结束。 |
| ResponseHeadersStart  | Informational (4) | 一个 HTTP 响应的头已经开始。       |
| ResponseHeaderStop    | Informational (4) | 一个针对头的 HTTP 响应已经结束。   |
| ResponseContentStart  | Informational (4) | 一个针对内容的 HTTP 响应已经开始。 |
| ResponseContentStop   | Informational (4) | 一个针对内容的 HTTP 响应已经结束。 |

**"System.Net.NameResolution" 提供者**

这个提供者记录了与域名解析有关的信息。下表显示了由 `System.Net.NameResolution` 记录的事件：

| Event name         | Level             | Description            |
| ------------------ | ----------------- | ---------------------- |
| `ResolutionStart`  | Informational (4) | 一个域名解析已经开始。 |
| `ResolutionStop`   | Informational (4) | 一个域名解析已经完成。 |
| `ResolutionFailed` | Informational (4) | 一个域名解析失败。     |

**"System.Net.Sockets" 提供者**

这个提供者记录了来自Socket的信息。下表显示了 `System.Net.Sockets` 提供者所记录的事件：

| Event name      | Level             | Description                        |
| --------------- | ----------------- | ---------------------------------- |
| `ConnectStart`  | Informational (4) | 启动一个套接字连接的尝试已经开始。 |
| `ConnectStop`   | Informational (4) | 启动一个套接字连接的尝试已经结束。 |
| `ConnectFailed` | Informational (4) | 启动一个套接字连接的尝试已经失败。 |
| `AcceptStart`   | Informational (4) | 接受一个套接字连接的尝试已经开始。 |
| `AcceptStop`    | Informational (4) | 一个接受套接字连接的尝试已经结束。 |
| `AcceptFailed`  | Informational (4) | 一个接受套接字连接的尝试已经失败。 |

**"System.Threading.Tasks.TplEventSource" 提供者**

这个提供者记录了[任务并行库](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl)的信息，例如任务调度器事件。下表显示了由 `TplEventSource` 记录的事件：

| Event name                       | Keyword                                  | Level             | Description                                     |
| -------------------------------- | ---------------------------------------- | ----------------- | ----------------------------------------------- |
| `TaskScheduled`                  | `TaskTransfer`(`0x1`)  `Tasks`(`0x2`)    | Informational (4) | 任务被排入任务调度器。                          |
| `TaskStarted`                    | `Tasks`(`0x2`)                           | Informational (4) | 任务开始执行。                                  |
| `TaskCompleted`                  | `TaskStops`(`0x40`)                      | Informational (4) | 任务已经完成执行。                              |
| `TaskWaitBegin`                  | `TaskTransfer`(`0x1`)  `TaskWait`(`0x2`) | Informational (4) | 当一个隐式或显式的任务完成等待开始时启动。      |
| `TaskWaitEnd`                    | `Tasks`(`0x2`)                           | Verbose (5)       | 当对任务完成的等待返回时触发。                  |
| `TaskWaitContinuationStarted`    | `Tasks`(`0x2`)                           | Verbose (5)       | 当与 TaskWaitEnd 相关的工作（方法）开始时触发。 |
| `TaskWaitContinuationCompleted`  | `TaskStops`(`0x40`)                      | Verbose (5)       | 当与 TaskWaitEnd 相关的工作（方法）完成时触发。 |
| `AwaitTaskContinuationScheduled` | `TaskTransfer`(`0x1`)  `Tasks`(`0x2`)    | Informational (4) | 当一个任务的异步延续被安排时启动。              |

