# 过滤

Rx 为我们提供了各种工具，可用于处理潜在的大量事件，并从中获得更高层次的见解。这通常需要减少事件量。如果数量较少的事件流中的单个事件平均信息量更大，那么少量事件可能比大量事件更有用。实现这一目标的最简单机制就是过滤掉我们不想要的事件。Rx 定义了几种操作符可以实现这一目的。

在继续介绍新的操作符之前，我们将快速定义一个扩展方法，以帮助阐明几个示例。这个 `Dump` 扩展方法可以订阅任何 `IObservable<T>`，其处理程序可以为源产生的每个通知显示消息。该方法接收一个 `name` 参数，该参数将作为每条消息的一部分显示出来，从而使我们能够在订阅多个源的示例中查看事件的来源。

```c#
public static class SampleExtensions
{
    public static void Dump<T>(this IObservable<T> source, string name)
    {
        source.Subscribe(
            value =>Console.WriteLine($"{name}-->{value}"), 
            ex => Console.WriteLine($"{name} failed-->{ex.Message}"),
            () => Console.WriteLine($"{name} completed"));
    }
}
```

## Where

对序列应用过滤器是一种极为常见的操作，而 LINQ 中最简单的过滤器就是 `Where` 运算符。与 LINQ 一样，Rx 以扩展方法的形式提供操作符。如果您已经熟悉了 LINQ，那么 Rx 的 `Where` 方法的签名就不会让您感到意外了：

```c#
IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
```

请注意，`source` 参数的元素类型与返回类型相同。这是因为 `Where` 不会修改元素。它可以过滤掉一些元素，但那些没有删除的元素会原封不动地通过。

本示例使用 `Where` 过滤掉 `Range` 序列中的所有奇数值，这意味着只有偶数会出现。

```c#
IObservable<int> xs = Observable.Range(0, 10); // The numbers 0-9
IObservable<int> evenNumbers = xs.Where(i => i % 2 == 0);

evenNumbers.Dump("Where");
```

输出：

```
Where-->0
Where-->2
Where-->4
Where-->6
Where-->8
Where completed
```

`Where` 操作符是所有 LINQ 提供程序中的众多标准 LINQ 操作符之一。例如，LINQ to Objects 的 `IEnumerable<T>` 实现就提供了一个等效方法。在大多数情况下，Rx 操作符的行为与它们在 `IEnumerable<T>` 实现中的行为一样，不过也有一些例外，我们稍后会看到。我们将讨论每种实现，并解释其中的任何差异。通过实现这些常用操作符，Rx 还可以通过 C# 查询表达式语法免费获得语言支持。例如，我们可以这样编写第一条语句，这样编译出来的代码实际上是完全一样的：

```c#
IObservable<int> evenNumbers =
    from i in xs
    where i % 2 == 0
    select i;
```

本书中的示例大多使用扩展方法，而不是查询表达式，部分原因是 Rx 实现的某些操作符没有相应的查询语法，另一部分原因是方法调用方法有时更容易看清发生了什么。

和大多数 Rx 操作符一样，`Where` 操作符不会立即订阅其源。（Rx的 LINQ 操作符与 LINQ to Objects 中的操作符非常类似：`Where` 的 `IEnumerable<T>` 版本返回时，并不尝试对其源进行枚举。只有当某个东西尝试对返回的 `IEnumerable<T>` 进行枚举时，`Where` 才会开始枚举源。）只有当某个东西对 `Where` 返回的 `IObservable<T>` 调用 `Subscribe` 时，它才会调用其源的 `Subscribe` 方法。而且，对于每次这样的 `Subscribe` 调用，它都会执行一次。更一般地说，当你将 LINQ 操作符链接在一起时，对结果 `IObservable<T>` 的每次 `Subscribe` 调用都会导致一系列级联的 `Subscribe` 调用一直传递到链的最底部。

这种级联 `Subscribe` 的一个副作用是，`Where`（与大多数其他 LINQ 操作符一样）本质上既不是热操作符，也不是冷操作符：因为它只是订阅其源，所以如果源是热的，它就是热操作符；如果源是冷的，它就是冷操作符。

`Where` 操作符会传递所有使其 `predicate` 回调返回 `true` 的元素。要更准确地说，在订阅 `Where` 操作符时，它会创建自己的 `IObserver<T>`，并将其作为参数传递给 `source.Subscribe` 方法，该观察者会在每次调用 `OnNext` 时调用 `predicate`。如果 `predicate` 返回 `true`，那么只有在 `Where` 创建的观察者调用传递给 `Where` 的观察者的 `OnNext` 方法时，才会发生这种情况。

`Where` 始终会将对 `OnComplete` 或 `OnError` 的最终调用传递出去。这意味着，如果您写下以下代码：

```c#
IObservable<int> dropEverything = xs.Where(_ => false);
```

那么，尽管这会过滤掉所有元素（因为谓词会忽略其参数并总是返回 `false`，指令 `Where` 删除所有内容），但这不会过滤掉错误或完成。

事实上，如果你想要的是一个能丢弃所有元素的运算符，只告诉你源代码是完成了还是失败了，那么还有一种更简单的方法。

