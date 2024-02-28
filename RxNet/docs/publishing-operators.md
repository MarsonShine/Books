# 发布操作符

热源需要能够向多个订阅者传递事件。虽然我们可以自己实现订阅者跟踪，但编写一个仅适用于单个订阅者的过度简化的源可能更容易。尽管这不是 `IObservable<T>` 的完整实现，但如果我们使用 Rx 的多播运算符之一将其发布为多个订阅者的热源，那就无关紧要了。在[Rx 中表示文件系统事件的样例](creating-observable-sequences.md#在 Rx 中表示文件系统事件)中使用了这个技巧，但正如你在本章中将看到的，这个主题有一些变化。

## Multicast

Rx 提供了三种操作符，使我们只需订阅一个底层源就能支持多个订阅者：发布（`Publish`）、最后发布（`PublishLast`）和重播（`Replay`）。这三个操作符都是 Rx 的多播操作符的封装器，而多播操作符则是所有操作符的核心。

`Multicast` 将任何 `IObservable<T>` 变成 `IConnectableObservable<T>`，如你所见，这只是增加了一个 `Connect` 方法：

```c#
public interface IConnectableObservable<out T> : IObservable<T>
{
    IDisposable Connect();
}
```

由于它派生自 `IObservable<T>`，因此可以在 `IConnectableObservable<T>` 上调用 `Subscribe`，但 `Multicast` 返回的实现不会在调用时在底层源上调用 `Subscribe`。**只有当你调用 `Connect` 时，它才会在底层源上调用 `Subscribe`**。为了能够看到这一点，让我们定义一个源，每次调用 `Subscribe` 时，它都会打印出一条消息：

```c#
IObservable<int> src = Observable.Create<int>(obs =>
{
    Console.WriteLine("Create callback called");
    obs.OnNext(1);
    obs.OnNext(2);
    obs.OnCompleted();
    return Disposable.Empty;
});
```

由于无论有多少观察者订阅，该方法都只会被调用一次，因此 `Multicast` 无法传递给自己的 `Subscribe` 方法中传递的 `IObserver<T>`，因为可能有任意数量的观察者。它使用 `Subject` 作为传递给底层源的单个 `IObserver<T>`，该 `Subject` 还负责跟踪所有订阅者。如果我们直接调用 `Multicast`，则需要传入我们要使用的 `Subject`：

```c#
IConnectableObservable<int> m = src.Multicast(new Subject<int>());
```

我们现在可以多次订阅：

```c#
m.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
m.Subscribe(x => Console.WriteLine($"Sub2: {x}"));
m.Subscribe(x => Console.WriteLine($"Sub3: {x}"));
```

除非我们调用 `Connect`，否则这些用户都不会收到任何信息：

```c#
m.Connect();
```

**注意：**`Connect` 返回一个 `IDisposable`。调用 `Dispose` 会从底层源取消订阅。

调用 `Connect` 会导致以下输出：

```
Create callback called
Sub1: 1
Sub2: 1
Sub3: 1
Sub1: 2
Sub2: 2
Sub3: 2
```

正如你所看到的，我们传递给 `Create` 的方法只运行了一次，这证实了 `Multicast` 只订阅了一次，尽管我们调用了三次 `Subscribe`。但每个项目都被发送到了所有三个订阅中。

`Multicast` 的工作方式相当简单：它让 `Subject` 完成大部分工作。每当你对 `Multicast` 返回的可观察对象调用 `Subscribe` 时，它就会对 `Subject` 调用 `Subscribe`。而当你调用 `Connect` 时，它只是将 `Subject` 传递给底层源的 `Subscribe`。因此，这段代码会产生相同的效果：

```c#
var s = new Subject<int>();

s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));
s.Subscribe(x => Console.WriteLine($"Sub3: {x}"));

src.Subscribe(s);
```

不过，`Multicast` 的一个优点是它会返回 `IConnectableObservable<T>`，正如我们稍后将看到的，Rx 的一些其他部分知道如何使用这个接口。

`Multicast` 提供了一种工作方式完全不同的重载：它适用于需要编写两次使用源可观察对象的查询的情况。例如，我们可能想使用 `Zip` 获取相邻的项目对：

```c#
IObservable<(int, int)> ps = src.Zip(src.Skip(1));
ps.Subscribe(ps => Console.WriteLine(ps));
```

(虽然 [Buffer](operation-partitioning.md#Buffer) 看起来是一种更明显的方法，但这种 `Zip` 方法的一个优点是，它永远不会给我们对的一半。当我们要求 `Buffer` 提供成对的数据时，当我们到达终点时，它会给我们一个单项缓冲区，这可能需要额外的代码来解决）。

这种方法的问题在于，源会看到两个订阅：一个直接来自 `Zip`，另一个通过 `Skip`。如果我们运行上面的代码，就会看到这样的输出：

```
Create callback called
Create callback called
(1, 2)
```

我们的 `Create` 回调运行了两次。第二个 `Multicast` 重载可以避免这种情况：

```c#
IObservable<(int, int)> ps = src.Multicast(() => new Subject<int>(), s => s.Zip(s.Skip(1)));
ps.Subscribe(ps => Console.WriteLine(ps));
```

正如输出结果所示，这避免了多重订阅：

```
Create callback called
(1, 2)
```

`Multicast` 的重载返回一个普通的 `IObservable<T>`。这意味着我们不需要调用 `Connect`。但这也意味着，对生成的 `IObservable<T>` 的每次订阅都会导致对底层源的订阅。但就其设计目的而言，这并无大碍：我们只是想避免获得两倍于底层源的订阅。

本节中定义的其余操作符（`Publish`、`PublishLast` 和 `Replay`）都是 `Multicast` 的包装器，每个都为您提供了特定类型的 `Subject`。

### Publish

`Publish` 操作符使用 [Subject<T>](creating-observable-sequences.md#Subject<T>) 调用 `Multicast`。这样做的效果是，一旦在结果上调用了 `Connect`，源生成的任何项目都将传递给所有订阅者。这样，我就可以替换之前的示例了：

```c#
IConnectableObservable<int> m = src.Multicast(new Subject<int>());
```

使用下面代码：

```c#
IConnectableObservable<int> m = src.Publish();
```

这两者完全等同。

由于 `Subject<T>` 会立即将所有进入的 `OnNext` 调用转发给它的每个订阅者，而且它不会存储任何先前进行的调用，因此结果是一个热源。如果您在调用 `Connect` 之前附加了一些订阅者，然后又在调用 `Connect` 之后附加了更多的订阅者，那么这些后来的订阅者将只接收他们订阅后发生的事件。本例对此进行了演示：

```c#
IConnectableObservable<long> publishedTicks = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Take(4)
    .Publish();

publishedTicks.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
publishedTicks.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));

publishedTicks.Connect();
Thread.Sleep(2500);
Console.WriteLine();
Console.WriteLine("Adding more subscribers");
Console.WriteLine();

publishedTicks.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
publishedTicks.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));
```

下面的输出显示，我们只看到 Sub3 和 Sub4 订阅最后两个事件的输出：

```
Sub1: 0 (10/08/2023 16:04:02)
Sub2: 0 (10/08/2023 16:04:02)
Sub1: 1 (10/08/2023 16:04:03)
Sub2: 1 (10/08/2023 16:04:03)

Adding more subscribers

Sub1: 2 (10/08/2023 16:04:04)
Sub2: 2 (10/08/2023 16:04:04)
Sub3: 2 (10/08/2023 16:04:04)
Sub4: 2 (10/08/2023 16:04:04)
Sub1: 3 (10/08/2023 16:04:05)
Sub2: 3 (10/08/2023 16:04:05)
Sub3: 3 (10/08/2023 16:04:05)
Sub4: 3 (10/08/2023 16:04:05)
```

与 [Multicast](#Multicast) 一样，`Publish` 也提供了按顶层订阅组播的重载。这样，我们就可以简化该节末尾的示例：

```c#
IObservable<(int, int)> ps = src.Multicast(() => new Subject<int>(), s => s.Zip(s.Skip(1)));
ps.Subscribe(ps => Console.WriteLine(ps));
```

等价于：

```c#
IObservable<(int, int)> ps = src.Publish(s => s.Zip(s.Skip(1)));
ps.Subscribe(ps => Console.WriteLine(ps));
```

`Publish` 提供了可以指定初始值的重载。这些重载使用 [BehaviorSubject<T>](creating-observable-sequences.md#BehaviorSubject<T>) 代替 `Subject<T>`。这里的区别在于，所有订阅者一旦订阅，就会立即收到一个值。如果底层源尚未生成一个项目（或者如果 `Connect` 尚未被调用，这意味着我们甚至尚未订阅源），他们将收到初始值。如果从源接收到至少一个项目，任何新的订阅者都将立即收到源产生的最新值，然后继续接收任何新值。

### PublishLast

`PublishLast` 操作符使用 [AsyncSubject<T>](creating-observable-sequences.md#AsyncSubject<T>) 调用 `Multicast`。这样做的效果是，源生成的最终项目将被传递给所有订阅者。您仍然需要调用 `Connect`。这将决定何时发生对底层源的订阅。但是，无论何时订阅，所有订阅者都将收到最终事件，因为 `AsyncSubject<T>` 会记住最终结果。我们可以通过下面的示例看到这一点：

```c#
IConnectableObservable<long> pticks = Observable
    .Interval(TimeSpan.FromSeconds(0.1))
    .Take(4)
    .PublishLast();

pticks.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
pticks.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));

pticks.Connect();
Thread.Sleep(1000);
Console.WriteLine();
Console.WriteLine("Adding more subscribers");
Console.WriteLine();

pticks.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
pticks.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));
```

这将创建一个在 0.4 秒内产生 4 个值的源。它将几个订阅者附加到 `PublishLast` 返回的 `IConnectableObservable<T>` 上，然后立即调用 `Connect`。然后，它会休眠 1 秒钟，这为源完成提供了时间。这意味着，在调用 `Thread.Sleep` 返回之前，前两个订阅者将收到它们将收到的唯一值（序列中的最后一个值）。但我们接着又附加了两个订阅器。正如输出所显示的，这两个订阅器也会收到同样的最后事件：

```
Sub1: 3 (11/14/2023 9:15:46 AM)
Sub2: 3 (11/14/2023 9:15:46 AM)

Adding more subscribers

Sub3: 3 (11/14/2023 9:15:49 AM)
Sub4: 3 (11/14/2023 9:15:49 AM)
```

最后两个订阅者收到值的时间较晚，因为他们订阅的时间较晚，但 `PublishLast` 创建的 `AsyncSubject<T>` 只是向这些较晚的订阅者重放它收到的最终值。

### Replay

`Replay` 操作符使用 [ReplaySubject<T>](creating-observable-sequences.md#ReplaySubject<T>) 调用 `Multicast`。这样做的效果是，在调用 `Connect` 之前连接的任何订阅者都只能接收底层源产生的所有事件，但之后连接的任何订阅者都能有效地“赶上”，因为 `ReplaySubject<T>` 会记住它已经看到的事件，并将其重播给新的订阅者。

这个示例与用于 `Publish` 的示例非常相似：

```c#
IConnectableObservable<long> pticks = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Take(4)
    .Replay();

pticks.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
pticks.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));

pticks.Connect();
Thread.Sleep(2500);
Console.WriteLine();
Console.WriteLine("Adding more subscribers");
Console.WriteLine();

pticks.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
pticks.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));
```

这将创建一个 4 秒钟内定期产生项目的源。在调用 `Connect` 之前，它会附加两个订阅者。然后，它会等待足够长的时间，等前两个事件出现后，再附加两个订阅者。但与 `Publish` 不同的是，这些较晚的订阅者将看到他们订阅之前发生的事件：

```
Sub1: 0 (10/08/2023 16:18:22)
Sub2: 0 (10/08/2023 16:18:22)
Sub1: 1 (10/08/2023 16:18:23)
Sub2: 1 (10/08/2023 16:18:23)

Adding more subscribers

Sub3: 0 (10/08/2023 16:18:24)
Sub3: 1 (10/08/2023 16:18:24)
Sub4: 0 (10/08/2023 16:18:24)
Sub4: 1 (10/08/2023 16:18:24)
Sub1: 2 (10/08/2023 16:18:24)
Sub2: 2 (10/08/2023 16:18:24)
Sub3: 2 (10/08/2023 16:18:24)
Sub4: 2 (10/08/2023 16:18:24)
Sub1: 3 (10/08/2023 16:18:25)
Sub2: 3 (10/08/2023 16:18:25)
Sub3: 3 (10/08/2023 16:18:25)
Sub4: 3 (10/08/2023 16:18:25)
```

当然，它们收到得比较晚，因为它们订阅得比较晚。因此，当 `sub3` 和 `sub4` 赶上进度时，我们会看到快速报告的事件，但一旦它们赶上进度，就会立即收到所有其他事件。

启用这种行为的 `ReplaySubject<T>` 将消耗内存来存储事件。您可能还记得，这种主体类型可以配置为只存储有限数量的事件，或不保留超过某个指定时限的事件。`Replay` 操作符提供的重载功能可以让您配置这些限制。

`Replay` 还支持我在本节中为其他基于组播的操作符所展示的按订阅组播模式。

## RefCount

我们在上一节中看到，`Multicast`（及其各种封装器）支持两种使用模式：

- 返回 `IConnectableObservable<T>`，以允许对何时订阅底层源进行顶层控制
- 返回一个普通的 `IObservable<T>`，使我们在使用在多个地方使用源的查询（例如，`s.Zip(s.Take(1))`）时，可以避免不必要地多次订阅源，但仍要为每个顶层 `Subject` 调用一次底层源

`RefCount` 提供了一种略有不同的模式。它允许通过普通 `Subscribe` 触发对底层源的订阅，但仍可以只对底层源进行一次调用。这在本书中使用的 AIS 示例中可能很有用。您可能希望将多个订阅者附加到一个可观察源，该源可报告船只和其他船只广播的位置信息，但您通常希望提供基于 Rx API 的库只连接一次提供这些信息的任何底层服务。而且您很可能希望它只在至少有一个订阅者在监听时进行连接。`RefCount` 是实现这一点的理想工具，因为它能让单个消息源支持多个订阅者，并让底层消息源知道我们何时在“无订阅者”和“至少有一个订阅者”两种状态之间切换。

为了观察 `RefCount` 如何操作，我将使用一个修改过的源版本，在订阅发生时报告订阅情况：

```c#
IObservable<int> src = Observable.Create<int>(async obs =>
{
    Console.WriteLine("Create callback called");
    obs.OnNext(1);
    await Task.Delay(250).ConfigureAwait(false);
    obs.OnNext(2);
    await Task.Delay(250).ConfigureAwait(false);
    obs.OnNext(3);
    await Task.Delay(250).ConfigureAwait(false);
    obs.OnNext(4);
    await Task.Delay(100).ConfigureAwait(false);
    obs.OnCompleted();
});
```

与前面的示例不同，该示例使用 `async` 并在每个 `OnNext` 之间延迟，以确保主线程在生成所有项目之前有时间设置多个订阅。然后，我们可以用 `RefCount` 将其封装：

```c#
IObservable<int> rc = src
    .Publish()
    .RefCount();
```

请注意，我必须先调用 `Publish`。这是因为 `RefCount` 期望使用 `IConnectableObservable<T>`。它希望只有在有第一个订阅者时才启动源。一旦有至少一个订阅者，它就会调用 `Connect`。让我们试试看：

```c#
rc.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
rc.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
Thread.Sleep(600);
Console.WriteLine();
Console.WriteLine("Adding more subscribers");
Console.WriteLine();
rc.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
rc.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));
```

下面是输出：

```
Create callback called
Sub1: 1 (10/08/2023 16:36:44)
Sub1: 2 (10/08/2023 16:36:45)
Sub2: 2 (10/08/2023 16:36:45)
Sub1: 3 (10/08/2023 16:36:45)
Sub2: 3 (10/08/2023 16:36:45)

Adding more subscribers

Sub1: 4 (10/08/2023 16:36:45)
Sub2: 4 (10/08/2023 16:36:45)
Sub3: 4 (10/08/2023 16:36:45)
Sub4: 4 (10/08/2023 16:36:45)
```

请注意，只有 `Sub1` 接收到第一个事件。这是因为传给 `Create` 的回调立即产生了第一个事件。只有当它调用第一个 `await` 时，才会返回给调用者，使我们能够附加第二个订阅者。它已经错过了第一个事件，但正如你所看到的，它接收到了第二个和第三个事件。代码等待了足够长的时间，等前三个事件发生后才附加另外两个订阅者，可以看到所有四个订阅者都收到了最后一个事件。

顾名思义，`RefCount` 计算的是活跃订阅者的数量。如果这个数字降到 0，它将对 `Connect` 返回的对象调用 `Dispose`，关闭订阅。如果有更多订阅者加入，它将重新启动。本例说明了这一点：

```c#
IDisposable s1 = rc.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
IDisposable s2 = rc.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
Thread.Sleep(600);

Console.WriteLine();
Console.WriteLine("Removing subscribers");
s1.Dispose();
s2.Dispose();
Thread.Sleep(600);
Console.WriteLine();

Console.WriteLine();
Console.WriteLine("Adding more subscribers");
Console.WriteLine();
rc.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
rc.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));
```

输出：

```
Create callback called
Sub1: 1 (10/08/2023 16:40:39)
Sub1: 2 (10/08/2023 16:40:39)
Sub2: 2 (10/08/2023 16:40:39)
Sub1: 3 (10/08/2023 16:40:39)
Sub2: 3 (10/08/2023 16:40:39)

Removing subscribers


Adding more subscribers

Create callback called
Sub3: 1 (10/08/2023 16:40:40)
Sub3: 2 (10/08/2023 16:40:40)
Sub4: 2 (10/08/2023 16:40:40)
Sub3: 3 (10/08/2023 16:40:41)
Sub4: 3 (10/08/2023 16:40:41)
Sub3: 4 (10/08/2023 16:40:41)
Sub4: 4 (10/08/2023 16:40:41)
```

这一次，`Create` 回调运行了两次。这是因为活跃订阅者的数量降到了 0，所以 `RefCount` 调用了 `Dispose` 关闭了一切。当有新的订阅者出现时，它会再次调用 `Connect` 来重新启动。有一些重载可以让你指定一个 `disconnectDelay`（断开延迟）。这会告诉它在用户数量降为零后等待指定时间再断开连接，以查看是否有新用户加入。但如果指定时间已过，它仍会断开连接。如果这不是你想要的，下一个操作符可能会适合你。

## AutoConnect

`AutoConnect` 操作符的行为方式与 `RefCount` 基本相同，即当第一个订阅者订阅时，它会在其底层 `IConnectableObservable<T>` 上调用 `Connect`。不同之处在于，它不会试图检测活动用户数量何时降为零：一旦连接，即使没有用户，也会无限期地保持连接。

虽然 `AutoConnect` 很方便，但你需要小心一点，因为它可能会造成泄漏：它永远不会自动断开连接。你仍然可以切断它创建的连接：`AutoConnect` 接受一个 `Action<IDisposable>` 类型的可选参数。它在第一次连接到源时会调用该参数，并将源的 `Connect` 方法返回的 `IDisposable` 传递给你。您可以调用 `Dispose` 关闭它。

本章中的操作符对处理多个订阅者的源非常有用。它提供了多种方法来附加多个订阅者，同时只触发对底层源的单个 `Subscribe`。