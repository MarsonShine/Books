# 快速开始

Rx 是一个用于处理事件流的 .NET 库。为什么需要它？

## 为什么需要 Rx？

用户需要实时的信息。如果您正在等待一个包裹的到达，送货车的实时进度报告比令人怀疑的 2 小时送货时间窗口给您更多的自由。金融应用依赖于持续不断的最新数据流。我们希望手机和电脑能为我们提供各种重要通知。而有些应用程序在没有实时信息的情况下根本无法运行。在线协作工具和多人游戏绝对依赖于数据的快速分发和交付。

简而言之，我们的系统需要在有趣的事情发生时做出反应。

实时信息流是计算机系统中的基本普遍元素。尽管如此，在编程语言中，它们往往是二等公民。大多数语言通过类似于数组的方式支持数据序列，这意味着数据已经准备就绪，我们的代码可以随时读取。如果您的应用程序处理事件，数组可能适用于历史数据，但不适合表示在应用程序运行时发生的事件。尽管流式数据在计算中是一个非常古老的概念，但它往往很笨重，其抽象通常是通过与我们编程语言的类型系统集成不佳的应用程序接口来实现的。	

这很糟糕。实时数据对各种应用都至关重要。它应该像列表、字典和其他集合一样易于使用。

.NET 的 Reactive Extensions（Rx.NET 或简称 Rx，作为 System.Reactive NuGet 软件包提供）将实时数据源提升为一等公民。Rx 不需要任何特殊的编程语言支持。它利用 .NET 的类型系统来表示数据流，C#、F# 和 VB.NET 等 .NET 语言都能像使用集合类型一样自然地使用它。

