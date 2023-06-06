# 教程：使用 .NET Core EventCounters 测量性能

本文适用于:✔️.NET Core 3.0 SDK 及更高版本。

在本教程中，您将学习如何使用 [EventCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventcounter) 来测量高频率事件的性能。您可以使用各种[官方 .NET Core 包](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters)、第三方提供商发布的计数器，或者创建您自己的监控指标。

在本教程中，您将:

- 实现一个 [EventSource](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters)。
- 使用 [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) 监控计数器。

## 要求

该教程准备环境如下：

- .NET Core 3.1 SDK 及以上版本。
- dotnet-counters 监控事件计数器。
- 一个用于[诊断的样本调试目标](https://learn.microsoft.com/en-us/samples/dotnet/samples/diagnostic-scenarios)应用程序。

## 获取信息来源

示例应用程序将用作监视的基础。[示例 ASP.NET Core 存储库](https://learn.microsoft.com/en-us/samples/dotnet/samples/diagnostic-scenarios)可从示例浏览器中获得。下载 zip 文件，下载后解压缩，然后在您喜欢的 IDE 中打开它。构建并运行应用程序以确保其正常工作，然后停止应用程序。

## 实现一个 EventSource

对于每隔几毫秒发生一次的事件，您希望每个事件的开销较低(少于一毫秒)。否则，对性能的影响将非常大。记录事件意味着要将某些内容写入磁盘。如果磁盘速度不够快，将会丢失事件。您需要一个解决方案，而不是记录事件本身。

在处理大量事件时，了解每个事件的度量也没有用处。大多数时候，您所需要的只是一些统计数据。您可以在进程本身中获得统计数据，然后偶尔写一个事件来报告统计数据，那是 [EventCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventcounter) 会做的。

下面是一个实现 [System.Diagnostics.Tracing.EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource) 的例子。创建新文件，命名为 `MinimalEventCounterSource.cs` 并用如下代码片段：

```c#
using System.Diagnostics.Tracing;

[EventSource(Name = "Sample.EventCounter.Minimal")]
public sealed class MinimalEventCounterSource : EventSource
{
    public static readonly MinimalEventCounterSource Log = new MinimalEventCounterSource();

    private EventCounter _requestCounter;

    private MinimalEventCounterSource() =>
        _requestCounter = new EventCounter("request-time", this)
        {
            DisplayName = "Request Processing Time",
            DisplayUnits = "ms"
        };

    public void Request(string url, long elapsedMilliseconds)
    {
        WriteEvent(1, url, elapsedMilliseconds);
        _requestCounter?.WriteMetric(elapsedMilliseconds);
    }

    protected override void Dispose(bool disposing)
    {
        _requestCounter?.Dispose();
        _requestCounter = null;

        base.Dispose(disposing);
    }
}
```

[EventSource.WriteEvent](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource.writeevent) 行是 EventSource 的一部分，而不是 EventCounter 的一部分，它这么写是为了想您展示它能同 EventCounter 一起记录消息。

### 增加一个行为过滤器

示例源码是 ASP.NET Core 项目。您可以添加全局[行为过滤器](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters#action-filters)，它记录记录总请求时间。创建一个新的文件命名为 `LogRequestTimeFilterAttribute.cs`，并使用如下代码：

```c#
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DiagnosticScenarios
{
    public class LogRequestTimeFilterAttribute : ActionFilterAttribute
    {
        readonly Stopwatch _stopwatch = new Stopwatch();

        public override void OnActionExecuting(ActionExecutingContext context) => _stopwatch.Start();

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch.Stop();

            MinimalEventCounterSource.Log.Request(
                context.HttpContext.Request.GetDisplayUrl(), _stopwatch.ElapsedMilliseconds);
        }
    }
}
```

过滤器用 Stopwatch 来记录一个请求开始的时间，并于请求结束时结束，来捕捉一个请求的运行时间。总时长是被记录到 `MinimalEventCounterSource` 单例上。为了使用这个过滤器，您必须要将它添加到过滤器集合中。在 Startup 文件中，更新 ConfigureServices 方法中的代码：

```c#
public void ConfigureServices(IServiceCollection services) =>
    services.AddControllers(options => options.Filters.Add<LogRequestTimeFilterAttribute>())
            .AddNewtonsoftJson();
```

## 监控事件计数器

在 EventSource 和自定义动作过滤器上实现后，构建并启动应用程序。您将度量记录到 EventCounter，但除非您从中访问统计数据，否则它是没有用的。为了获得统计数据，您需要通过创建一个定时器来启用 EventCounter，该定时器可以根据需要频繁地触发事件，还需要创建一个侦听器来捕获事件。要做到这一点，您可以使用 [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)。

使用 `dotnet-counters ps` 命令显示可监视的 .net 进程列表。

```
dotnet-counters ps
```

使用 `dotnet-counters ps` 命令输出的进程标识符，你可以用以下 `dotnet-counters monitor` 命令开始监控事件计数器：

```cmd
dotnet-counters monitor --process-id 2196 --counters Sample.EventCounter.Minimal,Microsoft.AspNetCore.Hosting[total-requests,requests-per-second],System.Runtime[cpu-usage]
```

当 `dotnet-counters` 监控命令运行时，在浏览器上按住 F5，开始向 https://localhost:5001/api/values 端点发出连续请求。几秒钟后，按 q 停止。

```cmd
Press p to pause, r to resume, q to quit.
    Status: Running

[Microsoft.AspNetCore.Hosting]
    Request Rate / 1 sec                               9
    Total Requests                                   134
[System.Runtime]
    CPU Usage (%)                                     13
[Sample.EventCounter.Minimal]
    Request Processing Time (ms)                      34.5
```

`dotnet-counters monitor` 命令很适合主动监控。然而，您可能想收集这些诊断指标用于后期处理和分析。为此，请使用 `dotnet-counters collect` 命令。`collect` 命令与 `monitor` 命令类似，但接受一些额外的参数。你可以指定所需的输出文件名和格式。对于一个名为 diagnostics.json 的 JSON 文件，使用以下命令：

```cmd
dotnet-counters collect --process-id 2196 --format json -o diagnostics.json --counters Sample.EventCounter.Minimal,Microsoft.AspNetCore.Hosting[total-requests,requests-per-second],System.Runtime[cpu-usage]
```

同样，在命令运行时，按住浏览器上的 F5，开始向 https://localhost:5001/api/values 端点发出连续的请求。几秒钟后，按 q 停止。Json 文件被写入。但是，写入的 JSON 文件没有缩进;为了可读性，这里缩进了。

```json
{
  "TargetProcess": "DiagnosticScenarios",
  "StartTime": "8/5/2020 3:02:45 PM",
  "Events": [
    {
      "timestamp": "2020-08-05 15:02:47Z",
      "provider": "System.Runtime",
      "name": "CPU Usage (%)",
      "counterType": "Metric",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:47Z",
      "provider": "Microsoft.AspNetCore.Hosting",
      "name": "Request Rate / 1 sec",
      "counterType": "Rate",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:47Z",
      "provider": "Microsoft.AspNetCore.Hosting",
      "name": "Total Requests",
      "counterType": "Metric",
      "value": 134
    },
    {
      "timestamp": "2020-08-05 15:02:47Z",
      "provider": "Sample.EventCounter.Minimal",
      "name": "Request Processing Time (ms)",
      "counterType": "Metric",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:47Z",
      "provider": "System.Runtime",
      "name": "CPU Usage (%)",
      "counterType": "Metric",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:48Z",
      "provider": "Microsoft.AspNetCore.Hosting",
      "name": "Request Rate / 1 sec",
      "counterType": "Rate",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:48Z",
      "provider": "Microsoft.AspNetCore.Hosting",
      "name": "Total Requests",
      "counterType": "Metric",
      "value": 134
    },
    {
      "timestamp": "2020-08-05 15:02:48Z",
      "provider": "Sample.EventCounter.Minimal",
      "name": "Request Processing Time (ms)",
      "counterType": "Metric",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:48Z",
      "provider": "System.Runtime",
      "name": "CPU Usage (%)",
      "counterType": "Metric",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:50Z",
      "provider": "Microsoft.AspNetCore.Hosting",
      "name": "Request Rate / 1 sec",
      "counterType": "Rate",
      "value": 0
    },
    {
      "timestamp": "2020-08-05 15:02:50Z",
      "provider": "Microsoft.AspNetCore.Hosting",
      "name": "Total Requests",
      "counterType": "Metric",
      "value": 134
    },
    {
      "timestamp": "2020-08-05 15:02:50Z",
      "provider": "Sample.EventCounter.Minimal",
      "name": "Request Processing Time (ms)",
      "counterType": "Metric",
      "value": 0
    }
  ]
}
```

