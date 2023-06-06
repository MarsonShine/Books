# 事件源活动 IDs

> 注意：
>
> 这篇文章适用于： ✔️ .NET Core 3.1 及以上版本 ✔️ .NET Framework 4.5 及以上版本

本指南解释了活动 ID，这是一个可选的标识符，可以与使用 [System.Diagnostics.Tracing.EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 生成的每个事件一起被记录。关于介绍，请参见 [EventSource 入门](eventsource-getting-started.md)。

## 记录并发作业的挑战

很久以前，一个典型的应用程序可能是简单的、单线程的，这使得日志记录很直接。你可以按顺序将每个步骤写入日志文件，然后按照写入的顺序读回日志，了解期间发生了什么。如果该应用程序处理请求，一次只处理一个请求。请求 A 的所有日志信息将被按顺序打印，然后是请求 B 的所有信息，以此类推。当应用程序变成多线程时，这种策略不再起作用，因为多个请求在同一时间被处理。然而，如果每个请求都被分配给一个单独的线程来完全处理，你可以通过为每个日志消息记录一个线程 ID 来解决这个问题。例如，一个多线程的应用程序可能会记录：

```
Thread Id      Message
---------      -------
12             BeginRequest A
12             Doing some work
190            BeginRequest B
12             uh-oh error happened
190            Doing some work
```

通过读取线程 ID，你知道线程 12 正在处理请求 A，线程 190 正在处理请求 B，因此 "uh-oh error happened" 的消息与请求 A 有关。现在，使用 `async` 和 `await` 是很常见的，这样，在工作完成之前，**一个请求可以在许多不同的线程上被部分处理**。线程 ID 不再足以将一个请求产生的所有消息关联起来。活动 ID（Activity IDs） 解决了这个问题。它们提供了一个更精细的标识符，可以跟踪单个请求或请求的一部分，无论工作是否分散在不同的线程中。

> 这里提到的活动 ID 概念与 System.Diagnostics.Tracing.Activity 不一样，尽管命名相似。

## 使用 ActivityID 来跟踪作业

你可以运行下面的代码，看看活动追踪的效果。

```c#
using System.Diagnostics.Tracing;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleWriterEventListener listener = new ConsoleWriterEventListener();

        Task a = ProcessWorkItem("A");
        Task b = ProcessWorkItem("B");
        await Task.WhenAll(a, b);
    }

    private static async Task ProcessWorkItem(string requestName)
    {
        DemoEventSource.Log.WorkStart(requestName);
        await HelperA();
        await HelperB();
        DemoEventSource.Log.WorkStop();
    }

    private static async Task HelperA()
    {
        DemoEventSource.Log.DebugMessage("HelperA");
        await Task.Delay(100); // pretend to do some work
    }

    private static async Task HelperB()
    {
        DemoEventSource.Log.DebugMessage("HelperB");
        await Task.Delay(100); // pretend to do some work
    }
}

[EventSource(Name ="Demo")]
class DemoEventSource : EventSource
{
    public static DemoEventSource Log = new DemoEventSource();

    [Event(1)]
    public void WorkStart(string requestName) => WriteEvent(1, requestName);
    [Event(2)]
    public void WorkStop() => WriteEvent(2);

    [Event(3)]
    public void DebugMessage(string message) => WriteEvent(3, message);
}

class ConsoleWriterEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if(eventSource.Name == "Demo")
        {
            Console.WriteLine("{0,-5} {1,-40} {2,-15} {3}", "TID", "Activity ID", "Event", "Arguments");
            EnableEvents(eventSource, EventLevel.Verbose);
        }
        else if(eventSource.Name == "System.Threading.Tasks.TplEventSource")
        {
            // Activity IDs aren't enabled by default.
            // Enabling Keyword 0x80 on the TplEventSource turns them on
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        lock (this)
        {
            Console.Write("{0,-5} {1,-40} {2,-15} ", eventData.OSThreadId, eventData.ActivityId, eventData.EventName);
            if (eventData.Payload.Count == 1)
            {
                Console.WriteLine(eventData.Payload[0]);
            }
            else
            {
                Console.WriteLine();
            }
        }
    }
}
```

下面给出我机器上运行的结果：

```
TID   Activity ID                              Event           Arguments
30744 00000011-0000-0000-0000-000082869d59     WorkStart       A
30744 00000011-0000-0000-0000-000082869d59     DebugMessage    HelperA
30744 00000012-0000-0000-0000-000083869d59     WorkStart       B
30744 00000012-0000-0000-0000-000083869d59     DebugMessage    HelperA
23652 0000000d-0001-0000-3c1f-0000ffdcd7b5     DebugMessage    HelperB
28592 00000010-0001-0000-3c1f-0000ffdcd7b5     DebugMessage    HelperB
28592 00000012-0000-0000-0000-000083869d59     WorkStop
23652 00000011-0000-0000-0000-000082869d59     WorkStop
```

> 有一个已知的问题，Visual Studio 调试器可能导致生成无效的活动ID。在继续解决这个问题之前，要么不要在调试器下运行这个样本，要么在 Main 的开头设置一个断点，并在即时窗口中评估表达式 'System.Threading.Tasks.TplEventSource.Log.TasksSetActivityIds = false'

使用活动 ID，你可以看到工作项目 A 的所有消息都有 ID 00000011-...，工作项目 B 的所有消息都有ID 00000012-.... 这两个工作项目首先在线程 30744 上做了一些工作，但随后它们各自在独立的线程池线程 28592 和 23652 上继续工作，所以试图只用线程 ID 来跟踪请求是行不通的。

EventSource 有一个自动的启发式（automatic heuristic）方法，即定义一个名为 `_Something_Start` 的事件，紧接着另一个名为 `_Something_Stop` 的事件被认为是一个工作单元的开始和停止。记录一个新工作单元的开始事件会创建一个新的活动 ID，并开始记录同一线程上所有具有该活动 ID 的事件，直到停止事件被记录。当使用 async 和 await 时，该 ID 也会自动跟随 async 控制流。尽管我们推荐你使用 Start/Stop 的命名后缀，但你可以通过使用 [EventAttribute.Opcode](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute.opcode#system-diagnostics-tracing-eventattribute-opcode) 属性明确地注释事件，来命名任何你喜欢的事件。将第一个事件设置为 EventOpcode.Start，第二个事件设置为 EventOpcode.Stop。

## 并行作业的日志请求

有时，单个请求可能**并行**地完成其工作的不同部分，并且您希望对所有日志事件和子部分进行分组。下面的示例模拟了一个请求，该请求并行执行两个数据库查询，然后对每个查询的结果进行一些处理。您希望隔离每个查询的工作，但也要了解在可能运行许多并发请求时哪些查询属于同一个请求。这被建模为一个树，其中每个顶级请求是根，然后工作的子部分是分支。树中的每个节点都有自己的活动 ID，使用新子活动 ID 记录的第一个事件记录了一个名为 Related 活动 ID 的额外字段，用于描述其父节点。

运行下面的代码：

```c#
using System.Diagnostics.Tracing;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleWriterEventListener listener = new ConsoleWriterEventListener();

        await ProcessWorkItem("A");
    }

    private static async Task ProcessWorkItem(string requestName)
    {
        DemoEventSource.Log.WorkStart(requestName);
        Task query1 = Query("SELECT bowls");
        Task query2 = Query("SELECT spoons");
        await Task.WhenAll(query1, query2);
        DemoEventSource.Log.WorkStop();
    }

    private static async Task Query(string query)
    {
        DemoEventSource.Log.QueryStart(query);
        await Task.Delay(100); // pretend to send a query
        DemoEventSource.Log.DebugMessage("processing query");
        await Task.Delay(100); // pretend to do some work
        DemoEventSource.Log.QueryStop();
    }
}

[EventSource(Name = "Demo")]
class DemoEventSource : EventSource
{
    public static DemoEventSource Log = new DemoEventSource();

    [Event(1)]
    public void WorkStart(string requestName) => WriteEvent(1, requestName);
    [Event(2)]
    public void WorkStop() => WriteEvent(2);
    [Event(3)]
    public void DebugMessage(string message) => WriteEvent(3, message);
    [Event(4)]
    public void QueryStart(string query) => WriteEvent(4, query);
    [Event(5)]
    public void QueryStop() => WriteEvent(5);
}

class ConsoleWriterEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Demo")
        {
            Console.WriteLine("{0,-5} {1,-40} {2,-40} {3,-15} {4}", "TID", "Activity ID", "Related Activity ID", "Event", "Arguments");
            EnableEvents(eventSource, EventLevel.Verbose);
        }
        else if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
        {
            // Activity IDs aren't enabled by default.
            // Enabling Keyword 0x80 on the TplEventSource turns them on
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        lock (this)
        {
            Console.Write("{0,-5} {1,-40} {2, -40} {3,-15} ", eventData.OSThreadId, eventData.ActivityId, eventData.RelatedActivityId, eventData.EventName);
            if (eventData.Payload.Count == 1)
            {
                Console.WriteLine(eventData.Payload[0]);
            }
            else
            {
                Console.WriteLine();
            }
        }
    }
}
```

下面是输出结果：

```
TID   Activity ID                              Event           Arguments
24736 00000011-0000-0000-0000-00008afd9d59     00000000-0000-0000-0000-000000000000     WorkStart       A
24736 00001011-0000-0000-0000-00008acd9d59     00000011-0000-0000-0000-00008afd9d59     QueryStart      SELECT bowls
24736 00002011-0000-0000-0000-00008add9d59     00000011-0000-0000-0000-00008afd9d59     QueryStart      SELECT spoons
21928 00001011-0000-0000-0000-00008acd9d59     00000000-0000-0000-0000-000000000000     DebugMessage    processing query
31652 00002011-0000-0000-0000-00008add9d59     00000000-0000-0000-0000-000000000000     DebugMessage    processing query
31652 00002011-0000-0000-0000-00008add9d59     00000000-0000-0000-0000-000000000000     QueryStop
21928 00001011-0000-0000-0000-00008acd9d59     00000000-0000-0000-0000-000000000000     QueryStop
21928 00000011-0000-0000-0000-00008afd9d59     00000000-0000-0000-0000-000000000000     WorkStop
```

这个例子只运行一个顶级请求，它被分配了活动 ID 00000011-.... 然后每个 QueryStart 事件开始了一个新的请求分支，其活动ID分别为 00001011-...和 00002011-...。你可以确定这些 ID 是原始请求的子女，因为两个开始事件都在相关活动 ID 字段中记录了他们的父辈 0000000011-...。

> 注意：
>
> 你可能已经注意到 ID 的数值在父辈和子辈之间有一些明显的模式，并不是随机的。尽管在简单的情况下，它可以帮助人们从视觉上发现关系，但对于工具来说，最好不要依赖这一点，把ID当作不透明的标识符。随着嵌套层次的加深，字节模式将发生变化。使用相关活动ID字段是确保工具可靠工作的最好方法，无论嵌套级别如何。

因为具有复杂的子工作项目树的请求将产生许多不同的活动 ID，这些 ID 通常最好由工具来解析，而不是试图用手来重建树。[PerfView](https://github.com/Microsoft/perfview) 是一个知道如何与这些 ID 注释的事件进行关联的工具。