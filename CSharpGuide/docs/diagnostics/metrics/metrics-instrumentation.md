# 创建 Metrics

这篇文章适用于： ✔️ .NET Core 3.1 及以上版本 ✔️ .NET Framework 4.6.1 及以上版本

.NET 应用程序可以使用 System.Diagnostics.Metrics APIs 进行检测，以跟踪重要指标。一些指标包含在标准的 .NET 库中，但你可能想添加与你的应用程序和库有关的新的自定义指标。在本教程中，你将添加新的度量，并了解有哪些类型的度量可用。

> 注意
>
> .NET 有一些旧的度量标准 API，即 [EventCounters](event-counters.md) 和 [System.Diagnostics.PerformanceCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter)，这里没有涉及。要了解更多关于这些替代品的信息，请看[比较指标的 APIs](compare-metric-apis.md)。

## 创建自定义指标

要求：.NET Core 3.1 及以上版本

创建一个新的控制台程序，并引入指标 nuget 包 [System.Diagnostics.DiagnosticSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/) 6.0 以上的版本。应用程序默认自动在 .NET6 目标框架上，然后修改 Program.cs 文件代码：

```bash
> dotnet new console
> dotnet add package System.Diagnostics.DiagnosticSource
```

```c#
using System;
using System.Diagnostics.Metrics;
using System.Threading;

class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");

    static void Main(string[] args)
    {
        Console.WriteLine("Press any key to exit");
        while(!Console.KeyAvailable)
        {
            // Pretend our store has a transaction each second that sells 4 hats
            Thread.Sleep(1000);
            s_hatsSold.Add(4);
        }
    }
}
```

[System.Diagnostics.Metrics.Meter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter) 类型是一个库的入口点，用于创建一个命名的仪表组。仪表（Instruments）记录了计算度量衡所需的数字测量。这里我们使用 [CreateCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createcounter) 来创建一个名为 "hats-sold" 的计数器工具。在每个假装交易中，代码调用 [Add](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1.add) 来记录售出的帽子的测量值，在这个例子中是 4 。"hats-sold" 工具隐含地定义了一些可以从这些测量值中计算出来的指标，如售出的帽子总数或售出的帽子/秒。最终是由度量衡收集工具来决定计算哪些度量衡，以及如何进行这些计算，但是每个工具都有一些默认的约定，来表达开发者的意图。对于计数器工具，惯例是收集工具显示总计数和/或计数增加的速度。

`Counter<int>` 和 `CreateCounter<int>(...)` 上的通用参数 `int` 定义了这个计数器必须能够存储到 `Int32.MaxValue` 的值。你可以使用 `byte`、`short`、`int`、`long`、`float`、`double` 或 `decimal` 中的任何一种，这取决于你需要存储的数据大小以及是否需要小数点值。

运行该应用程序，让它暂时运行。我们接下来将查看指标。

```bash
> dotnet run
Press any key to exit
```

### 最佳实践

- 创建一次 Meter，将其存储在一个静态变量或 DI 容器中，并根据需要使用该实例。每个库或库的子组件都可以（而且通常应该）创建自己的 [Meter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter)。如果你预计应用程序开发人员会喜欢能够单独启用和禁用指标组，那么可以考虑创建一个新的 Meter，而不是重复使用现有的。
- 传递给 [Meter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter) 构造函数的名称必须是唯一的，以避免与任何其他 Meter 冲突。使用一个带点的分层名称，其中包含组件名称和可选的子组件名称。如果一个程序集正在为第二个独立的程序集的代码添加仪器，那么名称应该基于定义 Meter 的程序集，而不是其代码正在被仪表化的程序集。
- [Meter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter) 构造函数的版本参数是可选的。我们建议你提供一个版本，以备你发布多个版本的库并对仪器进行修改。
- .NET 并不强制任何度量的具名方案（naming scheme），但根据惯例，所有的 .NET 运行库的度量名称都使用 '-'，如果需要分隔符的话。其他度量衡生态系统鼓励使用 '.' 或 '_' 作为分隔符。微软的建议是在代码中使用 '-'，如果需要的话，让 OpenTelemetry 或 Prometheus 等度量消费者转换为其他的分隔符。
- 创建仪表和记录测量的 API 是线程安全的。在 .NET 库中，大多数实例方法在从多个线程调用同一个对象时需要同步，但在这种情况下不需要。
- 记录测量结果的仪表 API（本例中的 [Add](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1.add)）在没有收集数据的情况下通常在 <10 纳秒内运行，而在由高性能收集库或工具收集测量结果的情况下，则需要几十到几百纳秒。这使得这些 API 在大多数情况下可以自由使用，但要注意那些对性能极为敏感的代码。

