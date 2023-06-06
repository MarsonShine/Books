# EventSource 快速开始

本攻略展示了如何用 System.Diagnostics.Tracing.EventSource 记录一个新的事件，在跟踪文件中收集事件，查看跟踪，并理解基本的 EventSource 概念。

> 注意：
>
> 许多与 EventSource 集成的技术使用术语"Tracing"和"Traces"，而不是"Logging"和"Logs"。尽管含义是一样的。

## 记录一个事件

EventSource 的目标是允许 .NET 开发人员编写这样的代码来记录一个事件：

```
DemoEventSource.Log.AppStarted("Hello World!", 12);
```

这一行代码有一个记录对象（`DemoEventSource.Log`），一个代表要记录的事件的方法（`AppStarted`），以及一些可选择的强类型的事件参数（HelloWorld！和 12）。没有任何粗略的级别、事件 ID、消息模板或其他不需要在调用地点的东西。所有这些关于事件的其他信息都是通过定义一个源自 [System.Diagnostics.Tracing.EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 的新类来编写的。

下面是一个例子：

```c#
using System.Diagnostics.Tracing;

namespace EventSourceDemo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            DemoEventSource.Log.AppStarted("Hello World!", 12);
        }
    }

    [EventSource(Name = "Demo")]
    class DemoEventSource : EventSource
    {
        public static DemoEventSource Log { get; } = new DemoEventSource();

        [Event(1)]
        public void AppStarted(string message, int favoriteNumber) => WriteEvent(1, message, favoriteNumber);
    }
}
```

DemoEventSource 类声明了每种事件您想要日志的方法。在这种情况下，名为'AppStarted'的事件是由 `AppStarted()` 方法定义的。每当代码调用 `AppStarted()` 方法时，如果该事件是启用的，就会在日志中记录一个新的'AppStarted'事件。这是每个事件可以捕获的数据的一部分：

- 事件名称 - 识别被记录的事件种类的名称。事件名称将与方法名称相同，本例中为"AppStarted"。
- 事件ID - 一个数字 ID，用于识别被记录的事件类型。它的作用与名称相似，但可以帮助快速自动处理日志。AppStarted 事件的 ID 为1，在 [EventAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute) 中指定。
- Source 名称 - 包含该事件的 EventSource 的名称。这被用作事件的命名空间。事件名称和 ID 只需要在其源的范围内是唯一的。这里的源被命名为"Demo"，在类定义的 [EventSourceAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsourceattribute) 中指定。源名称通常也被称为提供者名称。
- 参数 - 所有的方法参数值都被序列化。
- 其他信息 - 事件也可以包含时间戳、线程 ID、处理器 ID、[活动 ID(Activity Ids)](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-activity-ids)、堆栈跟踪和事件元数据，如消息模板、口令级别和关键词。

关于创建事件的更多信息和最佳实践，请参见[仪表代码创建事件](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-instrumentation)。

## 收集和展示追踪文件

在代码中没有必要的配置来描述哪些事件应该被启用，记录的数据应该被发送到哪里，或者数据应该以什么格式存储。如果你现在运行这个应用程序，它默认不会产生任何跟踪文件。EventSource 使用["发布-订阅"模式](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern)，它要求订阅者指明应启用的事件，并控制订阅事件的所有序列化。EventSource 与 [Event Tracing for Windows（ETW）](https://learn.microsoft.com/en-us/windows/win32/etw/event-tracing-portal)和 [EventPipe](../EventPipe.md)（仅适用于 .NET Core）集成，用于订阅。也可以使用 [System.Diagnostics.Tracing.EventListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventlistener) API 创建自定义订阅者。

这个演示展示了一个用于 .NET Core 应用程序的 [EventPipe](../EventPipe.md) 例子。要了解更多选项，请参见[收集和查看事件追踪](eventsource-collect-and-view-traces.md)。[EventPipe](../EventPipe.md) 是一种开放的、跨平台的跟踪技术，内置于 .NET Core 运行时，为 .NET 开发人员提供跟踪收集工具和可移植的紧凑跟踪格式（`*.nettrace` 文件）。[dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) 是一种收集 EventPipe 跟踪的命令行工具。

1. 下载并安装 
2. 构建控制台应用程序。这个例子名为 EventSourceDemo.exe，并在当前目录中。执行如下命令行：

```
>dotnet-trace collect --providers Demo -- EventSourceDemo.exe
```

输出如下结果：

```
Provider Name                           Keywords            Level               Enabled By
Demo                                    0xFFFFFFFFFFFFFFFF  Verbose(5)          --providers

Launching: EventSourceDemo.exe
Process        : E:\temp\EventSourceDemo\bin\Debug\net6.0\EventSourceDemo.exe
Output File    : E:\temp\EventSourceDemo\bin\Debug\net6.0\EventSourceDemo.exe_20220303_001619.nettrace

[00:00:00:00]   Recording trace 0.00     (B)
Press <Enter> or <Ctrl+C> to exit...

Trace completed.
```

该命令运行 EventSourceDemo.exe，并启用 "Demo" EventSource 中的所有事件，并输出了追踪结果文件 EventSourceDemo.exe_20220303_001619.nettrace。在Visual Studio 中打开该文件可以看到被记录的事件。(也可以通过 [perfview](https://github.com/microsoft/perfview) 打开)

在列表视图中，你可以看到第一个事件是 Demo/AppStarted 事件。文本栏有保存的参数，时间戳栏显示事件发生在日志开始后 27 毫秒，在右边你可以看到调用栈。其他事件在 dotnet-trace 收集到的每一个跟踪中都是自动启用的，尽管它们可以被忽略，并在用户界面中被过滤掉，如果它们让人分心的话。这些额外的事件捕获了一些关于进程和 jitted 代码的信息，这使得 Visual Studio 能够重建事件堆栈的痕迹。