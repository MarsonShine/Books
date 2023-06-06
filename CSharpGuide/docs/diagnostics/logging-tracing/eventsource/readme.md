# 事件源（EventSource）

[System.Diagnostics.Tracing.EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 是一个内置于 .NET 运行时的快速结构化日志解决方案。在 .NET Framework 上，EventSource 可以将事件发送到 [Event Tracing for Windows（ETW）](https://learn.microsoft.com/en-us/windows/win32/etw/event-tracing-portal)和 [System.Diagnostics.Tracing.EventListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventlistener)。在 .NET Core 上，EventSource 还支持 [EventPipe](../EventPipe.md)，这是一个跨平台的追踪选项。开发人员通常将 EventSource 日志用于性能分析，但 EventSource 也可用于任何对日志有帮助的诊断任务。.NET 运行时已经有了[内置事件](../readme.md)的工具，您可以记录自己的自定义事件。

> 注意：
>
> 许多与 EventSource 集成的技术使用术语"Tracing"和"Traces"，而不是"Logging"和"Logs"。尽管这些术语的含义相同。

- [快速开始](eventsource-getting-started.md)
- [仪表化代码创建事件源事件](eventsource-instrumentation.md)
- [收集和查看事件源追踪](eventsource-collect-and-view-traces.md)
- [事件源的 Activity Ids](eventsource-activity-ids.md)