(一个简短的语法提示：虽然 "Reactive Extensions"是复数，但当我们将其简化为 Rx.NET 或 Rx 时，我们将其视为单数名词。这是不一致的，但说 "Rx are...... "听起来很奇怪。）

例如，C# 提供了集成查询功能，我们可以用它来查找列表中符合某些条件的所有条目。如果我们有一些 `List<Trade>` 交易变量，我们可以这样写：

```c#
var bigTrades =
    from trade in trades
    where trade.Volume > 1_000_000;
```

有了 Rx，我们就可以在实时数据中使用完全相同的代码。交易变量可以是 `IObservable<Trade>`，而不是 `List<Trade>`。`IObservable<T>` 是 Rx 的基本抽象。它本质上是 `IEnumerable<T>` 的实时版本。在这种情况下，bigTrades 也将是一个 `IObservable<Trade>`，它是一个实时数据源，能够通知我们交易量超过一百万的所有交易。最重要的是，它可以立即报告每笔此类交易——这就是我们所说的“实时”数据源。

Rx 是一种高效的开发工具。它使开发人员能够使用所有 .NET 开发人员都熟悉的语言功能来处理实时事件流。它采用声明式方法，通常能让我们用比没有 Rx 时更少的代码更优雅地表达复杂的行为。

Rx 基于 LINQ（语言集成查询）。这使我们能够使用上图所示的查询语法（也可以使用一些 .NET 开发人员喜欢的显式函数调用方法）。LINQ 在 .NET 中被广泛用于数据访问（如 Entity Framework Core），也用于处理内存集合（使用 LINQ to Objects），这意味着经验丰富的 .NET 开发人员会对 Rx 感到得心应手。最重要的是，LINQ 是一种高度可组合的设计：您可以任意组合将操作符连接在一起，以简单的方式表达潜在的复杂处理。这种可组合性源于其设计的数学基础，不过，但尽管您可以了解LINQ的这一方面，但这并不是前提条件：对于不对其背后的数学感兴趣的开发人员来说，他们只需欣赏 LINQ 提供者（如Rx）提供的一组构建块，可以以无数种不同的方式组合在一起，而且一切都能正常工作。

LINQ 在处理大量数据方面有着良好的记录。微软已将其广泛应用于一些系统的内部实施，包括支持数千万活跃用户的服务。

## 何时使用 Rx

Rx 是为处理事件序列而设计的，这意味着它在某些情况下比在其他情况下更适合。接下来的章节将介绍其中的一些应用场景，以及与 Rx 不太匹配但仍值得考虑的应用场景。最后，我们将介绍一些可以使用 Rx，但其他方案可能更好的情况。

### 适合使用 Rx

Rx 非常适合表示源于代码之外、应用程序需要响应的事件，例如：

- 集成事件，如来自消息总线的广播，或来自 WebSockets API 的推送事件，或通过 MQTT 或其他低延迟中间件（如 [Azure Event Grid](https://azure.microsoft.com/en-gb/products/event-grid/)、[Azure Event Hubs](https://azure.microsoft.com/en-gb/products/event-hubs/) 和 [Azure Service Bus](https://azure.microsoft.com/en-gb/products/service-bus/)）接收的消息，或非供应商特定表示法（如 [cloudevents](https://cloudevents.io/)）。
- 来自监控设备的遥测数据，如水务基础设施中的流量传感器，或宽带提供商网络设备中的监控和诊断功能。
- 来自移动系统的位置数据，如船舶的 [AIS](https://github.com/ais-dotnet/) 信息或汽车遥测数据。
- 操作系统事件，如文件系统活动或 WMI 事件。
- 道路交通信息，如事故通知或平均速度变化。
- 与[复杂事件处理（CEP）](https://en.wikipedia.org/wiki/Complex_event_processing)引擎集成。
- 用户界面事件，如鼠标移动或按钮点击。

Rx 也是对领域事件进行建模的好方法。这些事件可能是刚才描述的某些事件的结果，但经过处理后，会产生更直接代表应用概念的事件。这些事件可能包括

- 域对象的属性或状态变化，如“订单状态已更新”或“注册已接受”。
- 域对象集合的变化，如“创建了新注册”。

事件还可能代表从接收到的事件（或稍后分析的历史数据）中得出的见解，例如

- 一位宽带用户可能在不知情的情况下参与了 DDoS 攻击
- 两艘远洋轮船进行了通常与非法活动有关的移动模式（例如，在远海长时间并排行驶，足以转移货物或人员）。
- 数控铣床 MFZH12 的 4 号轴轴承出现磨损迹象，磨损率明显高于额定值。
- 如果用户希望准时到达在城市另一端的会议地点，当前的交通状况建议他们在接下来的 10 分钟内出发。

这三组示例展示了应用程序在处理事件时如何逐步提高信息的价值。我们从原始事件开始，然后对其进行增强，以产生特定领域的事件，接着进行分析，以产生应用程序用户真正关心的通知。每个处理阶段都会提高所产生信息的价值。每个阶段通常也会减少信息量。如果我们直接向用户展示第一类的原始事件，他们可能会被大量的信息所淹没，从而无法发现重要事件。但是，如果我们只在处理过程检测到重要事件时才向他们发出通知，这将使他们能够更高效、更准确地工作，因为我们已经大大提高了信噪比。

System.Reactive 库为构建这种增值流程提供了工具，我们可以通过这种流程驯服大量的原始事件源，从而产生高价值、实时、可操作的见解。它提供了一整套操作符，使我们的代码能够声明式地表达这种处理过程，您将在后续章节中看到这一点。

Rx也非常适合用于引入和管理并发性，以实现卸载的目的。也就是说，同时执行一组给定的工作，这样检测到事件的线程不必同时处理该事件。其中一个非常流行的用途是保持响应式的用户界面（UI）。（在.NET中，UI 事件处理已经成为 Rx 的非常流行的用途，而 RxJS 也是 Rx.NET 的一个衍生，因此很容易认为这就是它的用途。但是，它在那里的成功不应使我们对其更广泛的适用性视而不见。）

如果现有的 `IEnumerable<T>` 试图为实时事件建模，则应考虑使用 Rx。虽然 `IEnumerable<T>` 可以对运动中的数据建模（通过使用像 `yield return` 这样的懒惰评估），但存在一个问题。如果消耗集合的代码已到达需要下一个项目的位置（例如，foreach 循环刚刚完成一次迭代），但还没有项目可用，那么 `IEnumerable<T>` 的实现将别无选择，只能在 MoveNext 中阻塞调用线程，直到数据可用为止，这可能会在某些应用程序中造成可扩展性问题。即使在可以接受线程阻塞的情况下（或者如果您使用较新的 `IAsyncEnumerable<T>`，它可以利用 C# 的 await foreach 功能避免在这些情况下阻塞线程），`IEnumerable<T>` 和 `IAsyncEnumerable<T>` 也是表示实时信息源的具有欺骗性的类型。这些接口代表了一种“拉取”编程模型：代码会询问序列中的下一个项目。对于按照自己的计划自然生成信息的信息源建模来说，Rx 是更自然的选择。

### 可能适合 Rx

Rx 可用来表示异步操作。.NET 的 `Task` 或 `Task<T>` 有效地表示了单个事件，而 `IObservable<T>` 可以被认为是对事件序列的概括。(例如，`Task<int>` 和 `IObservable<int>` 之间的关系类似于 int 和 IEnumerable<int> 之间的关系）。

这意味着有些情况可以使用任务和 async 关键字或通过 Rx 来处理。如果在处理过程中需要同时处理多个值和单个值，Rx 可以同时处理这两种情况；而任务则不能很好地处理多个项目。您可以使用 `Task<IEnumerable<int>>`，它可以让您等待一个集合，如果集合中的所有项目都可以在一个步骤中收集，那就没问题了。这样做的限制是，一旦任务产生了 `IEnumerable<int>` 结果，您的 await 就完成了，您又回到了对该 `IEnumerable<int>` 的非异步迭代。如果数据不能在单步中获取--也许 `IEnumerable<int>` 代表的是来自 API 的数据，而在 API 中，结果是以每次 100 个条目为单位分批获取的--那么 MoveNext 在每次需要等待时都会阻塞线程。

可以说，在 Rx 存在的头 5 年中，它是表示不一定能立即获得所有项的集合的最佳方法。然而，.NET Core 3.0 和 C# 8 中引入的 [IAsyncEnumerable<T>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) 提供了一种处理序列的方法，同时还保留了 async/await 的世界（[Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) [NuGet 包](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/)使其可用于 .NET Framework 和 .NET Standard 2.0）。因此，现在使用 Rx 的选择可以归结为“拉模式”（以 `foreach` 或 `await foreach` 为例）还是“推”模式（当项目可用时，代码提供回调供事件源调用）更适合建模的概念。

自 Rx 首次出现以来，.NET 增加的另一个相关功能是[通道](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)。通道允许源产生对象，消费者处理对象，因此与 Rx 有明显的表面相似性。不过，Rx 的一个显著特点是它支持使用大量操作符进行组合，而通道中并没有直接的等价物。另一方面，通道提供了更多的选择，以适应生产和消费率的变化。

前面我提到了卸载：使用 Rx 将工作推送给其他线程。虽然这种技术能让 Rx 引入和管理并发性，以达到扩展或执行并行计算的目的，但其他专用框架（如 [TPL（任务并行库）Dataflow](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library) 或 [PLINQ](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/introduction-to-plinq)）更适合执行并行计算密集型工作。不过，TPL Dataflow 通过其 [AsObserver](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.dataflowblock.asobserver) 和 [AsObservable](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.dataflowblock.asobservable) 扩展方法与 Rx 进行了一些集成。因此，通常使用 Rx 将 TPL Dataflow 与应用程序的其他部分集成。

### 不适合 Rx

Rx 的 `IObservable<T>` 并不能替代 `IEnumerable<T>` 或 `IAsyncEnumerable<T>`。如果把天生基于拉的东西强加给基于推的东西，那将是一个错误。

此外，在某些情况下，Rx 编程模型的简单性可能会对您不利。例如，某些消息队列技术（如 MSMQ）顾名思义就是顺序技术，因此看起来很适合 Rx。然而，它们通常是为了支持事务处理而被选用的。Rx 没有任何直接的方法来显示事务语义，因此在需要事务语义的场景中，您最好直接使用相关技术的 API。(不过，[Reaqtor](https://reaqtive.net/) 为 Rx 增加了耐久性和持久性，因此你或许可以利用它将这类队列系统与 Rx 集成）。

选择最合适的工具，你的代码应该会更容易维护，性能可能会更好，也可能会得到更好的支持。

## Rx 实践

您可以快速运行一个简单的 Rx 示例。如果安装了 .NET SDK，就可以在命令行下运行以下程序：

```
mkdir TryRx
cd TryRx
dotnet new console
dotnet add package System.Reactive
```

或者，如果安装了 Visual Studio，创建一个新的 .NET Console 项目，然后使用 NuGet 软件包管理器添加 `System.Reactive` 引用。

这段代码创建了一个可观测源（`ticks`），每秒产生一次事件。代码还为该源传递了一个处理程序，该处理程序会为每个事件向控制台写入一条消息：

```c#
using System.Reactive.Linq;

IObservable<long> ticks = Observable.Timer(
    dueTime: TimeSpan.Zero,
    period: TimeSpan.FromSeconds(1));

ticks.Subscribe(
    tick => Console.WriteLine($"Tick {tick}"));

Console.ReadLine();
```

·如果这看起来并不令人兴奋，那是因为这只是一个尽可能基本的示例，而 Rx 的核心是一个非常简单的编程模型。强大的功能来自于组合——我们可以使用 `System.Reactive` 库中的构建模块来描述将原始、低级事件转化为高价值见解的处理过程。但要做到这一点，我们必须首先了解 [Rx 的关键类型，IObservable<T> 和 `IObserver<T>`](key-types.md)。

