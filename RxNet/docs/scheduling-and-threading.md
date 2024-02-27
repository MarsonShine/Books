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

请注意，所有通知都发生在不同的线程（id 12）上，而不是我们调用 `Subscribe` 的线程（id 1）上。这是因为 `TaskPoolScheduler` 的主要特点是通过任务并行库（TPL）的任务池调用所有工作。这就是我们看到不同线程 ID 的原因：任务池并不拥有我们应用程序的主线程。在这种情况下，它不需要启动多个线程。这很合理，因为这里只有一个源，一次只提供一个项目。在这种情况下，我们没有获得更多线程是件好事--当单线程按顺序处理工作项时，线程池的效率最高，因为这样可以避免上下文切换的开销，而且由于这里没有并发工作的实际范围，如果在这种情况下创建多个线程，我们将一无所获。

这个调度器还有一个非常重要的不同点：注意到在任何通知到达我们的观察者之前，调用 `Subscribe` 就已经返回了。这是因为这是我们看到的第一个引入真正并行性的调度程序。即时调度程序（`ImmediateScheduler`）和当前线程调度程序（`CurrentThreadScheduler`）无论执行的操作员多么希望执行并发操作，都不会自行启动新的线程。虽然 `TaskPoolScheduler` 认为没有必要创建多个线程，但它创建的线程与应用程序的主线程不同，这意味着主线程可以继续与该订阅并行运行。由于 `TaskPoolScheduler` 不会在启动工作的线程上执行任何工作，因此它可以在排队完成工作后立即返回，从而使订阅方法可以立即返回。

如果我们在示例中使用带有嵌套观察对象的 `TaskPoolScheduler` 呢？这只是在内部调用 `Range` 时使用，因此外部调用仍将使用默认的 `CurrentThreadScheduler`：

```c#
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5, TaskPoolScheduler.Default))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

现在，我们可以看到更多的线程参与进来：

```
Received 10 on thread: 13
Received 11 on thread: 13
Received 12 on thread: 13
Received 13 on thread: 13
Received 40 on thread: 16
Received 41 on thread: 16
Received 42 on thread: 16
Received 43 on thread: 16
Received 44 on thread: 16
Received 50 on thread: 17
Received 51 on thread: 17
Received 52 on thread: 17
Received 53 on thread: 17
Received 54 on thread: 17
Subscribe returned
Received 14 on thread: 13
Received 20 on thread: 14
Received 21 on thread: 14
Received 22 on thread: 14
Received 23 on thread: 14
Received 24 on thread: 14
Received 30 on thread: 15
Received 31 on thread: 15
Received 32 on thread: 15
Received 33 on thread: 15
Received 34 on thread: 15
```

由于本例中只有一个观察者，Rx 规则要求观察者一次只能得到一个项目，因此实际上并不存在并行性问题，但与上例相比，更复杂的结构会导致更多的工作项目进入调度程序的队列，这可能就是这次工作由多个线程执行的原因。实际上，这些线程的大部分时间都会被阻塞在 `SelectMany` 内部的代码中，以确保一次只向目标观察者传送一个项目。也许让人有点惊讶的是，这些项目并没有更加混乱。子范围本身似乎是以随机顺序出现的，但它几乎是按顺序在每个子范围内生成项目的（项目 14 是唯一的例外）。这是一个与 `Range` 与 `TaskPoolScheduler` 交互方式有关的怪异现象。

我还没有谈到调度程序的第三项工作：记录时间。`Range` 不会出现这种情况，因为它会尽可能快地生成所有项目。但对于我在[定时调用](#定时调用)部分展示的 `Delay` 运算符来说，时间显然是一个关键因素。事实上，这正是展示调度程序所提供的 API 的好时机：

```
public interface IScheduler
{
    DateTimeOffset Now { get; }
    
    IDisposable Schedule<TState>(TState state, 
                                 Func<IScheduler, TState, IDisposable> action);
    