## 显示新的指标

有许多选项可以存储和查看指标。本教程使用 [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) 工具，它对特定的分析很有用。你也可以参考[指标收集教程](metrics-collection.md)，了解其他替代方案。如果 dotnet-counters 工具还没有安装，请使用 SDK 来安装它：

```bash
> dotnet tool update -g dotnet-counters
可使用以下命令调用工具: dotnet-counters
已成功安装工具“dotnet-counters”(版本“7.0.421201”)。
```

当示例应用程序仍在运行时，在第二个 shell 中列出运行中的进程，以确定进程ID：

```c#
> dotnet-counters ps
     10180 dotnet     C:\Program Files\dotnet\dotnet.exe
     19964 metric-instr E:\temp\metric-instr\bin\Debug\netcoreapp3.1\metric-instr.exe
```

找到与示例应用程序相匹配的进程名称的 ID，让 dotnet-counters 监控新的计数器：

```c#
> dotnet-counters monitor -p 19964 HatCo.HatStore
Press p to pause, r to resume, q to quit.
    Status: Running

[HatCo.HatStore]
    hats-sold (Count / 1 sec)                          4
```

正如预期的那样，你可以看到 HatCo 商店每秒钟稳定地售出 4 顶帽子。

## 仪表类型

在前面的例子中，我们只演示了一个 Counter<T> 仪表类型，但还有更多的仪表类型可用。仪表在两个方面有所不同：

- **默认指标的计算** - 收集和分析仪器测量值的工具会根据仪器的不同计算不同的默认指标。
- **聚合数据的存储** - 大多数有用的度量需要从许多测量中聚合数据。一种选择是调用者在任意时间提供单独的测量，收集工具管理汇总。另外，调用者可以管理汇总的测量值，并在回调中按需提供。

目前可用的仪器类型：

- **Counter（[CreateCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createcounter)）**— 这种工具跟踪一个随时间增长的数值，调用者用加号报告增量。大多数工具会计算出总数和总数的变化率。对于只显示一件事的工具，建议使用变化率。例如，假设调用者每秒调用一次 Add()，连续的值是 1、2、4、5、4、3。如果收集工具每三秒更新一次，那么三秒后的总数是 1+2+4=7，六秒后的总数是1+2+4+5+4+3=19。变化率是（当前总数 - 上一个总数），所以在三秒时，工具报告 7-0=7，六秒后，它报告 19-7=12。
- **UpDownCounter（[CreateUpDownCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createupdowncounter)）**— 这个工具跟踪一个可能随时间增加或减少的值。调用者使用 Add 来报告增量和减量。例如，假设调用者每秒调用一次 Add()，连续的值为 1，5，-2，3，-1，-3。如果采集工具每三秒更新一次，那么三秒后的总数为 1+5-2=4，六秒后的总数为 1+5-2+3-1-3=3。
- **ObservableCounter（[CreateObservableCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createobservablecounter)）**— 这个工具与 Counter 类似，除了调用者现在负责维护聚合的总数。在创建 ObservableCounter 时，调用者提供了一个回调委托，只要工具需要观察当前的总数，回调就会被调用。例如，如果一个采集工具每三秒更新一次，那么回调函数也将每三秒被调用一次。大多数工具将同时提供总数和总数的变化率。如果只有一个可以显示，建议使用变化率。如果回调函数在初始调用时返回 0，三秒后再次调用时返回 7，六秒后调用时返回 19，那么工具将报告这些值，作为总数，没有变化。对于变化率，该工具将在三秒后显示 7-0=7，六秒后显示 19-7=12。
- **ObservableUpDownCounter（[CreateObservableUpDownCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createobservablecounter)）**— 这个工具与 UpDownCounter 类似，只是现在调用者负责维护汇总的总数。在创建 ObservableUpDownCounter 时，调用者提供了一个回调委托，只要工具需要观察当前的总数，就会调用回调。例如，如果一个集合工具每三秒更新一次，那么回调函数也将每三秒被调用一次。无论回调函数返回什么值，都将作为总数显示在收集工具中而不改变。
- **ObservableGauge（CreateObservableGauge）**— 这个工具允许调用者提供一个回调，在这个回调中，测量值被直接作为度量值传递过来。每次采集工具更新时，回调被调用，无论回调返回的是什么值，都会显示在工具中。
- **Histogram（CreateHistogram）**— 这个工具跟踪测量值的分布。没有一个单一的规范方式来描述一组测量值，但建议工具使用直方图或计算出的百分位数。例如，假设调用者在收集工具的更新时间间隔内调用 [Record](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1.record) 来记录这些测量值：1,5,2,3,10,9,7,4,6,8。采集工具可能会报告说，这些测量值的第 50、90 和 95 个百分位数分别为 5、9 和 9。

