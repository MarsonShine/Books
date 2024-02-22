# 关键类型

Rx 是一个强大的框架，可以大大简化响应事件的代码。但是，要编写出优秀的响应式代码，您必须了解基本概念。Rx 的基本构件是一个名为 `IObservable<T>` 的接口。理解这个接口及其对应的 `IObserver<T>` 是使用 Rx 取得成功的关键。

上一章的第一个示例是 LINQ 查询表达式：

```c#
var bigTrades =
    from trade in trades
    where trade.Volume > 1_000_000;
```

大多数 .NET 开发人员都熟悉 [LINQ](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)，至少熟悉其多种流行形式之一，如 [LINQ to Objects](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/linq-to-objects) 或 [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/querying/) 查询。大多数 LINQ 实现都允许您查询静态数据。LINQ to Objects 适用于数组或其他集合，而 Entity Framework Core 中的 LINQ 查询则针对数据库中的数据运行，但 Rx 则不同：它提供了定义实时事件流查询的能力，也就是你可能会说的移动中的数据。

如果你不喜欢查询表达式语法，可以直接调用 LINQ 操作符来编写完全相同的代码：

```c#
var bigTrades = trades.Where(trade => trade.Volume > 1_000_000);
```

无论我们使用哪种模式，这都是 LINQ 的一种表达方式，即我们希望 `bigTrades` 只包含交易中 `Volume` 属性大于 100 万的项目。

由于看不到 `trades` 或 `bigTrades` 变量的类型，我们无法准确判断这些示例的作用。根据这些类型的不同，这段代码的含义也会大不相同。如果我们使用的是 LINQ to objects，那么这两个变量都可能是 `IEnumerable<Trade>`。这意味着这些变量都引用了代表集合的对象，我们可以用 `foreach` 循环枚举这些集合的内容。这将代表静态数据，即我们的代码可以直接检查的数据。

但是，让我们通过明确类型来清楚代码的含义：

```c#
IObservable<Trade> bigTrades = trades.Where(trade => trade.Volume > 1_000_000);
```

这就消除了歧义。现在很清楚，我们处理的不是静态数据。我们处理的是 `IObservable<Trade>`。但这到底是什么呢？

## `IObservable<T>`

