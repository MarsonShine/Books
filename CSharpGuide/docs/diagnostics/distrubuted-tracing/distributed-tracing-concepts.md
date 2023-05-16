# .NET 分布式追踪概念

分布式跟踪是一种诊断技术，可以帮助工程师定位应用程序中的故障和性能问题，特别是那些可能分布在多个机器或进程中的应用程序。请看[分布式跟踪概述](distributed-tracing.md)，了解关于分布式跟踪有用的地方的一般信息和开始使用的示例代码。

## 追踪和活动

每当应用程序收到一个新的请求，它都可以与一个追踪相关联。在用 .NET 编写的应用程序组件中，跟踪中的工作单位由 [System.Diagnostics.Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) 的实例表示，跟踪作为一个整体形成了这些 Activity 树，可能跨越许多不同的进程。为一个新请求创建的第一个活动构成了跟踪树的根，它跟踪整个持续时间和处理请求的成功/失败。可以选择创建子活动，将工作细分为不同的步骤，可以单独跟踪。例如，给定一个跟踪 Web 服务器中特定入站 HTTP 请求的活动，可以创建子活动来跟踪完成该请求所需的每个数据库查询。这允许独立地记录每个查询的持续时间和成功率。活动可以为每个工作单位记录其他信息，如 [OperationName](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.operationname#system-diagnostics-activity-operationname)、称为 [Tags](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.tags#system-diagnostics-activity-tags) 的 name-value 键值对以及 [Events](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.events#system-diagnostics-activity-events)。名称确定了正在执行的工作类型，标签可以记录工作的描述性参数，而事件是一种简单的日志机制，用于记录有时间戳的诊断信息。

> 注意：
>
> 分布式跟踪中工作单元的另一个常见行业名称是“Span”，.NET 在 “Span” 这个概念被很好地确立之前，就采用了 “Activity” 这个术语。

## 活动Ids

**分布式跟踪树中的活动之间的父子关系是通过唯一的 ID 建立的。**.NET 对分布式跟踪的实现支持两种 ID 方案：W3C 标准的 [TraceContext](https://www.w3.org/TR/trace-context/)，它是 .NET 5+ 中的默认方案，还有一种较早的 .NET 惯例，称为 "分层"，可用于向后兼容。[Activity.DefaultIdFormat](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.defaultidformat#system-diagnostics-activity-defaultidformat) 控制哪种 ID 方案被使用。在 W3C TraceContext 标准中，每个跟踪都被分配了一个全局唯一的 16 字节的跟踪 ID（[Activity.TraceId](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.traceid#system-diagnostics-activity-traceid)），而跟踪中的每个 Activity 都被分配了一个唯一的 8 字节的跨度 ID（[Activity.SpanId](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.spanid#system-diagnostics-activity-spanid)）。每个活动都会记录追踪 ID，它自己的跨度 ID，以及它的父级跨度 ID（Activity.ParentSpanId）。因为分布式跟踪可以跨进程追踪工作，父级和子级 Activity 可能不在同一个进程中。追踪 ID 和父级 span-id 的组合可以在全球范围内唯一地识别父级 Activity，不管它位于哪个进程中。

[Activity.DefaultIdFormat](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.defaultidformat#system-diagnostics-activity-defaultidformat) 控制哪种 ID 格式用于启动新的跟踪，但默认情况下，将一个新的 Activity 添加到一个现有的跟踪中，使用父 Activity 正在使用的格式。将 [Activity.ForceDefaultIdFormat](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.forcedefaultidformat#system-diagnostics-activity-forcedefaultidformat) 设置为 "true" 将覆盖这一行为，并使用 DefaultIdFormat 创建所有新的 Activity，即使父级使用不同的 ID 格式。

## 开始和停止活动

进程中的每个线程都可以有一个相应的 Activity 对象，该对象正在跟踪该线程上发生的工作，可以通过 [Activity.Current](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.current#system-diagnostics-activity-current) 访问。当前的活动自动地沿着线程上的所有同步调用流动，以及跟踪在不同线程上处理的异步调用。如果活动 A 是一个线程上的当前活动，并且代码启动了一个新的活动 B，那么 B 就成为该线程上新的当前活动。默认情况下，活动 B 也会把活动 A 当作它的父对象。当活动 B 后来被停止时，活动 A 将被恢复为该线程上的当前活动。当一个活动被启动时，它捕捉到当前的时间作为 [Activity.StartTimeUtc](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.starttimeutc#system-diagnostics-activity-starttimeutc)。当它停止时，[Activity.Duration](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.duration#system-diagnostics-activity-duration) 被计算为当前时间和开始时间的差值。

## 跨进程边界协作

为了跟踪跨进程的工作，需要在网络上传输 Activity 的父 ID，以便接收进程可以创建引用它们的 Activity。当使用 W3C TraceContext ID 格式时，.NET 也使用该[标准](https://www.w3.org/TR/trace-context/)推荐的 HTTP头 来传输该信息。当使用 Hierarchical ID 格式时，.NET 使用一个自定义的 request-id HTTP头来传输 ID。与许多其他语言的运行时不同，.NET 的盒式库，如 ASP.NET 网络服务器和 System.Net.Http 原生地理解如何对 HTTP 消息的活动 ID 进行解码和编码。运行时也了解如何通过同步和异步调用来流动 ID。这意味着接收和发射 HTTP 消息的 .NET 应用程序会自动参与分布式跟踪 ID 的流动，而不需要应用程序开发人员进行特殊编码或依赖第三方库。第三方库可以增加对通过非 HTTP 消息协议传输 ID 的支持，或支持 HTTP 的自定义编码约定。

## 收集跟踪

仪表代码可以创建[活动](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity)对象作为分布式跟踪的一部分，但这些对象中的信息需要在一个集中的持久性存储中传输和序列化，以便以后可以有效地审查整个跟踪。有几个遥测收集库可以完成这个任务，如 [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/distributed-tracing)、[OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/getting-started-console/README.md) 或由第三方遥测或 APM 供应商提供的库。另外，开发者可以通过使用 [System.Diagnostics.ActivityListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener) 或 [System.Diagnostics.DiagnosticListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticlistener) 编写自己的自定义 Activity 遥测集合。ActivityListener 支持观察任何活动，无论开发者是否对其有任何预先的了解。这使得 ActivityListener 成为简单而灵活的通用解决方案。相比之下，使用 DiagnosticListener 是一个更复杂的方案，它需要被检测的代码通过调用 [DiagnosticSource.StartActivity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource.startactivity) 来选择加入，并且集合库需要知道被检测的代码在启动时使用的确切命名信息。使用 DiagnosticSource 和 DiagnosticListener 允许创建者和监听器交换任意的 .NET 对象并建立自定义的信息传递约定。

## 采样

为了提高高吞吐量应用程序的性能，.NET 上的分布式跟踪支持只对跟踪的一个子集进行采样，而不是记录所有的跟踪。对于用推荐的 [ActivitySource.StartActivity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource.startactivity) API 创建的活动，遥测收集库可以用 [ActivityListener.Sample](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener.sample) 回调控制采样。日志库可以选择根本不创建活动，用传播分布式跟踪 ID 所需的最小信息来创建它，或者用完整的诊断信息来填充它。这些选择权衡了增加性能的开销和增加诊断的效用。使用调用 [Activity.Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.-ctor) 和 [DiagnosticSource.StartActivity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource.startactivity) 的旧模式启动的活动也可以通过首先调用 [DiagnosticSource.IsEnabled](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource.isenabled) 支持 DiagnosticListener 采样。即使在捕获完整的诊断信息时，.NET 的实现也被设计成快速的--与高效的收集器相结合，在现代硬件上可以在大约一微秒内创建、填充和传输一个活动。采样可以将每个没有被记录的活动的仪器成本降低到 100 纳秒以下。

## 下一步

在 .NET 应用程序中如何使用示例代码，可详见[分布式追踪探测器](distributed-tracing-instrumentation-walkthroughs.md)