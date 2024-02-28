# 测试 Rx

现代质量保证标准要求进行全面的自动测试，以帮助评估和预防错误。良好的做法是制定一套测试程序来验证行为的正确性，并将其作为构建过程的一部分来运行，以便及早发现问题。

`System.Reactive` 源代码包含了一个全面的测试套件。测试基于 Rx 的代码存在一些挑战，特别是涉及到时间敏感操作符时。Rx.NET的测试套件包括许多测试，旨在测试各种边缘情况，以确保在负载下具有可预测的行为。这只有因为 Rx.NET 被设计为可测试的，才能实现这一点。

在本章中，我们将介绍如何在自己的代码中利用 Rx 的可测试性。

## 虚拟时间

在 Rx 中处理时间问题很常见。正如您所看到的，它提供了多个将时间考虑在内的运算符，这给我们带来了挑战。我们不想引入慢速测试，因为这会导致测试套件执行时间过长，但如果应用程序在提交查询前等待用户停止键入半秒，我们该如何测试呢？非确定性测试也是一个问题：当出现竞赛条件时，很难可靠地重新创建这些条件。

[调度和线程](scheduling-and-threading.md)章节介绍了调度程序如何使用虚拟化的时间表示法。这对于测试验证与时间相关的行为至关重要。它使我们能够控制 Rx 对时间进程的感知，从而编写出逻辑上需要几秒但执行起来却只需几微秒的测试。

请看下面的示例，我们创建了一个序列，在 5 秒钟内每秒发布一次值。

```c#
IObservable<long> interval = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Take(5);
```

要确保以一秒钟的间隔产生五个值，一个天真的测试需要五秒钟的时间来运行。这可不行；我们希望在五秒钟内运行数百个甚至数千个测试。另一个非常常见的需求是测试超时。在这里，我们尝试测试一分钟的超时。

```c#
var never = Observable.Never<int>();
var exceptionThrown = false;

never.Timeout(TimeSpan.FromMinutes(1))
     .Subscribe(
        i => Console.WriteLine("This will never run."),
        ex => exceptionThrown = true);

Assert.IsTrue(exceptionThrown);
```

看起来我们别无选择，只能让测试等待一分钟再运行断言。实际上，我们希望等待一分钟多一点，因为如果运行测试的计算机很忙，可能会比我们要求的时间稍晚一些触发超时。即使被测试的代码没有真正的问题，这种情况也会导致测试偶尔失败。

没有人愿意看到缓慢、不一致的测试。因此，让我们看看 Rx 如何帮助我们避免这些问题。

## TestScheduler

在[调度与线程](scheduling-and-threading.md)一章中，我们了解到调度器决定何时以及如何执行代码，并记录时间。我们在该章中看到的大多数调度程序都解决了各种线程问题，在时间方面，它们都试图在要求的时间运行工作。但 Rx 提供的 `TestScheduler` 对时间的处理方式完全不同。它利用调度程序控制所有与时间有关的行为这一事实，让我们可以模拟和控制时间。

注意：`TestScheduler` 不在 `System.Reactive` 主包中。您需要添加对 `Microsoft.Reactive.Testing` 的引用才能使用它。