[IObservable<T> 接口](https://learn.microsoft.com/en-us/dotnet/api/system.iobservable-1)代表了 Rx 的基本抽象：某个类型 `T` 的值序列。从非常抽象的意义上讲，这意味着它与 `IEnumerable<T>` 表示的是同一件事。

区别在于代码如何使用这些值。`IEnumerable<T>` 可以让代码检索值（通常使用 `foreach` 循环），而 `IObservable<T>` 则在值可用时提供值。这种区别有时被称为“推”与“拉”。我们可以通过执行 `foreach` 循环从 `IEnumerable<T>` 中提取值，但 `IObservable<T>` 会将值推送到我们的代码中。

`IObservable<T>` 如何将其值推送到我们的代码中？如果我们想要这些值，我们的代码就必须订阅 `IObservable<T>`，这意味着为它提供一些可以调用的方法。事实上，订阅是 `IObservable<T>` 直接支持的唯一操作。下面是接口的整个定义：

```c#
public interface IObservable<out T>
{
    IDisposable Subscribe(IObserver<T> observer);
}
```

您可以在 [GitHub 上查看 IObservable<T> 的源代码](https://github.com/dotnet/runtime/blob/b4008aefaf8e3b262fbb764070ea1dd1abe7d97c/src/libraries/System.Private.CoreLib/src/System/IObservable.cs)。请注意，它是 .NET 运行库的一部分，而不是 `System.Reactive` NuGet 包的一部分。`IObservable<T>` 代表了一个如此重要的抽象，以至于它被嵌入了 .NET。(所以你可能想知道 System.Reactive NuGet 包是用来做什么的。.NET 运行库只定义了 `IObservable<T>` 和 `IObserver<T>` 接口，而没有定义 LINQ 实现。`System.Reactive` NuGet 包为我们提供了 LINQ 支持，同时还处理了线程问题）。

该接口的唯一方法清楚地说明了我们能用 `IObservable<T>` 做什么：如果我们想接收它提供的事件，我们可以订阅它。(我们也可以取消订阅：`Subscribe` 方法返回一个 `IDisposable`，如果我们调用 `Dispose`，它就会取消我们的订阅）。`Subscribe` 方法要求我们传入一个 `IObserver<T>` 的实现，这一点我们很快就会讲到。

细心的读者会注意到，上一章中的一个示例看起来不应该工作。这段代码创建了一个每秒产生一次事件的 `IObservable<long>`，然后用这段代码订阅了它：

```c#
ticks.Subscribe(
    tick => Console.WriteLine($"Tick {tick}"));
```

这是传递一个委托，而不是 `IObservable<T>.Subscribe` 所需的 `IObserver<T>`。我们很快就会讲到 `IObserver<T>`，但这里发生的一切只是本示例使用了 `System.Reactive` NuGet 软件包中的一个扩展方法：

```c#
// From the System.Reactive library's ObservableExtensions class
public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext)
```

这是一个辅助方法，它将委托封装在 `IObserver<T>` 的实现中，然后将其传递给 `IObservable<T>.Subscribe`。这样做的效果是，我们只需编写一个简单的方法（而不是 `IObserver<T>` 的完整实现），可观察源就会在每次需要提供值时调用我们的回调。使用这种辅助工具比自己实现 Rx 接口更为常见。

## 热冷资源

由于 `IObservable<T>` 在我们订阅之前无法为我们提供值，因此我们订阅的时间可能非常重要。试想一下，一个 `IObservable<Trade>` 描述了某个市场中发生的交易。如果它提供的信息是实时的，那么它就不会告诉你在你订阅之前发生的任何交易。在 Rx 中，这类信息源被描述为热源。

并非所有信息源都是热门的。无论何时调用 `Subscribe`，`IObservable<T>` 都可以向任何订阅者提供完全相同的事件序列。(想象一下，一个 `IObservable<Trade>` 不是报告实时信息，而是根据记录的历史交易数据生成通知）。何时订阅完全无关紧要的源被称为冷源。

以下是一些可表示为热观测对象的源：

- 传感器的测量值
- 来自交易交易所的价格变动
- 立即分发事件的事件源，如 Azure 事件网格
- 鼠标移动
- 定时器事件
- ESB 通道或 UDP 网络数据包等广播

并举例说明一些可能成为冷观测物的来源：

- 集合的内容（如 [IEnumerable<T> 的 ToObservable 扩展方法](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/03_CreatingObservableSequences.md#from-ienumerablet)返回的内容）
- 固定范围的值，如 [Observable.Range](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/03_CreatingObservableSequences.md#observablerange) 产生的事件
- 根据算法生成的事件，如 [Observable.Generate](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/03_CreatingObservableSequences.md#observablegenerate) 生成的事件
- 异步操作的工厂，如 [FromAsync](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/03_CreatingObservableSequences.md#from-task) 返回
- 通过运行循环等传统代码生成的事件；可使用 [Observable.Create](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/03_CreatingObservableSequences.md#observablecreate) 创建此类源
- 提供流式事件，如 Azure Event Hub 或 Kafka（或任何其他流式源，可保留过去的事件，以便从流中的特定时刻开始交付事件；因此不是 Azure Event Grid 风格的事件源）

并非所有的源都是严格意义上的完全热源或冷源。例如，您可以对实时 `IObservable<Trade>` 稍作改动，使信息源始终向新订阅者报告最新交易。订阅者可以立即收到一些信息，并随着新信息的到来不断更新。新订阅者总是会收到（可能是相当旧的）信息，这是一个类似冷源的特性，但冷源的只是第一个事件。一个全新的订阅者仍有可能错过许多早先的订阅者可以获得的信息，这就使得这个信息源更热而不是更冷。

还有一种有趣的特殊情况，即事件源的设计可以让应用程序按顺序接收每一个事件，而且只接收一次。Kafka 或 Azure Event Hub 等事件流系统就具有这种特性--它们会将事件保留一段时间，以确保消费者即使偶尔落后也不会错过。进程的标准输入（stdin）也具有这种特性：如果您运行命令行工具，并在它准备好处理之前开始键入输入，操作系统会将输入保留在缓冲区中，以确保不会丢失任何内容。Windows 对桌面应用程序也有类似的处理方式：每个应用程序线程都有一个消息队列，如果你在它无法响应时点击或键入，输入最终也会被处理。我们可以将这些源视为先冷后热的源。它们就像冷源，我们不会因为开始接收事件花了一些时间而错过任何事情，但一旦我们开始检索数据，一般就不能再倒退回起点了。因此，一旦我们开始运行，它们就更像是热事件。

如果我们想附加多个订阅者，这种先冷后热的源可能会带来问题。如果源在订阅发生后立即开始提供事件，那么对于第一个订阅者来说就没有问题：它将接收到任何等待我们启动而被备份的事件。但如果我们想附加多个订阅者，那就有问题了：在我们设法附加第二个订阅者之前，第一个订阅者可能会收到所有在缓冲区中等待的通知。第二个订阅者就会错过。

在这种情况下，我们确实需要某种方法，在开始工作之前将所有订阅者都设置好。我们希望订阅与启动行为是分开的。默认情况下，订阅源意味着我们希望它启动，但 Rx 定义了一个专门的接口，可以给我们更多的控制权：[IConnectableObservable<T>](https://github.com/dotnet/reactive/blob/f4f727cf413c5ea7a704cdd4cd9b4a3371105fa8/Rx.NET/Source/src/System.Reactive/Subjects/IConnectableObservable.cs)。它派生自 `IObservable<T>`，只增加了一个方法 `Connect`：

```c#
public interface IConnectableObservable<out T> : IObservable<T>
{
    IDisposable Connect();
}
```

这在某些情况下非常有用，因为在这些情况下会有一些进程来获取或生成事件，而我们需要确保在这些进程开始之前做好准备。由于 `IConnectableObservable<T>` 在您调用 `Connect` 之前不会启动，因此它为您提供了一种方法，可以在事件开始流动之前附加您需要的多个订阅者。

源的“温度”并不一定能从其类型中看出来。即使底层源是 `IConnectableObservable<T>`，它也往往隐藏在代码层之后。因此，无论源是热的、冷的还是介于两者之间的，大多数情况下我们看到的只是一个 `IObservable<T>`。由于 `IObservable<T>` 只定义了一个方法，即 `Subscribe`，您可能想知道我们如何用它做任何有趣的事情。这得益于 `System.Reactive` NuGet 库提供的 LINQ 操作符。

## LINQ 操作符和组合