## IgnoreElements

通过 `IgnoreElements` 扩展方法，可以只接收 `OnCompleted` 或 `OnError` 通知。它等同于使用 `Where` 运算符和一个总是返回 `false` 的谓词，如以下示例所示：

```c#
IObservable<int> xs = Observable.Range(1, 3);
IObservable<int> dropEverything = xs.IgnoreElements();

xs.Dump("Unfiltered");
dropEverything.Dump("IgnoreElements");
```

如输出所示，`xs` 源生成数字 1 至 3，然后完成，但如果我们通过 `IgnoreElements` 运行该源，我们看到的只是 `OnCompleted`。

```
Unfiltered-->1
Unfiltered-->2
Unfiltered-->3
Unfiltered completed
IgnoreElements completed
```

## OfType

一些可观测序列会产生各种类型的项目。例如，考虑一个想要跟踪船只移动情况的应用程序。使用 AIS 接收器就可以做到这一点。AIS 是自动识别系统（Automatic Identification System）的缩写，大多数远洋船只都使用它来报告自己的位置、航向、速度和其他信息。AIS 信息种类繁多。有些会报告船只的位置和航速，但船名会在另一种信息中报告。(这是因为大多数船只移动的频率要高于更改船名的频率，因此它们会以相当不同的间隔广播这两类信息）。

想象一下这在 Rx 中会是什么样子。其实你不必想象。开源的 [Ais.Net 项目](https://github.com/ais-dotnet)包含一个 [ReceiverHost 类](https://github.com/ais-dotnet/Ais.Net.Receiver/blob/15de7b2908c3bd67cf421545578cfca59b24ed2c/Solutions/Ais.Net.Receiver/Ais/Net/Receiver/Receiver/ReceiverHost.cs)，可通过 Rx 获取 AIS 信息。`ReceiverHost` 定义了一个 `IObservable<IAisMessage>` 类型的 `Messages` 属性。由于 AIS 定义了许多消息类型，因此这个可观察源可以产生许多不同类型的对象。它所发出的所有信息都将实现 `IAisMessage` 接口，该接口会报告船只的唯一标识符，除此之外就没有其他信息了。但 [Ais.Net.Models 库](https://www.nuget.org/packages/Ais.Net.Models/)定义了许多其他接口，包括 [IVesselNavigation](https://github.com/ais-dotnet/Ais.Net.Receiver/blob/15de7b2908c3bd67cf421545578cfca59b24ed2c/Solutions/Ais.Net.Models/Ais/Net/Models/Abstractions/IVesselNavigation.cs)（报告位置、速度和航向）和 [IVesselName](https://github.com/ais-dotnet/Ais.Net.Receiver/blob/15de7b2908c3bd67cf421545578cfca59b24ed2c/Solutions/Ais.Net.Models/Ais/Net/Models/Abstractions/IVesselName.cs)（告诉你船只的名称）。

假设您只对水中船只的位置感兴趣，而不关心船只的名称。您希望看到所有实现了 `IVesselNavigation` 接口的信息，而忽略所有未实现该接口的信息。您可以尝试使用 `Where` 运算符来实现这一目的：

```c#
// Won't compile!
IObservable<IVesselNavigation> vesselMovements = 
   receiverHost.Messages.Where(m => m is IVesselNavigation);
```

但是，这将无法编译。你会得到以下错误信息：

```
Cannot implicitly convert type 
'System.IObservable<Ais.Net.Models.Abstractions.IAisMessage>' 
to 
'System.IObservable<Ais.Net.Models.Abstractions.IVesselNavigation>'
```

请记住，`Where` 的返回类型始终与输入类型相同。由于 `receiverHost.Messages` 的类型是 `IObservable<IAisMessage>`，这也是 `Where` 将返回的类型。我们的谓词恰好可以确保只有那些实现了 `IVesselNavigation` 的消息才能通过，但 C# 编译器无法理解谓词和输出之间的关系。(它所知道的是，`Where` 可能会做完全相反的事情，只包含谓词返回 false 的元素。事实上，编译器无法猜测 `Where` 如何使用其谓词）。

幸运的是，Rx 为这种情况提供了专门的操作符。`OfType` 运算符只过滤特定类型的项目。项目必须是指定的确切类型，或者是从该类型继承而来，如果是接口，则必须实现该接口。这样，我们就可以修正上一个示例：

```c#
IObservable<IVesselNavigation> vesselMovements = 
   receiverHost.Messages.OfType<IVesselNavigation>();
```

## 位置过滤

有时，我们并不关心元素是什么，而只关心它在序列中的位置。Rx 定义了一些操作符，可以帮助我们解决这个问题。

### FirstAsync 和 FirstOrDefaultAsync

LINQ 提供者通常会实现一个 `First` 运算符，提供序列的第一个元素。Rx 也不例外，但 Rx 的性质意味着我们通常需要以略微不同的方式来实现这一功能。对于静态数据提供程序（如 LINQ to Objects 或 Entity Framework Core）来说，源元素已经存在，因此检索第一项只需读取即可。但对于 Rx，源元素会根据自己的选择生成数据，因此无法知道第一项数据何时可用。

因此，对于 Rx，我们通常使用 `FirstAsync`。这将返回一个 `IObservable<T>`，它将产生源序列中出现的第一个值，然后完成处理。(Rx 也提供了一种更传统的 `First` 方法，但可能会有问题。详情请参阅后面的 [First/Last/Single[OrDefault]的阻塞版本部分](First/Last/Single[OrDefault]的阻塞版本 "部分)）。

例如，这段代码使用前面介绍的 AIS.NET 源代码来报告某艘船（恰好命名为 HMS Example）第一次报告正在移动的情况：

```c#
uint exampleMmsi = 235009890;
IObservable<IVesselNavigation> moving = 
   receiverHost.Messages
    .Where(v => v.Mmsi == exampleMmsi)
    .OfType<IVesselNavigation>()
    .Where(vn => vn.SpeedOverGround > 1f)
    .FirstAsync();
```

如果 `FirstAsync` 的输入为空怎么办？如果 `FirstAsync` 完成后没有生成任何项目，则会调用其订阅者的 `OnError`，并传递一个 `InvalidOperationException`，其中包含一条错误信息，报告序列不包含任何元素。如果我们使用谓词的表单（如第二个示例），但没有出现与谓词匹配的元素，情况也是如此。这与 LINQ to Objects `First` 操作符是一致的。(请注意，我们并不希望在刚才的示例中出现这种情况，因为只要应用程序还在运行，源代码就会继续报告 AIS 消息，这意味着它没有理由完成）。

有时，我们可能希望容忍这种事件的缺失。大多数 LINQ 提供者不仅提供 `First`，还提供 `FirstOrDefault`。我们可以通过修改前面的示例来使用它。该示例使用 [`TakeUntil` 操作符](#SkipUntil 和 TakeUntil)引入了一个截止时间：该示例准备等待 5 分钟，但 5 分钟后就放弃了。(因此，虽然 AIS 接收机可以无休止地发送信息，但本例决定不会永远等待下去）。由于这意味着我们可能会在没有看到船移动的情况下完成操作，因此我们用 `FirstOrDefaultAsync` 代替了 `FirstAsync`：

```c#
IObservable<IVesselNavigation?> moving = 
   receiverHost.Messages
    .Where(v => v.Mmsi == exampleMmsi)
    .OfType<IVesselNavigation>()
    .TakeUntil(DateTimeOffset.Now.AddMinutes(5))
    .FirstOrDefaultAsync(vn => vn.SpeedOverGround > 1f);
```

如果 5 分钟后，我们仍未收到船只以 1 节或更快速度行驶的信息，`TakeUntil` 将取消订阅其上游源，并在 `FirstOrDefaultAsync` 提供的观察者上调用 `OnCompleted`。`FirstAsync` 会将此视为错误，而 `FirstOrDefaultAsync` 则会生成其元素类型的默认值（本例中为 `IVesselNavigation`；接口类型的默认值为空），将其传递给订阅者的 `OnNext`，然后调用 `OnCompleted`。

简而言之，这个移动观察对象将始终产生一个项目。要么产生一个 `IVesselNavigation` 表示船已经移动，要么产生一个 `null` 表示在代码允许的 5 分钟内没有移动。

生成空值可能是一种表示某件事情没有发生的好方法，但也有一些略显笨拙的地方：任何使用该移动源的程序现在都必须弄清楚，通知是表示感兴趣的事件，还是表示没有发生任何此类事件。如果这对你的代码很方便，那很好，但 Rx 提供了一种更直接的方法来表示没有事件：空序列。

你可以想象一个 `FirstOrEmpty` 以这种方式工作。对于返回实际值的 LINQ 提供者来说，这种方法并不合理。例如，LINQ to Objects 的 `First` 返回 `T`，而不是 `IEnumerable<T>`，因此它无法返回空序列。但是，由于 Rx 提供了返回 `IObservable<T>` 的类似于 `First` 的操作符，因此从技术上讲，可以使用一个操作符来返回第一个项目或空任何项目。Rx 中并没有内置这样的操作符，但我们可以通过使用更通用的操作符 `Take` 来获得完全相同的效果。

### Take

`Take` 是一个标准的 LINQ 操作符，它从序列中提取前几项，然后丢弃其余项。

从某种意义上说，`Take` 是 `First` 的泛化：`Take(1)`只返回第一个项，因此可以把 LINQ 的 `First` 视为 `Take` 的特例。严格来说，这并不正确，因为这些操作符对缺失元素的反应不同：正如我们刚才看到的，`First`（以及 Rx 的 `FirstAsync`）坚持至少接收一个元素，如果提供的是空序列，就会产生 `InvalidOperationException` 异常。即使是存在性更宽松的 `FirstOrDefault`，也仍然坚持要产生一些元素。`Take` 的工作方式略有不同。

如果 `Take` 的输入在产生指定数量的元素之前就完成了，`Take` 不会抱怨，只会转发源提供的任何内容。如果源除了调用 `OnCompleted` 之外什么也没做，那么 `Take` 只会在它的观察者上调用 `OnCompleted`。如果我们使用了 `Take(5)`，但源产生了三个条目，然后完成了，那么 `Take(5)` 将把这三个条目转发给它的订阅者，然后完成。这意味着我们可以使用 `Take` 来实现上一节讨论的假设的 `FirstOrEmpty`：

```c#
public static IObservable<T> FirstOrEmpty<T>(this IObservable<T> src) => src.Take(1);
```

现在是提醒大家注意的好时机，大多数 Rx 运算符（以及本章中的所有运算符）本质上都不是冷或热的。它们服从源。如果有热源，`source.Take(1)` 也是热源。我在这些示例中使用的 AIS.NET `receiverHost.Messages` 源是热源（因为它报告来自船只的实时信息广播），因此从它派生出来的可观测序列也是热源。为什么现在是讨论这个问题的好时机呢？因为这让我可以说下面这个绝对可怕的双关语：

```c#
IObservable<IAisMessage> hotTake = receiverHost.Messages.Take(1);
```

`FirstAsync` 和 `Take` 操作符从序列的起点开始工作。如果我们只对尾部感兴趣呢？

### LastAsync, LastOrDefaultAsync 和 PublishLast

LINQ 提供者通常会提供 `Last` 和 `LastOrDefault`。它们的功能与 `First` 或 `FirstOrDefault` 几乎完全相同，只是顾名思义，它们返回的是最后一个元素，而不是第一个元素。与 `First` 一样，Rx 的特性意味着，与处理静态数据的 LINQ 提供程序不同，最终元素可能并不在那里等待获取。因此，就像 Rx 提供 `FirstAsync` 和 `FirstOrDefault` 一样，它也提供 `LastAsync` 和 `LastOrDefaultAsync`。(它也提供 `Last`，但正如 [First/Last/Single[OrDefault]的阻塞版本](#阻塞版本的 First/Last/Single[OrDefault])部分所讨论的，这可能会有问题）。

还有 [PublishLast](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/15_PublishingOperators.md#publishlast)。它的语义与 `LastAsync` 类似，但处理多个订阅的方式不同。每次订阅 `LastAsync` 返回的 `IObservable<T>` 时，它都会订阅底层源，但 `PublishLast` 只对底层源进行一次订阅调用。(为了准确控制何时发生这种情况，`PublishLast` 返回一个 `IConnectableObservable<T>`。正如[第2章“热源”和“冷源”一节](key-types.md#热冷源)所述，它提供了一个 `Connect` 方法，当你调用该方法时，`PublishLast` 返回的可连接观察对象就会订阅其底层源）。一旦单个订阅收到源的 `OnComplete` 通知，它就会将最终值发送给所有订阅者。(它还会记住最终值，因此如果有新的观察者在最终值产生后订阅，他们在订阅时将立即收到该值）。最终值产生后，会立即发出 `OnCompleted` 通知。这是基于[组播](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/15_PublishingOperators.md#multicast)操作符的操作符系列之一，后面章节将详细介绍。

`LastAsync` 和 `LastOrDefaultAsync` 之间的区别与 `FirstAsync` 和 `FirstOrDefaultAsync` 相同。如果源代码完成时什么也没生成，`LastAsync` 就会报错，而 `LastOrDefaultAsync` 则会为其元素类型输出默认值，然后完成。`PublishLast` 处理空源的方式也不同：如果源完成时没有产生任何元素，`PublishLast` 返回的可观察对象也会这样做：在这种情况下，它既不会产生错误，也不会产生默认值。

报告序列的最后一个元素会带来 `First` 所没有的挑战。要知道何时从源代码中接收到第一个元素非常容易：如果源代码产生了一个元素，而它之前没有产生过元素，那么这就是第一个元素。这意味着 `FirstAsync` 等操作符可以立即报告第一个元素。但 `LastAsync` 和 `LastOrDefaultAsync` 就没有这种特权了。

如果你从一个源接收到一个元素，你怎么知道它是最后一个元素呢？一般来说，你无法在收到元素的瞬间知道这一点。只有当源继续调用你的 `OnCompleted` 方法时，你才会知道自己收到的是最后一个元素。但这并不一定会立即发生。前面的示例使用 `TakeUntil(DateTimeOffset.Now.AddMinutes(5))` 在 5 分钟后结束序列，如果您这样做，从最后一个元素发出到 `TakeUntil` 关闭完全有可能间隔相当长的时间。在 AIS 情景中，小船可能每隔几分钟才发出一次信息，因此我们很有可能在 `TakeUntil` 转发信息后，几分钟后发现已经到了截止时间，却没有任何信息进来。在最后的 `OnNext` 和 `OnComplete` 之间可能已经过去了几分钟。

正因为如此。`LastAsync` 和 `LastOrDefaultAsync` 在其源代码完成之前不会发出任何信息。这将产生一个重要的后果：从 `LastAsync` 收到源的最后一个元素到它将该元素转发给订阅者之间可能会有很大的延迟。

### TakeLast

前面我们看到 Rx 实现了标准的 `Take` 操作符，它可以从序列的起点转发指定数量的元素，然后停止。而 `TakeLast` 则是转发序列末尾的元素。例如，`TakeLast(3)` 要求转发源序列的最后 3 个元素。与 `Take` 一样，`TakeLast` 也能容忍产生过少元素的源。如果源产生的条目少于 3 个，`TaskLast(3)` 将直接转发整个序列。

`TakeLast` 面临着与 `Last` 相同的挑战：它不知道自己何时接近序列的终点。因此，它必须保留最近看到的值的副本。它需要内存来保存您指定的多个值。如果写入 `TakeLast(1_000_000)`，它需要分配一个足够大的缓冲区来存储 1,000,000 个值。它不知道接收到的第一个元素是否是最后一百万个元素中的一个。在数据源完成之前，或者在数据源发出超过 1,000,000 个项目之前，它都无法知道。当源代码最终完成时，`TakeLast` 将知道最后一百万个元素是什么，并需要将所有元素一个接一个地传递给订阅者的 `OnNext` 方法。

### Skip 和 SkipLast

如果我们想要与 `Take` 或 `TakeLast` 运算符完全相反的结果呢？也许我不想从一个源中获取前 5 个项，而是想丢弃前 5 个项？也许我有一些 `IObservable<float>` 从传感器中获取读数，而我发现传感器在前几个读数中会产生垃圾值，因此我想忽略这些读数，等它稳定下来后再开始监听。我可以用 `Skip(5)` 来实现这个目标。

`SkipLast` 在序列末尾做了同样的事情：在尾端省略指定数量的元素。与我们刚才看到的其他一些运算符一样，`SkipLast` 必须解决一个问题，即它无法判断何时接近序列的末尾。它只能在源代码发出所有元素并发出 `OnComplete` 之后，才能发现最后（例如）4 个元素。因此，`SkipLast` 会带来延迟。如果使用 `SkipLast(4)`，在源产生第 5 个元素之前，它不会将第一个元素转发出去。因此，在开始工作之前，它不需要等待 `OnCompleted` 或 `OnError`，它只需要等待确定某个元素不是我们要丢弃的元素。

过滤的其他关键方法非常相似，我认为我们可以将它们视为一个大组。首先，我们来看看 `Skip` 和 `Take`。这两种方法的作用与 `IEnumerable<T>` 实现的作用相同。它们是最简单的 `Skip/Take` 方法，也可能是最常用的 `Skip/Take` 方法。这两种方法都只有一个参数，即要跳过或取走的值的个数。

### SingleAsync 和 SingleOrDefaultAsync

LINQ 操作符通常会提供一个 `Single` 操作符，用于在源应仅提供一个项目时使用，如果源包含更多项目或为空，则会出错。Rx 在这方面的考虑与适用于 `First` 和 `Last` 的考虑相同，因此您可能会毫不惊讶地发现，Rx 提供了一个 `SingleAsync` 方法，该方法返回一个 `IObservable<T>`，该 `IObservable<T>` 要么调用其观察者的 `OnNext` 一次，要么调用其 `OnError` 以表明源报告了一个错误，或者源没有生成一个准确的项目。

与 `FirstAsync` 和 `LastAsync` 一样，使用 `SingleAsync` 时，如果源是空的，就会出错，但如果源包含多个项目，也会出错。`SingleOrDefault` 与 `First/Async` 和 `LastAsync` 一样，可以容忍输入序列为空，并在这种情况下生成一个具有元素类型默认值的单一元素。

`Single` 和 `SingleAsync` 与 `Last` 和 `LastAsync` 有一个共同的特点，那就是当它们从源接收到一个项目时，最初并不知道它是否应该成为输出。这看起来可能很奇怪：既然 `Single` 只要求源流提供一个项目，那么它肯定知道它将交付给订阅者的项目将是它收到的第一个项目。的确如此，但它在收到第一个项目时还不知道源流是否会产生第二个项目。除非源完成，否则它无法转发第一个项目。我们可以说，`SingleAsync` 的工作是首先验证源中是否正好包含一个项目，如果包含则转发该项目，如果不包含则报错。在出错的情况下，如果 `SingleAsync` 收到第二个项目，它就会知道自己出错了，因此它可以立即调用订阅者的 `OnError`。但在成功的情况下，直到源确认完成后不会再有新的项目，它才会知道一切正常。只有到那时，`SingleAsync` 才会发出结果。

### 阻塞版本的 First/Last/Single[OrDefault]

前面几节介绍的几个操作符都以 `Async` 结尾。这有点奇怪，因为通常情况下，以 `Async` 结尾的 .NET 方法会返回一个 `Task` 或 `Task<T>`，而这些方法都会返回一个 `IObservable<T>`。另外，正如前面已经讨论过的，这些方法中的每一个都对应于标准的 LINQ 操作符，而这些操作符通常不会以 `Async` 结尾。(更让人困惑的是，一些 LINQ 提供者（如 Entity Framework Core）确实包含其中一些操作符的 `Async` 版本，但它们是不同的。与 Rx 不同的是，这些操作符实际上返回的是 `Task<T>`，因此它们产生的仍然是单个值，而不是 `IQueryable<T>` 或 `IEnumerable<T>`）。这种命名方式源于 Rx 设计初期的一个不幸选择。

如果 Rx 是今天从头开始设计的，上一节中的相关操作符就会使用正常的名称： `First`、`FirstOrDefault` 等等。它们之所以都以 `Async` 结尾，是因为这些操作符是在 Rx 2.0 中添加的，而 Rx 1.0 已经定义了具有这些名称的操作符。本示例使用 `First` 运算符：

```c#
int v = Observable.Range(1, 10).First();
Console.WriteLine(v);
```

这将打印出值 1，也就是 `Range` 返回的第一个项目。但看看变量 v 的类型，它不是 `IObservable<int>`，只是一个 `int`。如果我们把它用在一个 Rx 操作符上，而该操作符在订阅后不会立即产生值，会发生什么情况呢？下面是一个例子

```c#
long v = Observable.Timer(TimeSpan.FromSeconds(2)).First();
Console.WriteLine(v);
```

如果运行这个运算，你会发现在产生值之前，对 `First` 的调用不会返回。这是一个阻塞操作符。我们通常避免在 Rx 中使用阻塞操作符，因为使用它们很容易造成死锁。Rx 的全部意义在于我们可以创建对事件做出反应的代码，因此仅仅坐等特定的可观测源产生值并不符合 Rx 的精神。如果你发现自己想这样做，通常有更好的方法来实现你想要的结果。(或许 Rx 并不适合你正在做的事情）。

如果你真的需要像这样等待一个值，使用 `Async` 窗体和 Rx 对 C# 的 `async/await` 的集成支持可能会更好：

```c#
long v = await Observable.Timer(TimeSpan.FromSeconds(2)).FirstAsync();
Console.WriteLine(v);
```

这在逻辑上具有相同的效果，但因为我们使用的是 `await`，所以在调用线程等待可观察源产生值时，不会阻塞调用线程。这可能会减少死锁的机会。

事实上，我们可以使用 `await` 来解释这些以 `Async` 结束的方法，但你可能想知道这到底是怎么回事。我们看到这些方法都返回 `IObservable<T>`，而不是 `Task<T>`，那么我们是如何使用 `await` 的？在[离开 Rx 的世界](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/13_LeavingIObservable.md#integration-with-async-and-await)一章中有完整的解释，但简而言之，Rx 提供的扩展方法使其得以实现。当你等待一个可观察的序列时，`await` 会在源完成时完成，并返回源产生的最终值。这对于像 `FirstAsync` 和 `LastAsync` 这样只能产生一个项目的操作符来说非常有效。

需要注意的是，偶尔也会出现值可以立即获得的情况。例如，[第3章中的 BehaviourSubject<T> 部分](creating-observable-sequences.md#BehaviorSubject<T>)显示，`BehaviourSubject<T>` 的决定性特征是它始终有一个当前值。这意味着 Rx 的 `First` 方法实际上不会阻塞--它会订阅 `BehaviourSubject<T>`，而 `BehaviourSubject<T>.Subscribe` 在返回之前会在其订阅者的可观察对象上调用 `OnNext`。这样，`First` 就能在不阻塞的情况下立即返回值。(当然，如果您使用了接受谓词的 `First` 重载，并且如果 `BehaviourSubject<T>` 的值不满足谓词，`First` 就会阻塞）。

### ElementAt

还有另一种标准 LINQ 操作符，用于从源代码中选择一个特定元素：`ElementAt`。您可以向它提供一个数字，表示您需要的元素在序列中的位置。在静态数据 LINQ 提供者中，这在逻辑上等同于通过索引访问数组元素。Rx 实现了这个操作符，但大多数 LINQ 提供者的 `ElementAt<T>` 实现返回的是 `T`，而 Rx 的实现返回的是 `IObservable<T>`。与 `First`、`Last` 和 `Single` 不同，Rx 没有提供 `ElementAt<T>` 的阻塞形式。但是，由于您可以等待任何 `IObservable<T>`，因此您可以一直这样做：

```c#
IAisMessage fourth = await receiverHost.Message.ElementAt(4);
```

如果您的源序列只产生五个值，而我们要求 `ElementAt(5)`，那么当源完成时，`ElementAt` 返回的序列将向其订阅者报告 `ArgumentOutOfRangeException` 错误。我们有三种方法来处理这个问题：

- 优雅地处理 OnError
- 使用 `.Skip(5).Take(1)`; 这将忽略前 5 个值，只取第 6 个值。如果序列中的元素少于 6 个，我们只会得到一个空序列，但不会出错。
- 使用 ElementAtOrDefault

`ElementAtOrDefault` 扩展方法将在索引超出范围的情况下，通过推送 `default(T)` 值来保护我们。目前还没有提供默认值的选项。

## 时间过滤

通过 `Take` 和 `TakeLast` 操作符，我们可以过滤掉除最开始或最后的元素之外的所有元素（而 `Skip` 和 `SkipLast` 则可以让我们看到除这些元素之外的所有元素），但这些操作符都要求我们知道元素的确切数量。如果我们想指定的截断点不是元素数量，而是某个特定的时间瞬间呢？

事实上，你已经看到了一个例子：早些时候，我使用 `TakeUntil` 将一个无穷无尽的 `IObservable<T>` 转换为一个将在五分钟后完成的 `IObservable<T>`。这是一系列操作符中的一个。

### SkipWhile 和 TakeWhile

在 [Skip 和 SkipLast 部分](#Skip 和 SkipLast)，我描述了一种传感器，它的前几个读数会产生垃圾值。这种情况很常见。例如，气体监测传感器通常需要让某些组件达到正确的工作温度，然后才能产生准确的读数。在该部分的示例中，我使用了 `Skip(5)` 来忽略前几个读数，但这只是一个粗略的解决方案。我们怎么知道 5 次就够了？或者可能更早准备好，在这种情况下，5 个读数就太少了。

我们真正要做的是，在知道读数有效之前丢弃读数。而这正是 `SkipWhile` 可以派上用场的场景。假设我们有一个气体传感器，它不仅能报告某种特定气体的浓度，还能报告进行检测的传感器板的温度。与其希望跳过 5 个读数是一个合理的数字，我们可以表达实际需求：

```c#
const int MinimumSensorTemperature = 74;
IObservable<SensorReading> readings = sensor.RawReadings
    .SkipUntil(r => r.SensorTemperature >= MinimumSensorTemperature);
```

这直接表达了我们所需的逻辑：这将放弃读数，直到设备达到最低工作温度。

下一组方法允许您在谓词求值为真时跳过或从序列中取值。对于 `SkipWhile` 操作，这将过滤掉所有值，直到某个值无法通过谓词，然后返回剩余序列。

```c#
var subject = new Subject<int>();
subject
    .SkipWhile(i => i < 4)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnNext(4);
subject.OnNext(3);
subject.OnNext(2);
subject.OnNext(1);
subject.OnNext(0);

subject.OnCompleted();
```

输出：

```
4
3
2
1
0
Completed
```

当谓词通过时，`TakeWhile` 将返回所有值，当第一个值失败时，序列将完成。

```c#
var subject = new Subject<int>();
subject
    .TakeWhile(i => i < 4)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnNext(4);
subject.OnNext(3);
subject.OnNext(2);
subject.OnNext(1);
subject.OnNext(0);

subject.OnCompleted();
```

输出：

```
1
2
3
Completed
```

### SkipUntil 和 TakeUntil

除了 `SkipWhile` 和 `TakeWhile` 之外，Rx 还定义了 `SkipUntil` 和 `TakeUntil`。它们听起来可能只是同一概念的另一种表达方式：你可能会认为 `SkipUntil` 的功能与 `SkipWhile` 几乎完全相同，唯一不同的是，`SkipWhile` 在其谓词返回 `true` 时运行，而 `SkipUntil` 在其谓词返回 `false` 时运行。而且，`SkipUntil` 有一个重载（`TakeUntil` 也有一个相应的重载）。如果只是这样，那就没什么意思了。然而，`SkipUntil` 和 `TakeUntil` 的重载可以让我们做一些 `SkipWhile` 和 `TakeWhile` 做不到的事情。

你已经看到了一个例子。[`FirstAsync` 和 `FirstOrDefaultAsync`](#FirstAsync 和 FirstOrDefaultAsync) 包含了一个使用 `TakeUntil` 的重载并接受 `DateTimeOffset` 的示例。它封装了任何 `IObservable<T>`，返回一个 `IObservable<T>`，它会将源中的所有内容转发到指定的时间点，然后立即完成（并取消订阅底层源）。

我们无法通过 `TakeWhile` 来实现这一目标，因为只有当源产生一个项目时，它才会咨询其谓词。如果我们想让源在特定时间完成，那么使用 `TakeWhile` 的唯一方法就是，在我们想要完成的时刻，源恰好产生了一个项目。`TakeWhile` 只能在源产生一个项目时完成。`TakeUntil` 可以异步完成。如果我们指定了一个未来 5 分钟的时间，那么当时间到达时，如果源完全闲置也没关系。`TakeUntil` 还是会完成。(它依赖于 [Schedulers](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/11_SchedulingAndThreading.md#schedulers) 才能做到这一点）。

我们不必使用时间，`TakeUntil` 提供了一个重载，可以接受第二个 `IObservable<T>`。这样，我们就可以告诉它在有趣的事情发生时停止，而无需提前知道确切的时间。`TakeUntil` 的重载将转发来自源的项目，直到第二个 `IObservable<T>` 产生一个值。`SkipUntil` 提供了类似的重载，其中第二个 `IObservable<T>` 决定何时应开始从源转发项目。

注意：这些重载要求第二个可观察对象产生一个值，以触发开始或结束。如果第二个可观察变量完成后没有产生任何通知，那么它就没有任何作用--`TakeUntil` 将继续无限期地获取项目；而 `SkipUntil` 将永远不会产生任何结果。换句话说，这些操作符将 `Observable.Empty<T>()` 与 `Observable.Never<T>()` 等价。

### Distinct 和 DistinctUntilChanged

`Distinct` 是另一个标准的 LINQ 操作符。它可以删除序列中的重复项。要做到这一点，它需要记住源产生过的所有值，这样就能过滤掉之前看到过的任何项目。Rx 包含 `Distinct` 的实现，本示例使用它来显示生成 AIS 信息的船只的唯一标识符，但确保只在第一次看到时显示每个此类标识符：

```c#
IObservable<uint> newIds = receiverHost.Messages
    .Select(m => m.Mmsi)
    .Distinct();

newIds.Subscribe(id => Console.WriteLine($"New vessel: {id}"));
```

(这有点超前--它使用了 Select，我们将在[序列转换](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/06_Transformation.md)一章中介绍。不过，这是一个使用非常广泛的 LINQ 操作符，所以您可能已经很熟悉了。我在这里使用它，只是为了从报文中提取 MMSI（船只标识符）。

如果我们只对船只的标识符感兴趣，那么这个示例就很好。但如果我们想查看这些信息的细节呢？我们如何才能既能只查看我们以前从未听说过的船只的信息，又能查看这些信息中的信息呢？使用 `Select` 来提取 id 会阻止我们这样做。幸运的是，`Distinct` 提供了一个重载功能，使我们能够改变它确定唯一性的方式。与其让 `Distinct` 查看它正在处理的值，我们可以为它提供一个函数，让我们挑选自己喜欢的任何特征。因此，我们可以不过滤数据流中从未见过的值，而是过滤数据流中具有我们从未见过的某些特定属性或属性组合的值。下面是一个简单的例子：

```c#
IObservable<IAisMessage> newVesselMessages = 
   receiverHost.Messages.Distinct(m => m.Mmsi);
```

在这里，`Distinct` 的输入现在是 `IObservable<IAisMessage>`。(在前面的示例中，它实际上是 `IObservable<uint>`，因为选择子句只选出了 MMSI）。因此，每次源发出 `IAisMessage` 时，`Distinct` 都会接收到整个 `IAisMessage`。但由于我们提供了一个回调，它不会尝试将整个 `IAisMessage` 消息相互比较。取而代之的是，每次收到信息时，它都会把信息传递给我们的回调函数，然后查看我们的回调函数返回的值，并将其与回调函数返回的所有以前看到过的信息的值进行比较，只有当信息是新的时，才让信息通过。

因此，效果与之前类似。只有当报文的 MMSI 是以前未见过的，才允许报文通过。但不同之处在于 `Distinct` 操作符的输出是 `IObservable<IAisMessage>`，因此当 `Distinct` 允许一个项目通过时，整个原始消息仍然可用。

除了标准的 LINQ `Distinct` 操作符，Rx 还提供了 `DistinctUntilChanged`。只有在内容发生变化时，它才会让通知通过，而这是通过只过滤掉相邻的重复内容来实现的。例如，给定序列 `1,2,2,3,4,4,5,4,3,3,2,1,1` 它将产生 `1,2,3,4,5,4,3,2,1`。`Distinct` 会记住产生过的每个值，而 `DistinctUntilChanged` 只记住最近发出的值，并且只有在新值与最近值匹配时才会将其过滤掉。

本示例使用 `DistinctUntilChanged` 检测特定船只何时报告 `NavigationStatus` 发生变化。

```c#
uint exampleMmsi = 235009890;
IObservable<IAisMessageType1to3> statusChanges = receiverHost.Messages
    .Where(v => v.Mmsi == exampleMmsi)
    .OfType<IAisMessageType1to3>()
    .DistinctUntilChanged(m => m.NavigationStatus)
    .Skip(1);
```

例如，如果船只多次报告 `AtAnchor` 状态，那么 `DistinctUntilChanged` 会放弃每一条此类报文，因为该状态与之前的状态相同。但如果状态变为 `UnderwayUsingEngine`，`DistinctUntilChanged` 就会允许报告该状态的第一条信息通过。然后，它将不允许任何其他信息通过，直到值再次发生变化，或者变回 `AtAnchor`，或者变回 `Moored` 等其他状态。(末尾的 `Skip(1)` 出现就是因为 `DistinctUntilChanged` 总是允许它看到的第一条信息通过。我们无法知道这是否真的代表了状态的改变，但很可能不是：船只每隔几分钟就会报告一次它们的状态，但它们改变状态的频率要低得多，所以我们第一次收到船只状态报告时，很可能并不代表状态的改变。通过放弃第一项，我们可以确保 `statusChanges` 只在我们可以确定状态发生变化时才提供通知）。

以上就是我们对 Rx 中可用过滤方法的简单介绍。虽然它们相对简单，但正如我们已经开始看到的那样，Rx 的强大之处在于其操作符的可组合性。

在这个信息丰富的时代，过滤运算符是我们管理潜在海量数据的第一站。我们现在知道了如何应用各种条件来移除数据。接下来，我们将学习可以转换数据的运算符。