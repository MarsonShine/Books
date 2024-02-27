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

现在，所有通知都来自线程 12，即为 `EventLoopScheduler` 创建的线程。请注意，即使在这里，`Range` 也没有使用该调度程序。不同之处在于，`Range` 默认使用 `CurrentThreadScheduler`，因此无论你从哪个线程调用它，它都会产生输出。因此，尽管它实际上没有使用 `EventLoopScheduler`，但它最终确实使用了该调度程序的线程，因为我们使用了该调度程序来订阅 `Range`。

因此，这说明 `SubscribeOn` 正在履行它的承诺：它确实确定了调用 `Subscribe` 的上下文。只是，这并不总是很重要。如果 `Subscribe` 执行的是非琐碎的工作，它就会变得重要。例如，如果你使用 `Observable.Create` 创建自定义序列，`SubscribeOn` 就会决定调用你传递给 `Create` 的回调的上下文。但 Rx 并没有“当前”调度器的概念--我们无法询问 “我是从哪个调度器调用的？”因此 Rx 操作符不会从它们被订阅的上下文中继承它们的调度器。

就发出值而言，Rx 提供的大多数源可分为三类。首先，响应上游源输入（如 `Where`、`Select` 或 `GroupBy`）而产生输出的操作符通常会在自己的 `OnNext` 中调用其观察者方法。因此，无论源观察符在调用 `OnNext` 时运行在什么上下文中，操作符在调用其观察符时都将使用该上下文。其次，迭代式或基于定时生成项的操作符将使用调度器（明确提供的调度器或未指定调度器时的默认类型）。第三，有些源会根据自己喜欢的上下文生成项目。例如，如果一个异步方法使用 `await` 并指定了 `ConfigureAwait(false)`，那么在 `await` 完成后，它或多或少可以在任何线程和任何上下文中运行，然后可能会继续在观察者上调用 `OnNext`。
只要源遵循 [Rx 序列的基本规则](key-types.md#Rx 序列的基本规则)，它就可以在任何上下文中调用其观察者的方法。它可以选择接受调度程序作为输入并使用它，但没有义务这样做。如果你想驯服这样一个不守规矩的源，这就是 `ObserveOn` 扩展方法的用武之地。请看下面这个相当愚蠢的例子：

```c#
Observable
    .Interval(TimeSpan.FromSeconds(1))
    .SelectMany(tick => Observable.Return(tick, NewThreadScheduler.Default))
    .Subscribe(tick => 
      Console.WriteLine($"{DateTime.Now}-{Environment.CurrentManagedThreadId}: Tick {tick}"));
```

正如输出结果所示，这故意导致每个通知都到达不同的线程：

```
Main thread: 1
21/07/2023 15:19:56-12: Tick 0
21/07/2023 15:19:57-13: Tick 1
21/07/2023 15:19:58-14: Tick 2
21/07/2023 15:19:59-15: Tick 3
...
```

(要做到这一点，需要为 `Interval` 中出现的每个 tick 调用 `Observable.Return`，并告诉 `Return` 使用 `NewThreadScheduler`。每次调用 `Return` 都会创建一个新线程。这是个糟糕的想法，但却是获得每次都从不同上下文调用的源的简单方法）。如果我想强加一些秩序，可以添加对 `ObserveOn` 的调用：

```c#
Observable
    .Interval(TimeSpan.FromSeconds(1))
    .SelectMany(tick => Observable.Return(tick, NewThreadScheduler.Default))
    .ObserveOn(new EventLoopScheduler())
    .Subscribe(tick => 
      Console.WriteLine($"{DateTime.Now}-{Environment.CurrentManagedThreadId}: Tick {tick}"));
```

我在这里创建了一个 `EventLoopScheduler`，因为它会创建一个单独的线程，并在该线程上运行每个计划工作项。现在的输出每次都显示相同的线程 ID（13）：

```
Main thread: 1
21/07/2023 15:24:23-13: Tick 0
21/07/2023 15:24:24-13: Tick 1
21/07/2023 15:24:25-13: Tick 2
21/07/2023 15:24:26-13: Tick 3
...
```

因此，尽管 `Observable.Return` 创建的每个新观察对象都会创建一个全新的线程，但 `ObserveOn` 会确保我的观察对象的 `OnNext`（以及调用 `OnCompleted` 或 `OnError`）通过指定的调度器被调用。

### 在 UI 程序中的 SubscribeOn 和 ObserveOn

如果您在用户界面中使用 Rx，那么当您处理不在用户界面线程上提供通知的信息源时，`ObserveOn` 就会非常有用。您可以使用 `ObserveOn` 封装任何 `IObservable<T>`，并传递 `SynchronizationContextScheduler`（或特定于框架的类型，如 `DispatcherScheduler`），以确保观察者在用户界面线程上接收通知，从而安全地更新用户界面。

`SubscribeOn` 在用户界面中也很有用，它可以确保观察源启动时的初始化工作不会在用户界面线程上进行。

大多数用户界面框架都会指定一个特定的线程，用于接收来自用户的通知和更新任何一个窗口的用户界面。避免阻塞 UI 线程至关重要，因为这样做会导致糟糕的用户体验--如果您正在 UI 线程上执行工作，那么在工作完成之前，该线程将无法响应用户输入。一般来说，如果导致用户界面无响应的时间超过 100 毫秒，用户就会感到恼火，因此不应在用户界面线程上执行任何耗时超过 100 毫秒的工作。当微软首次推出应用商店（随 Windows 8 一起推出）时，他们规定了更严格的限制：如果您的应用程序阻塞 UI 线程的时间超过 50 毫秒，则可能不允许进入商店。以现代处理器的处理能力，50 毫秒就能完成大量处理。即使在移动设备中相对低能耗的处理器上，这个时间也足以执行数百万条指令。不过，任何涉及 I/O（读取或写入文件，或等待任何类型网络服务的响应）的操作都不应在用户界面线程上进行。创建响应式用户界面应用程序的一般模式是

- 接收有关某种用户操作的通知
- 如果慢工作是不可避免的，则在后台线程上完成
- 将结果传回用户界面线程
- 更新用户界面

这非常适合 Rx：响应事件、组合多个事件、向链式方法调用传递数据。有了调度功能，我们甚至可以脱离用户界面线程，然后再回到用户界面线程，从而获得用户所要求的响应式应用程序的感觉。

考虑一个使用 Rx 填充 `ObservableCollection<T>` 的 WPF 应用程序。您可以使用 `SubscribeOn` 来确保主要工作不在 UI 线程上完成，然后使用 `ObserveOn` 来确保您在正确的线程上收到通知。如果您没有使用 `ObserveOn` 方法，那么您的 `OnNext` 处理程序将在发出通知的同一线程上调用。在大多数 UI 框架中，这会导致某种不支持/跨线程异常。在本例中，我们订阅了一系列客户。我使用了 `Defer` 功能，这样如果 `GetCustomers` 在返回其 `IObservable<Customer>` 之前做了任何缓慢的初始工作，在我们订阅之前都不会发生。然后，我们使用 `SubscribeOn` 调用该方法，并在任务池线程上执行订阅。然后，我们确保在收到客户通知时，将其添加到 `Dispatcher` 的客户集合中。

```c#
Observable
    .Defer(() => _customerService.GetCustomers())
    .SubscribeOn(TaskPoolScheduler.Default)
    .ObserveOn(DispatcherScheduler.Instance) 
    .Subscribe(Customers.Add);
```

Rx 还为 `IObservable<T>` 提供了 `SubscribeOnDispatcher()` 和 `ObserveOnDispatcher()` 扩展方法，可自动使用当前线程的 `Dispatcher`（以及 `CoreDispatcher` 的等效方法）。虽然这些方法可能会稍微方便一些，但会增加测试代码的难度。我们将在[测试 Rx](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/16_TestingRx.md)一章中解释原因。

## 并发陷阱

在应用程序中引入并发性会增加其复杂性。如果添加一层并发性并不能明显改善您的应用程序，那么您就应该避免这样做。并发应用程序会在调试、测试和重构方面出现维护问题。

并发带来的常见问题是时间不可预测。系统负载的变化以及系统配置的变化（如不同的内核时钟速度和处理器的可用性）都可能导致时间上的不可预测性。这些因素最终可能导致[死锁](http://en.wikipedia.org/wiki/Deadlock)、[活锁](http://en.wikipedia.org/wiki/Deadlock#Livelock)和状态损坏。

在应用程序中引入并发性的一个特别大的危险是，你可能会悄无声息地引入错误。由不可预测的时间引起的错误是众所周知的难以检测，因此这类缺陷很容易通过开发、质量保证和统一测试（UAT）等环节，而只在生产环境中表现出来。然而，Rx 很好地简化了对可观测序列的并发处理，从而减轻了许多此类问题。虽然仍有可能出现问题，但如果能遵循相关指导原则，就能大大减少不必要的竞赛条件，从而让您感到更加安全。

在后面的章节[测试 Rx](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/16_TestingRx.md)中，我们将介绍 Rx 如何提高并发工作流的测试能力。

### 死锁

Rx 可以简化并发处理，但它并不免疫死锁。一些调用（如 `First`、`Last`、`Single` 和 `ForEach`）是阻塞的，它们在等待某些事件发生之前不会返回。下面的示例说明了这种情况非常容易导致死锁的发生：

```c#
var sequence = new Subject<int>();

Console.WriteLine("Next line should lock the system.");

IEnumerable<int> value = sequence.First();
sequence.OnNext(1);

Console.WriteLine("I can never execute....");
```

在 `First` 方法的源发出序列之前，该方法不会返回。但导致源发出序列的代码在调用 `First` 之后的一行。因此，在 `First` 返回之前，源无法发出序列。这种僵局通常被称为死锁，即双方都无法继续运行，直到另一方继续运行。正如这段代码所示，即使在单线程代码中，也完全有可能出现死锁。事实上，这段代码的单线程特性正是造成死锁的原因：我们有两个操作（等待第一个通知和发送第一个通知），但只有一个单线程。这本不是问题。如果我们使用 `FirstAsync` 并为其附加一个观察者，那么当源 `Subject<int>` 调用其 `OnNext` 时，`FirstAsync` 就会执行其逻辑。但这比调用 `First` 并将结果赋值给变量要复杂得多。

这只是一个过于简化的示例，我们绝不会在生产中编写这样的代码。(即使我们写了，它也会很快地、持续地失败，以至于我们会立即意识到问题所在）。但在实际应用代码中，这类问题可能更难发现。竞争条件通常会在集成点潜入系统，因此问题并不一定存在于某一段代码中：时序问题的出现可能是我们将多段代码连接在一起的结果。

下一个示例可能更难发现，但与第一个不切实际的示例相差无几。基本原理是，我们有一个表示用户界面中按钮点击的主题。用户界面框架会调用代表用户输入的事件处理程序。我们只需向框架提供事件处理程序方法，每当感兴趣的事件（如按钮被点击）发生时，框架就会为我们调用这些方法。这段代码在代表点击的主题上调用了 `First`，但与前面的示例相比，这可能造成的问题并不明显：

```c#
public Window1()
{
    InitializeComponent();
    DataContext = this;
    Value = "Default value";
    
    // 死锁！我们需要调度程序继续允许我点击按钮来产生一个值
    Value = _subject.First();
    
    // 这将达到预期效果，但由于它不会阻塞、
    // 我们可以在用户界面线程上调用此功能，而不会造成死锁。
    //_subject.FirstAsync(1).Subscribe(value => Value = value);
}

private void MyButton_Click(object sender, RoutedEventArgs e)
{
    _subject.OnNext("New Value");
}

public string Value
{
    get { return _value; }
    set
    {
        _value = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
    }
}
```

前面的示例在 `First` 返回后调用了主题的 `OnNext`，因此可以比较直观地看出，如果 `First` 没有返回，那么主题就不会发出通知。但在这里就不那么明显了。`MyButton_Click` 事件处理程序将在调用 `InitializeComponent` 时设置（这在 WPF 代码中很正常），因此显然我们已经完成了必要的设置，使事件得以流动。当我们调用 `First` 时，UI 框架已经知道，如果用户点击了 `MyButton`，它就应该调用 `MyButton_Click`，而该方法将导致主体发出一个值。

使用 `First` 本身并没有什么问题。(是有风险，但在某些情况下，这样的代码是完全没有问题的）。问题在于我们使用它的上下文。**这段代码位于 UI 元素的构造函数中，而这些构造函数总是运行在与该窗口的 UI 元素相关联的特定线程上**。(这恰好是一个 WPF 示例，但其他 UI 框架也是如此）。用户界面框架将使用同一个线程来发送用户输入通知。如果我们阻塞 UI 线程，就会阻止 UI 框架调用按钮点击事件处理程序。因此，这个阻塞调用正在等待一个事件，而这个事件只能从被阻塞的线程中调用，从而造成死锁。

你可能开始觉得，我们应该尽量避免在 Rx 中进行阻塞调用。这是一个很好的经验法则。我们可以通过注释掉使用 First 的那一行，并取消注释下面包含这段代码的那一行，来修复上面的代码：

```
_subject.FirstAsync(1).Subscribe(value => Value = value);
```

它使用 `FirstAsync` 完成相同的工作，但采用了不同的方法。它实现了相同的逻辑，但会返回一个 `IObservable<T>`，如果我们想接收最终出现的第一个值，就必须订阅该 `IObservable<T>`。它比直接将 `First` 的结果赋值给 `Value` 属性要复杂得多，但它能更好地适应我们无法知道该源何时会产生值这一事实。

如果你经常进行用户界面开发，你可能会觉得最后一个例子明显是错误的：我们在一个窗口的构造函数中编写了代码，在用户点击窗口中的按钮之前，构造函数不会完成。在构造完成之前，窗口根本不会出现，因此等待用户点击按钮毫无意义。在构造函数完成之前，按钮甚至都不会在屏幕上显示。此外，经验丰富的用户界面开发人员都知道，不能让世界停止运行并等待用户的特定操作。(即使是模态对话框，也不会阻塞 UI 线程，因为模态对话框在继续之前确实会要求用户做出响应）。但正如下一个示例所示，问题很容易变得难以察觉。在这个示例中，按钮的点击处理程序将尝试从通过接口公开的可观察序列中获取第一个值。

```c#
public partial class Window1 : INotifyPropertyChanged
{
    //Imagine DI here.
    private readonly IMyService _service = new MyService(); 
    private int _value2;

    public Window1()
    {
        InitializeComponent();
        DataContext = this;
    }

    public int Value2
    {
        get { return _value2; }
        set
        {
            _value2 = value;
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(nameof(Value2)));
        }
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    private void MyButton2_Click(object sender, RoutedEventArgs e)
    {
        Value2 = _service.GetTemperature().First();
    }
}
```

与前面的示例不同，这个示例并没有试图在构造函数中阻塞进度。这里对 `First` 的阻塞调用发生在按钮点击处理程序中（靠近结尾的 `MyButton2_Click` 方法）。这个示例比较有趣，因为这种情况并不一定是错误的。应用程序经常在点击处理程序中执行阻塞操作：当我们点击按钮保存文档副本时，我们希望应用程序执行所有必要的 IO 工作，将我们的数据写入存储设备。在现代固态存储设备中，这通常会迅速发生，甚至可以说是瞬间完成，但在机械硬盘驱动器盛行的年代，应用程序在保存文档时出现短暂无响应的情况并不罕见。即使在今天，如果您的存储设备是远程的，并且网络问题导致延迟，这种情况也可能发生。

因此，即使我们已经学会对 `First` 这样的阻塞操作心存疑虑，在这个例子中也有可能是正常的。单看这段代码是无法确定的。这完全取决于 `GetTemperature` 返回什么样的可观测值，以及它生成项的方式。对 `First` 的调用将阻塞 UI 线程，直到第一个项目可用，因此如果第一个项目的生成需要访问 UI 线程，就会产生死锁。下面是一个略显复杂的方法来解决这个问题：

```c#
class MyService : IMyService
{
    public IObservable<int> GetTemperature()
    {
        return Observable.Create<int>(
            o =>
            {
                o.OnNext(27);
                o.OnNext(26);
                o.OnNext(24);
                return () => { };
            })
            .SubscribeOnDispatcher();
    }
}
```

它通过对 `OnNext` 的一系列调用，模拟实际温度传感器的行为。但它做了一些奇怪的显式调度：调用 `SubscribeOnDispatcher`。这是一个扩展方法，可有效调用 `SubscribeOn(DispatcherScheduler.Current.Dispatcher) `。这实际上是告诉 Rx，当有东西试图订阅 `GetTemperature` 返回的 `IObservable<int>` 时，该订阅调用应通过特定于 WPF 的调度器完成，该调度器在 UI 线程上运行其工作项。(严格来说，WPF 确实允许多个 UI 线程，因此更准确地说，只有在 UI 线程上调用该代码时，该代码才会起作用，而且如果这样做，调度程序将确保工作项被调度到同一个 UI 线程上）。

这样做的效果是，当我们的点击处理程序调用 `First` 时，它将反过来订阅 `GetTemperature` 返回的 `IObservable<int>`，由于使用了 `SubscribeOnDispatcher`，因此不会立即调用传递给 `Observable.Create` 的回调。相反，它会调度一个工作项，在用户界面线程（即我们正在运行的线程）空闲时执行调用。现在还不能认为它是空闲的，因为它正在处理按钮点击。将这个工作项交给调度程序后，`Subscribe` 调用返回到 First 方法。现在，`First` 方法坐等第一个项目出现。由于在出现之前它不会返回，因此在出现之前 UI 线程不会被认为是可用的，这意味着本应产生第一个项目的调度工作项目永远无法运行，我们就出现了死锁。

归根结底，这个基本问题与第一个与 `First`-相关的死锁示例相同。我们有两个流程：生成项目和等待项目发生。这两个过程需要同时进行--我们需要“等待第一个条目”逻辑在源发出第一个条目时启动并运行。这些示例都只使用了一个线程，因此使用单个阻塞调用（`First`）来设置观察第一个条目的过程并等待其发生并不是一个好主意。尽管这三种情况的基本问题都是一样的，但随着代码变得越来越复杂，这个问题也就越来越难发现了。在实际应用代码中，通常比这更难发现死锁的根本原因。

到目前为止，本章通过重点介绍并发可能面临的问题，以及这些问题在实践中往往难以发现的事实，似乎在说并发问题都是灾难性的；但这并不是本章的本意。虽然采用 Rx 并不能神奇地避免典型的并发问题，但只要遵循以下两条规则，Rx 就能使并发问题更容易得到解决。

- 只有顶级用户才能做出调度决策
- 避免使用阻塞调用：如 `First`、`Last` 和 `Single`

上一个示例中出现了一个简单的问题：`GetTemperature` 服务决定了调度模型，而实际上它根本就没必要这样做。表示温度传感器的代码不应该知道我正在使用特定的 UI 框架，当然也不应该单方面决定要在 WPF 用户界面线程上运行某些工作。

在开始使用 Rx 时，很容易让自己相信，将调度决策纳入下层在某种程度上是一种“帮助”。“看！”你可能会说。“我不仅提供了温度读数，还让它在用户界面线程上自动通知你，这样你就不用费心使用 `ObserveOn` 了”。出发点可能是好的，但却很容易造成线程噩梦。

只有设置订阅并消耗订阅结果的代码才能全面了解并发需求，因此这才是选择使用哪种调度程序的正确层级。较低层次的代码不应试图参与其中；它们只需按指令行事即可。(可以说，Rx 在需要调度器的地方选择了默认调度器，稍微违反了这一规则。但 Rx 所做的选择非常保守，旨在尽量减少死锁的机会，并始终允许应用程序通过指定调度器来控制调度器）。

请注意，在本例中，遵循上述两条规则中的任何一条都足以防止死锁。但最好同时遵循这两条规则。

这就留下了一个问题：顶级订阅者应该如何做出调度决策？我已经确定了需要做出决策的代码区域，但决策应该是什么呢？这取决于您编写的应用程序的类型。对于用户界面代码来说，这种模式通常效果很好：“在后台线程上订阅；在用户界面线程上观察”。对于用户界面代码来说，出现死锁的风险在于用户界面线程实际上是一个共享资源，而对该资源的争夺会产生死锁。因此，策略是尽可能避免占用该资源：不需要在线程上进行的工作不应在该线程上进行，这就是为什么在工作线程上执行订阅（如使用 `TaskPoolScheduler`）会降低死锁风险。

由此可见，如果您有决定何时产生事件的可观测源（例如计时器或代表外部信息源或设备输入的源），您也希望这些源在工作线程上安排工作。只有当我们需要更新用户界面时，我们才需要在用户界面线程上运行代码，因此我们可以通过将 `ObserveOn` 与合适的用户界面感知调度程序（如 WPF `DispatcherScheduler`）结合使用，将更新推迟到最后一刻。**如果我们有一个由多个操作符组成的复杂 Rx 查询，那么 `ObserveOn` 就应该放在最后，就在我们调用 `Subscribe` 来附加将更新 UI 的处理程序之前。这样，只有最后一步，即更新用户界面，才需要访问用户界面线程。运行时，所有复杂的处理都将完成，因此运行速度会很快，几乎可以立即放弃对用户界面线程的控制，提高应用程序的响应速度，降低死锁的风险。**

其他场景需要其他策略，但处理死锁的一般原则始终是相同的：了解哪些共享资源需要独占访问。例如，如果您有一个传感器库，它可能会创建一个专门的线程来监控设备并报告新的测量结果，如果它规定某些工作必须在该线程上完成，这将与用户界面场景非常相似：您需要避免阻塞的特定线程。同样的方法在这里也可能适用。但这并不是唯一的一种情况。

你可以想象一个数据处理应用程序，其中某些数据结构是共享的。在这种情况下，允许从任何线程访问这些数据结构，但要求每次只能由一个线程访问，这是很常见的。通常，我们会使用线程同步原语来防止并发使用这些关键数据结构。在这种情况下，死锁的风险并不来自特定线程的使用。相反，这些风险来自于这样一种可能性，即一个线程无法取得进展，因为其他线程正在使用共享数据结构，但其他线程正在等待第一个线程做某些事情，并且在此之前不会放弃对该数据结构的锁定。避免问题的最简单方法就是尽可能避免阻塞。避免使用像 `First` 这样的方法，最好使用 `FirstAsync` 这样的非阻塞等价方法。(如果在某些情况下无法避免阻塞，则应尽量避免在使用锁时执行阻塞，因为锁会保护对共享数据的访问。如果确实也无法避免阻塞，那就没有简单的答案了。现在，你必须开始考虑锁的层次结构，以系统地避免死锁，就像不使用 Rx 时一样）。非阻塞风格是使用 Rx 的自然方式，也是 Rx 在这些情况下帮助你避免并发相关问题的主要方法。

## 调度器的高级特性

调度器提供的一些功能主要是在编写需要与调度器交互的可观测源时才会用到。使用调度器的最常见方式是在设置订阅时，或者在创建可观察源时将其作为参数提供，或者将其传递给 `SubscribeOn` 和 `ObserveOn`。 但是，如果您需要编写一个可观察源，它可以按照自己选择的时间表产生项目（例如，假设您正在编写一个表示某些外部数据源的库，并希望将其作为 `IObservable<T>` 来表示），您可能需要使用其中一些更高级的功能。

### 状态传递

`IScheduler` 定义的所有方法都包含一个 `state` 参数。下面是接口定义：

```c#
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

调度器并不关心 `state` 参数中的内容。调度器在执行工作项时，只会把它原封不动地传给你的回调。这提供了一种为回调提供上下文的方法。严格来说，这并不是必须的：作为 `action` 传递的委托可以包含我们需要的任何状态。最简单的方法是在 lambda 中捕获变量。但是，如果查看 [Rx 源代码](https://github.com/dotnet/reactive/)，你会发现它通常不会这样做。例如，`Range` 运算符的核心是一个名为 [LoopRec](https://github.com/dotnet/reactive/blob/95d9ea9d2786f6ec49a051c5cff47dc42591e54f/Rx.NET/Source/src/System.Reactive/Linq/Observable/Range.cs#L55-L73) 的方法：

```c#
var next = scheduler.Schedule(this, static (innerScheduler, @this) => @this.LoopRec(innerScheduler));
```

从逻辑上讲，`Range` 只是一个循环，每产生一个项目就执行一次。但为了实现并发执行并避免堆栈溢出，它将循环的每次迭代作为一个单独的工作项来调度。(该方法之所以称为 `LoopRec`，是因为它在逻辑上是一个递归循环：它通过调用 `Schedule` 来启动，调度程序每次调用该方法时，都会再次调用 `Schedule` 来请求运行下一个项目。实际上，Rx 的任何内置调度程序，甚至是 `ImmediateScheduler`，都不会造成递归，因为它们都能检测到这一点，并安排在当前项目返回后运行下一个项目。但是，如果你编写了一个最简单的调度程序，它实际上会在运行时递归，如果你试图创建一个大的序列，很可能会导致堆栈溢出）。

请注意，传递给 `Schedule` 的 lambda 已被注释为 `static`。**这就告诉 C# 编译器，我们的意图是不捕获任何变量，任何捕获变量的尝试都会导致编译器出错**。这样做的好处是，编译器可以为每次调用生成重复使用相同委托实例的代码。第一次运行时，它将创建一个委托，并将其存储在一个隐藏字段中。在随后的每次执行中（无论是在同一范围的未来迭代中，还是在全新的范围实例中），它都可以一次又一次地使用同一个委托。之所以能做到这一点，是因为委托不捕获任何状态。这就避免了每次循环都分配一个新对象。

难道 Rx 库不能使用更直接的方法吗？我们可以选择不使用状态，向调度器传递一个空状态，然后丢弃传递给回调的状态参数：

```c#
// 效率很低:
var next = scheduler.Schedule<object?>(null, (innerScheduler, _) => LoopRec(innerScheduler));
```

这避免了上一个示例中传递我们自己的 `this` 参数的怪异之处：现在我们只是以普通方式调用 `LoopRec` 实例成员：我们隐式地使用了作用域中的 `this` 引用。因此，这将创建一个委托来捕获隐式 `this` 引用。这种方法可行，但效率不高：它会迫使编译器生成分配多个对象的代码。编译器会创建一个对象，该对象有一个字段，用于捕获 `this`，然后编译器还需要创建一个不同的委托实例，该委托实例有一个指向捕获对象的引用。

`Range` 实现中更复杂的代码可以避免这种情况。它通过在 lambda 中注释 `static` 来禁用捕获。这样代码就不会依赖于隐式的 `this` 引用。因此，它必须为回调安排 `this` 引用。这正是状态参数的作用所在。它提供了一种按工作项目传递状态的方法，从而避免了每次迭代时捕获变量的开销。

### 将来时调度

我在前面谈到了基于时间的操作符，也谈到了 `ISchedule` 的两个基于时间的成员可以实现这一点，但我还没有展示如何使用它。这两个成员可以让你安排在未来执行一个操作。(这依赖于进程继续运行所需的时间。正如前面几章所提到的，`System.Reactive` 不支持持久、耐用的订阅。因此，如果您想在未来几天内安排某些事情，您可能需要考虑一下 [Reaqtor](https://reaqtive.net/)）。您可以通过调用接受 `DateTimeOffset` 的重载 `Schedule` 来指定调用操作的确切时间点，也可以通过基于 `TimeSpan` 的重载来指定调用操作前的等待时间。

您可以这样使用 `TimeSpan` 重载：

```c#
var delay = TimeSpan.FromSeconds(1);
Console.WriteLine("Before schedule at {0:o}", DateTime.Now);

scheduler.Schedule(delay, () => Console.WriteLine("Inside schedule at {0:o}", DateTime.Now));
Console.WriteLine("After schedule at  {0:o}", DateTime.Now);
```

输出：

```
Before schedule at 2012-01-01T12:00:00.000000+00:00
After schedule at 2012-01-01T12:00:00.058000+00:00
Inside schedule at 2012-01-01T12:00:01.044000+00:00
```

这说明这里的调度是非阻塞的，因为 'Before' 和 'After' 的调用在时间上非常接近。(大多数调度程序都是如此，但如前所述，`ImmediateScheduler` 的工作方式有所不同。这就是为什么所有定时运算符默认情况下都不使用 `ImmediateScheduler` 的原因）。您还可以看到，在调度操作约一秒后，该操作被调用。

您可以使用 `DateTimeOffset` 重载指定一个特定的时间点来调度任务。如果由于某种原因，您指定的时间点已经过去，那么将尽快调度该操作。请注意，系统时钟的变化会使问题变得复杂。Rx 的调度程序会对时钟漂移做出一些调整，但系统时钟的突然大幅变化可能会造成短期混乱。

### 取消

`Schedule` 的每个重载都会返回一个 `IDisposable`，调用 `Dispose` 将取消已安排的工作。在前面的示例中，我们计划在一秒钟后调用工作。我们可以通过处置返回值来取消该工作：

```c#
var delay = TimeSpan.FromSeconds(1);
Console.WriteLine("Before schedule at {0:o}", DateTime.Now);

var workItem = scheduler.Schedule(delay, 
   () => Console.WriteLine("Inside schedule at {0:o}", DateTime.Now));

Console.WriteLine("After schedule at  {0:o}", DateTime.Now);

workItem.Dispose();
```

输出：

```
Before schedule at 2012-01-01T12:00:00.000000+00:00
After schedule at 2012-01-01T12:00:00.058000+00:00
```

请注意，计划中的操作从未执行过，因为我们几乎立即取消了它。

当用户在调度程序能够调用之前取消已计划的操作方法时，该操作就会从工作队列中删除。这就是我们在上例中看到的情况。取消已在运行的计划工作是有可能的，这也是工作项回调必须返回 `IDisposable` 的原因：如果当你试图取消工作项时工作已经开始，Rx 会对工作项回调返回的 `IDisposable` 调用 `Dispose`。这就为用户提供了一种取消已在运行的工作的方法。这项工作可能是某种 I/O、繁重的计算，也可能是使用任务执行某些工作。

你可能想知道这种机制有什么用：工作项回调必须已经返回，Rx 才能调用它返回的 `IDisposable`。实际上，只有在返回调度程序后工作仍在继续的情况下，才能使用这种机制。你可以启动另一个线程，让工作同时进行，不过我们一般会尽量避免在 Rx 中创建线程。另一种可能性是，如果调度的工作项调用了某个异步 API，并且没有等待完成就返回了。如果该 API 提供取消功能，则可以返回一个 `IDisposable` 来取消它。

为了说明取消操作，这个略显不切实际的示例以任务的形式运行一些工作，以便在回调返回后继续执行。它只是通过执行旋转等待和向列表参数添加值来伪造一些工作。这里的关键在于，我们创建了一个 `CancellationToken`，以便告诉任务我们希望它停止，然后我们返回一个 `IDisposable`，将此标记置于取消状态。

```c#
public IDisposable Work(IScheduler scheduler, List<int> list)
{
    CancellationTokenSource tokenSource = new();
    CancellationToken cancelToken = tokenSource.Token;
    Task task = new(() =>
    {
        Console.WriteLine();
   
        for (int i = 0; i < 1000; i++)
        {
            SpinWait sw = new();
   
            for (int j = 0; j < 3000; j++) sw.SpinOnce();
   
            Console.Write(".");
   
            list.Add(i);
   
            if (cancelToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation requested");
                
                // cancelToken.ThrowIfCancellationRequested();
                
                return;
            }
        }
    }, cancelToken);
   
    task.Start();
   
    return Disposable.Create(tokenSource.Cancel);
}
```

该代码调度上述代码，允许用户按回车键取消处理工作：

```c#
List<int> list = new();
Console.WriteLine("Enter to quit:");

IDisposable token = scheduler.Schedule(list, Work);
Console.ReadLine();

Console.WriteLine("Cancelling...");

token.Dispose();

Console.WriteLine("Cancelled");
```

输出：

```
Enter to quit:
........
Cancelling...
Cancelled
Cancellation requested
```

这里的问题在于我们引入了任务的显式使用，因此我们正在以一种调度程序无法控制的方式增加并发性。Rx 库通常允许通过接受调度程序参数来控制并发性的引入方式。如果目标是实现长期运行的迭代工作，我们可以使用 Rx 的递归调度器功能来避免启动新线程或任务。我在[传递状态](#状态传递)一节中已经谈到过一些这方面的内容，但还是有几种方法可以做到这一点。

### Recursion

除了 `IScheduler` 方法外，Rx 还以扩展方法的形式定义了 `Schedule` 的各种重载。其中有些扩展方法会将一些看起来很奇怪的委托作为参数。请特别注意 `Schedule` 扩展方法的每个重载中的最后一个参数。

```c#
public static IDisposable Schedule(
    this IScheduler scheduler, 
    Action<Action> action)
{...}

public static IDisposable Schedule<TState>(
    this IScheduler scheduler, 
    TState state, 
    Action<TState, Action<TState>> action)
{...}

public static IDisposable Schedule(
    this IScheduler scheduler, 
    TimeSpan dueTime, 
    Action<Action<TimeSpan>> action)
{...}

public static IDisposable Schedule<TState>(
    this IScheduler scheduler, 
    TState state, 
    TimeSpan dueTime, 
    Action<TState, Action<TState, TimeSpan>> action)
{...}

public static IDisposable Schedule(
    this IScheduler scheduler, 
    DateTimeOffset dueTime, 
    Action<Action<DateTimeOffset>> action)
{...}

public static IDisposable Schedule<TState>(
    this IScheduler scheduler, 
    TState state, DateTimeOffset dueTime, 
    Action<TState, Action<TState, DateTimeOffset>> action)
{...}   
```

这些重载中的每一个都使用一个委托 "action"，允许你递归调用 "action"。这看起来可能是一个非常奇怪的签名，但它允许我们实现与[状态传递](#状态传递)一节中看到的类似的逻辑递归迭代方法，而且可能更简单。

本例使用了最简单的递归重载。我们有一个可以递归调用的 `Action`。

```c#
Action<Action> work = (Action self) =>
{
    Console.WriteLine("Running");
    self();
};

var token = s.Schedule(work);
    
Console.ReadLine();
Console.WriteLine("Cancelling");

token.Dispose();

Console.WriteLine("Cancelled");
```

输出：

```
Enter to quit:
Running
Running
Running
Running
Cancelling
Cancelled
Running
```

请注意，我们无需在委托中编写任何取消代码。Rx 代表我们处理循环并检查取消。由于每个迭代都是作为单独的工作项进行调度的，因此不会有长期运行的工作，让调度程序完全处理取消问题就足够了。

这些重载与直接使用 `IScheduler` 方法的主要区别在于，你不需要直接向调度器传递另一个回调。你只需调用提供的 `Action`，它就会调度对你的方法的另一次调用。此外，如果您不需要状态参数，也可以不传递状态参数。

正如前文所述，虽然这在逻辑上代表了递归，但 Rx 可以保护我们避免堆栈溢出。调度程序在执行递归调用之前，会等待方法返回，从而实现这种递归方式。

调度和线程之旅到此结束。接下来，我们将探讨与之相关的定时话题。