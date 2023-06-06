# 收集分布式跟踪

这篇文章适用于：✔️ .NET Core 2.1 及以后版本 ✔️ .NET Framework 4.5 及以后版本

探测代码可以创建 [Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) 对象作为分布式跟踪的一部分，但这些对象中的信息需要被收集到集中存储中，以便以后可以审查整个跟踪。在本教程中，你将以不同的方式收集分布式跟踪遥测信息，以便在需要时可用于诊断应用程序问题。如果你需要添加新的探测数据，请参见[探测教程](distributed-tracing-instrumentation-walkthroughs.md)。

## 使用 OpenTelemetry 收集跟踪

[OpenTelemetry](https://opentelemetry.io/) 是一个供应商中立的开源项目，由[云原生计算基金会](https://www.cncf.io/)支持，旨在标准化生成和收集云原生软件的遥测信息。在这些例子中，你将在控制台中收集和显示分布式跟踪信息。要了解如何配置 OpenTelemetry 以将信息发送到其他地方，请参阅 [OpenTelemetry 入门指南](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/getting-started-console/README.md)。

### ASP.NET 例子

**要求**：.NET Core 7.0 SDK 及以上

#### 创建示例应用程序

首先创建一个 webapp 示例程序：

```
dotnet new webapp
```

这个应用程序显示一个网页，但如果我们浏览网页，还没有收集分布式跟踪信息。

#### 收集配置

为了使用 OpenTelemetry，你需要添加对几个 NuGet 包的引用。

```
dotnet add package OpenTelemetry --version 1.4.0-rc1
dotnet add package OpenTelemetry.Exporter.Console --version 1.4.0-rc1
dotnet add package OpenTelemetry.Extensions.Hosting --version 1.4.0-rc1
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.0.0-rc9.10
```

> 注意
>
> 在撰写本文时，1.4.0 Release Candidate 1 的构建是 OpenTelemetry 的最新版本。一旦有了最终版本，就用这个版本代替。

接下来，修改 Program.cs 中的源代码，使其看起来像这样：

```c#
using OpenTelemetry;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddAspNetCoreInstrumentation();
        builder.AddConsoleExporter();
    }).StartWithHost();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
```

运行该应用程序，使用网络浏览器浏览被托管的网页。现在你已经启用了 OpenTelemetry 分布式跟踪，你应该看到浏览器的网页请求信息被打印到控制台：

```
Activity.TraceId:            9c4519ce65a667280daedb3808d376f0
Activity.SpanId:             727c6a8a6cff664f
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: Microsoft.AspNetCore
Activity.DisplayName:        /
Activity.Kind:               Server
Activity.StartTime:          2023-01-08T01:56:05.4529879Z
Activity.Duration:           00:00:00.1048255
Activity.Tags:
    net.host.name: localhost
    net.host.port: 5163
    http.method: GET
    http.scheme: http
    http.target: /
    http.url: http://localhost:5163/
    http.flavor: 1.1
    http.user_agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 Edg/108.0.1462.76
    http.status_code: 200
Resource associated with Activity:
    service.name: unknown_service:demo
```

所有的 OpenTelemetry 配置都发生在以 builder.Services.AddOpenTelemetry() 开头的行中。你用 `.WithTracing(...)` 来启用分布式跟踪。`AddAspNetCoreInstrumentation()` 使 OpenTelemetry 能够收集由 ASP.NET Core Web 服务器产生的所有分布式跟踪活动，`AddConsoleExporter()` 指示 OpenTelemetry 将这些信息发送到控制台。对于一个不那么琐碎的应用程序，你可以添加更多的探测库来收集数据库查询或出站 HTTP 请求的追踪。你也可以用 Jaeger、Zipken 或其他你选择使用的监控服务的导出器来代替控制台导出器。

### 控制台应用程序

**要求：**.NET Core 2.1 SDK 及以上版本

#### 创建示例应用程序

在收集任何分布式跟踪遥测数据之前，你需要制作它。通常这种工具化是在库中，但为了简单起见，你将创建一个小的应用程序，它有一些使用 [StartActivity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource.startactivity) 的探测实例。在这一点上，没有发生任何收集，StartActivity() 没有副作用，返回 null。更多细节请参见[探测教程](distributed-tracing-instrumentation-walkthroughs.md)。

```
dotnet new console
```

针对 .NET 5 及以后版本的应用程序已经包含了必要的分布式跟踪 API。对于以较早的 .NET 版本为目标的应用程序，请添加 [System.Diagnostics.DiagnosticSource NuGet 包](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/)，版本为 5 或更高。

```
dotnet add package System.Diagnostics.DiagnosticSource
```

用这个例子的源代码替换生成的 Program.cs 的内容:

```c#
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sample.DistributedTracing
{
    class Program
    {
        static ActivitySource s_source = new ActivitySource("Sample.DistributedTracing");

        static async Task Main(string[] args)
        {
            await DoSomeWork();
            Console.WriteLine("Example work done");
        }

        static async Task DoSomeWork()
        {
            using (Activity a = s_source.StartActivity("SomeWork"))
            {
                await StepOne();
                await StepTwo();
            }
        }

        static async Task StepOne()
        {
            using (Activity a = s_source.StartActivity("StepOne"))
            {
                await Task.Delay(500);
            }
        }

        static async Task StepTwo()
        {
            using (Activity a = s_source.StartActivity("StepTwo"))
            {
                await Task.Delay(1000);
            }
        }
    }
}
```

运行该应用程序还不能收集任何跟踪数据：

```
> dotnet run
Example work done
```

#### 收集配置

添加 OpenTelemetry.Exporter.Console NuGet 包：

```
dotnet add package OpenTelemetry.Exporter.Console
```

更新 Program.cs，增加 OpenTelemetry 的命名空间：

```c#
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
```

更新 Main() 以创建 OpenTelemetry TracerProvider：

```c#
public static async Task Main()
{
    using var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySample"))
        .AddSource("Sample.DistributedTracing")
        .AddConsoleExporter()
        .Build();

    await DoSomeWork();
    Console.WriteLine("Example work done");
}
```

现在，该应用程序收集分布式跟踪信息并将其显示在控制台：

```
> dotnet run
Activity.Id:          00-7759221f2c5599489d455b84fa0f90f4-6081a9b8041cd840-01
Activity.ParentId:    00-7759221f2c5599489d455b84fa0f90f4-9a52f72c08a9d447-01
Activity.DisplayName: StepOne
Activity.Kind:        Internal
Activity.StartTime:   2021-03-18T10:46:46.8649754Z
Activity.Duration:    00:00:00.5069226
Resource associated with Activity:
    service.name: MySample
    service.instance.id: 909a4624-3b2e-40e4-a86b-4a2c8003219e

Activity.Id:          00-7759221f2c5599489d455b84fa0f90f4-d2b283db91cf774c-01
Activity.ParentId:    00-7759221f2c5599489d455b84fa0f90f4-9a52f72c08a9d447-01
Activity.DisplayName: StepTwo
Activity.Kind:        Internal
Activity.StartTime:   2021-03-18T10:46:47.3838737Z
Activity.Duration:    00:00:01.0142278
Resource associated with Activity:
    service.name: MySample
    service.instance.id: 909a4624-3b2e-40e4-a86b-4a2c8003219e

Activity.Id:          00-7759221f2c5599489d455b84fa0f90f4-9a52f72c08a9d447-01
Activity.DisplayName: SomeWork
Activity.Kind:        Internal
Activity.StartTime:   2021-03-18T10:46:46.8634510Z
Activity.Duration:    00:00:01.5402045
Resource associated with Activity:
    service.name: MySample
    service.instance.id: 909a4624-3b2e-40e4-a86b-4a2c8003219e

Example work done
```

#### Sources

在示例代码中，你调用了 `AddSource("Sample.DistributedTracing")`，这样 OpenTelemetry 就能捕获代码中已经存在的 ActivitySource 所产生的 Activity：

```c#
static ActivitySource s_source = new ActivitySource("Sample.DistributedTracing");
```

通过调用带有源名称的 `AddSource()`，可以捕获来自任何 ActivitySource 的遥测数据。

#### 导出器

控制台导出器对于快速举例或本地开发很有帮助，但在生产部署中，你可能想把跟踪发送到一个集中的商店。OpenTelemetry 支持使用不同导出器的各种目的地。关于配置 OpenTelemetry 的更多信息，请参阅 [OpenTelemetry 入门指南](https://github.com/open-telemetry/opentelemetry-dotnet#getting-started)。

### 自定义逻辑收集跟踪

开发者可以自由地为 Activity 追踪数据创建他们自己的定制收集逻辑。这个例子使用 .NET 提供的 [System.Diagnostics.ActivityListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener) API 收集遥测数据并将其打印到控制台。

**要求：**.NET Core 2.1 SDK 及以上

#### 创建示例应用程序

```
dotnet new console
```

添加对应的 nuget 包：

```
dotnet add package System.Diagnostics.DiagnosticSource
```

替换 Program.cs 中的代码：

```c#
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sample.DistributedTracing
{
    class Program
    {
        static ActivitySource s_source = new ActivitySource("Sample.DistributedTracing");

        static async Task Main(string[] args)
        {
            await DoSomeWork();
            Console.WriteLine("Example work done");
        }

        static async Task DoSomeWork()
        {
            using (Activity a = s_source.StartActivity("SomeWork"))
            {
                await StepOne();
                await StepTwo();
            }
        }

        static async Task StepOne()
        {
            using (Activity a = s_source.StartActivity("StepOne"))
            {
                await Task.Delay(500);
            }
        }

        static async Task StepTwo()
        {
            using (Activity a = s_source.StartActivity("StepTwo"))
            {
                await Task.Delay(1000);
            }
        }
    }
}
```

运行该应用程序还不能收集任何跟踪数据：

```
> dotnet run
Example work done
```

#### 添加代码收集跟踪

更改 Main 中的代码：

```c#
static async Task Main(string[] args)
{
    Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    Activity.ForceDefaultIdFormat = true;

    Console.WriteLine("         {0,-15} {1,-60} {2,-15}", "OperationName", "Id", "Duration");
    ActivitySource.AddActivityListener(new ActivityListener()
    {
        ShouldListenTo = (source) => true,
        Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
        ActivityStarted = activity => Console.WriteLine("Started: {0,-15} {1,-60}", activity.OperationName, activity.Id),
        ActivityStopped = activity => Console.WriteLine("Stopped: {0,-15} {1,-60} {2,-15}", activity.OperationName, activity.Id, activity.Duration)
    });

    await DoSomeWork();
    Console.WriteLine("Example work done");
}
```

日志输出如下：

```
> dotnet run
         OperationName   Id                                                           Duration
Started: SomeWork        00-bdb5faffc2fc1548b6ba49a31c4a0ae0-c447fb302059784f-01
Started: StepOne         00-bdb5faffc2fc1548b6ba49a31c4a0ae0-a7c77a4e9a02dc4a-01
Stopped: StepOne         00-bdb5faffc2fc1548b6ba49a31c4a0ae0-a7c77a4e9a02dc4a-01      00:00:00.5093849
Started: StepTwo         00-bdb5faffc2fc1548b6ba49a31c4a0ae0-9210ad536cae9e4e-01
Stopped: StepTwo         00-bdb5faffc2fc1548b6ba49a31c4a0ae0-9210ad536cae9e4e-01      00:00:01.0111847
Stopped: SomeWork        00-bdb5faffc2fc1548b6ba49a31c4a0ae0-c447fb302059784f-01      00:00:01.5236391
Example work done
```

设置 [DefaultIdFormat](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.defaultidformat#system-diagnostics-activity-defaultidformat) 和 [ForceDefaultIdFormat](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.forcedefaultidformat#system-diagnostics-activity-forcedefaultidformat) 是可选的，但有助于确保样本在不同的 .NET 运行时间版本上产生类似的输出。.NET 5 默认使用 W3C TraceContext ID 格式，但早期的 .NET 版本默认使用 [Hierarchical](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activityidformat#system-diagnostics-activityidformat-hierarchical) ID 格式。欲了解更多信息，请参阅[活动ID](distributed-tracing-concepts.md#activity-ids)。

[System.Diagnostics.ActivityListener](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener) 用于在一个 Activity 的生命周期内接收回调。

- 每个活动都与一个 ActivitySource 相关联，它作为它的命名空间和生产者。这个回调对于流程中的每个 ActivitySource 都会被调用一次。如果你对执行采样感兴趣，或者对由这个源产生的 Activity 的开始/停止事件感兴趣，则返回 true。
- [Sample](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener.sample#system-diagnostics-activitylistener-sample) - 默认情况下，[StartActivity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource.startactivity) 不会创建一个 Activity 对象，除非某个 ActivityListener 指示它应该被采样。返回 [AllDataAndRecorded](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysamplingresult#system-diagnostics-activitysamplingresult-alldataandrecorded) 表示活动应该被创建，[IsAllDataRequested](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.isalldatarequested#system-diagnostics-activity-isalldatarequested) 应该被设置为true，[ActivityTraceFlags](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.activitytraceflags#system-diagnostics-activity-activitytraceflags) 将被设置为 [Recorded](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitytraceflags#system-diagnostics-activitytraceflags-recorded) 标志。IsAllDataRequested 可以被探测代码观察到，作为一个提示，监听器想要确保辅助活动信息，如标签和事件被填充。Recorded 标志在 W3C TraceContext ID 中被编码，是对参与分布式跟踪的其他进程的提示，该跟踪应被采样。
- [ActivityStarted](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener.activitystarted#system-diagnostics-activitylistener-activitystarted) 和 [ActivityStopped](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener.activitystarted#system-diagnostics-activitylistener-activitystarted) 分别在一个活动开始和停止时被调用。这些回调提供了一个机会来记录关于该活动的相关信息或潜在地修改它。当一个活动刚刚开始时，许多数据可能仍然是不完整的，在活动停止前它将被填充。

一旦 ActivityListener 被创建并且回调被填充，调用 [ActivitySource.AddActivityListener(ActivityListener)](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource.addactivitylistener#system-diagnostics-activitysource-addactivitylistener(system-diagnostics-activitylistener)) 将启动回调的调用。调用 [ActivityListener.Dispose()](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource.addactivitylistener#system-diagnostics-activitysource-addactivitylistener(system-diagnostics-activitylistener)) 来停止回调的流动。请注意，在多线程代码中，当 `Dispose()` 运行时，甚至在它返回后不久，就可以收到正在进行的回调通知。