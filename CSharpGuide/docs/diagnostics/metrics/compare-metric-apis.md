# Metric APIs 比较

当向 .NET 应用程序或库添加新的度量工具时，有各种不同的 api 可供选择。本文将帮助您了解哪些是可用的，以及涉及到的一些权衡。

API 主要有两大类，与供应商无关的和特定于供应商的。特定于供应商的 api 的优势在于，供应商可以快速地迭代他们的设计，添加专门的功能，并在他们的检测 api 和后端系统之间实现紧密集成。举个例子，如果你用 [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) 提供的度量 api 来检测你的应用，那么当你使用他们的分析工具时，你会期望找到集成良好的功能和所有 Application Insight 的最新特性。然而，库或应用程序现在也将耦合到这个供应商，并且在将来更改到不同的供应商将需要重写工具。对于库来说，这种耦合可能会产生特别大的问题，因为库开发人员可能使用一个供应商的 API，而引用库的应用程序开发人员则希望使用不同的供应商。为了解决这个耦合问题，与供应商无关的选项提供了一个标准化的 API 接口和可扩展性点，以便根据配置将数据路由到不同的供应商后端系统。但是，与供应商无关的 API 可能提供的功能较少，并且您仍然需要选择已与外观的扩展性机制集成的供应商。

## .NET APIs

。NET 有 20+ 年的历史，我们已经对指标 API 的设计进行了几次迭代，所有这些设计都受支持且与供应商无关：

### PerformanceCounter

[System.Diagnostics.PerformanceCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter) API 是最古老的指标 API。它们仅在 Windows 上受支持，并为 Windows 操作系统[性能计数器](https://learn.microsoft.com/en-us/windows/win32/perfctrs/performance-counters-portal)技术提供托管包装器。它们在所有受支持的 .NET 版本中都可用。

提供这些 API 主要是为了兼容性;.NET 团队认为这是一个稳定的领域，除了错误修复之外，不太可能得到进一步的改进。不建议将这些 API 用于新的开发项目，除非该项目仅限 Windows，并且您希望使用 Windows 性能计数器工具。

有关详细信息，请参阅 [.NET Framework 中的性能计数器](https://learn.microsoft.com/en-us/dotnet/framework/debug-trace-profile/performance-counters)。

### EventCounters

[EventCounters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/event-counters) API 紧随 `PerformanceCounters` 之后。此 API 旨在提供统一的跨平台体验。这些 API 以 .NET Core 3.1+ 为目标提供，一小部分在 .NET Framework 4.7.1 及更高版本上可用。这些 API 完全受支持，并且由关键 .NET 库主动使用，但它们的功能比较新的 [System.Diagnostics.Metrics](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) API 少。事件计数器能够报告变化率和平均值，但不支持直方图和百分位数。也不支持多维指标。自定义工具可以通过事件侦听器 API 实现，尽管它不是强类型，仅允许访问聚合值，并且在同时使用多个侦听器时存在限制。事件计数器由 Visual Studio、Application Insights、dotnet 计数器和 dotnet-monitor 直接支持。有关第三方工具支持，请查看供应商或项目文档以查看其是否可用。

在撰写本文时，这是具有最广泛和最稳定的生态系统支持的跨平台 .NET 运行时 API。但是，它可能会很快被对 [System.Diagnostics.Metrics](metrics-instrumentation.md) 的支持所取代。.NET 团队预计今后不会在此 API 上进行大量新投资，但与性能计数器一样，所有当前和未来用户仍积极支持该 API。

### System.Diagnostics.Metrics

[System.Diagnostics.Metrics](metrics-instrumentation.md) API 是最新的跨平台 API，是与 [OpenTelemetry](https://opentelemetry.io/) 项目密切合作设计的。OpenTelemetry 工作是遥测工具供应商、编程语言和应用程序开发人员之间的全行业协作，旨在为遥测 API 创建广泛兼容的标准。为了消除与添加第三方依赖项相关的任何摩擦，.NET 将指标 API 直接嵌入到基类库中。它可以通过面向 .NET 6 或在较旧的 .NET Core 和 .NET Framework 应用中通过添加对 .NET [System.Diagnostics.DiagnosticsSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource) 6.0 NuGet 包的引用来实现。除了旨在实现广泛的兼容性之外，此 API 还增加了对事件计数器所缺少的许多内容的支持，例如：

- 直方图和百分位数
- 多维度指标
- 强类型高性能侦听器 API
- 多个同时侦听器
- 侦听器访问未聚合的度量

尽管此 API 旨在与 OpenTelemetry 及其不断增长的可插拔供应商集成库生态系统很好地配合使用，但应用程序也可以选择直接使用 .NET 内置侦听器 API。使用此选项，您可以创建自定义指标工具，而无需使用任何外部库依赖项。在撰写本文时，System.Diagnostics.Metrics API 是全新的，支持仅限于 dotnet 计数器和 OpenTelemetry.NET。但是，鉴于 OpenTelemetry 项目的活跃性，我们预计对这些 API 的支持将快速增长。

## 第三方 APIs

大多数应用程序性能监控 （APM） 供应商（如 AppDynamics、Application Insights、DataDog、DynaTrace 和 NewRelic）都包含指标 API 作为其检测库的一部分。 [Prometheus](https://github.com/prometheus-net/prometheus-net) 和 [AppMetrics](https://www.app-metrics.io/) 也是流行的 .NET OSS 项目。要了解有关这些项目的更多信息，请查看各个项目网站。

