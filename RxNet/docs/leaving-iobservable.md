# 离开 Rx 的世界

可观察序列是一种非常有用的结构，尤其是当我们利用 LINQ 的强大功能对其进行复杂查询时。尽管我们认识到可观察序列的好处，但有时我们必须离开 `IObservable<T>` 范式。当我们需要与现有的非基于 Rx 的 API（如使用事件或 `Task<T>` 的 API）集成时，就必须这样做。如果您觉得可观察范式更便于测试，您可能会选择它，或者在可观察范式和更熟悉的范式之间转换，可能会让您更容易学习 Rx。

Rx 的组合特性是其强大功能的关键，但当你需要与不理解 Rx 的组件集成时，这可能会成为一个问题。到目前为止，我们所看到的大多数 Rx 库功能都将其输入和输出表达为可观察对象。你该如何将现实世界中的一些事件源转化为可观测事件？如何对可观察对象的输出进行有意义的处理？

你已经看到了这些问题的答案。创[建可观测序列](creating-observable-sequences.md)一章展示了创建可观测源的各种方法。但在处理从 `IObservable<T>` 中产生的项目时，我们真正看到的只是如何实现 `IObserver<T>`，以及[如何使用基于回调的 `Subscribe` 扩展方法来订阅 `IObservable<T>`]()。

在本章中，我们将了解 Rx 中允许您离开 `IObservable<T>` 世界的方法，这样您就可以根据 Rx 源发出的通知采取行动。

## 集成 `async` 和 `await`

您可以在任何 `IObservable<T>` 中使用 C# 的 `await` 关键字。我们在前面的 [FirstAsync](operation-filtering.md#FirstAsync 和 FirstOrDefaultAsync) 中看到过这一点：

```c#
long v = await Observable.Timer(TimeSpan.FromSeconds(2)).FirstAsync();
Console.WriteLine(v);
```

虽然 `await` 最常用于 `Task`、`Task<T>` 或 `ValueTask<T>`，但它实际上是一种可扩展的语言特性。通过提供一个名为 `GetAwaiter` 的方法（通常作为扩展方法）和一个适合 `GetAwaiter` 返回的类型，为 C# 提供 `await` 所需的功能，就可以使 `await` 或多或少地适用于任何类型。Rx 正是这样做的。如果您的源文件包含 `using System.Reactive.Linq`; 指令，就会有一个合适的扩展方法，因此您可以 `await` 任何任务。

实际的工作方式是，相关的 `GetAwaiter` 扩展方法将 `IObservable<T>` 封装为 `AsyncSubject<T>`，它提供了 C# 支持 `await` 所需的一切。这些封装器的工作方式是，每次对 `IObservable<T>` 执行 `await` 时都会调用 `Subscribe`。

如果源通过调用其观察者的 `OnError` 来报错，Rx 的 `await` 集成会将任务置入故障状态，以便 `await` 重新抛出异常。

序列可以是空的。它们可能会调用 `OnCompleted`，而从未调用过 `OnNext`。然而，由于无法从源的类型判断它是否为空，这与 `await` 范式并不十分匹配。对于任务，我们可以在编译时通过查看等待的是 `Task` 还是 `Task<T>`，知道是否会得到结果，因此编译器可以知道特定的 `await` 表达式是否会产生值。但是，当您等待 `IObservable<T>` 时，编译时没有任何区别，因此 Rx 在等待时报告序列为空的唯一方法是抛出一个 `InvalidOperationException`，报告序列不包含任何元素。

