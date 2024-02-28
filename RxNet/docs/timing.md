# 基于时间序列

对于事件源来说，时间往往很重要。在某些情况下，人们对某些事件感兴趣的唯一信息可能就是事件发生的时间。核心 `IObservable<T>` 和 `IObserver<T>` 接口在其方法签名中完全没有提及时间，但它们并不需要提及时间，因为源可以决定何时调用观察者的 `OnNext` 方法。订阅者知道事件发生的时间，是因为事件正在发生。这并不总是处理时间的最方便方式，因此 Rx 库提供了一些与时间相关的操作符。我们已经看到了几个提供可选基于时间操作的操作符：缓冲区 [Buffer](operation-partitioning.md#Buffer) 和 [Window](operation-partitioning.md#Window)。本章将介绍各种与时间有关的操作符。

## Timestamp 和 TimeInterval

由于可观察序列是异步的，因此很容易知道何时收到元素。显然，订阅者总是可以直接使用 `DateTimeOffset.Now`，但如果您想将**到达时间**作为大查询的一部分，`Timestamp` 扩展方法是一种方便的便捷方法，它可以为每个元素附加一个时间戳。它将源序列中的元素包装成轻量级的 `Timestamped<T>` 结构。`Timestamped<T>` 类型是一个结构体，它暴露了所封装元素的值，同时还暴露了一个日期时间偏移（`DateTimeOffset`），表明 `Timestamp` 操作符何时收到该值。

在本示例中，我们创建了一个由三个值组成的序列，三个值之间相隔一秒，然后将其转换为时间戳序列。

```c#
Observable.Interval(TimeSpan.FromSeconds(1))
          .Take(3)
          .Timestamp()
          .Dump("Timestamp");
```

正如您所看到的，`Timestamped<T>` 的 `ToString()` 实现为我们提供了可读的输出。

```
Timestamp-->0@07/08/2023 10:03:58 +00:00
Timestamp-->1@07/08/2023 10:03:59 +00:00
Timestamp-->2@07/08/2023 10:04:00 +00:00
TimeStamp completed
```

我们可以看到，0、1 和 2 的产生时间各相隔一秒。

Rx 还提供 `TimeInterval`。它不报告项目到达的时间，而是报告项目之间的**时间间隔**（或者，对于第一个元素，报告订阅后出现该元素所需的时间）。与 `Timestamp` 方法类似，元素也被包裹在一个轻量级结构中。但是，`Timestamped<T>` 会在每个条目上标注到达时间，而 `TimeInterval` 会使用 `TimeInterval<T>` 类型对每个元素进行包装，并添加一个 `TimeSpan`。我们可以修改前面的示例，使用 `TimeInterval`：

```c#
Observable.Interval(TimeSpan.FromSeconds(1))
          .Take(3)
          .TimeInterval()
          .Dump("TimeInterval");
```

如您所见，输出结果现在报告的是元素之间的时间间隔，而不是收到元素的时间：

```
Timestamp-->0@00:00:01.0183771
Timestamp-->1@00:00:00.9965679
Timestamp-->2@00:00:00.9908958
Timestamp completed
```

从输出结果中可以看出，时间并不精确到一秒，但非常接近。这其中有些是 `TimeInterval` 运算符中的测量噪声，但大部分变化可能来自 `Observable.Interval` 类。调度程序满足时间要求的精度总是有限的。有些调度器引入的变化比其他调度器更多。通过用户界面线程交付工作的调度程序最终会受到该线程的消息循环响应速度的限制。但即使在最有利的条件下，调度程序也会受到限制，因为 .NET 并不是为在实时系统中使用而构建的（Rx 可用于的大多数操作系统也是如此）。因此，对于本节中的所有操作符，您都应该知道，在 Rx 中，时间总是尽力而为的事情。

事实上，时间的固有变化使得 `Timestamp` 特别有用。如果只是查看 `DateTimeOffset.Now`，问题在于处理一个事件所需的时间并不为零，因此在处理一个事件的过程中，每次尝试读取当前时间时，看到的时间都可能略有不同。如果只附加一次时间戳，我们就能捕捉到事件被观测到的时间，那么下游处理过程会增加多少延迟就无关紧要了。事件将被注释为一个单一、固定的时间，表明它经过 `Timestamp` 的时间。

## Delay

`Delay` 扩展方法对整个序列进行时移。`Delay` 尝试保留各值之间的相对时间间隔。但它的精度不可避免地会受到限制--它无法将时间重现到最接近的纳秒。具体精度由您使用的调度程序决定，通常在负载较重的情况下会变差，但通常会将时序还原到几毫秒以内。

`Delay` 有多种重载，提供了各种不同的指定时间偏移的方法。(在所有选项中，你都可以选择传递一个调度程序，但如果你调用的重载不需要调度程序，它就会默认为 `DefaultScheduler`）。最直接的方法是传递一个 `TimeSpan`，它会将序列延迟指定的时间。还有一种延迟方法接受一个 `DateTimeOffset`，它会等到指定的时间发生，然后开始重放输入。(第二种基于绝对时间的方法本质上等同于 `TimeSpan` 重载。通过从目标时间中减去当前时间得到 `TimeSpan`，可以得到大致相同的效果，只不过 `DateTimeOffset` 版本试图处理在调用 `Delay` 和指定时间到达之间系统时钟发生的变化）。

为了展示 `Delay` 方法的实际效果，本示例创建了一串相隔一秒的值，并为它们打上时间戳。这将表明，被延迟的不是订阅，而是将通知转发给最终订阅者的实际过程。

```c#
IObservable<Timestamped<long>> source = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Take(5)
    .Timestamp();

IObservable<Timestamped<long>> delay = source.Delay(TimeSpan.FromSeconds(2));

delay.Subscribe(value => 
   Console.WriteLine(
     $"Item {value.Value} with timestamp {value.Timestamp} received at {DateTimeOffset.Now}"),
   () => Console.WriteLine("delay Completed"));
```

如果查看输出中的时间戳，就会发现 `Timestamp` 捕捉到的时间都比订阅报告的时间早两秒：

```
Item 0 with timestamp 09/11/2023 17:32:20 +00:00 received at 09/11/2023 17:32:22 +00:00
Item 1 with timestamp 09/11/2023 17:32:21 +00:00 received at 09/11/2023 17:32:23 +00:00
Item 2 with timestamp 09/11/2023 17:32:22 +00:00 received at 09/11/2023 17:32:24 +00:00
Item 3 with timestamp 09/11/2023 17:32:23 +00:00 received at 09/11/2023 17:32:25 +00:00
Item 4 with timestamp 09/11/2023 17:32:24 +00:00 received at 09/11/2023 17:32:26 +00:00
delay Completed
```

请注意，延迟不会对 `OnError` 通知进行时移。这些通知会立即传播。

## Sample

`Sample` 方法会按照您要求的时间间隔生成项目。每次产生一个值时，它都会报告从数据源产生的最后一个值。如果您的数据源产生数据的速率高于您的需要（例如，假设您有一个每秒报告 100 次测量值的加速度计，但您只需要每秒读取 10 次），`Sample` 提供了一种降低数据速率的简便方法。本例展示了 Sample 的实际应用。

```c#
IObservable<long> interval = Observable.Interval(TimeSpan.FromMilliseconds(150));
interval.Sample(TimeSpan.FromSeconds(1)).Subscribe(Console.WriteLine);
```

输出：

```
5
12
18
```

如果您仔细观察这些数字，可能会发现每次数值之间的间隔并不相同。我选择 150 毫秒的源间隔和 1 秒的采样间隔，是为了强调采样中需要谨慎处理的一个方面：如果源产生项目的速率与采样速率不一致，这可能意味着 `Sample` 引入了源中没有的不规则性。如果我们列出底层序列产生值的时间，以及 `Sample` 提取每个值的时间，我们可以看到，在这些特定的时间下，采样间隔每 3 秒才与源时间一致。

| Relative time (ms) | Source value | Sampled value |
| ------------------ | ------------ | ------------- |
| 0                  |              |               |
| 50                 |              |               |
| 100                |              |               |
| 150                | 0            |               |
| 200                |              |               |
| 250                |              |               |
| 300                | 1            |               |
| 350                |              |               |
| 400                |              |               |
| 450                | 2            |               |
| 500                |              |               |
| 550                |              |               |
| 600                | 3            |               |
| 650                |              |               |
| 700                |              |               |
| 750                | 4            |               |
| 800                |              |               |
| 850                |              |               |
| 900                | 5            |               |
| 950                |              |               |
| 1000               |              | 5             |
| 1050               | 6            |               |
| 1100               |              |               |
| 1150               |              |               |
| 1200               | 7            |               |
| 1250               |              |               |
| 1300               |              |               |
| 1350               | 8            |               |
| 1400               |              |               |
| 1450               |              |               |
| 1500               | 9            |               |
| 1550               |              |               |
| 1600               |              |               |
| 1650               | 10           |               |
| 1700               |              |               |
| 1750               |              |               |
| 1800               | 11           |               |
| 1850               |              |               |
| 1900               |              |               |
| 1950               | 12           |               |
| 2000               |              | 12            |
| 2050               |              |               |
| 2100               | 13           |               |
| 2150               |              |               |
| 2200               |              |               |
| 2250               | 14           |               |
| 2300               |              |               |
| 2350               |              |               |
| 2400               | 15           |               |
| 2450               |              |               |
| 2500               |              |               |
| 2550               | 16           |               |
| 2600               |              |               |
| 2650               |              |               |
| 2700               | 17           |               |
| 2750               |              |               |
| 2800               |              |               |
| 2850               | 18           |               |
| 2900               |              |               |
| 2950               |              |               |
| 3000               | 19           | 19            |

由于第一个样本是在信号源发出 5 信号后采集的，而在三分之二的间隙之后，信号源将发出 6 信号，因此从某种意义上说，“正确”值应该是 5.67，但样本并没有试图进行这样的插值。它只是报告从信号源中产生的最后一个值。与此相关的一个后果是，如果采样间隔足够短，以至于你要求 `Sample` 报告的数值快于从数据源中产生的数值，那么它就会重复报告数值。

## Throttle

`Throttle` 扩展方法提供了一种保护措施，防止序列以不同的速度产生数值，有时甚至太快。与 `Sample` 方法一样，`Throttle` 将返回一段时间内的最后采样值。但与 `Sample` 不同的是，`Throttle` 的周期是一个滑动窗口。每当 `Throttle` 收到一个值，窗口就会重置。只有当时间段过去后，最后一个值才会被传播。这意味着 `Throttle` 方法只适用于以可变速率产生数值的序列。以恒定速度产生值的序列（如 `Interval` 或 `Timer`），如果产生的值快于节流周期，其所有值都将被抑制；而如果产生的值慢于节流周期，其所有值都将被传播。

```c#
// Ignores values from an observable sequence which 
// are followed by another value before dueTime.
public static IObservable<TSource> Throttle<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime)
{...}
public static IObservable<TSource> Throttle<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IScheduler scheduler)
{...}
```

我们可以应用 `Throttle` 来使用实时搜索功能，在输入时提出建议。我们通常希望等到用户停止键入一段时间后再搜索建议，否则我们可能会连续启动多个搜索，每次用户按下另一个键时都会取消最后一个搜索。只有暂停后，我们才能使用用户输入的完整内容执行搜索。`Throttle` 非常适合这种情况，因为如果**数据源产生的值快于指定的速度，它就不会允许任何事件通过**。

请注意，RxJS 库决定让他们版本的节流阀以不同的方式工作，因此如果您发现自己同时使用 Rx.NET 和 RxJS，请注意它们的工作方式并不相同。在 RxJS 中，当源代码超过指定速率时，节流阀不会完全关闭：它只是减少足够多的项目，使输出永远不会超过指定速率。因此，RxJS 的节流阀实现是一种速率限制器，而 Rx.NET 的节流阀更像是一个自复位断路器，在过载时完全关闭。

## Timeout

`Timeout` 操作符方法允许我们在源在给定时间内未产生任何通知的情况下，以错误方式终止序列。我们可以使用 `TimeSpan` 将时间段指定为一个滑动窗口，也可以通过提供 `DateTimeOffset` 将时间段指定为序列必须完成的绝对时间。

```c#
// Returns either the observable sequence or a TimeoutException
// if the maximum duration between values elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime)
{...}
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IScheduler scheduler)
{...}

// Returns either the observable sequence or a  
// TimeoutException if dueTime elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime)
{...}
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IScheduler scheduler)
{...}
```

如果我们提供了一个 `TimeSpan`，但在该时间跨度内没有产生任何值，那么序列就会出现超时异常（`TimeoutException`）而失败。

```c#
var source = Observable.Interval(TimeSpan.FromMilliseconds(100))
                       .Take(5)
                       .Concat(Observable.Interval(TimeSpan.FromSeconds(2)));

var timeout = source.Timeout(TimeSpan.FromSeconds(1));
timeout.Subscribe(
    Console.WriteLine, 
    Console.WriteLine, 
    () => Console.WriteLine("Completed"));
```

起初，产生值的频率足以满足 `Timeout` 的要求，因此 `Timeout` 返回的可观测变量只是转发来自源的项目。但一旦源停止产生项目，我们就会得到一个 `OnError`：

```
0
1
2
System.TimeoutException: The operation has timed out.
```

还有其他一些 `Timeout` 重载，使我们能够在超时发生时替代其他序列。

```c#
// Returns the source observable sequence or the other observable 
// sequence if the maximum duration between values elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IObservable<TSource> other)
{...}

public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IObservable<TSource> other, 
    IScheduler scheduler)
{...}

// Returns the source observable sequence or the 
// other observable sequence if dueTime elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IObservable<TSource> other)
{...}  

public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IObservable<TSource> other, 
    IScheduler scheduler)
{...}
```

正如我们现在看到的，Rx 提供了以响应模式管理时间的功能。数据可以定时、节流或采样，以满足您的需求。整个序列可以通过延迟功能进行时间转移，数据的及时性可以通过 `Timeout` 运算符来确定。

接下来，我们将探讨 Rx 与世界其他部分之间的边界。