    IDisposable Schedule<TState>(TState state, 
                                 TimeSpan dueTime, 
                                 Func<IScheduler, TState, IDisposable> action);
    
    IDisposable Schedule<TState>(TState state, 
                                 DateTimeOffset dueTime, 
                                 Func<IScheduler, TState, IDisposable> action);
}
```

可以看出，除了一个重载外，其他所有重载都与时间有关。只有第一个 `Schedule` 重载与时间无关，操作员在调度工作时会调用它，以便在调度程序允许的情况下尽快运行工作。这就是 `Range` 使用的重载。(严格来说，`Range` 会询问调度程序是否支持长时间运行操作，在这种情况下，操作员可以对线程进行长时间的临时控制。在可能的情况下，`Range` 更倾向于使用这种方式，因为这往往比向调度程序提交每一个它希望生成的项目的工作更有效率。`TaskPoolScheduler` 确实支持长时间运行的操作，这也解释了我们前面看到的略微令人惊讶的输出，但 `Range` 默认选择的 `CurrentThreadScheduler` 却不支持。因此，默认情况下，`Range` 会为它希望生成的每一个项目调用一次第一个 `Schedule` 重载。

`Delay` 使用第二个重载。具体的实现相当复杂（主要是因为当繁忙的源导致它落后时，它如何有效地跟上），但实质上，每当一个新的项目到达 `Delay` 操作符时，它就会调度一个工作项目在配置的延迟后运行，这样它就能在预期的时间转移后向其订阅者提供该项目。

调度程序必须负责管理时间，因为 .NET 有几种不同的定时器机制，而定时器的选择往往取决于要处理定时器回调的上下文。由于调度程序决定了工作运行的上下文，这意味着他们也必须选择定时器类型。例如，用户界面框架通常提供在适合更新用户界面的上下文中调用回调的定时器。Rx 提供了一些特定于用户界面框架的调度程序来使用这些定时器，但这些定时器在其他情况下并不合适。因此，每个调度程序都使用适合其运行工作项的上下文的计时器。

这样做还有一个有用的结果：由于 `IScheduler` 为定时相关的细节提供了一个抽象，因此可以将时间虚拟化。这对测试非常有用。如果你查看 [Rx 软件仓库](https://github.com/dotnet/reactive)中的大量测试套件，就会发现其中有许多测试验证了与时间相关的行为。如果这些测试是实时运行的，那么测试套件的运行时间就会太长，而且还可能会产生一些虚假故障，因为与测试在同一台机器上运行的后台任务偶尔会改变执行速度，从而可能会混淆测试。相反，这些测试使用专门的调度程序，可以完全控制时间的流逝。(更多信息，请参阅后面的[测试调度程序](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/11_SchedulingAndThreading.md#test-schedulers)部分，后面还有一[整章的测试内容](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/16_TestingRx.md)）。

请注意，所有三个 `IScheduler.Schedule` 方法都需要回调。调度程序会在它选择的时间和上下文中调用回调。调度器回调的第一个参数是另一个 `IScheduler`。这用于需要重复调用的情况，我们稍后会看到。

Rx 提供多种调度程序。下文将介绍使用最广泛的调度程序。

### ImmediateScheduler

`ImmediateScheduler` 是 Rx 提供的最简单的调度程序。正如你在前面的章节中所看到的，只要要求它调度一些工作，它就会立即运行。这是在 `IScheduler.Schedule` 方法中实现的。

这是一种非常简单的策略，它使 `ImmediateScheduler` 非常高效。因此，许多操作员默认使用 `ImmediateScheduler`。不过，对于即时生成多个项目的操作符来说，这可能会有问题，尤其是当项目数量可能很多时。例如，Rx 为 `IEnumerable<T>` 定义了 `ToObservable` 扩展方法。当您订阅由该方法返回的 `IObservable<T>` 时，它将立即开始遍历集合，如果您让它使用 `ImmediateScheduler`，`Subscribe` 将在到达集合的末尾时才返回。这对于无限序列来说显然是个问题，这也是此类操作符默认不使用 `ImmediateScheduler` 的原因。

当调用使用 `TimeSpan` 的 `Schedule` 重载时，`ImmediateScheduler` 还可能出现令人惊讶的行为。这要求调度程序在指定的时间长度后运行某些工作。它的实现方式是调用 `Thread.Sleep`。对于大多数 Rx 的调度器，这个重载方法会安排某种定时器机制来延迟执行代码，使当前线程可以继续进行其他操作，但是 `ImmediateScheduler` 在这里真正做到了立即执行。它会阻塞当前线程，直到执行工作的时间到来。这意味着如果你指定了这个调度器，像 `Interval` 返回的基于时间的可观察对象仍然可以工作，但代价是阻止线程执行其他任务。

使用 `DateTime` 的 `Schedule` 重载略有不同。如果指定的时间小于未来 10 秒，它将阻塞调用线程，就像使用 `TimeSpan` 时一样。但如果你传递的 `DateTime` 是更远的未来时间，它就会放弃立即执行，转而使用定时器。

### CurrentThreadScheduler

`CurrentThreadScheduler` 与 `ImmediateScheduler` 非常相似。不同之处在于，当当前线程已在处理现有工作项时，它如何处理调度工作的请求。如果将多个使用调度程序执行工作的操作符连锁在一起，就会出现这种情况。

要了解会发生什么，了解快速连续生成多个项目的源是很有帮助的，例如 `IEnumerable<T>` 或 `Observable.Range` 的 `ToObservable` 扩展方法是如何使用调度程序的。这类操作符不使用普通的 `for` 或 `foreach` 循环。它们通常会为每次迭代安排一个新的工作项（除非调度器碰巧为长期运行的工作做出了特殊规定）。`ImmediateScheduler` 会立即运行这些工作，而 `CurrentThreadScheduler` 则会检查它是否已经在处理一个工作项。我们可以从前面的例子中看到这一点：

```c#
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5))
    .Subscribe(
        m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

让我们来看看这里到底发生了什么。首先，假设这段代码只是在正常运行，而不是在任何不寻常的上下文中--也许是在程序的主入口中。当这段代码在 `SelectMany` 返回的 `IObservable<int>` 上调用 `Subscribe` 时，`SelectMany` 将反过来在第一个 `Observable.Range` 返回的 `IObservable<int>` 上调用 `Subscribe`，这将反过来调度一个工作项，用于生成范围中的第一个值 range(1)。

由于我们没有向 `Range` 明确传递调度程序，它将使用默认的 `CurrentThreadScheduler`，并会自问“我是否已在处理此线程上的某个工作项？”在这种情况下，答案将是“否”，因此它会立即运行该工作项（在返回 `Range` 操作符调用的 `Schedule` 之前）。然后，`Range` 操作符将产生它的第一个值，并在 `IObserver<int>` 上调用 `OnNext`，该 `IObserver<int>` 是 `SelectMany` 操作符在订阅 `Range` 时提供的。

`SelectMany` 操作符的 `OnNext` 方法现在将调用其 lambda，并传递所提供的参数（来自 `Range` 操作符的值 1）。从上面的示例中可以看到，这个 lambda 会再次调用 `Observable.Range`，返回一个新的 `IObservable<int>`。`SelectMany` 将立即对此进行订阅（在从其 `OnNext` 返回之前）。这是这段代码第二次调用 `Range` 返回的 `IObservable<int>` 的 `Subscribe`（但与上次不同），`Range` 将再次默认使用 `CurrentThreadScheduler`，并再次调度一个工作项来执行第一次迭代。

因此，`CurrentThreadScheduler` 会再次自问：“我是否已在处理此线程上的某个工作项？”但这次，答案是肯定的。这就是当前线程调度器的行为与即时调度器的不同之处。`CurrentThreadScheduler` 会为每个使用它的线程维护一个工作队列，在这种情况下，它只是将新安排的工作添加到队列中，然后返回到 `SelectMany` 操作符 `OnNext`。

现在，`SelectMany` 已经完成了对第一个 `Range` 中的项（值 `1`）的处理，因此它的 `OnNext` 返回。此时，外部 `Range` 操作符会调度另一个工作项。同样，`CurrentThreadScheduler` 会检测到当前正在运行一个工作项，因此会将其添加到队列中。

在调度了将生成第二个值（`2`）的工作项后，`Range` 运算符返回。请记住，此时 `Range` 运算符中正在运行的代码是第一个已调度工作项的回调，因此它将返回到 `CurrentThreadScheduler`--我们又回到了它的 `Schedule` 方法（由 `Range` 运算符的 `Subscribe` 方法调用）中。

此时，`CurrentThreadScheduler` 不会从 `Schedule` 返回，因为它会检查其工作队列，并会发现队列中现在有两个项目。(其中一个是嵌套 `Range` 观察对象计划生成第一个值的工作项，另一个是顶层 `Range` 观察对象刚刚计划生成第二个值的工作项）。现在，`CurrentThreadScheduler` 将执行其中的第一项：嵌套 `Range` 运算符现在可以生成第一个值（将是 `10`），因此它将调用 `SelectMany` 提供的观察者的 `OnNext`，该观察者将调用其观察者。该观察者将调用我们传递给 `Subscribe` 的 lambda，从而运行 `Console.WriteLine`。返回后，嵌套的 `Range` 操作符将调度另一个工作项来生成第二个工作项。同样，`CurrentThreadScheduler` 会意识到它已经在处理该线程上的一个工作项，因此它只是将其放入队列，然后立即从 `Schedule` 返回。嵌套 `Range` 操作符现在已完成本次迭代，因此它返回调度程序。调度程序现在将拾取队列中的下一个项目，在本例中就是由顶层 `Range` 添加的工作项目，从而产生第二个项目。

如此循环往复。当工作已经在进行时，工作项的这种排队方式使多个可观测源可以并行地进行工作。

相比之下，`ImmediateScheduler` 会立即运行新的工作项，因此我们看不到这种并行进展。

(准确地说，在某些情况下，`ImmediateScheduler` 无法立即运行工作）。在这些迭代场景中，它实际上提供了一个稍有不同的调度程序，操作员用它来调度第一个项目后的所有工作，并检查它是否被要求同时处理多个工作项目。如果是，它就会退回到与 `CurrentThreadScheduler` 类似的队列策略，只不过它是初始工作项的本地队列，而不是每个线程队列。这样可以避免多线程带来的问题，还可以避免迭代操作员在当前工作项的处理程序内调度新工作项时出现堆栈溢出。由于队列不是线程中所有工作的共享队列，因此仍能确保工作项排队的任何嵌套工作在调用 Schedule 返回前完成。因此，即使这种队列启动，我们通常也不会看到来自不同来源的工作交错，就像使用 `CurrentThreadScheduler` 时那样。例如，如果我们让嵌套 `Range` 使用 `ImmediateScheduler`，那么当 `Range` 开始迭代时，这种队列行为就会启动，但由于队列是该嵌套 `Range` 执行的初始工作项的本地队列，因此它最终会在返回前完成所有嵌套 `Range` 项的工作。

### DefaultScheduler

`DefaultScheduler` 适用于需要在一段时间内分散执行的工作，或者可能需要并发执行的工作。这些特性意味着它不能保证在任何特定线程上运行工作，实际上它是通过 CLR 的线程池来调度工作的。这是 Rx 所有基于时间的操作符的默认调度器，也是 `Observable.ToAsync` 操作符的默认调度器，它可以将 .NET 方法封装为 `IObservable<T>`。

如果您希望工作不在当前线程上进行，这个调度器就非常有用--也许您正在编写一个带有用户界面的应用程序，您希望避免在负责更新用户界面和响应用户输入的线程上进行过多的工作。如果你想让所有工作都在一个线程上进行，而不是你现在所在的线程呢？有另一个调度程序可以解决这个问题。

### EventLoopScheduler

`EventLoopScheduler` 提供一次性调度，对新调度的工作项进行排队。这与 `CurrentThreadScheduler` 仅在一个线程中使用时的运行方式类似。不同的是，`EventLoopScheduler` 为这项工作创建了一个专用线程，而不是使用你碰巧调度工作的任意线程。

与我们迄今为止研究过的调度程序不同，`EventLoopScheduler` 没有静态属性。这是因为每个调度器都有自己的线程，所以需要明确创建一个。它提供了两个构造函数：

```c#
public EventLoopScheduler()
public EventLoopScheduler(Func<ThreadStart, Thread> threadFactory)
```

第一种是为你创建线程。第二种可以让你控制线程创建过程。它会调用你提供的回调，并将自己的回调传递给你，要求你在新创建的线程上运行。

`EventLoopScheduler` 实现了 `IDisposable`，调用 `Dispose` 可以终止线程。这可以很好地与 `Observable.Using` 方法配合使用。下面的示例展示了如何使用 `EventLoopScheduler` 在专用线程上遍历 `IEnumerable<T>` 的所有内容，并确保在遍历结束后退出线程：

```c#
IEnumerable<int> xs = GetNumbers();
Observable
    .Using(
        () => new EventLoopScheduler(),
        scheduler => xs.ToObservable(scheduler))
    .Subscribe(...);
```

### NewThreadScheduler

`NewThreadScheduler` 会创建一个新线程来执行给定的每个工作项。这在大多数情况下都没有意义。不过，在需要执行一些长期运行的工作并通过 `IObservable<T>` 表示其完成的情况下，它可能会很有用。`Observable.ToAsync` 正是这样做的，它通常会使用 `DefaultScheduler`，这意味着它会在线程池线程上运行工作。**但如果工作时间可能超过一到两秒，线程池可能就不是一个好的选择，因为它是为短执行时间而优化的，而且其管理线程池大小的启发式设计并没有考虑到长时间运行的操作**。在这种情况下，`NewThreadScheduler` 可能是更好的选择。

虽然每次调用 `Schedule` 都会创建一个新线程，但 `NewThreadScheduler` 会在工作项回调中传递一个不同的调度器，这意味着任何试图执行迭代工作的操作都不会为每次迭代创建一个新线程。例如，如果将 `NewThreadScheduler` 与 `Observable.Range` 结合使用，每次订阅所产生的 `IObservable<int>` 时都会获得一个新线程，但不会为每个项目都创建一个新线程，即使 `Range` 确实为每个生成的值安排了一个新的工作项。它通过工作项回调中提供的嵌套调度器安排这些每个值的工作项，而 `NewThreadScheduler` 在这些情况下提供的嵌套调度器会在同一个线程上执行所有这样的嵌套工作项。

### SynchronizationContextScheduler

这将通过同步上下文调用所有工作。这在用户界面场景中非常有用。大多数 .NET 客户端用户界面框架都提供了一个 `SynchronizationContext`，可用于在适合更改用户界面的上下文中调用回调。(通常这涉及在正确的线程上调用它们，但个别实现可以决定什么是适当的上下文）。

### TaskPoolScheduler

使用 [TPL 任务](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl)通过线程池调用所有工作。TPL 是在 CLR 线程池多年后引入的，现在是通过线程池启动工作的推荐方式。在添加 TPL 时，线程池在通过任务调度工作时使用的算法与使用旧版线程池 API 时使用的算法略有不同。这种更新的算法使其在某些情况下更有效率。现在的文档对此含糊其辞，因此不清楚这些差异在现代 .NET 中是否仍然存在，但任务仍然是使用线程池的推荐机制。出于向后兼容的原因，Rx 的 `DefaultScheduler` 使用了较早的 CLR 线程池 API。在性能至关重要的代码中，如果有大量工作在线程池线程上运行，您可以尝试使用 `TaskPoolScheduler` 来代替，看看它是否能为您的工作负载带来任何性能优势。

### ThreadPoolScheduler

使用 TPL 之前的旧 线程池 API，通过线程池调用所有工作。这种类型是一种历史产物，可以追溯到并非所有平台都提供同类线程池的时代。几乎在所有情况下，如果你想要这种类型所设计的行为，就应该使用 `DefaultScheduler`（尽管 `TaskPoolScheduler` 可能会提供不同的行为）。只有在编写 UWP 应用程序时，使用 `ThreadPoolScheduler` 才会有所区别。`System.Reactive` v6.0 的 UWP 目标为该类提供了与所有其他目标不同的实现。它使用 `Windows.System.Threading.ThreadPool`，而所有其他目标都使用 `System.Threading.ThreadPool`。UWP 版本提供的属性可让你配置 UWP 线程池的一些特定功能。

实际上，在新代码中最好避免使用该类。UWP 目标有不同实现的唯一原因是，UWP 以前不提供 `System.Threading.ThreadPool`。但当 UWP 在 Windows 10.0.19041 版本中添加了对 .NET Standard 2.0 的支持后，情况就发生了变化。现在已经没有任何充分的理由再需要 UWP 专用的 `ThreadPoolScheduler` 了，而且这种类型在 UWP 目标中截然不同，这也是造成混乱的原因之一，但出于向后兼容的目的，它必须保留。(它很可能会被弃用，因为 Rx 7 将解决 `System.Reactive` 组件目前直接依赖 UI 框架这一事实所带来的一些问题）。如果使用 `DefaultScheduler`，无论您在哪个平台上运行，都将使用 `System.Threading.ThreadPool`。

### UI 框架调度器：ControlScheduler, DispatcherScheduler and CoreDispatcherScheduler

尽管 `SynchronizationContextScheduler` 适用于 .NET 中所有广泛使用的客户端 UI 框架，但 Rx 提供了更专业的调度程序。`ControlScheduler` 适用于 Windows 窗体应用程序，`DispatcherScheduler` 适用于 WPF，`CoreDispatcherScheduler` 适用于 UWP。

这些更专业的类型有两个好处。首先，你不一定要在目标 UI 线程上才能获得这些调度器的实例。而对于 `SynchronizationContextScheduler`，一般来说，获得所需的 `SynchronizationContext` 的唯一方法是在 UI 线程上运行时检索 `SynchronizationContext.Current`。但其他这些特定于 UI 框架的调度器可以通过一个合适的 `Control`、`Dispatcher` 或 `CoreDispatcher` 来获取，而这些调度器可以从非 UI 线程中获取。其次，`DispatcherScheduler` 和 `CoreDispatcherScheduler` 提供了一种使用 `Dispatcher` 和 `CoreDispatcher` 类型所支持的优先级机制的方法。

### Test Schedulers

Rx 库定义了几种虚拟化时间的调度程序，包括 `HistoricalScheduler`、`TestScheduler`、`VirtualTimeScheduler` 和 `VirtualTimeSchedulerBase`。我们将在[测试](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/16_TestingRx.md)一章介绍这类调度程序。

## SubscribeOn 和 ObserveOn

到目前为止，我已经讲过为什么有些 Rx 源需要访问调度器。这对于与时间相关的行为以及尽快生产项目的源都是必要的。但请记住，调度器程序控制三件事：

- 确定执行工作的上下文（如某个线程）
- 决定何时执行工作（如立即执行或延迟执行）
- 跟踪时间

迄今为止的讨论主要集中在第二和第三项功能上。当涉及到我们自己的应用代码时，我们最有可能使用调度程序来控制第一个方面。为此，Rx 为 `IObservable<T>` 定义了两个扩展方法： 这两个方法都接收一个 `IScheduler` 并返回一个 `IObservable<T>`，因此您可以在它们的下游链入更多操作符。

这些方法的作用和它们的名字一样。如果使用 `SubscribeOn`，那么当你在生成的 `IObservable<T>` 上调用 `Subscribe` 时，它会通过指定的调度程序来调用原始 `IObservable<T>` 的 `Subscribe` 方法。下面是一个例子：

```c#
Console.WriteLine($"[T:{Environment.CurrentManagedThreadId}] Main thread");

Observable
    .Interval(TimeSpan.FromSeconds(1))
    .SubscribeOn(new EventLoopScheduler((start) =>
    {
        Thread t = new(start) { IsBackground = false };
        Console.WriteLine($"[T:{t.ManagedThreadId}] Created thread for EventLoopScheduler");
        return t;
    }))
    .Subscribe(tick => 
          Console.WriteLine(
            $"[T:{Environment.CurrentManagedThreadId}] {DateTime.Now}: Tick {tick}"));

Console.WriteLine($"[T:{Environment.CurrentManagedThreadId}] {DateTime.Now}: Main thread exiting");
```

这将调用 `Observable.Interval`（默认情况下使用 `DefaultScheduler`），但不是直接订阅，而是首先获取 `Interval` 返回的 `IObservable<T>` 并调用 `SubscribeOn`。 我使用了 `EventLoopScheduler`，并给它传递了一个它将使用的线程的工厂回调，以确保它是一个非后台线程。(默认情况下，`EventLoopScheduler` 会将自己创建为后台线程，这意味着该线程不会强制进程保持活动状态。通常情况下，这是你想要的，但我在本例中改变了这一设置，以显示正在发生的事情）。

当我在 `SubscribeOn` 返回的 `IObservable<long>` 上调用 `Subscribe` 时，它会在我提供的 `EventLoopScheduler` 上调用 `Schedule`，然后在该工作项的回调中，它会在原始 `Interval` 源上调用 `Subscribe`。因此，对底层源的订阅不会在我的主线程中进行，而是在为 `EventLoopScheduler` 创建的线程中进行。运行程序会产生以下输出：

```
[T:1] Main thread
[T:12] Created thread for EventLoopScheduler
[T:1] 21/07/2023 14:57:21: Main thread exiting
[T:6] 21/07/2023 14:57:22: Tick 0
[T:6] 21/07/2023 14:57:23: Tick 1
[T:6] 21/07/2023 14:57:24: Tick 2
...
```

请注意，我的应用程序主线程在源代码开始产生通知之前就退出了。但也请注意，新创建线程的线程 ID 是 12，而我的通知是在另一个线程上发出的，线程 ID 是 6！这是怎么回事？

这种情况经常让人措手不及。订阅可观察源的调度器并不一定会影响源启动和运行后的表现。还记得我之前说过 `Observable.Interval` 默认使用 `DefaultScheduler` 吗？我们在这里没有为 `Interval` 指定调度程序，因此它将使用默认调度程序。它并不关心我们从哪个上下文调用它的 `Subscribe` 方法。因此，在这里引入 `EventLoopScheduler` 的唯一作用就是让进程在主线程退出后仍然存活。调度器线程在对 `Observable.Interval` 返回的 `IObservable<long>` 进行初始 `Subscribe` 调用后，实际上再也不会被使用。它只是耐心地等待着对 `Schedule` 的进一步调用，而这些调用从未出现过。

不过，并非所有源都完全不受调用 `Subscribe` 的上下文的影响。如果我替换这一行

```c#
.Interval(TimeSpan.FromSeconds(1))
```

用下面来替代：

```c#
.Range(1, 5)
```

输出：

```
[T:1] Main thread
[T:12] Created thread for EventLoopScheduler
[T:12] 21/07/2023 15:02:09: Tick 1
[T:1] 21/07/2023 15:02:09: Main thread exiting
[T:12] 21/07/2023 15:02:09: Tick 2
[T:12] 21/07/2023 15:02:09: Tick 3
[T:12] 21/07/2023 15:02:09: Tick 4
[T:12] 21/07/2023 15:02:09: Tick 5
```