任何调度程序都会维护一个待执行操作队列。每个动作都被分配了一个应执行的时间点。(有时这个时间点是“尽快”，但基于时间的操作员通常会将工作安排在未来的某个特定时间运行）。如果我们使用 `TestScheduler`，它就会像时间静止一样，直到我们告诉它我们希望时间继续前进。

在本例中，我们通过使用最简单的 `Schedule` 重载来安排任务立即运行。尽管这实际上是要求尽快运行工作，但 `TestScheduler` 在处理新排队的工作之前，总是会等待我们告诉它我们准备好了。我们将虚拟时钟向前拨动一个刻度，这时它就会执行队列中的工作。(在我们将虚拟时间提前的任何时候，它都会执行所有新排队的“尽快”工作）。如果我们将时间提前了足够长的时间，意味着以前在逻辑上属于未来的工作现在可以运行了，那么它也会运行这些工作）。

```c#
var scheduler = new TestScheduler();
var wasExecuted = false;
scheduler.Schedule(() => wasExecuted = true);
Assert.IsFalse(wasExecuted);
scheduler.AdvanceBy(1); // execute 1 tick of queued actions
Assert.IsTrue(wasExecuted);
```

`TestScheduler` 实现了 `IScheduler` 接口，还定义了允许我们控制和监控虚拟时间的方法。下面显示了这些附加方法：

```c#
public class TestScheduler : // ...
{
    public bool IsEnabled { get; private set; }
    public TAbsolute Clock { get; protected set; }
    public void Start()
    public void Stop()
    public void AdvanceTo(long time)
    public void AdvanceBy(long time)
    
    ...
}
```

`TestScheduler` 的工作单位与 `TimeSpan.Ticks` 相同。如果要将时间向前推移 1 秒，可以调用 `scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks)`。一个 `Ticks` 相当于 100ns，因此 1 秒等于 10,000,000 个 Ticks。

### AdvanceTo

`AdvanceTo(long)` 方法将虚拟时间设置为指定的滴答数(number of ticks)。这将执行所有已计划到指定的绝对时间的操作。`TestScheduler` 使用滴答作为时间的测量单位。在这个例子中，我们安排了立即执行的操作，在 10 个滴答后执行的操作，以及在 20 个滴答后执行的操作（分别为 1 微秒和 2 微秒）。

```c#
var scheduler = new TestScheduler();
scheduler.Schedule(() => Console.WriteLine("A")); // Schedule immediately
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("B"));
scheduler.Schedule(TimeSpan.FromTicks(20), () => Console.WriteLine("C"));

Console.WriteLine("scheduler.AdvanceTo(1);");
scheduler.AdvanceTo(1);

Console.WriteLine("scheduler.AdvanceTo(10);");
scheduler.AdvanceTo(10);

Console.WriteLine("scheduler.AdvanceTo(15);");
scheduler.AdvanceTo(15);

Console.WriteLine("scheduler.AdvanceTo(20);");
scheduler.AdvanceTo(20);
```

输出：

```
scheduler.AdvanceTo(1);
A
scheduler.AdvanceTo(10);
B
scheduler.AdvanceTo(15);
scheduler.AdvanceTo(20);
C
```

请注意，当我们前进到 15 个滴答时，什么也没发生。在 15 个滴答之前安排的所有工作都已完成，而我们还没有推进到足够的程度来执行下一个安排的操作。

### AdvanceBy

`AdvanceBy(long)` 方法允许我们将时钟向前移动一定时间。与 `AdvanceTo` 不同，这里的参数是相对于当前虚拟时间的。同样，测量的单位是 ticks。我们可以将上一个示例修改为使用 `AdvanceBy(long)`。·

```c#
var scheduler = new TestScheduler();
scheduler.Schedule(() => Console.WriteLine("A")); // Schedule immediately
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("B"));
scheduler.Schedule(TimeSpan.FromTicks(20), () => Console.WriteLine("C"));

Console.WriteLine("scheduler.AdvanceBy(1);");
scheduler.AdvanceBy(1);

Console.WriteLine("scheduler.AdvanceBy(9);");
scheduler.AdvanceBy(9);

Console.WriteLine("scheduler.AdvanceBy(5);");
scheduler.AdvanceBy(5);

Console.WriteLine("scheduler.AdvanceBy(5);");
scheduler.AdvanceBy(5);
```

输出：

```
scheduler.AdvanceBy(1);
A
scheduler.AdvanceBy(9);
B
scheduler.AdvanceBy(5);
scheduler.AdvanceBy(5);
C
```

### Start

`TestScheduler` 的 `Start()` 方法会运行所有已调度的工作，并根据需要将排队等待特定时间的工作项的虚拟时间提前。我们再举一个同样的例子，将 `AdvanceBy(long)` 调用换成单一的 `Start()` 调用。

```c#
var scheduler = new TestScheduler();
scheduler.Schedule(() => Console.WriteLine("A")); // Schedule immediately
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("B"));
scheduler.Schedule(TimeSpan.FromTicks(20), () => Console.WriteLine("C"));

Console.WriteLine("scheduler.Start();");
scheduler.Start();

Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
```

输出：

```
scheduler.Start();
A
B
C
scheduler.Clock:20
```

请注意，一旦所有计划的操作都执行完毕，虚拟时钟就会与最后一个计划项目（20 个 ticks）相匹配。

我们还可以进一步扩展示例，在调用 `Start()` 后安排一个新的操作。

```c#
var scheduler = new TestScheduler();
scheduler.Schedule(() => Console.WriteLine("A"));
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("B"));
scheduler.Schedule(TimeSpan.FromTicks(20), () => Console.WriteLine("C"));

Console.WriteLine("scheduler.Start();");
scheduler.Start();

Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);

scheduler.Schedule(() => Console.WriteLine("D"));
```

输出：

```
scheduler.Start();
A
B
C
scheduler.Clock:20
```

请注意，输出结果完全相同；如果我们想执行第四个操作，就必须再次调用 `Start()` （或 `AdvanceTo` 或 `AdvanceBy`）。

### Stop

有一个 `Stop()` 方法，它的名字似乎暗示了与 `Start()` 方法的某种对称性。该方法会将调度程序的 `IsEnabled` 属性设置为 `false`，如果 `Start` 正在运行，这就意味着它将停止检查队列中的其他工作，并在当前正在处理的工作项完成后立即返回。

在本示例中，我们将展示如何使用 `Stop()` 来暂停处理已计划的操作。

```c#
var scheduler = new TestScheduler();
scheduler.Schedule(() => Console.WriteLine("A"));
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("B"));
scheduler.Schedule(TimeSpan.FromTicks(15), scheduler.Stop);
scheduler.Schedule(TimeSpan.FromTicks(20), () => Console.WriteLine("C"));

Console.WriteLine("scheduler.Start();");
scheduler.Start();
Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
```

输出：

```
scheduler.Start();
A
B
scheduler.Clock:15
```

请注意，"C"永远不会被打印出来，因为我们会将时钟停在 15 ticks 处。

由于`Start`会在耗尽工作队列后自动停止，因此您没有义务调用 `Stop`。只有当你想调用 `Start`，但又想在测试中途暂停处理时，才会调用 `Stop`。

### Schedule 碰撞

在安排操作时，有可能甚至很有可能在同一时间点安排多个操作。最常见的情况是为现在安排多个操作。也有可能在未来的同一时刻安排多个操作。`TestScheduler` 有一种简单的方法来处理这种情况。在安排操作时，它们会被标上安排的时间。如果在同一时间点安排了多个项目，它们将按照安排的顺序排队；当时钟前进时，该时间点的所有项目将按照顺序执行。

```c#
var scheduler = new TestScheduler();
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("A"));
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("B"));
scheduler.Schedule(TimeSpan.FromTicks(10), () => Console.WriteLine("C"));

Console.WriteLine("scheduler.Start();");
scheduler.Start();
Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
```

输出：

```
scheduler.AdvanceTo(10);
A
B
C
scheduler.Clock:10
```

请注意，虚拟时钟是 10 ticks，也就是我们前进到的时间。

## 测试 Rx 代码

既然我们已经对 `TestScheduler` 有了一些了解，那么让我们来看看如何使用它来测试使用 `Interval` 和 `Timeout` 的两个初始代码片段。我们希望尽可能快地执行测试，但仍要保持时间的语义。在这个示例中，我们生成的五个值相隔一秒，但我们将 `TestScheduler` 传递给 `Interval` 方法使用，而不是默认的调度程序。

```c#
[TestMethod]
public void Testing_with_test_scheduler()
{
    var expectedValues = new long[] {0, 1, 2, 3, 4};
    var actualValues = new List<long>();
    var scheduler = new TestScheduler();

    var interval = Observable.Interval(TimeSpan.FromSeconds(1), scheduler).Take(5);
    
    interval.Subscribe(actualValues.Add);

    scheduler.Start();
    CollectionAssert.AreEqual(expectedValues, actualValues);
    // Executes in less than 0.01s "on my machine"
}
```

虽然这有点意思，但我认为更重要的是我们如何测试一段真正的代码。请想象一下，一个订阅价格流的 ViewModel。当价格发布时，它会将它们添加到一个集合中。假设这是一个 WPF 实现，我们可以冒昧地强制要求在 `ThreadPool` 上完成订阅，并在 `Dispatcher` 上执行观察。

```c#
public class MyViewModel : IMyViewModel
{
    private readonly IMyModel _myModel;
    private readonly ObservableCollection<decimal> _prices;

    public MyViewModel(IMyModel myModel)
    {
        _myModel = myModel;
        _prices = new ObservableCollection<decimal>();
    }

    public void Show(string symbol)
    {
        // TODO: resource mgt, exception handling etc...
        _myModel.PriceStream(symbol)
                .SubscribeOn(Scheduler.ThreadPool)
                .ObserveOn(Scheduler.Dispatcher)
                .Timeout(TimeSpan.FromSeconds(10), Scheduler.ThreadPool)
                .Subscribe(
                    Prices.Add,
                    ex=>
                        {
                            if(ex is TimeoutException)
                                IsConnected = false;
                        });
        IsConnected = true;
    }

    public ObservableCollection<decimal> Prices
    {
        get { return _prices; }
    }

    public bool IsConnected { get; private set; }
}
```

### 注入调度器依赖

虽然上面的代码段可以实现我们想要的功能，但由于它是通过静态属性访问调度程序，因此很难进行测试。在测试过程中，你需要某种方法让测试提供不同的调度程序。在本例中，我们将为此定义一个接口：

```c#
public interface ISchedulerProvider
{
    IScheduler CurrentThread { get; }
    IScheduler Dispatcher { get; }
    IScheduler Immediate { get; }
    IScheduler NewThread { get; }
    IScheduler ThreadPool { get; }
    IScheduler TaskPool { get; } 
}
```

我们在生产中运行的默认执行方式如下：

```c#
public sealed class SchedulerProvider : ISchedulerProvider
{
    public IScheduler CurrentThread => Scheduler.CurrentThread;
    public IScheduler Dispatcher => DispatcherScheduler.Instance;
    public IScheduler Immediate => Scheduler.Immediate;
    public IScheduler NewThread => Scheduler.NewThread;
    public IScheduler ThreadPool => Scheduler.ThreadPool;
    public IScheduler TaskPool => Scheduler.TaskPool;
}
```

我们可以替换 `ISchedulerProvider` 的实现来帮助测试。例如

```c#
public sealed class TestSchedulers : ISchedulerProvider
{
    // Schedulers available as TestScheduler type
    public TestScheduler CurrentThread { get; }  = new TestScheduler();
    public TestScheduler Dispatcher { get; }  = new TestScheduler();
    public TestScheduler Immediate { get; }  = new TestScheduler();
    public TestScheduler NewThread { get; }  = new TestScheduler();
    public TestScheduler ThreadPool { get; }  = new TestScheduler();
    
    // ISchedulerService needs us to return IScheduler, but we want the properties
    // to return TestScheduler for the convenience of test code, so we provide
    // explicit implementations of all the properties to match ISchedulerService.
    IScheduler ISchedulerProvider.CurrentThread => CurrentThread;
    IScheduler ISchedulerProvider.Dispatcher => Dispatcher;
    IScheduler ISchedulerProvider.Immediate => Immediate;
    IScheduler ISchedulerProvider.NewThread => NewThread;
    IScheduler ISchedulerProvider.ThreadPool => ThreadPool;
}
```

请注意，`ISchedulerProvider` 是显式实现的，因为该接口要求每个属性都返回一个 `IScheduler`，但我们的测试需要直接访问 `TestScheduler` 实例。现在我可以为 ViewModel 写一些测试了。下面，我们将测试 `MyViewModel` 类的一个修改版本，该版本将接收一个 `ISchedulerProvider` 并使用它来代替来自 `Scheduler` 类的静态调度器。我们还使用了流行的 [Moq](https://github.com/Moq) 框架来为我们的模型提供一个合适的假实现。

```c#
[TestInitialize]
public void SetUp()
{
    _myModelMock = new Mock<IMyModel>();
    _schedulerProvider = new TestSchedulers();
    _viewModel = new MyViewModel(_myModelMock.Object, _schedulerProvider);
}

[TestMethod]
public void Should_add_to_Prices_when_Model_publishes_price()
{
    decimal expected = 1.23m;
    var priceStream = new Subject<decimal>();
    _myModelMock.Setup(svc => svc.PriceStream(It.IsAny<string>())).Returns(priceStream);

    _viewModel.Show("SomeSymbol");
    
    // Schedule the OnNext
    _schedulerProvider.ThreadPool.Schedule(() => priceStream.OnNext(expected));  

    Assert.AreEqual(0, _viewModel.Prices.Count);

    // Execute the OnNext action
    _schedulerProvider.ThreadPool.AdvanceBy(1);  
    Assert.AreEqual(0, _viewModel.Prices.Count);
    
    // Execute the OnNext handler
    _schedulerProvider.Dispatcher.AdvanceBy(1);  
    Assert.AreEqual(1, _viewModel.Prices.Count);
    Assert.AreEqual(expected, _viewModel.Prices.First());
}

[TestMethod]
public void Should_disconnect_if_no_prices_for_10_seconds()
{
    var timeoutPeriod = TimeSpan.FromSeconds(10);
    var priceStream = Observable.Never<decimal>();
    _myModelMock.Setup(svc => svc.PriceStream(It.IsAny<string>())).Returns(priceStream);

    _viewModel.Show("SomeSymbol");

    _schedulerProvider.ThreadPool.AdvanceBy(timeoutPeriod.Ticks - 1);
    Assert.IsTrue(_viewModel.IsConnected);
    _schedulerProvider.ThreadPool.AdvanceBy(timeoutPeriod.Ticks);
    Assert.IsFalse(_viewModel.IsConnected);
}
```

输出：

```
2 passed, 0 failed, 0 skipped, took 0.41 seconds (MSTest 10.0).
```

这两个测试确保以下五个方面：

- 当模型生成价格时，`Price` 属性会将其添加进来。
- 序列会在线程池上进行订阅。
- `Price` 属性会在调度器上进行更新，即序列会在调度器上进行观察。
- 10 秒之间没有价格更新会将 ViewModel 设置为断开连接状态。
- 测试运行速度快。

尽管运行测试的时间并不令人印象深刻，但大部分时间似乎都花在了测试工具的初始化上。此外，将测试数量增加到 10 增加了 0.03 秒的时间。通常情况下，现代的 CPU 应该能够每秒执行数千个单元测试。

在第一个测试中，只有在 `ThreadPool` 和 `Dispatcher` 调度器都运行完毕后，我们才能得到结果。在第二个测试中，它有助于验证超时时间不少于 10 秒。

在某些情况下，您可能对调度器不感兴趣，并且希望将测试重点放在其他功能上。如果是这种情况，您可以创建另一个实现了 `ISchedulerProvider` 接口的测试实现，其中所有成员都返回 `ImmediateScheduler`。这有助于减少测试中的干扰噪音。

```c#
public sealed class ImmediateSchedulers : ISchedulerService
{
    public IScheduler CurrentThread => Scheduler.Immediate;
    public IScheduler Dispatcher => Scheduler.Immediate;
    public IScheduler Immediate => Scheduler.Immediate;
    public IScheduler NewThread => Scheduler.Immediate;
    public IScheduler ThreadPool => Scheduler.Immediate;
}
```

## 高级特性 - ITestableObserver

`TestScheduler` 提供更多高级功能。当测试设置的某些部分需要在特定虚拟时间运行时，这些功能会非常有用。

### `Start(Func<IObservable<T>>)`

`Start` 有三个重载，分别用于在给定时间启动一个可观察序列、记录其发出的通知以及在给定时间处理订阅。起初可能会让人感到困惑，因为 `Start` 的无参重载与此无关。这三个重载都会返回一个 `ITestableObserver<T>`，它允许你记录来自可观察序列的通知，就像我们在[转换](transformation-sequences.md)一章中看到的 `Materialize` 方法一样。

```c#
public interface ITestableObserver<T> : IObserver<T>
{
    // Gets recorded notifications received by the observer.
    IList<Recorded<Notification<T>>> Messages { get; }
}
```

虽然有三种重载，但我们先看最具体的一种。该重载需要四个参数：

- 一个可观察序列工厂委托
- 调用工厂的时间点
- 订阅工厂返回的可观察序列的时间点
- 取消订阅的时间点

后三个参数的时间单位是 ticks，与 `TestScheduler` 其他成员一样。

```c#
public ITestableObserver<T> Start<T>(
    Func<IObservable<T>> create, 
    long created, 
    long subscribed, 
    long disposed)
{...}
```

我们可以用这个方法来测试 `Observable.Interval` 工厂方法。在这里，我们创建了一个可观察序列，每秒产生一个值用时 4 秒。我们使用 `TestScheduler.Start` 方法创建并立即订阅该序列（第二和第三个参数均为 0）。5 秒后，我们将取消订阅。一旦 `Start` 方法运行完毕，我们将输出所记录的内容。

```c#
var scheduler = new TestScheduler();
var source = Observable.Interval(TimeSpan.FromSeconds(1), scheduler)
    .Take(4);

var testObserver = scheduler.Start(
    () => source, 
    0, 
    0, 
    TimeSpan.FromSeconds(5).Ticks);

Console.WriteLine("Time is {0} ticks", scheduler.Clock);
Console.WriteLine("Received {0} notifications", testObserver.Messages.Count);

foreach (Recorded<Notification<long>> message in testObserver.Messages)
{
    Console.WriteLine("{0} @ {1}", message.Value, message.Time);
}
```

输出：

```
Time is 50000000 ticks
Received 5 notifications
OnNext(0) @ 10000001
OnNext(1) @ 20000001
OnNext(2) @ 30000001
OnNext(3) @ 40000001
OnCompleted() @ 40000001
```

请注意，`ITestObserver<T>` 会记录 `OnNext` 和 `OnCompleted` 通知。如果序列因错误而终止，`ITestObserver<T>` 将记录 `OnError` 通知。

我们可以使用输入变量来查看其影响。我们知道，`Observable.Interval` 方法是冷观察对象，因此创建的虚拟时间并不重要。更改订阅的虚拟时间会改变我们的结果。如果我们将其改为 2 秒，就会发现如果将取消时间设为 5 秒，就会错过一些信息。

```c#
var testObserver = scheduler.Start(
    () => Observable.Interval(TimeSpan.FromSeconds(1), scheduler).Take(4), 
    0,
    TimeSpan.FromSeconds(2).Ticks,
    TimeSpan.FromSeconds(5).Ticks);
```

输出：

```
Time is 50000000 ticks
Received 2 notifications
OnNext(0) @ 30000000
OnNext(1) @ 40000000
```

我们在 2 秒时开始订阅；`Interval` 在每秒（即第 3 秒和第 4 秒）后产生值，我们在第 5 秒时进行取消。因此，我们错过了另外两条 `OnNext` 消息和 `OnCompleted` 消息。

`TestScheduler.Start` 方法还有两个重载。

```c#
public ITestableObserver<T> Start<T>(Func<IObservable<T>> create, long disposed)
{
    if (create == null)
    {
        throw new ArgumentNullException("create");
    }
    else
    {
        return this.Start<T>(create, 100L, 200L, disposed);
    }
}

public ITestableObserver<T> Start<T>(Func<IObservable<T>> create)
{
    if (create == null)
    {
        throw new ArgumentNullException("create");
    }
    else
    {
        return this.Start<T>(create, 100L, 200L, 1000L);
    }
}
```

如您所见，这些重载只是调用我们上面一直在研究的变量，但传递了一些默认值。这些默认值在创建之前以及创建和订阅之间提供了短暂的间隙，为在它们之间发生其他事情提供了足够的配置空间。然后在稍后的时间进行处理，为运行留出更长的时间。这些默认值并没有什么特别神奇的地方，但如果你看重的是不杂乱，而不是什么时候会发生什么，并且乐于依赖约定俗成的隐形效果，那么你可能会更喜欢这样。Rx 源代码本身包含成千上万个测试，其中很多都使用了最简单的 `Start` 重载，日复一日在代码库中工作的开发人员很快就会习惯于这样的想法：创建发生在时间 100，订阅发生在时间 200，在 1000 之前测试所有需要测试的内容。

### CreateColdObservable

正如我们可以记录一个可观察序列一样，我们也可以使用 `CreateColdObservable` 来回放一组 `Recorded<Notification<int>>` 。`CreateColdObservable` 的签名只需接收一个记录通知的 `params` 数组。

```c#
// 从通知数组中创建冷观察对象。
// 返回显示指定消息行为的冷观察对象。
public ITestableObservable<T> CreateColdObservable<T>(
    params Recorded<Notification<T>>[] messages)
{...}
```

`CreateColdObservable` 返回一个 `ITestableObservable<T>`。该接口扩展了 `IObservable<T>`，公开了“订阅”列表和它将产生的消息列表。

```c#
public interface ITestableObservable<T> : IObservable<T>
{
    // Gets the subscriptions to the observable.
    IList<Subscription> Subscriptions { get; }

    // Gets the recorded notifications sent by the observable.
    IList<Recorded<Notification<T>>> Messages { get; }
}
```

使用 `CreateColdObservable`，我们可以模拟之前的 `Observable.Interval` 测试。

```c#
var scheduler = new TestScheduler();
var source = scheduler.CreateColdObservable(
    new Recorded<Notification<long>>(10000000, Notification.CreateOnNext(0L)),
    new Recorded<Notification<long>>(20000000, Notification.CreateOnNext(1L)),
    new Recorded<Notification<long>>(30000000, Notification.CreateOnNext(2L)),
    new Recorded<Notification<long>>(40000000, Notification.CreateOnNext(3L)),
    new Recorded<Notification<long>>(40000000, Notification.CreateOnCompleted<long>())
    );

var testObserver = scheduler.Start(
    () => source,
    0,
    0,
    TimeSpan.FromSeconds(5).Ticks);

Console.WriteLine("Time is {0} ticks", scheduler.Clock);
Console.WriteLine("Received {0} notifications", testObserver.Messages.Count);

foreach (Recorded<Notification<long>> message in testObserver.Messages)
{
    Console.WriteLine("  {0} @ {1}", message.Value, message.Time);
}
```

输出：

```
Time is 50000000 ticks
Received 5 notifications
OnNext(0) @ 10000001
OnNext(1) @ 20000001
OnNext(2) @ 30000001
OnNext(3) @ 40000001
OnCompleted() @ 40000001
```

请注意，我们的输出结果与之前使用 `Observable.Interval` 的示例完全相同。

### CreateHotObservable

我们还可以使用 `CreateHotObservable` 方法创建热测试观察序列。它的参数和返回值与 `CreateColdObservable` 相同；不同之处在于，为每条信息指定的虚拟时间现在是相对于创建的时间，而不是 `CreateColdObservable` 方法中订阅的时间。

本示例只是最后一个“冷”示例，但创建的是热观测值。

```c#
var scheduler = new TestScheduler();
var source = scheduler.CreateHotObservable(
    new Recorded<Notification<long>>(10000000, Notification.CreateOnNext(0L)),
// ...    
```

输出：

```
Time is 50000000 ticks
Received 5 notifications
OnNext(0) @ 10000000
OnNext(1) @ 20000000
OnNext(2) @ 30000000
OnNext(3) @ 40000000
OnCompleted() @ 40000000
```

请注意，输出结果几乎相同。创建和订阅的调度不会影响热观测值，因此通知发生的时间比冷观测值早 1 tick。

通过将虚拟创建时间和虚拟订阅时间改为不同的值，我们可以看到热观察对象的主要区别。对于冷观察对象，虚拟创建时间没有实际影响，因为订阅才是启动任何操作的关键。这意味着我们不会错过冷观察对象上的任何早期信息。而对于热观测对象，如果我们订阅得太晚，就有可能错过信息。在这里，我们立即创建热观测对象，但只在 1 秒后才订阅（因此错过了第一条信息）。

```c#
var scheduler = new TestScheduler();
var source = scheduler.CreateHotObservable(
    new Recorded<Notification<long>>(10000000, Notification.CreateOnNext(0L)),
    new Recorded<Notification<long>>(20000000, Notification.CreateOnNext(1L)),
    new Recorded<Notification<long>>(30000000, Notification.CreateOnNext(2L)),
    new Recorded<Notification<long>>(40000000, Notification.CreateOnNext(3L)),
    new Recorded<Notification<long>>(40000000, Notification.CreateOnCompleted<long>())
    );

var testObserver = scheduler.Start(
    () => source,
    0,
    TimeSpan.FromSeconds(1).Ticks,
    TimeSpan.FromSeconds(5).Ticks);

Console.WriteLine("Time is {0} ticks", scheduler.Clock);
Console.WriteLine("Received {0} notifications", testObserver.Messages.Count);

foreach (Recorded<Notification<long>> message in testObserver.Messages)
{
    Console.WriteLine("  {0} @ {1}", message.Value, message.Time);
}
```

输出：

```
Time is 50000000 ticks
Received 4 notifications
OnNext(1) @ 20000000
OnNext(2) @ 30000000
OnNext(3) @ 40000000
OnCompleted() @ 40000000
```

### CreateObserver

最后，如果您不想使用 `TestScheduler.Start` 方法，并且需要对观察者进行更精细的控制，您可以使用 `TestScheduler.CreateObserver()`。这将返回一个 `ITestObserver`，您可以用它来管理对可观察序列的订阅。此外，您仍然可以看到记录的消息和任何订阅者。

当前的行业标准要求自动化单元测试的覆盖面要广，以满足质量保证标准。然而，并发编程往往是一个难以很好测试的领域。Rx 提供了精心设计的测试功能，允许进行确定性和高吞吐量测试。`TestScheduler` 提供了控制虚拟时间和生成可观察测试序列的方法。这种轻松可靠地测试并发系统的能力使 Rx 有别于许多其他库。