您可能还记得第 3 章的 [AsyncSubject<T>](creating-observable-sequences.md#AsyncSubject<T>) 部分，`AsyncSubject<T>` 只报告从其源中产生的最终值。因此，如果您等待一个报告多个项的序列，除了最后一项外，所有项都将被忽略。如果您想查看所有项目，但仍想使用 `await` 来处理完成和错误，该怎么办？

## ForEachAsync

`ForEachAsync` 方法支持 `await`，但它提供了一种处理每个元素的方法。你可以把它看作是前一节中描述的 `await` 行为和基于回调的 `Subscribe` 的混合体。我们仍然可以使用 `await` 来检测完成情况和错误，但我们提供了一个回调，使我们能够处理每项：

```c#
IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
await source.ForEachAsync(i => Console.WriteLine($"received {i} @ {DateTime.Now}"));
Console.WriteLine($"finished @ {DateTime.Now}");
```

输出：

```
received 0 @ 02/08/2023 07:53:46
received 1 @ 02/08/2023 07:53:47
received 2 @ 02/08/2023 07:53:48
received 3 @ 02/08/2023 07:53:49
received 4 @ 02/08/2023 07:53:50
finished @ 02/08/2023 07:53:50
```

请注意，如你所料，完成行是最后一行。让我们将其与 `Subscribe` 扩展方法进行比较，后者也能让我们为处理项目提供单个回调：

```c#
IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
source.Subscribe(i => Console.WriteLine($"received {i} @ {DateTime.Now}"));
Console.WriteLine($"finished @ {DateTime.Now}");
```

如输出所示，`Subscribe` 立即返回。我们对每个项的回调就像之前一样被调用，但这一切都发生在之后：

```
finished @ 02/08/2023 07:55:42
received 0 @ 02/08/2023 07:55:43
received 1 @ 02/08/2023 07:55:44
received 2 @ 02/08/2023 07:55:45
received 3 @ 02/08/2023 07:55:46
received 4 @ 02/08/2023 07:55:47
```

这在执行一些工作然后退出的批处理式程序中非常有用。在这种情况下使用 `Subscribe` 的问题是，我们的程序很容易在没有完成开始的工作的情况下退出。使用 `ForEachAsync` 可以轻松避免这种情况，因为我们只需使用 `await` 来确保我们的方法在工作完成前不会结束。

当我们直接对 `IObservable<T>` 使用 `await` 或通过 `ForEachAsync` 使用 `await` 时，我们基本上是选择以传统方式而非响应式来处理序列完成。错误和完成处理不再由回调驱动--Rx 为我们提供 `OnCompleted` 和 `OnError` 处理程序，而是通过 C# 的 awaiter 机制来表示这些处理程序。(具体来说，当我们直接 `await` 一个源时，Rx 会提供一个自定义等待器，而当我们使用 `ForEachAsync` 时，它只会返回一个任务）。

请注意，在某些情况下，`Subscribe` 会阻塞直到其源完成。`Observable.Return` 和 `Observable.Range` 默认会这样做。我们可以尝试通过指定不同的调度器使最后一个示例做到这一点：

```c#
// Don't do this!
IObservable<long> source = 
   Observable.Interval(TimeSpan.FromSeconds(1), ImmediateScheduler.Instance)
             .Take(5);
source.Subscribe(i => Console.WriteLine($"received {i} @ {DateTime.Now}"));
Console.WriteLine($"finished @ {DateTime.Now}");
```

不过，这也凸显了非同步阻塞调用的危险性：虽然这看起来应该能正常工作，但实际上在当前版本的 Rx 中会造成死锁。Rx 认为即时调度程序（`ImmediateScheduler`）不适合基于定时器的操作，这也是它不是默认调度程序的原因，而这种情况就是一个很好的例子。(**根本问题在于，取消已计划工作项的唯一方法是对调用 `Schedule` 返回的对象调用 `Dispose`。根据定义，`ImmediateScheduler` 在完成工作后才会返回，这意味着它实际上不支持取消。因此，对 `Interval` 的调用实际上创建了一个无法取消的定期计划工作项，因此它注定要永远运行下去**）。

这就是我们需要 `ForEachAsync` 的原因。看起来我们可以通过巧妙地使用调度器来达到同样的效果，但实际上，如果你需要等待异步事件的发生，使用 `await` 总比使用阻塞调用线程的方法要好。

## ToEnumerable

到目前为止，我们已经探索了两种机制，将 Rx 的回调机制中的完成和错误处理转换为 `await` 所支持的更传统的方法，但我们仍然需要提供一个回调才能处理每个单独的项。但是，`ToEnumerable` 扩展方法更进一步：它使整个序列可以通过传统的 `foreach` 循环来消耗：

```c#
var period = TimeSpan.FromMilliseconds(200);
IObservable<long> source = Observable.Timer(TimeSpan.Zero, period).Take(5);
IEnumerable<long> result = source.ToEnumerable();

foreach (long value in result)
{
    Console.WriteLine(value);
}

Console.WriteLine("done");
```

输出：

```
0
1
2
3
4
done
```

当你开始枚举序列（即惰性地枚举）时，源可观察序列将被订阅。如果还没有可用的元素，或者您已经消耗了迄今为止生成的所有元素，`foreach` 对枚举器 `MoveNext` 的调用就会阻塞，直到源生成一个元素。因此，这种方法依赖于源能够从其他线程生成元素。(在本例中，`Timer` 默认使用 [DefaultScheduler](scheduling-and-threading.md#DefaultScheduler)，它在线程池上运行定时工作）。如果序列产生值的速度快于您消耗值的速度，它们就会为您排队。(这意味着使用 `ToEnumerable` 时，在同一线程上消耗和生成项目在技术上是可行的，但这依赖于生产者始终保持领先。这将是一种危险的方法，因为如果 `foreach` 循环跟不上，就会出现死锁）。

与 `await` 和 `ForEachAsync` 一样，如果源程序报错，就会抛出异常，因此您可以使用普通的 C# 异常处理方法，本示例就说明了这一点：

```c#
try 
{ 
    foreach (long value in result)
    { 
        Console.WriteLine(value); 
    } 
} 
catch (Exception e) 
{ 
    Console.WriteLine(e.Message);
} 
```

## 转成单个集合

有时，您需要将源产生的所有项作为一个单独的列表。例如，您可能无法单独处理这些元素，因为有时您需要回溯之前收到的元素。下面介绍的四种操作会将所有项目收集到一个单一的集合中。它们仍然都会产生 `IObservable<T>`（例如，`IObservable<int[]>` 或 `IObservable<Dictionary<string,long>>`），但这些都是单元素可观察对象，正如你已经看到的，你可以使用 `await` 关键字来获取单个输出。

### ToArray 和 ToList

`ToArray` 和 `ToList` 接收一个可观察的序列，并分别将其打包成数组或 `List<T>` 的实例。与所有单一收集操作一样，这些操作会返回一个可观察的源，等待输入序列完成，然后生成数组或列表作为单一值，之后立即完成。本示例使用 `ToArray` 将源序列中的全部 5 个元素收集到一个数组中，然后使用 `await` 从 `ToArray` 返回的序列中提取该数组：

```c#
TimeSpan period = TimeSpan.FromMilliseconds(200);
IObservable<long> source = Observable.Timer(TimeSpan.Zero, period).Take(5);
IObservable<long[]> resultSource = source.ToArray();

long[] result = await resultSource;
foreach (long value in result)
{
    Console.WriteLine(value);
}
```

输出：

```
0
1
2
3
4
```

由于这些方法仍然返回可观测序列，因此您也可以使用正常的 Rx 订阅机制，或将其用作其他操作符的输入。

如果源产生值后出错，您将不会收到任何这些值。在此之前收到的所有值都将被丢弃，操作符将调用其观察者的 `OnError`（在上面的示例中，这将导致从 `await` 抛出异常）。所有四个操作符（`ToArray`、`ToList`、`ToDictionary` 和 `ToLookup`）都是这样处理错误的。

### ToDictionary 和 ToLookup

Rx 可以使用 `ToDictionary` 和 `ToLookup` 方法将可观测序列打包成字典或查找。这两种方法采用的基本方法与 `ToArray` 和 `ToList` 方法相同：它们返回一个单元素序列，在输入源完成后生成集合。

`ToDictionary` 提供了四个重载，直接对应于 LINQ to Objects 为 `IEnumerable<T>` 定义的 `ToDictionary` 扩展方法：

```c#
// Creates a dictionary from an observable sequence according to a specified 
// key selector function, a comparer, and an element selector function.
public static IObservable<IDictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector, 
    IEqualityComparer<TKey> comparer) 
{...} 

// Creates a dictionary from an observable sequence according to a specified
// key selector function, and an element selector function. 
public static IObservable<IDictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector) 
{...} 

// Creates a dictionary from an observable sequence according to a specified
// key selector function, and a comparer. 
public static IObservable<IDictionary<TKey, TSource>> ToDictionary<TSource, TKey>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector,
    IEqualityComparer<TKey> comparer) 
{...} 

// Creates a dictionary from an observable sequence according to a specified 
// key selector function. 
public static IObservable<IDictionary<TKey, TSource>> ToDictionary<TSource, TKey>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector) 
{...} 
```

`ToLookup` 扩展提供了外观几乎相同的重载，区别在于返回类型（显然还有名称）。它们都返回 `IObservable<ILookup<TKey, TElement>>`。与 LINQ to Objects 一样，字典和查找之间的区别在于 `ILookup<TKey, TElement>>` 接口允许每个键具有任意数量的值，而字典将每个键映射为一个值。

## ToTask

虽然 Rx 提供了对 `IObservable<T>` 使用 `await` 的直接支持，但有时获取代表 `IObservable<T>` 的 `Task<T>` 也很有用。这很有用，因为某些应用程序接口需要一个 `Task<T>`。您可以在任何 `IObservable<T>` 上调用 `ToTask()`，它将订阅该可观察对象，并返回一个任务<T>，该任务将在任务完成时完成，产生序列的最终输出作为任务的结果。如果源完成时没有产生元素，任务将进入故障状态，并出现 `InvalidOperation` 异常，说明输入序列不包含任何元素。

您可以选择传递一个取消令牌。如果在可观察序列完成前取消，Rx 将取消订阅源，并将任务置于取消状态。

这是如何使用 `ToTask` 操作符的一个简单示例。请注意，`ToTask` 方法属于 `System.Reactive.Threading.Tasks` 命名空间。

```c#
IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
Task<long> resultTask = source.ToTask();
long result = await resultTask; // Will take 5 seconds. 
Console.WriteLine(result);
```

输出：

```
4
```

如果源序列调用 `OnError`，Rx 会使用提供的异常将任务置于故障状态。

有了任务后，当然就可以使用 TPL 的所有功能，例如连续运行。

## ToEvent

正如您可以使用 [FromEventPattern](creating-observable-sequences.md#源自事件) 将事件作为可观察序列的源一样，您也可以使用 `ToEvent` 扩展方法使您的可观察序列看起来像一个标准的 .NET 事件。

```c#
// Exposes an observable sequence as an object with a .NET event. 
public static IEventSource<unit> ToEvent(this IObservable<Unit> source)
{...}

// Exposes an observable sequence as an object with a .NET event. 
public static IEventSource<TSource> ToEvent<TSource>(this IObservable<TSource> source) 
{...}
```

`ToEvent` 方法返回一个 `IEventSource<T>`，其中只有一个成员：`OnNext` 事件。

```c#
public interface IEventSource<T> 
{ 
    event Action<T> OnNext; 
} 
```

当我们使用 `ToEvent` 方法转换可观察序列时，只需提供一个 `Action<T>` 即可进行订阅，这里我们使用 lambda 进行订阅。

```c#
var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5); 
var result = source.ToEvent(); 
result.OnNext += val => Console.WriteLine(val);
```

输出：

```
0
1
2
3
4
```

虽然这是将 Rx 通知转换为事件的最简单方法，但它并不遵循标准的 .NET 事件模式。如果我们想这样做，我们可以使用稍有不同的方法。

### ToEventPattern

通常，.NET 事件会向其处理程序提供 `sender` 和 `EventArgs` 参数。在上面的示例中，我们只是获取值。如果您想将序列作为遵循标准模式的事件公开，则需要使用 `ToEventPattern`。

```c#
// Exposes an observable sequence as an object with a .NET event. 
public static IEventPatternSource<TEventArgs> ToEventPattern<TEventArgs>(
    this IObservable<EventPattern<TEventArgs>> source) 
    where TEventArgs : EventArgs 
```

`ToEventPattern` 将接收 `IObservable<EventPattern<TEventArgs>>` 并将其转换为 `IEventPatternSource<TEventArgs>`。这些类型的公共接口非常简单。

```c#
public class EventPattern<TEventArgs> : IEquatable<EventPattern<TEventArgs>>
    where TEventArgs : EventArgs 
{ 
    public EventPattern(object sender, TEventArgs e)
    { 
        this.Sender = sender; 
        this.EventArgs = e; 
    } 
    public object Sender { get; private set; } 
    public TEventArgs EventArgs { get; private set; } 
    //...equality overloads
} 

public interface IEventPatternSource<TEventArgs> where TEventArgs : EventArgs
{ 
    event EventHandler<TEventArgs> OnNext; 
} 
```

为此，我们需要一个合适的 `EventArgs` 类型。您可以使用 .NET 运行库提供的类型，如果没有，也可以自己编写：

`EventArgs` 类型：

```c#
public class MyEventArgs : EventArgs 
{ 
    private readonly long _value; 
    
    public MyEventArgs(long value) 
    { 
        _value = value; 
    } 

    public long Value 
    { 
        get { return _value; } 
    } 
} 
```

然后，我们可以使用 `Select` 功能对 Rx 进行简单的变换：

```c#
IObservable<EventPattern<MyEventArgs>> source = 
   Observable.Interval(TimeSpan.FromSeconds(1))
             .Select(i => new EventPattern<MyEventArgs>(this, new MyEventArgs(i)));
```

现在，我们有了一个兼容的序列，可以使用 `ToEventPattern`，进而使用标准事件处理程序。

```c#
IEventPatternSource<MyEventArgs> result = source.ToEventPattern(); 
result.OnNext += (sender, eventArgs) => Console.WriteLine(eventArgs.Value);
```

既然我们已经知道了如何回到 .NET 事件中，让我们休息一下，回忆一下为什么 Rx 是一个更好的模型。

- 事件难以组成
- 事件不能作为参数传递或存储在字段中
- 事件不具备随时间推移轻松查询的能力
- 事件没有报告错误的标准模式
- 事件没有指示值序列结束的标准模式
- 事件几乎无法帮助管理并发或多线程应用程序

## Do

生产系统的非功能性要求通常要求高可用性、质量监控功能和较短的缺陷解决周期。日志、调试、仪表化和日志是实现非功能性要求的常见实施选择。为了实现这些功能，“接入”您的 Rx 查询，使其在正常运行的同时提供监控和诊断信息，通常是非常有用的。

`Do` 扩展方法允许你注入副作用行为。从 Rx 的角度来看，`Do` 似乎什么也没做：您可以将它应用于任何 `IObservable<T>`，它将返回另一个 `IObservable<T>`，该 `IObservable<T>` 将报告与其源完全相同的元素、错误或完成情况。然而，它的各种重载都需要回调参数，这些参数看起来与 `Subscribe` 的参数一样：你可以为单个项目、完成和错误提供回调。与 `Subscribe` 不同的是，`Do` 并不是最终目的地--`Do` 回调所看到的一切也将转发给 `Do` 的订阅者。这使得它在日志记录和类似的仪器操作中非常有用，因为你可以用它来报告信息是如何在不改变查询行为的情况下流经 Rx 查询的。

当然，你必须小心谨慎。使用 `Do` 会影响性能。如果你提供给 `Do` 的回调执行了任何可能改变 Rx 查询输入的操作，你就会创建一个反馈回路，使行为变得更加难以理解。

让我们先定义一些日志记录方法，然后在示例中继续使用：

```c#
private static void Log(object onNextValue)
{
    Console.WriteLine($"Logging OnNext({onNextValue}) @ {DateTime.Now}");
}

private static void Log(Exception error)
{
    Console.WriteLine($"Logging OnError({error}) @ {DateTime.Now}");
}

private static void Log()
{
    Console.WriteLine($"Logging OnCompleted()@ {DateTime.Now}");
}
```

这段代码使用 `Do` 引入了一些日志记录方法。

```c#
IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(3);
IObservable<long> loggedSource = source.Do(
    i => Log(i),
    ex => Log(ex),
    () => Log());

loggedSource.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("completed"));
```

输出：

```
Logging OnNext(0) @ 01/01/2012 12:00:00
0
Logging OnNext(1) @ 01/01/2012 12:00:01
1
Logging OnNext(2) @ 01/01/2012 12:00:02
2
Logging OnCompleted() @ 01/01/2012 12:00:02
completed
```

请注意，由于 `Do` 是查询的一部分，因此它必然比 `Subscribe` 更早看到值，后者是链中的最后一环。这就是日志信息出现在 `Subscribe` 回调产生的行之前的原因。我喜欢把 `Do` 方法看作是序列的窃听器。它能让你在不修改序列的情况下监听序列。

与 `Subscribe` 方法一样，`Do` 方法并不传递回调，而是通过重载为 `OnNext`、`OnError` 和 `OnCompleted` 通知提供回调，或者将 `IObserver<T>` 传递给 Do 方法。

## AsObservable 封装

不良封装是开发人员为错误敞开大门的一种方式。下面是一些粗心大意导致抽象泄漏的情况。我们的第一个示例乍一看似乎无害，但却存在许多问题。

```c#
public class UltraLeakyLetterRepo
{
    public ReplaySubject<string> Letters { get; }

    public UltraLeakyLetterRepo()
    {
        Letters = new ReplaySubject<string>();
        Letters.OnNext("A");
        Letters.OnNext("B");
        Letters.OnNext("C");
    }
}
```

在本例中，我们将可观察序列作为一个属性公开。我们使用了 `ReplaySubject<string>`，这样每个订阅者在订阅时都会收到所有值。但是，在 `Letters` 属性的公共类型中公开这一实现选择的封装性很差，因为消费者可以调用 `OnNext/OnError/OnCompleted`。为了堵住这个漏洞，我们可以简单地将公开可见的属性类型设为 `IObservable<string>`。

```c#
public class ObscuredLeakinessLetterRepo
{
    public IObservable<string> Letters { get; }

    public ObscuredLeakinessLetterRepo()
    {
        var letters = new ReplaySubject<string>();
        letters.OnNext("A");
        letters.OnNext("B");
        letters.OnNext("C");
        this.Letters = letters;
    }
}
```

这是一项重大改进：编译器不会让使用此源实例的人编写 `source.Letters.OnNext("1")`。因此，API 的表面区域正确地封装了实现细节，但如果我们过于偏执，我们就无法阻止消费者将结果转换回 `ISubject<string>` 并调用他们喜欢的任何方法。在这个示例中，我们看到外部代码将它们的值推送到序列中。

```c#
var repo = new ObscuredLeakinessLetterRepo();
IObservable<string> good = repo.GetLetters();
    
good.Subscribe(Console.WriteLine);

// Be naughty
if (good is ISubject<string> evil)
{
    // So naughty, 1 is not a letter!
    evil.OnNext("1");
}
else
{
    Console.WriteLine("could not sabotage");
}
```

输出：

```
A
B
C
1
```

可以说，代码中出现这种情况是自找麻烦，但如果我们想积极避免这种情况，那么解决这个问题的方法就非常简单了。通过应用 `AsObservable` 扩展方法，我们可以修改构造函数中设置 `this.Letters` 的那一行，将主体封装在仅实现 `IObservable<T>` 的类型中。

```c#
this.Letters = letters.AsObservable();
```

输出：

```
A
B
C
could not sabotage
```

虽然我在这些例子中使用了“邪恶”和“破坏”等字眼，[但造成问题的往往不是恶意，而是疏忽](https://en.wikipedia.org/wiki/Hanlon%27s_razor)。失败的责任首先落在设计泄漏类的程序员身上。设计接口很难，但我们应该尽最大努力，通过提供可发现的、一致的类型，帮助我们代码的用户掉入[成功的深渊](https://learn.microsoft.com/en-gb/archive/blogs/brada/the-pit-of-success)。如果我们缩小类型的表面积，只暴露我们希望用户使用的功能，那么类型就更容易被发现。在本例中，我们缩小了类型的表面积。为此，我们为属性选择了一个合适的面向公共的类型，然后使用 `AsObservable` 方法阻止了对底层类型的访问。

我们在本章中学习的一系列方法完成了从[创建序列](creating-observable-sequences.md)一章开始的循环。现在，我们已经掌握了进入和离开 Rx 世界的方法。在选择进出 `IObservable<T>` 时要小心。最好不要来回转换--先进行一些基于 Rx 的处理，然后再编写一些更传统的代码，然后再将处理结果导入 Rx，这样会很快把代码库搞得一团糟，而且可能会导致设计缺陷。通常情况下，最好将所有 Rx 逻辑放在一起，这样只需与外部世界整合两次：一次是输入，一次是输出。