## 当选择一个仪表类型时的最佳实践

- 对于计数，或任何其他只随时间增加的值，使用 Counter 或 ObservableCounter。在 Counter 和 ObservableCounter 之间选择，取决于哪个更容易添加到现有的代码中：要么为每个增量操作调用 API，要么通过回调从代码维护的变量中读取当前总数。在极热的代码路径中，性能很重要，而使用 Add 会使每个线程每秒产生一百万次以上的调用，使用 ObservableCounter 可能提供更多的优化机会。
- 对于计时的事情，通常首选 Histogram。通常，了解这些分布的尾部（第 90、95、99 个百分点）而不是平均数或总数是很有用的。
- 其他常见的情况，如缓存命中率或缓存、队列和文件的大小，通常很适合用 UpDownCounter 或 ObservableUpDownCounter。选择它们取决于哪一个更容易添加到现有的代码中：要么为每个增量和减量操作调用 API，要么用回调从代码维护的变量中读取当前值。

> 注意
>
> 如果你使用的是旧版本的 .NET 或不支持 `UpDownCounter` 和 `ObservableUpDownCounter` 的 DiagnosticSource NuGet 包（版本7之前），`ObservableGauge` 通常是一个不错的替代品。

## 不同仪表类型的例子

停止之前启动的示例进程，并将 Program.cs 中的示例代码替换为：

```c#
using System;
using System.Diagnostics.Metrics;
using System.Threading;

class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");
    static Histogram<int> s_orderProcessingTimeMs = s_meter.CreateHistogram<int>("order-processing-time");
    static int s_coatsSold;
    static int s_ordersPending;

    static Random s_rand = new Random();

    static void Main(string[] args)
    {
        s_meter.CreateObservableCounter<int>("coats-sold", () => s_coatsSold);
        s_meter.CreateObservableGauge<int>("orders-pending", () => s_ordersPending);

        Console.WriteLine("Press any key to exit");
        while(!Console.KeyAvailable)
        {
            // 假设每 100 毫秒有一个交易，每个交易出售 4 顶帽子
            Thread.Sleep(100);
            s_hatsSold.Add(4);

            // 假设我们也卖了3件外套。对于 ObservableCounter，我们跟踪变量中的值，并在回调中根据需要报告它
            s_coatsSold += 3;

            // 假设我们有一些随时间变化的订单队列。“订单挂起”度量的回调将按需报告此值
            s_ordersPending = s_rand.Next(0, 20);

            // 最后，我们假设我们测量了完成事务所需的时间(例如，我们可以使用 Stopwatch 计时)。
            s_orderProcessingTimeMs.Record(s_rand.Next(5, 15));
        }
    }
}
```

运行新的进程，像以前一样在第二个 shell 中使用 dotnet-counters 来查看指标：

```c#
> dotnet-counters ps
      2992 dotnet     C:\Program Files\dotnet\dotnet.exe
     20508 metric-instr E:\temp\metric-instr\bin\Debug\netcoreapp3.1\metric-instr.exe
> dotnet-counters monitor -p 20508 HatCo.HatStore
Press p to pause, r to resume, q to quit.
    Status: Running

[HatCo.HatStore]
    coats-sold (Count / 1 sec)                        30
    hats-sold (Count / 1 sec)                         40
    order-processing-time
        Percentile=50                                125
        Percentile=95                                146
        Percentile=99                                146
    orders-pending                                     3
```

