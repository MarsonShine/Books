# 调度和线程

Rx 主要是一个异步处理运动数据的系统。如果我们要处理多个信息源，它们很可能同时生成数据。在处理数据时，我们可能需要一定程度的并行性，以实现我们的可扩展性目标。我们需要对系统的这些方面进行控制。

到目前为止，我们已经成功地避免了对线程或并发性的明确使用。我们已经看到了一些必须处理时序才能完成工作的方法。(例如，`Buffer`）、`Delay`和 `Sample` 必须安排工作按特定时间表进行）。然而，我们一直依赖于默认行为，虽然默认行为往往能满足我们的要求，但有时我们需要进行更多的控制。本章将介绍 Rx 的调度系统，它为管理这些问题提供了一种优雅的方法。

## Rx，线程和并发

Rx 并不限制我们使用哪些线程。`IObservable<T>` 可以自由地在任何线程上调用其订阅者的 `OnNext/Completed/Error` 方法，也许每次调用都会使用不同的线程。尽管如此，Rx 有一个方面可以防止混乱：可观察源在任何情况下都必须遵守 [Rx 序列基本规则](key-types.md#Rx 序列的基本规则)。

当我们第一次探索这些规则时，我们关注的是它们如何决定调用任何单一观察者的顺序。对 `OnNext` 的调用次数不限，但一旦调用了 `OnError` 或 `OnCompleted`，就不得再调用。但是，既然我们正在研究并发性，这些规则的另一个方面就变得更加重要：对于任何单个订阅，可观察源都不得并发调用该订阅的观察者。因此，如果一个源调用 `OnNext`，它必须等到调用返回后才能再次调用 `OnNext`，或者调用 `OnError` 或 `OnComplete` 也是如此。

对观察者来说，这样做的好处是，只要观察者只参与一个订阅，它就只能同时处理一件事。如果它所订阅的源是一个涉及许多不同操作符的冗长而复杂的处理链，那也没有关系。即使您通过组合多个输入（例如，使用 [Merge](operation-combination.md#Merge)）来构建该源，基本规则也要求，如果您只在单个 `IObservable<T>` 上调用了一次 `Subscribe`，那么该源永远不允许多次并发调用您的 `IObserver<T>` 方法。

因此，尽管每次调用都可能在不同的线程上进行，但调用都是严格按顺序进行的（除非单个观察者涉及多个订阅）。

接收传入通知并生成通知的 Rx 操作符将在传入通知恰好到达的线程上通知其观察者。假设您有这样一串操作符：

```c#
source
    .Where(x => x.MessageType == 3)
    .Buffer(10)
    .Take(20)
    .Subscribe(x => Console.WriteLine(x));
```

当调用 `Subscribe` 时，我们会得到一连串的观察者。Rx 提供的观察者将调用我们的回调，并传递给 `Take` 返回的观察者，而 `Take` 返回的观察者又会创建一个观察者，订阅 `Buffer` 返回的观察者，而 `Buffer` 返回的观察者又会创建一个观察者，订阅 `Where` 观察者，而 `Where` 观察者又会创建另一个观察者，订阅 `source` 观察者。

因此，当源决定生成一个项目时，它将调用 `Where` 操作符的观察者的 `OnNext`。这将调用谓词，如果消息类型确实是 3，`Where` 观察者将调用 `Buffer` 观察者的 `OnNext`，并在同一线程上进行。在 `Buffer` 观察者的 `OnNext` 返回之前，`Where` 观察者的 `OnNext` 不会返回。现在，如果 `Buffer` 观察者确定它已经完全填满了一个缓冲区（例如，它刚刚收到第 10 个项目），那么它也不会返回--它会调用 `Take` 观察器的 `OnNext`，只要 `Take` 还没有收到 20 个缓冲区，它就会在 Rx 提供的观察器上调用 `OnNext`，该观察器将调用我们的回调。

因此，如果源通知一直传递到 `subscribe` 的回调中的 `Console.WriteLine`，我们就会在堆栈上产生大量嵌套调用：

```
`source` calls:
  `Where` observer, which calls:
    `Buffer` observer, which calls:
      `Take` observer, which calls:
        `Subscribe` observer, which calls our lambda
```

这一切都发生在一个线程上。大多数 Rx 操作员都没有自己的特定线程。它们只是在调用进来的任何线程上完成工作。这使得 Rx 非常高效。从一个操作符到下一个操作符的数据传递只涉及方法调用，而方法调用是非常快的（事实上，通常还有更多层。Rx 往往会添加一些封装器来处理错误和早期退订。因此，调用栈看起来会比我刚才展示的复杂一些。但通常仍只是方法调用）。

有时，你会听到 Rx 被描述为具有自由线程模型。这只是说，操作员通常并不关心他们使用的是哪个线程。正如我们将要看到的，虽然也有例外情况，但这种由一个操作员直接调用下一个操作员的情况是普遍存在的。

这样做的结果是，通常是由原始源决定使用哪个线程。下一个示例通过创建一个主题，然后在不同线程上调用 `OnNext` 并报告线程 ID 来说明这一点。

```c#
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
var subject = new Subject<string>();

subject.Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

object sync = new();
ParameterizedThreadStart notify = arg =>
{
    string message = arg?.ToString() ?? "null";
    Console.WriteLine(
        $"OnNext({message}) on thread: {Environment.CurrentManagedThreadId}");
    lock (sync)
    {
        subject.OnNext(message);
    }
};

notify("Main");
new Thread(notify).Start("First worker thread");
new Thread(notify).Start("Second worker thread");
```

输出：

```
Main thread: 1
OnNext(Main) on thread: 1
Received Main on thread: 1
OnNext(First worker thread) on thread: 10
Received First worker thread on thread: 10
OnNext(Second worker thread) on thread: 11
Received Second worker thread on thread: 11
```

在每种情况下，传给 `Subscribe` 的处理程序都会在调用 `subject.OnNext` 的同一线程上被调用。这样做既简单又高效。然而，事情并不总是这么简单。

## 定时调用

有些通知并不是来源提供项目的直接结果。例如，Rx 提供了一个 `Defer` 操作符，它可以对项目的交付进行时间上的转移。下一个示例基于上一个示例，主要区别在于我们不再直接订阅源。我们通过延迟操作：

```c#
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
var subject = new Subject<string>();

subject
    .Delay(TimeSpan.FromSeconds(0.25))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

object sync = new();
ParameterizedThreadStart notify = arg =>
{
    string message = arg?.ToString() ?? "null";
    Console.WriteLine(
        $"OnNext({message}) on thread: {Environment.CurrentManagedThreadId}");
    lock (sync)
    {
        subject.OnNext(message);
    }
};

notify("Main 1");
Thread.Sleep(TimeSpan.FromSeconds(0.1));
notify("Main 2");
Thread.Sleep(TimeSpan.FromSeconds(0.3));
notify("Main 3");
new Thread(notify).Start("First worker thread");
Thread.Sleep(TimeSpan.FromSeconds(0.1));
new Thread(notify).Start("Second worker thread");

Thread.Sleep(TimeSpan.FromSeconds(2));
```

这也会在发送源项目之间等待一段时间，因此我们可以看到延迟的效果。下面是输出结果：

```
Main thread: 1
OnNext(Main 1) on thread: 1
OnNext(Main 2) on thread: 1
Received Main 1 on thread: 12
Received Main 2 on thread: 12
OnNext(Main 3) on thread: 1
OnNext(First worker thread) on thread: 13
OnNext(Second worker thread) on thread: 14
Received Main 3 on thread: 12
Received First worker thread on thread: 12
Received Second worker thread on thread: 12
```

请注意，在这种情况下，每条 `Received` 的消息都位于线程 id 12 上，与发出通知的三个线程中的任何一个都不同。

这并不奇怪。在这里，Rx 使用原始线程的唯一方法是 `Delay` 在转发调用之前阻塞线程一段时间（这里是四分之一秒）。这在大多数情况下都是无法接受的，因此 `Defer` 操作符会在适当延迟后安排回调。从输出中可以看到，这些似乎都发生在一个特定的线程上。无论哪个线程调用 `OnNext`，延迟通知都会到达线程 id 12。但这并不是 `Defer` 操作符创建的线程。出现这种情况是因为 Delay 使用了调度器。

## 调度器

调度器要做三件事：

- 确定执行工作的上下文（如某个线程）
- 决定何时执行工作（如立即执行或延迟执行）
- 跟踪时间

下面我们通过一个简单的例子来探讨其中的前两项：

```c#
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");

Observable
    .Range(1, 5)
    .Subscribe(m => 
      Console.WriteLine(
        $"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

Console.WriteLine("Subscribe returned");
Console.ReadLine();
```

我们可能不清楚这是否与调度有关，但事实上，`Range` 总是使用调度器来完成工作。我们只是让它使用默认的调度器。下面是输出结果：

```
Main thread: 1
Received 1 on thread: 1
Received 2 on thread: 1
Received 3 on thread: 1
Received 4 on thread: 1
Received 5 on thread: 1
Subscribe returned
```

看看调度器工作列表中的前两项，我们可以发现，调度程序执行工作的上下文是我调用 `Subscribe` 的线程。至于它决定执行工作的时间，它决定在 `Subscribe` 返回之前完成所有工作。因此，你可能会认为 `Range` 会立即生成我们要求的所有项目，然后返回。然而，事情并非如此简单。让我们看看如果有多个 `Range` 实例同时运行会发生什么。这会引入一个额外的操作符：再次调用 `Range` 的 `SelectMany`：

```
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5))
    .Subscribe(m => 
      Console.WriteLine(
        $"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

输出结果显示，`Range` 事实上并不一定会立即生产出所有物品：

```
Received 10 on thread: 1
Received 11 on thread: 1
Received 20 on thread: 1
Received 12 on thread: 1
Received 21 on thread: 1
Received 30 on thread: 1
Received 13 on thread: 1
Received 22 on thread: 1
Received 31 on thread: 1
Received 40 on thread: 1
Received 14 on thread: 1
Received 23 on thread: 1
Received 32 on thread: 1
Received 41 on thread: 1
Received 50 on thread: 1
Received 24 on thread: 1
Received 33 on thread: 1
Received 42 on thread: 1
Received 51 on thread: 1
Received 34 on thread: 1
Received 43 on thread: 1
Received 52 on thread: 1
Received 44 on thread: 1
Received 53 on thread: 1
Received 54 on thread: 1
Subscribe returned
```

由 `SelectMany` 回调生成的第一个嵌套 `Range` 产生了几个值（10 和 11），但第二个嵌套 `Range` 在第一个嵌套 `Range` 产生第三个值（12）之前，设法得到了第一个值（20）。你可以看到这里有一些进度交错。因此，虽然执行工作的上下文仍然是我们调用 `Subscribe` 的线程，但调度程序必须做出的第二个选择--何时执行工作--比起最初看起来要更加微妙。这告诉我们，`Range` 并不像这个简单的天真实现那么简单：

```c#
public static IObservable<int> NaiveRange(int start, int count)
{
    return System.Reactive.Linq.Observable.Create<int>(obs =>
    {
        for (int i = 0; i < count; i++)
        {
            obs.OnNext(start + i);
        }

        return Disposable.Empty;
    });
}
```

如果 `Range` 是这样工作的，那么这段代码就会从 `SelectMany` 回调返回的第一个范围中生成所有项目，然后再转到下一个范围。事实上，Rx 确实提供了一个调度程序，如果我们想要的话，它就能为我们提供这种行为。本示例将 `ImmediateScheduler.Instance` 传递给嵌套的 `Observable.Range` 调用：

```c#
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5, ImmediateScheduler.Instance))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

输出：

```
Received 10 on thread: 1
Received 11 on thread: 1
Received 12 on thread: 1
Received 13 on thread: 1
Received 14 on thread: 1
Received 20 on thread: 1
Received 21 on thread: 1
Received 22 on thread: 1
Received 23 on thread: 1
Received 24 on thread: 1
Received 30 on thread: 1
Received 31 on thread: 1
Received 32 on thread: 1
Received 33 on thread: 1
Received 34 on thread: 1
Received 40 on thread: 1
Received 41 on thread: 1
Received 42 on thread: 1
Received 43 on thread: 1
Received 44 on thread: 1
Received 50 on thread: 1
Received 51 on thread: 1
Received 52 on thread: 1
Received 53 on thread: 1
Received 54 on thread: 1
Subscribe returned
```

通过在对 `Observable.Range` 的最内层调用中指定 `ImmediateScheduler.Instance`，我们要求使用一种特殊策略：在调用者的线程上调用所有工作，而且总是立即调用。这不是 `Range` 的默认设置，原因有以下几点。(它的默认值是 `Scheduler.CurrentThread`，它总是返回一个 `CurrentThreadScheduler` 实例）。首先，`ImmediateScheduler.Instance` 最终会导致相当深的调用堆栈。大多数其他调度器都维护工作队列，因此，如果一个运算符决定在另一个运算符正在执行某个操作的中间阶段（例如，一个嵌套的 `Range` 运算符决定开始发出其值），它不会立即开始工作（这将涉及调用执行工作的方法），而是将工作放入队列中，让正在进行的工作完成后再开始下一个工作。在所有地方使用立即调度器可能会在查询变得复杂时导致堆栈溢出。`Range` 不使用立即调度器的默认值的第二个原因是，当多个可观察对象同时活动时，它们都可以取得一些进展。`Range` 尽快生成其所有项，因此如果不使用启用运算符轮流执行工作的调度器，它可能会使其他运算符在 CPU 时间方面饥饿。

请注意，在这两个示例中，`Subscribe` 返回的消息都是最后出现的。因此，尽管当前线程调度器不像即时调度器那样急切，但它在完成所有未完成的工作之前，仍不会返回给调用者。它维护着一个工作队列，使公平性稍有提高，并避免了堆栈溢出，但只要有任何事情要求当前线程调度器去做，它就不会返回，直到耗尽其队列。

并非所有调度程序都具有这种特性。下面是前面例子的一个变体，其中我们只调用了 `Range` 一次，没有任何嵌套的可观测变量。这次我要求它使用 `TaskPoolScheduler`。

```c#
Observable
    .Range(1, 5, TaskPoolScheduler.Default)
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

从它的输出中我们可以看到，与立即线程调度器和当前线程调度器相比，它对运行工作的上下文做出了不同的决定：

```
Main thread: 1
Subscribe returned
Received 1 on thread: 12
Received 2 on thread: 12
Received 3 on thread: 12
Received 4 on thread: 12
Received 5 on thread: 12
```

