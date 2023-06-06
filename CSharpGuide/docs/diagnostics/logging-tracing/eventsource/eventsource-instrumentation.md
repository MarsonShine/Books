# 创建 EventSource 事件的工具代码

[快速开始](eventsource-getting-started.md)向您展示了如何创建最小事件源并在跟踪文件中收集事件。本教程将更详细地介绍如何使用 [System.Diagnostics.Tracing.EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 创建事件。

## 最小化的 EventSource

```c#
[EventSource(Name = "Demo")]
class DemoEventSource : EventSource
{
    public static DemoEventSource Log { get; } = new DemoEventSource();

    [Event(1)]
    public void AppStarted(string message, int favoriteNumber) => WriteEvent(1, message, favoriteNumber);
}
```

派生 EventSource 的基本结构总是相同的。特别是：

- 该类继承自 [System.Diagnostics.Tracing.EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource)
- 对于希望生成的每种不同类型的事件，都需要定义一个方法。此方法应该使用正在创建的事件的名称来命名。如果事件具有其他数据，则应使用参数传递这些数据。这些事件参数需要序列化，因此只允许使用特定类型（见后文）。
- 每个方法都有一个调用 WriteEvent 的主体，传递给它一个 ID(表示事件的数值)和事件方法的参数。ID 需要在 EventSource 中是唯一的。ID 是使用 [System.Diagnostics.Tracing.EventAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute) 显式分配的。
- EventSources 被设计成**单例**实例。因此，定义一个静态变量(按照约定称为 Log)来表示这个单例是很方便的。

## 定义时间方法的规则

1. 在 EventSource 类中定义的任何实例化的、非虚拟的、返回 void 类型的方法，默认情况下都是一个事件日志方法。
2. 如果该方法被标记为 [System.Diagnostics.Tracing.EventAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute)，则只能包含 virtual 或非 void 返回类型的方法。
3. 要将该方法标记为非日志方法，必须将其装饰为 [System.Diagnostics.Tracing.NonEventAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.noneventattribute)。
4. 时间日志方法必须通过使用 IDs 来关联。这可以通过显式地使用 [System.Diagnostics.Tracing.EventAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute) 修饰方法来实现，也可以通过类中方法的序号来隐式地实现。例如，使用隐式编号，类中的第一个方法的 ID 为 1，第二个方法的 ID 为 2，依此类推。
5. 事件日志方法必须调用 [WriteEvent](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.writeevent), [WriteEventCore](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.writeeventcore), [WriteEventWithRelatedActivityId](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.writeeventwithrelatedactivityid) 或 [WriteEventWithRelatedActivityIdCore](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.writeeventwithrelatedactivityidcore) 重载。
6. 事件 ID(无论是隐含的还是显式的)必须匹配传递给它调用的 WriteEvent* API的第一个参数。
7. 传递给 EventSource 方法的参数的数量、类型和顺序必须与传递给 `WriteEvent*` api 的方式保持一致。对于 `WriteEvent`，参数跟在事件 ID 后面，对于 `WriteEventWithRelatedActivityId`，参数跟在 relatedActivityId 后面。对于 `WriteEventCore` 方法，必须手动将参数序列化为 data 形参。
8. 事件名称不能包含 `<` 或 `>` 字符。虽然用户定义的方法也不能包含这些字符，但编译器将重写 `async` 方法以包含这些字符。为了确保这些生成的方法不会成为事件，请使用 [NonEventAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.noneventattribute) 在 [EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 上标记所有非事件方法。

## 最佳实践

1. 从 EventSource 派生的类型通常在层次结构或实现接口中没有中间类型。请参阅后续的[高级定制](eventsource-instrumentation.md#advanced-customizations)，了解可能有用的一些例外情况。
2. 一般来说，EventSource 类的名称对于 EventSource 来说是一个不好的公共名称。公共名称(将显示在日志配置和日志查看器中的名称)应该是全局唯一的。因此，使用[System.Diagnostics.Tracing.EventSourceAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsourceattribute) 给你的事件源一个公共名称是很好的做法。上面使用的名称 “Demo” 很短，不太可能是唯一的，因此不适合用于生产环境。一个常见的约定是使用具有层次结构的名称。或者-作为分隔符，例如 “MyCompany-Samples-Demo”，或者 EventSource 为其提供事件的程序集或命名空间的名称。不建议将 “EventSource” 作为公共名称的一部分。
3. 显式地分配事件 ID，这样对源类中的代码进行看似无害的更改(例如重新排列或在中间添加方法)不会更改与每个方法关联的事件 ID。
4. 在编写表示工作单元的开始和结束的事件时，按照惯例，这些方法使用后缀 “Start” 和 “Stop” 来命名。例如，'RequestStart' 和 'RequestStop'。
5. 不要为 EventSourceAttribute 的 Guid 属性指定显式的值，除非出于向后兼容性的原因需要它。默认 Guid 值是从源的名称派生出来的，这允许工具接受更容易读懂的名称并派生相同的 Guid。
6. 在执行与触发事件相关的任何资源密集型工作之前，调用 [IsEnabled()](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.isenabled#system-diagnostics-tracing-eventsource-isenabled)，例如计算一个昂贵的事件参数，如果事件被禁用，则不需要该参数。
7. 尝试保持 EventSource 对象的兼容性并对其进行适当的版本化。事件的默认版本为 0。版本可以通过设置 [EventAttribute.Version](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute.version) 来改变。每当更改与事件序列化的数据时，都要更改事件的版本。始终将新的序列化数据添加到事件声明的末尾，即方法参数列表的末尾。如果这是不可能的，创建一个新的事件与一个新的 ID 来取代旧的。
8. 声明事件方法时，在指定可变大小的数据之前指定固定大小的有效负载数据。
9. 不要使用包含空字符的字符串。当为 ETW 生成清单时，EventSource 会将所有字符串声明为 null 终止，即使 c# 字符串中可能有一个 null 字符。如果字符串包含空字符，则整个字符串将被写入事件有效负载，但任何解析器都会将第一个空字符视为字符串的结尾。如果字符串后面有有效载荷参数，将解析字符串的其余部分，而不是预期值。

## 典型的事件定制

### 设置事件冗长级别

每个事件都有一个详细级别，事件订阅者通常在某个详细级别之前启用 EventSource 上的所有事件。事件使用 [Level](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventattribute.level#system-diagnostics-tracing-eventattribute-level) 属性声明其冗长级别。例如，在下面的 EventSource 中，请求级别为 Informational，更低级别的事件的订阅者不会记录详细的 DebugMessage 事件。

```c#
[EventSource(Name = "MyCompany-MyEventSource-Demo")]
public class CustomEventSource : EventSource
{
    public static CustomEventSource Log { get; } = new CustomEventSource();
    [Event(1, Level = EventLevel.Informational)]
    public void AppStarted(string message, int favoriteNumber) => WriteEvent(1, message, favoriteNumber);
    [Event(2, Level = EventLevel.Verbose)]
    public void DebugMessage(string message) => WriteEvent(2, message);
}
```

如果事件的详细级别未在 EventAttribute 中指定，则默认为 Informational。

#### 最佳实践

对于相对罕见的警告或错误，使用低于 Informational 的级别。如果有疑问，坚持使用缺省的 Informational，对于发生频率超过 1000 个事件/秒的事件使用 Verbose。

### 设置事件关键字

一些事件跟踪系统支持关键字作为额外的过滤机制。与按级别详情对事件进行分类的冗长不同，关键字旨在根据其他标准(如代码功能区域或对诊断某些问题有用的标准)对事件进行分类。关键字被命名为位标志，每个事件可以应用关键字的任意组合。例如，下面的 EventSource 定义了一些与请求处理相关的事件和与启动相关的其他事件。如果开发人员想要分析启动的性能，他们可能只允许记录用 startup 关键字标记的事件。

```c#
[EventSource(Name = "MyCompany-MyEventSource-Demo")]
public class CustomEventSource : EventSource
{
    public static CustomEventSource Log { get; } = new CustomEventSource();
    ...
    [Event(3, Keywords = EventSourceKeywordsConsts.Startup)]
    public void RequestStart(int requestId) => WriteEvent(3, requestId);
    [Event(4, Keywords = EventSourceKeywordsConsts.Requests)]
    public void RequestStop(int requestId) => WriteEvent(4, requestId);
}
```

关键字必须使用一个名为 `Keywords` 的嵌套类来定义，每个关键字都由成员类型的 `public const EventKeywords` 定义。

#### 最佳实践

在区分高流量事件时，关键词更为重要。这可以让事件消费者提高事件的复杂性，但通过仅允许特定事件的一个窄子集来管理性能 overhead 和日志大小。触发频率超过每秒 1,000  的事件是设置关键词候选者。

## 支持的参数类型

EventSource 要求所有事件参数都可以序列化，因此它只接受有限的类型集。这些都是：

- 基本类型：bool、byte、sbyte、char、short、ushort、int、int、long、ulong、float、double、IntPtr和UIntPtr、Guid decimal、string、DateTime、DateTimeOffset、TimeSpan
- 枚举
- 用 [System.Diagnostics.Tracing.EventDataAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventdataattribute) 属性属性的结构。只有具有可序列化类型的公共实例属性才会被序列化。
- 匿名类型，其中所有公共属性都是可序列化的类型。
- 可序列化类型的数组。
- `Nullable<T>` 其中 T 是可序列化的类型。
- `KeyValuePair<T, U>` 其中 T 和 U 都是可序列化的类型。
- 实现了 `IEnumerable<T>` 的类型，其中 T 必须是可序列化的类型。

## 问题快照

EventSource 类被设计成在默认情况下**永远不会抛出异常**。这是一个有用的属性，因为日志记录通常被视为可选的，并且您通常不希望写入日志消息的错误导致应用程序失败。然而，这使得在 EventSource 中发现任何错误变得困难。这里有几种技术可以帮助排除故障：

1. EventSource 构造函数有重载，它接受 [EventSourcesSettings](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsourcesettings)。尝试暂时启用 `ThrowOnEventWriteErrors` 标志。
2. [EventSource.ConstructionException](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.constructionexception#system-diagnostics-tracing-eventsource-constructionexception) 属性存储验证事件日志方法时生成的任何 Exception。这可以显示各种编写错误。
3. EventSource 使用事件ID 为 0 来记录错误，这个错误事件有一个描述错误的字符串。
4. 在调试时，同样的错误字符串也将使用 `Debug.Writeline()` 进行记录，并显示在调试输出窗口中。
5. EventSource 在内部抛出，然后在发生错误时捕获异常。要观察这些异常何时发生，请在调试器中启用第一次机会异常(fist chance exception)，或者在启用 .net 运行时的 [Exception 事件](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-exception-events)来使用事件跟踪。

## 高级定制

### 设置 OpCodes 和 任务（Tasks）

ETW 有任务和操作码的概念，它们是标记和过滤事件的进一步机制。可以使用 Task 和 Opcode 属性将事件与特定的任务和操作码关联起来。这里有一个例子：

```c#
[EventSource(Name = "MyCompany-MyEventSource-AdvancedCustomize")]
public class AdvancedCustomizedEventSource : EventSource
{
    static public AdvancedCustomizedEventSource Log { get; } = new AdvancedCustomizedEventSource();

    [Event(1, Task = EventTaskConsts.Request, Opcode = EventOpcode.Start)]
    public void RequestStart(int RequestID, string Url)
    {
        WriteEvent(1, RequestID, Url);
    }

    [Event(2, Task = EventTaskConsts.Request, Opcode = EventOpcode.Info)]
    public void RequestPhase(int RequestID, string PhaseName)
    {
        WriteEvent(2, RequestID, PhaseName);
    }

    [Event(3, Keywords = EventSourceKeywordsConsts.Requests,
           Task = EventTaskConsts.Request, Opcode = EventOpcode.Stop)]
    public void RequestStop(int RequestID)
    {
        WriteEvent(3, RequestID);
    }
}
```

你可以通过声明两个具有后续事件 ID 的事件方法来隐含地创建 EventTask 对象，这些方法的命名模式为 `<EventName>Start` 和 `<EventName>Stop` 。这些事件必须在类定义中相邻声明，并且 `<EventName>Start` 方法必须在前。

### 自描述（追踪记录）vs 清单事件格式

这个概念只有在从 ETW 订阅 EventSource 时才有意义。ETW 有两种不同的方式可以记录事件，清单格式（Manifest format）和自描述（self-describing）（有时称为追踪记录）格式。基于清单的 EventSource 对象在初始化时生成并记录一个 XML 文档，代表该类上定义的事件。这需要 EventSource 对自身进行反射，以生成提供者和事件元数据。在自描述格式中，每个事件的元数据是与事件数据一起在线传输的，而不是提前传输。自描述的方法支持更灵活的 Write 方法，可以发送任意的事件，而不需要创建一个预先定义的事件记录方法。由于避免了反射，它在启动时也会稍微快一些。然而，与每个事件一起触发的额外元数据增加了少量的性能开销，这在发送大量事件时可能并不可取。

要使用自描述的事件格式，请使用 `EventSource(String)` 构造函数、`EventSource(String, EventSourceSettings)` 构造函数或在 `EventSourceSettings` 上设置 `EtwSelfDescribingEventFormat` 标志来构造你的 EventSource。

### EventSource 类型实现的接口

一个 EventSource 类型可以实现一个接口，以便在各种高级日志系统中无缝集成，这些系统使用接口来定义一个共同的日志目标。下面是一个可能使用的例子：

```c#
public interface IMyLogging
{
    void Error(int errorCode, string message);
    void Warning(string message);
}
[EventSource(Name = "MyCompany-MyEventSource-MyComponentLogging")]
internal class MyLoggingEventSource : EventSource, IMyLogging
{
    public static MyLoggingEventSource Log { get; } = new MyLoggingEventSource();
    [Event(1)]
    public void Error(int errorCode, string message)
    {
        WriteEvent(1, errorCode, message);
    }
    [Event(2)]
    public void Warning(string message)
    {
        WriteEvent(2, message);
    }
}
```

你必须在接口方法上指定 EventAttribute，否则（出于兼容性的原因）该方法将不会被视为一个追踪方法。为了防止命名冲突，不允许明确的接口方法实现。

### EventSource 类的层次结构

在大多数情况下，您可以编写直接从 EventSource 类派生出来的类型。然而，有时定义一个派生 EventSource 类型通用的功能是非常有用的，例如定制的 WriteEvent 重载（见下文[优化高容量事件的性能](#优化高容量事件的性能)）。

只要不定义任何关键字、任务、操作码、通道或事件，就可以使用抽象基类。这里有一个例子，UtilBaseEventSource 类定义了一个优化的 WriteEvent 重载，这是同一个组件中的多个派生 EventSources 需要的。这些派生类型可以通过下面的 OptimizedEventSource 类说明：

```c#
public abstract class UtilBaseEventSource : EventSource
{
    protected UtilBaseEventSource() : base()
    {
    }
    protected UtilBaseEventSource(bool throwOnEventWriteErrors) : base(throwOnEventWriteErrors)
    {
    }
    protected UtilBaseEventSource(string eventSourceName) : base(eventSourceName)
    {
    }

    protected unsafe void WriteEvent(int eventId, int arg1, short arg2, long arg3)
    {
        if (IsEnabled())
        {
            EventData* descrs = stackalloc EventData[2];
            descrs[0].DataPointer = (IntPtr)(&arg1);
            descrs[0].Size = 4;
            descrs[1].DataPointer = (IntPtr)(&arg2);
            descrs[1].Size = 2;
            descrs[2].DataPointer = (IntPtr)(&arg3);
            descrs[2].Size = 8;
            WriteEventCore(eventId, 3, descrs);
        }
    }
}

[EventSource(Name = "MyCompany-OptimizedEventSource")]
public sealed class OptimizedEventSource : UtilBaseEventSource
{
    public static OptimizedEventSource Log { get; } = new OptimizedEventSource();

    [Event(1, Keywords = EventSourceKeywordsConsts.Kwd1, Level = EventLevel.Informational,
       Message = "LogElements called {0}/{1}/{2}.")]
    public void LogElements(int n, short sh, long l)
    {
        WriteEvent(1, n, sh, l); // Calls UtilBaseEventSource.WriteEvent
    }
}
```

## 优化高容量事件的性能

EventSource 类有许多 WriteEvent 的重载，包括一个参数数量可变的重载。当其他重载都不匹配时，就会调用 params 方法。不幸的是，params 重载是相对昂贵的。尤其是它：

- 分配一个数组来保存可变参数。
- 将每个参数转为一个对象，这将导致对值类型的额外的分配（装箱操作）。
- 将这些对象分配到数组中。
- 调用函数。
- 计算出每个数组元素的类型，以确定如何将其序列化。

这可能是专用类的 10 到 20 倍的成本。这对于低容量的情况来说并不重要，但对于高容量的事件来说，这可能很重要。对于确保不使用 params 重载，有两种重要的情况：

- 确保枚举类型被转换为`int`，以便它们符合快速重载之一。
- 为大批量的有效载荷创建新的快速 WriteEvent 重载。

下面是一个添加 WriteEvent 重载的例子，它需要四个整数参数：

```c#
[NonEvent]
public unsafe void WriteEvent(int eventId, int arg1, int arg2,
                              int arg3, int arg4)
{
    EventData* descrs = stackalloc EventProvider.EventData[4];

    descrs[0].DataPointer = (IntPtr)(&arg1);
    descrs[0].Size = 4;
    descrs[1].DataPointer = (IntPtr)(&arg2);
    descrs[1].Size = 4;
    descrs[2].DataPointer = (IntPtr)(&arg3);
    descrs[2].Size = 4;
    descrs[3].DataPointer = (IntPtr)(&arg4);
    descrs[3].Size = 4;

    WriteEventCore(eventId, 4, (IntPtr)descrs);
}
```