这个例子使用了一些随机生成的数字，所以你的数值会有一些变化。你可以看到 `hats-sold`（计数器）和 `coats-sold`（ObservableCounter）都显示为一个比率。而 ObservableGauge, `orders-pending`, 显示为一个绝对值。dotnet-counters 将 Histogram 工具显示为三个百分位数的统计数据（第 50 位、第 95 位和第 99 位），但其他工具可能会以不同的方式总结分布，或提供更多的配置选项。

### 最佳实践

- 直方图往往比其他指标类型在内存中存储更多的数据，然而，确切的内存使用量是由正在使用的收集工具决定的。如果你定义了大量（>100）的直方图指标，你可能需要指导用户不要同时启用它们，或者配置他们的工具，通过降低精度来节省内存。一些采集工具可能对其监测的并发 Histograms 的数量有硬性限制，以防止过度使用内存。

- 所有可观察到的仪表的回调都是依次调用的，所以任何需要长时间的回调都会延迟或阻止所有指标的收集。支持快速读取缓存值，不返回测量值，或者抛出一个异常，而不是执行任何潜在的长时间运行或阻塞的操作。

- [CreateObservableGauge](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createobservablegauge) 和 [CreateObservableCounter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter.createobservablecounter) 函数确实返回了一个仪表对象，但是在大多数情况下，你不需要把它保存在一个变量中，因为不需要与该对象进一步交互。像我们为其他仪表所做的那样将其分配到一个静态变量中是合法的，但容易出错，因为 C# 的静态初始化是懒惰的，而且变量通常不会被引用。下面是这个问题的一个例子：

  ```c#
  using System;
  using System.Diagnostics.Metrics;
  
  class Program
  {
      // 小心！静态初始化器只有在当运行方法中的代码引用静态变量时运行。
      // 这些静态变量永远不会被初始化，因为它们都没有在Main()中被引用。
      //
      static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
      static ObservableCounter<int> s_coatsSold = s_meter.CreateObservableCounter<int>("coats-sold", () => s_rand.Next(1,10));
      static Random s_rand = new Random();
  
      static void Main(string[] args)
      {
          Console.ReadLine();
      }
  }
  ```

## 描述与单位

仪表可以指定可选的描述和单位。这些值对所有的公制计算是不透明的，但可以在采集工具 UI 中显示，以帮助工程师了解如何解释数据。停止你之前启动的例子进程，并将 Program.cs 中的例子代码替换为：

```c#
using System;
using System.Diagnostics.Metrics;
using System.Threading;

class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>(name: "hats-sold",
                                                                unit: "Hats",
                                                                description: "The number of hats sold in our store");

    static void Main(string[] args)
    {
        Console.WriteLine("Press any key to exit");
        while(!Console.KeyAvailable)
        {
            // Pretend our store has a transaction each 100ms that sells 4 hats
            Thread.Sleep(100);
            s_hatsSold.Add(4);
        }
    }
}
```

运行程序，并用 dotnet-counters 展示指标：

```bash
Press p to pause, r to resume, q to quit.
    Status: Running

[HatCo.HatStore]
    hats-sold (Hats / 1 sec)                           40
```

dotnet-counters 目前没有在用户界面中使用描述文本，但当提供描述时，它确实显示了该单位。在这种情况下，你看到 "Hats" 已经取代了以前描述中可见的通用术语 "Counter"。

### 最佳实践

构造函数中指定的单位应该描述适合个别测量的单位。这有时会与最终度量上的单位不同。在这个例子中，每个测量值是一个帽子的数量，所以 "帽子" 是在构造函数中传递的合适单位。采集工具计算了一个速率，并自行推导出计算出的公制的合适单位是 Hats/sec。

## 多维度的指标

测量也可以与称为标签的键值对相关联，允许对数据进行分类以便分析。例如，HatCo 可能不仅想记录售出的帽子数量，而且还想记录它们的尺寸和颜色。在以后分析数据时，HatCo  的工程师可以按尺寸、颜色或两者的任何组合来划分总数。

计数器和柱状图标签可以在接受一个或多个 KeyValuePair 参数的添加和记录的重载中指定。例如：

```c#
s_hatsSold.Add(2,
               new KeyValuePair<string, object>("Color", "Red"),
               new KeyValuePair<string, object>("Size", 12));
```

替换 `Program.cs` 中的代码并重新运行 app 和 dotnet-counters：

```c#
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");

    static void Main(string[] args)
    {
        Console.WriteLine("Press any key to exit");
        while(!Console.KeyAvailable)
        {
            // Pretend our store has a transaction, every 100ms, that sells 2 (size 12) red hats, and 1 (size 19) blue hat.
            Thread.Sleep(100);
            s_hatsSold.Add(2,
                           new KeyValuePair<string,object>("Color", "Red"),
                           new KeyValuePair<string,object>("Size", 12));
            s_hatsSold.Add(1,
                           new KeyValuePair<string,object>("Color", "Blue"),
                           new KeyValuePair<string,object>("Size", 19));
        }
    }
}
```

dotnet-counters 现在会展示一个基本的分类：

```bash
Press p to pause, r to resume, q to quit.
    Status: Running

[HatCo.HatStore]
    hats-sold (Count / 1 sec)
        Color=Blue,Size=19                             9
        Color=Red,Size=12                             18
```

对于 ObservableCounter 和 ObservableGauge，可以在传递给构造函数的回调中提供标记的测量值：

```c#
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");

    static void Main(string[] args)
    {
        s_meter.CreateObservableGauge<int>("orders-pending", GetOrdersPending);
        Console.WriteLine("Press any key to exit");
        Console.ReadLine();
    }

    static IEnumerable<Measurement<int>> GetOrdersPending()
    {
        return new Measurement<int>[]
        {
            // pretend these measurements were read from a real queue somewhere
            new Measurement<int>(6, new KeyValuePair<string,object>("Country", "Italy")),
            new Measurement<int>(3, new KeyValuePair<string,object>("Country", "Spain")),
            new Measurement<int>(1, new KeyValuePair<string,object>("Country", "Mexico")),
        };
    }
}
```

执行 dotnet-counters，结果如下：

```bash
Press p to pause, r to resume, q to quit.
    Status: Running

[HatCo.HatStore]
    orders-pending
        Country=Italy                                  6
        Country=Mexico                                 1
        Country=Spain                                  3
```

### 最佳实践

- 尽管 API 允许任何对象被用作标记值，但采集工具预计会使用数字类型和字符串。其他类型可能被给定的收集工具所支持，也可能不被支持。
- 在实践中，要小心有非常大的或无限制的标签值组合被记录。尽管.NET API 实现可以处理它，但收集工具很可能会为与每个标签组合相关的度量数据分配存储，这可能会变得非常大。例如，如果 HatCo 有 10 种不同的帽子颜色和 25 种帽子尺寸，最多可追踪 10*25=250 个销售总额，这是很好的。然而，如果 HatCo 公司增加了第三个标签，即销售的 CustomerID，而且他们向全球 1 亿客户销售，现在可能会有数十亿不同的标签组合被记录下来。大多数度量衡收集工具要么会放弃数据以保持在技术限制之内，要么会有大量的货币成本来支付数据的存储和处理。每个收集工具的实施将决定其限制，但一个仪表的组合可能少于 1000 个是安全的。任何超过 1000 个组合都需要采集工具应用过滤或设计成高规模的操作。直方图的实现往往比其他指标使用更多的内存，所以安全限制可能会低 10-100 倍。如果你预计会有大量独特的标签组合，那么日志、事务性数据库或大数据处理系统可能是更合适的解决方案，可以在所需规模下运行。
- 对于将有非常多的标签组合的仪表，最好使用较小的存储类型，以帮助减少内存开销。例如，为 `Counter<short>` 存储短，每个标签组合只占用 2 个字节，而为 `Counter<double>` 存储 double 的值，每个标签组合占用 8 字节。
- 我们鼓励采集工具对代码进行优化，在每次调用同一仪器记录测量结果时，以相同的顺序指定相同的标记名称集。对于需要频繁调用 [Add](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1.add) 和 [Record](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1.record) 的高性能代码，最好在每次调用时使用相同的标签名称序列。
- .NET API 进行了优化，对于单独指定三个或更少标签的 [Add](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1.add) 和 [Record](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1.record) 调用，是无分配的。要避免分配更多数量的标签，请使用 [TagList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.taglist)。一般来说，这些调用的性能开销会随着使用更多标签而增加。

> 注意
>
> OpenTelemetry 把标签称为 "属性（attributes）"。这是对同一功能的两种不同称呼。