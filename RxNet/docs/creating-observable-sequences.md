# 创建可观察序列

在上一章中，我们了解了两个基本的 Rx 接口：`IObservable<T>` 和 `IObserver<T>`。我们还了解了如何通过实现 `IObserver<T>` 以及使用 `System.Reactive` 包提供的实现来接收事件。在本章中，我们将了解如何创建 `IObservable<T>` 源来表示应用程序中感兴趣的源事件。

首先，我们将直接实现 `IObservable<T>`。在实践中，这样做是比较少见的，因此我们接下来将了解如何让 `System.Reactive` 提供一种实现，为您完成大部分工作。

## 非常基本的 IObservable<T> 实现

下面是一个 `IObservable<int>` 的实现，它可以产生一个数字序列：

```c#
public class MySequenceOfNumbers : IObservable<int>
{
    public IDisposable Subscribe(IObserver<int> observer)
    {
        observer.OnNext(1);
        observer.OnNext(2);
        observer.OnNext(3);
        observer.OnCompleted();
        return System.Reactive.Disposables.Disposable.Empty; // Handy do-nothing IDisposable
    }
}
```

我们可以通过构建一个实例，然后订阅它来测试这一点：

```c#
var numbers = new MySequenceOfNumbers();
numbers.Subscribe(
    number => Console.WriteLine($"Received value: {number}"),
    () => Console.WriteLine("Sequence terminated"));
```

输出结果如下：

```
Received value 1
Received value 2
Received value 3
Sequence terminated
```

虽然从技术上讲，`MySequenceOfNumbers` 是 `IObservable<int>` 的一个正确实现，但它有点过于简单，因此并不实用。首先，我们通常会在出现感兴趣的事件时使用 Rx，但这并不是真正的响应式，它只是立即生成一组固定的数字。此外，它的实现是阻塞的--直到生成所有值后，它才会从 `Subscribe` 返回。这个示例说明了源如何向订阅者提供事件的基本原理，但如果我们只想表示预定的数字序列，我们不妨使用 `IEnumerable<T>` 实现，如 `List<T>` 或数组。

## 在 Rx 中表示文件系统事件

让我们看看更现实一点的东西。这是一个 .NET `FileSystemWatcher` 的包装器，以 `IObservable<FileSystemEventArgs>` 的形式显示文件系统更改通知。(注意：这并不一定是 Rx `FileSystemWatcher` 封装程序的最佳设计。观察者为几种不同类型的更改提供事件，其中之一是 `Renamed`，它以 `RenamedEventArgs` 的形式提供详细信息。该事件源于 `FileSystemEventArgs`，因此将所有事件归结为一个事件流确实可行，但这对于希望访问重命名事件详细信息的应用程序来说并不方便。一个更严重的设计问题是，`FileSystemWatcher.Error` 无法报告一个以上的事件。这些错误可能是瞬时的、可恢复的，在这种情况下，应用程序可能希望继续运行，但由于该类选择用单个 `IObservable<T>` 表示一切，因此它通过调用观察者的 `OnError` 来报告错误，此时 Rx 规则要求我们停止运行。我们可以使用 Rx 的 `Retry` 操作符来解决这个问题，它可以在错误发生后自动重新订阅，但最好还是提供一个单独的 `IObservable<ErrorEventArgs>`，这样我们就能以非终止的方式报告错误。不过，这种额外的复杂性并不总是有必要的。这种设计的简洁性意味着它非常适合某些应用。软件设计通常就是这样，没有放之四海而皆准的方法。）

```c#
// Represents filesystem changes as an Rx observable sequence.
// NOTE: this is an oversimplified example for illustration purposes.
//       It does not handle multiple subscribers efficiently, it does not
//       use IScheduler, and it stops immediately after the first error.
public class RxFsEvents : IObservable<FileSystemEventArgs>
{
    private readonly string folder;

    public RxFsEvents(string folder)
    {
        this.folder = folder;
    }

    public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
    {
        // Inefficient if we get multiple subscribers.
        FileSystemWatcher watcher = new(this.folder);

        // FileSystemWatcher's documentation says nothing about which thread
        // it raises events on (unless you use its SynchronizationObject,
        // which integrates well with Windows Forms, but is inconvenient for
        // us to use here) nor does it promise to wait until we've
        // finished handling one event before it delivers the next. The Mac,
        // Windows, and Linux implementations are all significantly different,
        // so it would be unwise to rely on anything not guaranteed by the
        // documentation. (As it happens, the Win32 implementation on .NET 7
        // does appear to wait until each event handler returns before
        // delivering the next event, so we probably would get way with
        // ignoring this issue. For now. On Windows. And actually the Linux
        // implementation dedicates a single thread to this job, but there's
        // a comment in the source code saying that this should probably
        // change - another reason to rely only on documented behaviour.)
        // So it's our problem to ensure we obey the rules of IObserver<T>.
        // First, we need to make sure that we only make one call at a time
        // into the observer. A more realistic example would use an Rx
        // IScheduler, but since we've not explained what those are yet,
        // we're just going to use lock with this object.
        object sync = new();

        // More subtly, the FileSystemWatcher documentation doesn't make it
        // clear whether we might continue to get a few more change events
        // after it has reported an error. Since there are no promises about
        // threads, it's possible that race conditions exist that would lead to
        // us trying to handle an event from a FileSystemWatcher after it has
        // reported an error. So we need to remember if we've already called
        // OnError to make sure we don't break the IObserver<T> rules in that
        // case.
        bool onErrorAlreadyCalled = false;

        void SendToObserver(object _, FileSystemEventArgs e)
        {
            lock (sync)
            {
                if (!onErrorAlreadyCalled)
                {
                    observer.OnNext(e); 
                }
            }
        }

        watcher.Created += SendToObserver;
        watcher.Changed += SendToObserver;
        watcher.Renamed += SendToObserver;
        watcher.Deleted += SendToObserver;

        watcher.Error += (_, e) =>
        {
            lock (sync)
            {
                // The FileSystemWatcher might report multiple errors, but
                // we're only allowed to report one to IObservable<T>.
                if (onErrorAlreadyCalled)
                {
                    observer.OnError(e.GetException());
                    onErrorAlreadyCalled = true; 
                    watcher.Dispose();
                }
            }
        };

        watcher.EnableRaisingEvents = true;

        return watcher;
    }
}
```

这很快就变得复杂了。这说明 `IObservable<T>` 实现负责遵守 `IObserver<T>` 规则。这通常是件好事：它可以将并发性方面的混乱问题集中在一个地方。我通过 `RxFsEvents` 订阅的任何 `IObserver<FileSystemEventArgs>` 都不必担心并发问题，因为它可以依赖 `IObserver<T>` 规则，该规则保证它一次只需处理一件事。如果我不需要在源代码中执行这些规则，我的 `RxFsEvents` 类可能会变得更简单，但处理重叠事件的所有复杂性都会扩散到处理事件的代码中。当并发的影响被包含在内时，处理并发就已经很困难了。一旦并发开始蔓延到多个类型，就几乎无法进行推理了。Rx 的 `IObserver<T>` 规则可以防止这种情况发生。

(注：这是 Rx 的一个重要特征。这些规则使观察者的工作变得简单。随着事件源或事件流程复杂性的增加，这一点变得越来越重要）。

这段代码有几个问题（除了已经提到的 API 设计问题）。一个问题是，当 `IObservable<T>` 实现产生的事件模拟现实生活中的异步活动（如文件系统更改）时，应用程序通常希望通过某种方式控制通知到达的线程。例如，用户界面框架往往有线程亲和性要求。通常需要在特定线程上才能更新用户界面。Rx 提供了将通知重定向到不同调度程序的机制，因此我们可以绕过它，但我们通常希望能用 `IScheduler` 提供这种观察器，并通过它来发送通知。我们将在后面的章节中讨论调度程序。

另一个问题是，它不能有效地处理多个订阅者。你可以多次调用 `IObservable<T>.Subscribe`，如果你用这段代码这样做，每次都会创建一个新的 `FileSystemWatcher`。这种情况比你想象的更容易发生。假设我们有一个此观察者的实例，并希望以不同的方式处理不同的事件。我们可以使用 `Where` 运算符定义可观察源，按照我们想要的方式分割事件：

```c#
IObservable<FileSystemEventArgs> configChanges =
    fs.Where(e => Path.GetExtension(e.Name) == ".config");
IObservable<FileSystemEventArgs> deletions =
    fs.Where(e => e.ChangeType == WatcherChangeTypes.Deleted);
```

当你在 `Where` 操作符返回的 `IObservable<T>` 上调用 `Subscribe` 时，它会在其输入上调用 `Subscribe`。因此，在本例中，如果我们同时对 `configChanges` 和 `deletions` 调用 `Subscribe`，就会对 `fs` 调用两次 `Subscribe`。因此，如果 `fs` 是上述 `RxFsEvents` 类型的实例，那么每个实例都将构建自己的 `FileSystemEventWatcher`，这样做效率很低。

Rx 提供了几种方法来解决这个问题。它提供了专门设计的操作符，用于将不能容忍多个订阅者的 `IObservable<T>` 封装到可以容忍多个订阅者的适配器中：

```c#
IObservable<FileSystemEventArgs> fs =
    new RxFsEvents(@"c:\temp")
    .Publish()
    .RefCount();
```

但这是跳跃性的。(这些操作符将在[发布操作符](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/15_PublishingOperators.md)一章中进行介绍。）如果你想创建一个本质上对多订阅者友好的类型，你真正需要做的就是跟踪所有订阅者，并在一个循环中通知每一个订阅者。下面是文件系统监视器的修改版：

```c#
public class RxFsEventsMultiSubscriber : IObservable<FileSystemEventArgs>
{
    private readonly object sync = new();
    private readonly List<Subscription> subscribers = new();
    private readonly FileSystemWatcher watcher;

    public RxFsEventsMultiSubscriber(string folder)
    {
        this.watcher = new FileSystemWatcher(folder);

        watcher.Created += SendEventToObservers;
        watcher.Changed += SendEventToObservers;
        watcher.Renamed += SendEventToObservers;
        watcher.Deleted += SendEventToObservers;

        watcher.Error += SendErrorToObservers;
    }

    public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
    {
        Subscription sub = new(this, observer);
        lock (this.sync)
        {
            this.subscribers.Add(sub); 

            if (this.subscribers.Count == 1)
            {
                // We had no subscribers before, but now we've got one so we need
                // to start up the FileSystemWatcher.
                watcher.EnableRaisingEvents = true;
            }
        }

        return sub;
    }

    private void Unsubscribe(Subscription sub)
    {
        lock (this.sync)
        {
            this.subscribers.Remove(sub);

            if (this.subscribers.Count == 0)
            {
                watcher.EnableRaisingEvents = false;
            }
        }
    }

    void SendEventToObservers(object _, FileSystemEventArgs e)
    {
        lock (this.sync)
        {
            foreach (var subscription in this.subscribers)
            {
                subscription.Observer.OnNext(e);
            }
        }
    }

    void SendErrorToObservers(object _, ErrorEventArgs e)
    {
        Exception x = e.GetException();
        lock (this.sync)
        {
            foreach (var subscription in this.subscribers)
            {
                subscription.Observer.OnError(x);
            }

            this.subscribers.Clear();
        }
    }

    private class Subscription : IDisposable
    {
        private RxFsEventsMultiSubscriber? parent;

        public Subscription(
            RxFsEventsMultiSubscriber rxFsEventsMultiSubscriber,
            IObserver<FileSystemEventArgs> observer)
        {
            this.parent = rxFsEventsMultiSubscriber;
            this.Observer = observer;
        }
        
        public IObserver<FileSystemEventArgs> Observer { get; }

        public void Dispose()
        {
            this.parent?.Unsubscribe(this);
            this.parent = null;
        }
    }
}
```

这样，无论调用多少次 `Subscribe`，都只会创建一个 `FileSystemWatcher` 实例。请注意，我不得不引入一个嵌套类来提供 `Subscribe` 返回的 `IDisposable`。在本章的第一个 `IObservable<T>` 实现中，我并不需要这样做，因为它在返回之前已经完成了序列，所以它可以返回由 Rx 方便提供的 `Disposable.Empty` 属性。(这在必须提供 `IDisposable`，但实际上不需要在处置时做任何事情的情况下非常方便）。在我的第一个 `FileSystemWatcher` 封装程序 `RxFsEvents` 中，我只是通过 `Dispose` 返回 `FileSystemWatcher` 本身。(这样做是因为 `FileSystemWatcher.Dispose` 会关闭观察者，而且每个订阅者都有自己的 `FileSystemWatcher`）。但是，既然单个 `FileSystemWatcher` 支持多个观察者，我们就需要在观察者取消订阅时多做一些工作。

当我们从 `Subscribe` 返回的 `Subscription` 实例被弃置时，它会将自己从订阅者列表中删除，确保自己不会再收到任何通知。如果没有更多的订阅者，它还会将 `FileSystemWatcher` 的 `EnableRaisingEvents` 设置为 false，以确保在目前没有人需要通知的情况下，该源不会执行不必要的工作。

这看起来比第一个示例更真实。这确实是一个随时可能发生的事件源（这正是 Rx 的理想用途），而且它现在可以智能地处理多个订阅者。不过，我们通常不会这样写。这里的所有工作都是我们自己完成的--这段代码甚至不需要引用 `System.Reactive` 包，因为它引用的唯一 Rx 类型是 `IObservable<T>` 和 `IObserver<T>`，这两种类型都内置于 .NET 运行时库中。在实践中，我们通常会使用 `System.Reactive` 中的辅助方法，因为它们可以为我们做很多工作。

例如，假设我们只关心 `Changed` 事件。我们可以这样写：

```c#
FileSystemWatcher watcher = new (@"c:\temp");
IObservable<FileSystemEventArgs> changes = Observable
    .FromEventPattern<FileSystemEventArgs>(watcher, nameof(watcher.Changed))
    .Select(ep => ep.EventArgs);
watcher.EnableRaisingEvents = true;
```

这里我们使用了 `System.Reactive` 库的 `Observable` 类中的 `FromEventPattern` 辅助方法，它可以用于从符合常规模式的任何.NET事件（其中事件处理程序接受两个参数：一个类型为 `object` 的发送者，以及包含有关事件信息的某个派生自 `EventArgs` 的类型）构建一个 `IObservable<T>`。这种方法不如之前的示例灵活。它只报告其中一个事件，并且我们必须手动启动（如果需要的话停止）`FileSystemWatcher`。但对于某些应用程序来说，这已经足够好了，而且编写的代码量要少得多。如果我们的目标是编写一个适用于许多不同场景的完整功能的 `FileSystemWatcher` 包装器，那么像之前示例中展示的那样编写一个专门的 `IObservable<T>` 实现可能是值得的。（我们可以很容易地扩展最后一个示例以监视所有事件。我们只需为每个事件使用一次`FromEventPattern`，然后使用 `Observable.Merge` 将生成的四个 `Observables` 组合成一个。完全自定义实现唯一的好处是我们可以根据当前是否有观察者来自动启动和停止 `FileSystemWatcher`。）但如果我们只需要将一些事件表示为 `IObservable<T>`，以便在应用程序中使用它们，我们可以使用这种更简单的方法。

在实践中，我们几乎总是使用 `System.Reactive` 来为我们实现 `IObservable<T>`。即使我们想要控制某些方面（例如在这些示例中自动启动和关闭 `FileSystemWatcher`），我们几乎总能找到一组操作符来实现这一点。下面的代码使用 `System.Reactive` 中的各种方法返回一个 `IObservable<FileSystemEventArgs>`，它具有与上面手动编写的完整功能 `RxFsEventsMultiSubscriber` 相同的功能，但代码量要少得多。

```c#
IObservable<FileSystemEventArgs> ObserveFileSystem(string folder)
{
    return 
        // Observable.Defer enables us to avoid doing any work
        // until we have a subscriber.
        Observable.Defer(() =>
            {
                FileSystemWatcher fsw = new(folder);
                fsw.EnableRaisingEvents = true;

                return Observable.Return(fsw);
            })
        // Once the preceding part emits the FileSystemWatcher
        // (which will happen when someone first subscribes), we
        // want to wrap all the events as IObservable<T>s, for which
        // we'll use a projection. To avoid ending up with an
        // IObservable<IObservable<FileSystemEventArgs>>, we use
        // SelectMany, which effectively flattens it by one level.
        .SelectMany(fsw =>
            Observable.Merge(new[]
                {
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Created += h, h => fsw.Created -= h),
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Changed += h, h => fsw.Changed -= h),
                    Observable.FromEventPattern<RenamedEventHandler, FileSystemEventArgs>(
                        h => fsw.Renamed += h, h => fsw.Renamed -= h),
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Deleted += h, h => fsw.Deleted -= h)
                })
            // FromEventPattern supplies both the sender and the event
            // args. Extract just the latter.
            .Select(ep => ep.EventArgs)
            // The Finally here ensures the watcher gets shut down once
            // we have no subscribers.
            .Finally(() => fsw.Dispose()))
        // This combination of Publish and RefCount means that multiple
        // subscribers will get to share a single FileSystemWatcher,
        // but that it gets shut down if all subscribers unsubscribe.
        .Publish()
        .RefCount();
}
```

我在这里使用了很多方法，其中大部分我以前都没有讲过。为了让这个示例更有意义，我显然需要开始描述 `System.Reactive` 软件包为您实现 `IObservable<T>` 的那一系列方法。

## 简单的工厂函数

由于可用于创建可观测序列的方法数量众多，我们将把它们分成几类。第一类方法创建的 `IObservable<T>` 序列最多只能产生一个结果。

### Observable.Return

最简单的工厂方法之一是 `Observable.Return<T>(T value)`，你已经在上一章的 `Quiescent` 示例中看到过这个方法。该方法接收一个 T 类型的值，并返回一个 `IObservable<T>`，它将产生这个单一的值，然后完成。从某种意义上说，这个方法将一个值封装在一个 `IObservable<T>` 中；从概念上说，它与编写 `new T[] { value }` 类似，因为它是一个只包含一个元素的序列。你也可以把它看作是 `Task.FromResult` 的 Rx 等价物，当你有一个 `T` 类型的值，需要把它传递给需要 `Task<T>` 的东西时，就可以使用它。

```c#
IObservable<string> singleValue = Observable.Return<string>("Value");
```

为了清楚起见，我指定了类型参数，但这并非必要，因为编译器可以根据提供的参数推断出类型：

```c#
IObservable<string> singleValue = Observable.Return("Value");
```

`Return` 生成的是冷观测值：每个订阅者在订阅后将立即收到该值。([热观测值和冷观测值](key-types.md#热冷资源)已在前一章中介绍）。

### Observable.Empty

空序列有时可能很有用。.NET 的 `Enumerable.Empty<T>()` 可以为 `IEnumerable<T>` 实现这一功能，而 Rx 的 `Observable.Empty<T>()` 可以直接实现这一功能，它返回一个空的 `IObservable<T>`。我们需要提供类型参数，因为编译器无法从中推断出类型值。

```c#
IObservable<string> empty = Observable.Empty<string>();
```

实际上，空序列是指对任何订阅者都立即调用 `OnCompleted` 的序列。

与 `IEnumerable<T>` 相比，这只是一个空列表的 Rx 等价物，但我们可以从另一个角度来看待它。Rx 是一种强大的异步建模方式，因此您可以将其视为类似于立即完成而不产生任何结果的任务，因此它在概念上类似于 `Task.CompletedTask`。(这并不像 `Observable.Return` 和 `Task.FromResult` 之间的类比那么接近，因为在那种情况下，我们比较的是 `IObservable<T>` 和 `Task<T>`，而这里我们比较的是 `IObservable<T>` 和 `Task`--只有使用非泛型版本的 `Task`，任务才能在不产生任何结果的情况下完成）。

### Observable.Never

`Observable.Never<T>()`方法会返回一个序列，该序列与空序列一样，不会产生任何值，但与空序列不同的是，它永远不会结束。实际上，这意味着它永远不会调用订阅者的任何方法（`OnNext`、`OnCompleted` 或 `OnError`）。`Observable.Empty<T>()` 会立即完成，而 `Observable.Never<T>` 则会无限持续。

```c#
IObservable<string> never = Observable.Never<string>();
```

可能不太明显为什么这样做会有用。我在上一章中给出了一个可能的用途：你可以在测试中使用它来模拟一个不产生任何值的源，例如使你的测试能够验证超时逻辑。

它还可以用于表示基于时间的信息的场景。有时候我们实际上并不关心观察到的值是什么；我们可能只在某个事件发生时（任何事件）才关心。（我们在前面的章节中看到了这个“仅用于计时目的的可观察序列”概念的一个示例，尽管在那个特定的场景中 `Never` 不合理。`Quiescent` 示例中使用了 `Buffer` 操作符，它在两个可观察序列上工作：第一个包含感兴趣的项，第二个纯粹用于确定如何将第一个序列切分成块。`Buffer` 不会对第二个可观察序列产生的值做任何操作：它只关注值何时出现，并在第二个可观察序列产生值时完成上一个块。如果我们表示时间信息，有时候表示某个事件永远不会发生的方式可能很有用。）

作为一个使用 `Never` 进行计时目的的示例，假设你正在使用一些基于 Rx 的库，该库提供了一个超时机制，当超时发生时，操作将被取消，而超时本身被建模为可观察序列。如果出于某种原因你不想要超时，只想无限等待，你可以指定一个 `Observable.Never` 作为超时。

### Observable.Throw

`Observable.Throw<T>(Exception)` 返回一个序列，它会立即向任何订阅者报告错误。与 `Empty` 和 `Never` 一样，我们没有为该方法提供一个值（只有一个异常），因此我们需要提供一个类型参数，以便它知道在返回的 `IObservable<T>` 中使用什么 `T`。(实际上，它永远不会产生一个 `T`，但如果不选择某种特定类型的 `T`，就无法拥有 `IObservable<T>` 的实例）。

```c#
IObservable<string> throws = Observable.Throw<string>(new Exception()); 
```

### Observable.Create

`Create` 工厂方法比其他创建方法更强大，因为它可以用来创建任何类型的序列。你可以用 `Observable.Create` 实现前面四个方法中的任何一个。方法签名本身一开始可能看起来比必要的要复杂，但一旦习惯了，就会变得非常自然。

```c#
// Creates an observable sequence from a specified Subscribe method implementation.
public static IObservable<TSource> Create<TSource>(
    Func<IObserver<TSource>, IDisposable> subscribe)
{...}
public static IObservable<TSource> Create<TSource>(
    Func<IObserver<TSource>, Action> subscribe)
{...}
```

您可以为其提供一个委托，该委托将在每次订阅时执行。您的委托将传递一个 `IObserver<T>`。从逻辑上讲，这代表了传递给 `Subscribe` 方法的观察者，但在实际应用中，出于各种原因，Rx 会对其进行封装。你可以根据需要调用 `OnNext/OnError/OnCompleted` 方法。这是直接使用 `IObserver<T>` 接口的少数几种情况之一。下面是一个产生三个项目的简单示例：

```c#
private IObservable<int> SomeNumbers()
{
    return Observable.Create<int>(
        (IObserver<string> observer) =>
        {
            observer.OnNext(1);
            observer.OnNext(2);
            observer.OnNext(3);
            observer.OnCompleted();

            return Disposable.Empty;
        });
}
```

您的委托必须返回 `IDisposable` 或 `Action` 才能取消订阅。当订阅者为了取消订阅而处置他们的订阅时，Rx 将在你返回的 `IDisposable` 上调用 `Dispose()`，或者在你返回 `Action` 的情况下，它将调用 `Dispose()`。

这个示例让人想起本章开头的 `MySequenceOfNumbers` 示例，因为它立即产生了一些固定值。本例的主要区别在于 Rx 添加了一些包装器，可以处理诸如重入等尴尬情况。Rx 有时会自动延迟工作以防止死锁，因此，使用此方法返回的 `IObservable<string>` 的代码有可能会在上面代码中的回调运行之前看到对 `Subscribe` 的调用返回，在这种情况下，它们有可能在其 `OnNext` 处理程序中取消订阅。

下面的序列图显示了这种情况在实际中是如何发生的。假设 `SomeNumbers` 返回的 `IObservable<int>` 已被 Rx 封装，以确保订阅发生在某个不同的执行上下文中。我们通常会使用合适的调度程序来确定上下文。(我们可能会使用 `TaskPoolScheduler` 来确保订阅发生在某个任务池线程上。因此，当我们的应用代码调用 `Subscribe` 时，封装器 `IObservable<int>` 不会立即订阅底层可观察对象。相反，它会在调度程序中排队等待一个工作项，然后立即返回，无需等待该工作项运行。这样，我们的订阅者就能在 `Observable.Create` 调用我们的回调之前拥有一个代表订阅的 `IDisposable`。图中显示了订阅者如何将其提供给观察者。

![](./asserts/Ch03-Sequence-CreateWrappers.svg)

图中显示了调度程序在此之后对底层观察对象的调用 `Subscribe`，这意味着我们传递给 `Observable.Create<int>` 的回调现在将运行。我们的回调调用了 `OnNext`，但并没有传递给真正的观察者：而是传递给了另一个 Rx 生成的包装器。该封装器最初会将调用直接转发给真正的观察者，但我们的图表显示，当真正的观察者（位于右侧）接收到第二个调用（`OnNext(2)`）时，它会通过调用 `IDisposable` 上的 `Dispose` 取消订阅，该 `IDisposable` 是我们订阅 Rx `IObservable` 封装器时返回的。这里的两个封装器--`IObservable` 封装器和 `IObserver` 封装器--是相连的，因此当我们从 `IObservable` 封装器取消订阅时，它会告诉 `IObserver` 封装器订阅正在关闭。这意味着，当我们的 `Observable.Create<int>` 回调调用 `IObserver` 封装器上的 `OnNext(3)` 时，封装器不会将其转发给真正的观察者，因为它知道该观察者已经取消订阅。(出于同样的原因，它也不会转发 `OnCompleted`）。

你可能会想，我们返回给 `Observable.Create` 的 `IDisposable` 怎么会有用？它是回调的返回值，所以我们只能将它作为回调的最后一项操作返回给 Rx。在返回时，我们的工作不是已经完成了吗？不一定，我们可能会启动一些工作，在返回后继续运行。下一个示例就是这样做的，这意味着它返回的取消订阅操作可以做一些有用的事情：它设置了一个取消标记，该标记被生成观察对象输出的循环所观察。(这将返回一个回调，而不是 `IDisposable-Observable.Create`。在这种情况下，当订阅提前终止时，Rx 将调用我们的回调）。

```c#
IObservable<char> KeyPresses() =>
    Observable.Create<char>(observer =>
    {
        CancellationTokenSource cts = new();
        Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                ConsoleKeyInfo ki = Console.ReadKey();
                observer.OnNext(ki.KeyChar);
            }
        });

        return () => cts.Cancel();
    });
```

这说明了取消并不一定会立即生效。`Console.ReadKey` API 并不提供接受 CancellationToken 的重载，因此在用户下一次按键并导致 `ReadKey` 返回之前，此观察对象无法检测到取消请求。

考虑到我们在等待 `ReadKey` 返回时可能会请求取消，你可能会认为我们应该在 `ReadKey` 返回后、调用 `OnNext` 之前检查取消。事实上，不这样做也没关系。Rx 有一条规则规定，在调用观察者订阅的 `Dispose` 返回后，可观察源不得调用观察者。为了执行该规则，如果在请求取消订阅后，您传递给 `Observable.Create` 的回调继续调用其 `IObserver<T>` 上的方法，Rx 就会忽略该调用。这就是它传递给你的 `IObserver<T>` 是一个封装器的原因之一：它可以在调用传递给底层观察者之前拦截调用。然而，这种便利性意味着有两件重要的事情需要注意

如果您忽略了取消订阅的尝试，并继续工作以生成项目，那么您只是在浪费时间，因为没有任何东西会接收到这些项目
如果调用 `OnError`，可能没有人在监听，错误也会被完全忽略。
`Create` 有一些重载，旨在支持异步方法。下一个方法就是利用这一点，使用异步 `ReadLineAsync` 方法将文件中的文本行作为可观察源呈现出来。

```c#
IObservable<string> ReadFileLines(string path) =>
    Observable.Create<string>(async (observer, cancellationToken) =>
    {
        using (StreamReader reader = File.OpenText(path))
        {
            while (cancellationToken.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                observer.OnNext(line);
            }

            observer.OnCompleted();
        }
    });
```

从存储设备读取数据通常不会立即发生（除非数据已恰好在文件系统缓存中），因此该源代码会在从存储设备读取数据时尽快提供数据。

请注意，由于这是一个异步方法，它通常会在完成之前返回给调用者。(实际需要等待的第一个 `await` 会返回，而方法的其余部分会在工作完成后通过回调运行）。这意味着订阅者通常会在该方法完成之前拥有代表其订阅的 `IDisposable`，因此我们在这里使用了不同的机制来处理取消订阅。`Create` 的这个特定重载不仅传递了一个 `IObserver<T>` 回调，还传递了一个 `CancellationToken`，当取消订阅发生时，它将使用该 `CancellationToken` 请求取消订阅。

文件 IO 可能会出错。我们正在寻找的文件可能不存在，或者由于安全限制或其他应用程序正在使用它，我们可能无法打开它。文件可能在远程存储服务器上，我们可能会失去网络连接。因此，我们必须预料到此类代码会出现异常。这个示例没有做任何检测异常的工作，但 `ReadFileLines` 方法返回的 `IObservable<string>` 实际上会报告任何发生的异常。这是因为 `Create` 方法将捕获回调中出现的任何异常，并通过 `OnError` 报告。(如果我们的代码已经在观察者上调用了 `OnComplete`，Rx 不会调用 `OnError`，因为这违反了规则。相反，它将默默地丢弃异常，所以最好不要在调用 `OnCompleted` 后尝试做任何工作）。

这种自动异常传递是另一个例子，说明了为什么创建工厂方法是实现自定义可观察序列的首选方法。它几乎总是比创建实现 `IObservable<T>` 接口的自定义类型更好的选择。这不仅仅是因为它能节省时间。此外，Rx 还能解决你可能想不到的复杂问题，如通知的线程安全和订阅的处理。

创建方法包含懒评估，这是 Rx 非常重要的一部分。它开启了其他强大功能的大门，例如调度和序列组合，我们稍后将看到这些功能。只有在进行订阅时，才会调用委托。因此，在 `ReadFileLines` 示例中，只有在您订阅了返回的 `IObservable<string>` 后，它才会尝试打开文件。如果您多次订阅，每次都会执行回调。(因此，如果文件已更改，您可以再次调用订阅来获取最新内容）。

作为练习，请尝试使用 `Create` 方法自行创建 `Empty`、`Return`、`Never` 和 `Throw` 扩展方法。如果你现在有 Visual Studio 或 LINQPad，请尽快编写代码；如果你有 Visual Studio Code，可以创建一个新的 Polyglot Notebook。(Polyglot Notebook 会自动提供 Rx，所以你只需编写一个带有合适 using 指令的 C# 单元，然后就可以运行了）。如果没有（也许你正在上班的火车上），请试着构思一下如何解决这个问题。

在进入本段之前，你已经完成了最后一步，对吗？因为你现在可以将自己的版本与这些用 `Observable.Create` 重新创建的 `Empty`、`Return`、`Never` 和 `Throw` 示例进行比较：

```c#
public static IObservable<T> Empty<T>()
{
    return Observable.Create<T>(o =>
    {
        o.OnCompleted();
        return Disposable.Empty;
    });
}

public static IObservable<T> Return<T>(T value)
{
    return Observable.Create<T>(o =>
    {
        o.OnNext(value);
        o.OnCompleted();
        return Disposable.Empty;
    });
}

public static IObservable<T> Never<T>()
{
    return Observable.Create<T>(o =>
    {
        return Disposable.Empty;
    });
}

public static IObservable<T> Throws<T>(Exception exception)
{
    return Observable.Create<T>(o =>
    {
        o.OnError(exception);
        return Disposable.Empty;
    });
}
```

您可以看到，`Observable.Create` 为我们提供了创建自己的工厂方法的能力，如果我们愿意的话。

### Observable.Defer

`Observable.Create` 的一个非常有用的方面是，它提供了一个放置代码的地方，这些代码只应在订阅发生时运行。通常情况下，库会提供 `IObservable<T>` 属性，但这些属性并不一定会被所有应用程序使用，因此，将相关工作推迟到真正需要时再进行是非常有用的。这种延迟初始化是 `Observable.Create` 的固有工作方式，但如果我们的数据源的性质不适合 `Observable.Create`，该怎么办？在这种情况下，我们如何执行延迟初始化？Rx提供了 `Observable.Defer` 来实现这个目的。

我已经使用过一次 `Defer`。`ObserveFileSystem` 方法返回一个 `IObservable<FileSystemEventArgs>` 报告文件夹中的变化。它不适合使用 `Observable.Create`，因为它以 .NET 事件的形式提供了我们想要的所有通知，因此使用 Rx 的事件适配功能很有意义。但我们仍然希望将 `FileSystemWatcher` 的创建推迟到订阅的那一刻，这就是为什么该示例使用了 `Observable.Defer`。

`Observable.Defer` 接收一个返回 `IObservable<T>` 的回调，`Defer` 将其封装为一个 `IObservable<T>`，在订阅时调用该回调。为了展示效果，我将首先展示一个不使用 `Defer` 的示例：

```c#
static IObservable<int> WithoutDeferal()
{
    Console.WriteLine("Doing some startup work...");
    return Observable.Range(1, 3);
}

Console.WriteLine("Calling factory method");
IObservable<int> s = WithoutDeferal();

Console.WriteLine("First subscription");
s.Subscribe(Console.WriteLine);

Console.WriteLine("Second subscription");
s.Subscribe(Console.WriteLine);
```

下面是生成的输出：

```
Calling factory method
Doing some startup work...
First subscription
1
2
3
Second subscription
1
2
3
```

正如您所看到的，“正在进行一些启动工作...”消息是在我们调用工厂方法时出现的，而且是在我们订阅之前。因此，如果我们从未订阅过该方法返回的 `IObservable<int>`，那么这项工作无论如何都会完成，从而浪费时间和精力。下面是 `Defer` 版本：

```c#
static IObservable<int> WithDeferal()
{
    return Observable.Defer(() =>
    {
        Console.WriteLine("Doing some startup work...");
        return Observable.Range(1, 3);
    });
}
```

如果我们使用与第一个示例类似的代码，就会看到这样的输出结果：

```
Calling factory method
First subscription
Doing some startup work...
1
2
3
Second subscription
Doing some startup work...
1
2
3
```

有两个重要的不同点。首先，"Doing some startup work..."消息直到我们第一次订阅时才出现，这说明 `Defer` 已经完成了我们想要的工作。不过，请注意，这条信息现在出现了两次：我们每次订阅时，它都会执行这项工作。如果你既希望延迟初始化，又希望只执行一次，那么你应该看看[发布操作符一章](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/15_PublishingOperators.md)中的操作符，它们提供了多种方法，可以让多个订阅者共享对底层源的单个订阅。

## 序列生成器

到目前为止，我们所看到的创建方法都很简单，它们要么生成非常简单的序列（如单元素或空序列），要么依靠我们的代码来告诉它们到底要生成什么。现在我们来看看一些可以生成较长序列的方法。

### Observable.Range

`Observable.Range(int, int)` 返回一个 `IObservable<int>`，它会产生一个整数范围。第一个整数是初始值，第二个整数是要产生的值的个数。此示例将写入从 10-24 的值，然后结束。

```
IObservable<int> range = Observable.Range(10, 15);
range.Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
```

### Observable.Generate

假设您想使用 `Observable.Create` 来模拟 `Range` 工厂方法。您可以试试下面的方法

```c#
// Not the best way to do it!
IObservable<int> Range(int start, int count) =>
    Observable.Create<int>(observer =>
        {
            for (int i = 0; i < count; ++i)
            {
                observer.OnNext(start + i);
            }

            return Disposable.Empty;
        });
```

这会起作用，但不会遵守取消订阅的请求。这不会造成直接伤害，因为 Rx 会检测到退订，并会直接忽略我们生成的任何其他值。不过，在无人收听后继续生成数字会浪费 CPU 时间（因此也会浪费能源，进而影响电池寿命和/或环境）。至于浪费有多严重，这取决于所要求的范围有多长。但想象一下，你想要一个无限的序列？也许您需要一个 `IObservable<BigInteger>` 从斐波那契数列或素数中产生值。你将如何用 `Create` 来编写？在这种情况下，你肯定需要一些处理取消订阅的方法。我们需要回调来返回取消订阅的通知（或者我们可以提供一个异步方法，但在这里似乎并不合适）。

有一种不同的方法可以在这里更好地发挥作用： `Observable.Generate`。简单版本的 `Observable.Generate` 需要以下参数：

- 初始状态
- 定义序列何时终止的谓词
- 一个应用于当前状态以生成下一个状态的函数
- 将状态转换为所需输出的函数

```c#
public static IObservable<TResult> Generate<TState, TResult>(
    TState initialState, 
    Func<TState, bool> condition, 
    Func<TState, TState> iterate, 
    Func<TState, TResult> resultSelector)
```

这展示了如何使用 `Observable.Generate` 来构建 `Range` 方法：

```c#
// Example code only
public static IObservable<int> Range(int start, int count)
{
    int max = start + count;
    return Observable.Generate(
        start, 
        value => value < max, 
        value => value + 1, 
        value => value);
}
```

生成方法会反复调用我们，直到我们的条件回调说我们完成了，或者观察者取消订阅。我们可以定义一个无限序列，只需永远不说“完成”即可：

```c#
IObservable<BigInteger> Fibonacci()
{
    return Observable.Generate(
        (v1: new BigInteger(1), v2: new BigInteger(1)),
        value => true, // It never ends!
        value => (value.v2, value.v1 + value.v2),
        value => value.v1);
}
```

## 定时序列生成器

到目前为止，我们所研究的大多数方法都返回了能立即产生所有值的序列。(唯一的例外是我们调用了 `Observable.Create`，并在准备就绪时生成了值）。不过，Rx 可以按计划生成序列。

正如我们将要看到的，操作符可以通过一个名为调度程序的抽象概念来安排工作。如果不指定调度器，它们会选择默认的调度器，但有时定时器机制也很重要。例如，有一些定时器可以与用户界面框架集成，在与鼠标点击和其他输入相同的线程上发送通知，我们可能希望 Rx 基于时间的操作符使用这些定时器。出于测试目的，虚拟化定时可能很有用，这样我们就可以验证定时敏感代码中发生的情况，而不必等待测试实时执行。

调度程序是一个复杂的主题，不在本章讨论范围之内，但在后面的[调度和线程章节](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/11_SchedulingAndThreading.md)中会详细介绍。

产生定时事件有三种方法。

### Observable.Interval

第一种是 `Observable.Interval(TimeSpan)`，它会根据你选择的频率发布从零开始的递增值。

本示例每 250 毫秒发布一次值。

```c#
IObservable<long> interval = Observable.Interval(TimeSpan.FromMilliseconds(250));
interval.Subscribe(
    Console.WriteLine, 
    () => Console.WriteLine("completed"));
```

输出：

```
0
1
2
3
4
5
```

一旦订阅，就必须终止订阅才能停止序列，因为 `Interval` 返回的是无限序列。Rx 假定您有足够的耐心，因为 `Interval` 返回的序列是 `IObservable<long>` 类型（`long`，而不是 `int`），这意味着如果您产生的事件超过区区 `21.475` 亿个（即超过 `int.MaxValue`），就不会出现问题。

### Observable.Timer

用于生成基于恒定时间序列的第二个工厂方法是 `Observable.Timer`。它有几个重载。最基本的重载和 `Observable.Interval` 一样，只需要一个 `TimeSpan`。但与 `Observable.Interval` 不同的是，`Observable.Timer` 会在时间段结束后准确地发布一个值（数字 0），然后结束。

```c#
var timer = Observable.Timer(TimeSpan.FromSeconds(1));
timer.Subscribe(
    Console.WriteLine, 
    () => Console.WriteLine("completed"));
```

输出：

```
0
completed
```

或者，您也可以为 `dueTime` 参数提供一个 `DateTimeOffset`。这将产生 0 值，并在指定时间完成。

还有一组重载添加了一个 `TimeSpan`，表示产生后续值的时间段。这样我们就可以生成无限序列。这也说明了 `Observable.Interval` 实际上只是 `Observable.Timer` 的一个特例。`Interval` 可以这样实现：

```c#
public static IObservable<long> Interval(TimeSpan period)
{
    return Observable.Timer(period, period);
}
```

虽然 `Observable.Interval` 在产生第一个值之前总是会等待给定的时间段，但 `Observable.Timer` 重载提供了在您选择时启动序列的能力。使用 `Observable.Timer`，您可以编写以下代码，以获得立即开始的间隔序列。

```c#
Observable.Timer(TimeSpan.Zero, period);
```

这就是我们的第三种方法，也是生成定时器相关序列的最通用方法，即回到 `Observable.Generate`。

### Timed Observable.Generate

`Observable.Generate` 有一个更复杂的重载，允许你提供一个函数来指定下一个值的到期时间。

```c#
public static IObservable<TResult> Generate<TState, TResult>(
    TState initialState, 
    Func<TState, bool> condition, 
    Func<TState, TState> iterate, 
    Func<TState, TResult> resultSelector, 
    Func<TState, TimeSpan> timeSelector)
```

额外的 `timeSelector` 参数让我们可以告诉 `Generate` 何时生成下一个项目。我们可以用它来编写自己的 `Observable.Timer` 实现（正如你已经看到的，这反过来又使我们能够编写自己的 `Observable.Interval`）。

```c#
public static IObservable<long> Timer(TimeSpan dueTime)
{
    return Observable.Generate(
        0l,
        i => i < 1,
        i => i + 1,
        i => i,
        i => dueTime);
}

public static IObservable<long> Timer(TimeSpan dueTime, TimeSpan period)
{
    return Observable.Generate(
        0l,
        i => true,
        i => i + 1,
        i => i,
        i => i == 0 ? dueTime : period);
}

public static IObservable<long> Interval(TimeSpan period)
{
    return Observable.Generate(
        0l,
        i => true,
        i => i + 1,
        i => i,
        i => period);
}
```

这说明了如何使用 `Observable.Generate` 生成无限序列。作为使用 `Observable.Generate` 生成可变速率值的练习，我将把它留给读者。

## 可观察序列和状态

正如 `Observable.Generate` 所特别说明的，可观察序列可能需要保持状态。对于该操作符来说，它是显式的--我们传递初始状态，并在每次迭代时提供一个回调来更新它。很多其他操作符也会维护内部状态。`Timer` 会记住它的刻度计数，更巧妙的是，它必须以某种方式记录上一次触发事件的时间和下一次触发事件的时间。在接下来的章节中，我们还将看到许多其他操作符需要记住它们已经看到的信息。

这就提出了一个有趣的问题：如果进程关闭了，会发生什么？有没有一种方法可以保留该状态，并在新进程中将其重组。

对于普通的 Rx.NET，答案是否定的：所有这些状态都完全保存在内存中，没有办法获取这些状态，也无法要求正在运行的订阅序列化其当前状态。这意味着，如果要处理运行时间特别长的操作，您需要想办法重新启动，而不能依靠 `System.Reactive` 来帮助您。不过，还有一套基于 Rx 的相关库，统称为 [Reaqtive 库](https://reaqtive.net/)。这些库提供了与 `System.Reactive` 相同的大多数操作符的实现，但您可以通过它们收集当前状态，并从以前保存的状态中重新创建新的订阅。这些库还包括一个名为 `Reaqtor` 的组件，它是一种托管技术，可以管理自动检查点和崩溃后恢复，通过使订阅持久可靠，可以支持运行时间很长的 Rx 逻辑。请注意，该组件目前尚未以任何产品化形式出现，因此您需要做大量工作才能使用它，但如果您需要持久化版本的 Rx，请注意它的存在。

## 将通用类型适配为 IObservable<T>

尽管我们现在已经看到了生成任意序列的两种非常通用的方法--`Create` 和 `Generate`--但如果您已经有了其他形式的现有信息源，并希望将其作为 `IObservable<T>` 提供，该怎么办呢？Rx 为常见的源类型提供了一些适配器。

### 源自委托

通过 `Observable.Start` 方法，您可以将长时间运行的 `Func<T>` 或 `Action` 转化为单值可观察序列。默认情况下，处理将在 `ThreadPool` 线程上异步完成。如果您使用的重载是 `Func<T>`，那么返回类型将是 `IObservable<T`>。当函数返回值时，该值将被发布，然后序列完成。如果重载使用的是 `Action`，那么返回的序列将是 `IObservable<Unit>` 类型。`Unit` 类型表示信息的缺失，因此它有点类似于 `void`，但您可以拥有一个 `Unit` 类型的实例。它在 Rx 中特别有用，因为我们经常只关心事情发生的时间，而除了时间之外可能没有任何信息。在这种情况下，我们经常使用 `IObservable<Unit>`，这样即使没有意义的数据，也可以产生确定的事件。(这个名称来自函数式编程，在函数式编程中经常使用这种结构）。在本例中，`Unit` 用于发布 `Action` 已完成的确认信息，因为 `Action` 不会返回任何信息。`Unit` 类型本身没有价值；它只是作为 `OnNext` 通知的空有效载荷。下面是使用这两种重载的示例。

```c#
static void StartAction()
{
    var start = Observable.Start(() =>
        {
            Console.Write("Working away");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.Write(".");
            }
        });

    start.Subscribe(
        unit => Console.WriteLine("Unit published"), 
        () => Console.WriteLine("Action completed"));
}

static void StartFunc()
{
    var start = Observable.Start(() =>
    {
        Console.Write("Working away");
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(100);
            Console.Write(".");
        }
        return "Published value";
    });

    start.Subscribe(
        Console.WriteLine, 
        () => Console.WriteLine("Action completed"));
}
```

请注意 `Observable.Start` 和 `Observable.Return` 之间的区别。`Start` 方法只有在订阅后才会调用我们的回调，因此是“惰性”操作的一个例子。相反，`Return` 要求我们预先提供值。

由 `Start` 返回的可观察对象似乎与 `Task` 或 `Task<T>`（取决于您使用的是 `Action` 还是 `Func<T>` 重载）有表面上的相似之处。它们都代表可能需要一段时间才能最终完成的工作，也许会产生一个结果。但是，两者之间有一个显著的区别：在您订阅之前，`Start` 不会开始工作。此外，每次订阅后，它都会重新执行回调。因此，它更像是一个类似任务实体的工厂。

### 源自事件

正如我们在本书初期所讨论的，.NET 在其类型系统中内置了一个事件模型。这个模型早于 Rx（尤其是因为 Rx 在.NET 2.0 获得泛型之前并不可行），因此支持事件但不支持 Rx 的类型很常见。为了能够与现有的事件模型集成，Rx 提供了一些方法来获取事件并将其转化为可观察的序列。我在前面的文件系统监视器示例中简单介绍了这一点，下面让我们更详细地研究一下。你可以使用多种不同的方法。下面展示的是最简洁的形式：

```c#
FileSystemWatcher watcher = new (@"c:\incoming");
IObservable<EventPattern<FileSystemEventArgs>> changeEvents = Observable
    .FromEventPattern<FileSystemEventArgs>(watcher, nameof(watcher.Changed));
```

如果你有一个提供事件的对象，你可以使用 `FromEventPattern` 的重载，传入对象和你想在 Rx 中使用的事件名称。虽然这是将事件引入 Rx 世界的最简单方法，但也存在一些问题。

首先，为什么要以字符串形式传递事件名称？用字符串标识成员是一种容易出错的技术。编译器不会注意到第一个参数和第二个参数之间的不匹配（例如，如果我错误地传递了参数（`somethingElse, nameof(watcher.Changed)`））。难道我不能直接传递 `watcher.Changed` 本身吗？不幸的是，不能--这就是我在第一章中提到的问题的一个例子：.NET 事件不是一等公民。我们不能像使用其他对象或值那样使用它们。例如，我们不能将事件作为参数传递给方法。事实上，你能对 .NET 事件做的唯一事情就是附加和删除事件处理程序。如果我想让其他方法为我选择的事件附加处理程序（例如，在这里我想让 Rx 来处理事件），那么唯一的办法就是指定事件的名称，这样该方法（`FromEventPattern`）就可以使用反射来附加自己的处理程序。

这对某些部署方案来说是个问题。在 .NET 中，在构建时进行额外工作以优化运行时行为的做法越来越常见，而依赖反射可能会损害这些技术。例如，我们可以使用预编译（AOT），而不是依赖于代码的及时编译（JIT）。.NET的Ready to Run (R2R)系统使您可以在正常的 IL 旁边加入针对特定CPU类型的预编译代码，从而避免等待 .NET 将 IL 编译成可运行代码。这对启动时间有很大影响。在客户端应用程序中，它可以解决应用程序首次启动时运行缓慢的问题。在服务器端应用程序中，这一点也很重要，尤其是在代码可能会频繁从一个计算节点转移到另一个计算节点的环境中，因此尽量减少冷启动成本非常重要。在有些情况下，甚至无法选择 JIT 编译，在这种情况下，AOT 编译就不仅仅是一种优化：它是代码得以运行的唯一途径。

反射的问题在于，它使得构建工具难以确定哪些代码将在运行时执行。当它们检查对 `FromEventPattern` 的调用时，只会看到对象和字符串类型的参数。不言而喻，这将导致在运行时对 `FileSystemWatcher.Changed` 的 `add` 和 `remove` 方法进行反射驱动调用。有一些属性可以用来提供提示，但这些属性的作用是有限的。有时，构建工具无法确定需要对哪些代码进行 AOT 编译，以便在不依赖运行时 JIT 的情况下执行此方法。

还有另一个相关问题。.NET 编译工具支持一种名为“裁剪”的功能，即删除未使用的代码。`System.Reactive.dll` 文件的大小约为 1.3MB，但如果应用程序使用了该组件中每种类型的每个成员，那就很不寻常了。Rx 的基本使用可能只需要几十KB。修剪的目的是找出实际使用的位，并生成一个只包含这些代码的 DLL 副本。这可以大大减少为运行可执行文件而需要部署的代码量。这在客户端 Blazor 应用程序中尤为重要，因为 .NET 组件最终会被浏览器下载。必须下载整个 1.3MB 的组件可能会让您三思而后行。但是，如果修剪意味着基本使用只需要几十 KB，只有在更广泛地使用组件时才会增加组件大小，那么就可以合理地使用一个组件，如果不进行修剪，这个组件就会造成太大的损失，无法证明包含它是合理的。但是，与 AOT 编译一样，只有当工具能够确定哪些代码正在使用时，修剪才能奏效。如果工具无法做到这一点，就不能只是退回到较慢的路径，等待相关代码得到 JIT 编译。如果代码被删减，运行时将无法使用，应用程序可能会因缺少方法异常（MissingMethodException）而崩溃。

因此，如果使用这些技术，基于反射的应用程序接口就会出现问题。幸运的是，有一种替代方法。我们可以使用一个重载来接收几个委托，当 Rx 要为事件添加或删除处理程序时，就会调用这些委托：

```c#
IObservable<EventPattern<FileSystemEventArgs>> changeEvents = Observable
    .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
        h => watcher.Changed += h,
        h => watcher.Changed -= h);
```

这是 AOT 和修剪工具可以轻松理解的代码。我们编写了明确添加和移除 `FileSystemWatcher.Changed` 事件处理程序的方法，因此 AOT 工具可以预编译这两个方法，而修剪工具也知道它们不能移除这些事件的添加和移除处理程序。

这样做的缺点是代码编写起来相当麻烦。如果你还没有接受使用 Rx 的理念，这可能会让你觉得“我还是坚持使用普通的 .NET 事件吧，谢谢”。但是，这种繁琐的特性正是 .NET 事件问题的症结所在。如果事件一开始就是一等公民，我们就不会写出这么难看的东西。

二等公民的地位不仅意味着我们不能将事件本身作为参数传递，还意味着我们必须明确说明参数类型。一般来说，事件的委托类型（本例中为 `FileSystemEventHandler`）与其事件参数类型（此处为 `FileSystemEventArgs`）之间的关系不是 C# 的类型推断所能自动确定的，这就是为什么我们必须明确指定这两种类型的原因。(使用通用 `EventHandler<T>` 类型的事件更适合类型推断，可以使用稍显简洁的 `FromEventPattern` 版本。遗憾的是，真正使用这种方式的事件相对较少。有些事件除了说明刚刚发生的事情外，并不提供任何信息，它们使用的是基本的 `EventHandler` 类型，对于这类事件，实际上可以完全省略类型参数，从而使代码不那么难看。不过，您仍然需要提供添加和删除回调（add and remove callbacks）。

请注意，本例中 `FromEventPattern` 的返回类型是：

`IObservable<EventPattern<FileSystemEventArgs>>`

`EventPattern<T>` 类型封装了事件传递给处理程序的信息。大多数 .NET 事件都遵循一种常见的模式，即处理程序方法需要两个参数：一个是 `object sender`，它只是告诉您是哪个对象引发了事件（如果您将一个事件处理程序附加到多个对象上，它就会非常有用），另一个参数是从 `EventArgs` 派生的某种类型，它提供了有关事件的信息。`EventPattern<T>` 只是将这两个参数打包成一个提供 `Sender` 和 `EventArgs` 属性的对象。事实上，如果您不想将一个处理程序附加到多个源，您只需要 `EventArgs` 属性，这就是为什么早期的 `FileSystemWatcher` 示例会继续提取该属性，以获得 `IObservable<FileSystemEventArgs>` 类型的更简单结果。我们将在稍后详细介绍选择操作符：

```
IObservable<FileSystemEventArgs> changes = changeEvents.Select(ep => ep.EventArgs);
```

将属性更改事件作为可观察序列公开是很常见的。.NET 运行时库定义了一个基于 .NET 事件的接口 `INotifyPropertyChanged`，用于公布属性更改，而一些用户界面框架对此有更专门的系统，如 WPF 的 `DependencyProperty`。如果您正在考虑编写自己的封装程序来做这类事情，我强烈建议您先看看 [Reactive UI 库](https://github.com/reactiveui/ReactiveUI/)。它有一套将[属性封装为 IObservable<T> 的功能](https://www.reactiveui.net/docs/handbook/when-any/)。

### 源自任务

`Task` 和 `Task<T>` 类型在 .NET 中使用非常广泛。主流 .NET 语言都内置了对它们的支持（例如 C# 的 `async` 和 `await` 关键字）。`Task` 和 `IObservable<T>` 在概念上有一些重叠：两者都表示某种可能需要一段时间才能完成的工作。从某种意义上说，`IObservable<T>` 是对 `Task<T>` 的概括：两者都表示可能需要长时间运行的工作，但 `IObservable<T>` 可以产生多个结果，而 `Task<T>` 只能产生一个结果。

由于 `IObservable<T>` 是更通用的抽象，我们应该可以将 `Task<T>` 表示为 `IObservable<T>`。为此，Rx 为 `Task` 和 `Task<T>` 定义了各种扩展方法。这些方法都被称为 `ToObservable()`，它提供了各种重载，在需要时提供细节控制，并简化了最常见的应用场景。

虽然它们在概念上相似，但 `Task<T>` 在细节上有一些不同之处。例如，您可以检索其 `Status` 属性，该属性可能会报告任务处于取消或故障状态。而 `IObservable<T>` 没有提供一种询问源状态的方法；它只是告诉你事情发生了。因此，`ToObservable` 在以 Rx 方式呈现状态方面做出了一些决策，使其在 Rx 世界中更有意义：

- 如果任务被[取消](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-canceled)，`IObservable<T>` 会调用订阅者的 `OnError`，并传递一个 `TaskCanceledException`。
- 如果任务[发生故障](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-faulted)，`IObservable<T>` 会调用订阅者的 `OnError` 并传递任务的内部异常
- 如果任务尚未进入最终状态（既不是 [Cancelled](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-canceled)，也不是 [Faulted](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-faulted)，或 [RanToCompletion](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-rantocompletion)），`IObservable<T>` 将不会产生任何通知，直到任务进入这些最终状态之一为止

调用 `ToObservable` 时，任务是否已处于最终状态并不重要。如果任务已经结束，`ToObservable` 将只返回一个代表该状态的序列。(事实上，它会使用你之前看到的 `Return` 或 `Throw` 创建方法）。如果任务尚未完成，`ToObservable` 将为任务附加一个延续，以检测任务完成后的结果。

任务有两种形式：会产生结果的 `Task<T>` 和不会产生结果的 `Task`。但在 Rx 中，只有 `IObservable<T>`--没有无结果形式。我们以前已经遇到过一次这样的问题，当时 `Observable.Start` 方法需要将委托调整为 `IObservable<T>`，即使委托是一个不产生结果的 `Action`。解决方案是返回一个 `IObservable<Unit>`，这也正是在普通任务上调用 `ToObservable` 时得到的结果。

扩展方法的使用很简单：

```c#
Task<string> t = Task.Run(() =>
{
    Console.WriteLine("Task running...");
    return "Test";
});
IObservable<string> source = t.ToObservable();
source.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("completed"));
source.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("completed"));
```

输出：

```
Task running...
Test
completed
Test
completed
```

请注意，即使有两个订阅者，任务也只运行一次。这并不奇怪，因为我们只创建了一个任务。如果任务尚未完成，那么所有订阅者都将在任务完成时收到结果。如果任务已完成，那么 `IObservable<T>` 实际上就变成了一个单值冷观测值。

#### 每次订阅一个任务

还有一种不同的方法可以为源代码获取 `IObservable<T>`。我可以用以下语句替换前面示例中的第一条语句：

```c#
IObservable<string> source = Observable.FromAsync(() => Task.Run(() =>
{
    Console.WriteLine("Task running...");
    return "Test";
}));
```

两次订阅会产生略微不同的输出结果：

```
Task running...
Task running...
Test
Test
completed
completed
```

请注意，这样会执行两次任务，每次调用 `Subscribe` 都会执行一次。`FromAsync` 可以做到这一点，因为我们传递的不是一个 `Task<T>`，而是一个返回 `Task<T>` 的回调。当我们调用 `Subscribe` 时，它会调用该回调，因此每个订阅者基本上都会得到自己的任务。

如果我想使用 `async` 和 `await` 来定义我的任务，那么我就不需要使用 `Task.Run`，因为 `async` lambda 会创建一个 `Func<Task<T>>`，这正是 `FromAsync` 想要的类型：

```c#
IObservable<string> source = Observable.FromAsync(async () =>
{
    Console.WriteLine("Task running...");
    await Task.Delay(50);
    return "Test";
});
```

输出结果与之前完全相同。但这有一个微妙的区别。当我使用 `Task.Run` 时，lambda 从一开始就在任务池线程上运行。但当我这样写时，无论哪个线程调用 `Subscribe`，lambda 都会开始运行。只有当遇到第一个 `await` 时，它才会返回（调用 `Subscribe` 时也会返回），方法的其余部分将在线程池中运行。

### 源自 `IEnumberable<T>`

Rx 定义了另一个名为 `ToObservable` 的扩展方法，这次是针对 `IEnumerable<T>` 的。在前面的章节中，我描述了 `IObservable<T>` 是如何被设计来表示与 `IEnumerable<T>` 相同的基本抽象的，唯一的区别在于我们用来获取序列中元素的机制：对于 `IEnumerable<T>`，我们编写代码从集合中提取值（例如，`foreach` 循环），而 `IObservable<T>` 则通过调用 `IObserver<T>` 上的 `OnNext` 向我们推送值。

我们可以编写从拉到推的桥接代码：

```c#
// Example code only - do not use!
public static IObservable<T> ToObservableOversimplified<T>(this IEnumerable<T> source)
{
    return Observable.Create<T>(o =>
    {
        foreach (var item in source)
        {
            o.OnNext(item);
        }

        o.OnComplete();

        // Incorrectly ignoring unsubscription.
        return Disposable.Empty;
    });
}
```

这个粗糙的实现传达了基本思想，但还很幼稚。它没有尝试处理取消订阅问题，而且在这种特殊情况下使用 `Observable.Create` 时，要解决这个问题并不容易。正如我们在本书后面将看到的，Rx 源可能会尝试快速连续地交付大量事件，因此应与 Rx 的并发模型集成。当然，Rx 提供的实现方式会考虑到所有这些棘手的细节。这使得它变得更加复杂，但这正是 Rx 的问题所在；你可以认为它在逻辑上等同于上图所示的代码，只是没有缺点而已。

事实上，这是 Rx.NET 中反复出现的主题。许多内置运算符之所以有用，并不是因为它们做了什么特别复杂的事情，而是因为它们为你处理了许多微妙而棘手的问题。在考虑推出自己的解决方案之前，您应该总是尽量找到 Rx.NET 内置的、能满足您需要的东西。

从 `IEnumerable<T>` 过渡到 `IObservable<T>` 时，您应该仔细考虑您真正想要实现的目标。考虑到 `IEnumerable<T>` 的阻塞同步（拉）特性与 `IObservable<T>` 的异步（推）特性总是不能很好地结合。一旦有东西订阅了以这种方式创建的 `IObservable<T>`，它实际上就是要求遍历 `IEnumerable<T>`，立即产生所有值。对 `Subscribe` 的调用可能在到达 `IEnumerable<T>` 的末尾时才返回，这与[本章开头](#非常基本的 IObservable<T> 实现)所示的非常简单的示例类似。(我之所以说“可能”，是因为我们在学习调度程序时会发现，具体行为取决于上下文）。`ToObservable` 无法施展魔法--某个地方必须执行相当于 `foreach` 循环的操作。

因此，尽管这是一种将数据序列带入 Rx 世界的便捷方法，但还是应该仔细测试并衡量其对性能的影响。

### 源自APM

Rx 支持古老的 .NET [异步编程模型（APM）](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/asynchronous-programming-model-apm)。在.NET 1.0 中，这是表示异步操作的唯一模式。2010 年，.NET 4.0 引入[基于任务的异步模式](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap) (TAP) 后，该模式被取代。旧的 APM 与 TAP 相比没有任何优势。此外，C# 的 `async` 和 `await` 关键字（以及其他 .NET 语言中的对应关键字）只支持 TAP，这意味着最好避免使用 APM。不过，TAP 在 2011 年 Rx 1.0 发布时还是相当新的，因此它提供了将 APM 实现作为 `IObservable<T>` 呈现的适配器。

今天应该没有人会使用 APM，但为了完整起见（而且万一你不得不使用一个只提供 APM 的古老库），我将对 Rx 对 APM 的支持做一个非常简短的解释。

调用 `Observable.FromAsyncPattern` 的结果并不返回可观察序列。它返回的是一个能返回可观察序列的委托。(该委托的签名将与调用 `FromAsyncPattern` 的通用参数相匹配，只是返回类型将封装在可观察序列中。下面的示例封装了 `Stream` 类的 `BeginRead/EndRead` 方法（这是 APM 的实现）。

注意：这纯粹是为了说明如何封装 APM。由于 Stream 多年来一直支持 TAP，因此在实践中永远不会这样做。

```c#
Stream stream = GetStreamFromSomewhere();
var fileLength = (int) stream.Length;

Func<byte[], int, int, IObservable<int>> read = 
            Observable.FromAsyncPattern<byte[], int, int, int>(
              stream.BeginRead, 
              stream.EndRead);
var buffer = new byte[fileLength];
IObservable<int> bytesReadStream = read(buffer, 0, fileLength);
bytesReadStream.Subscribe(byteCount =>
{
    Console.WriteLine(
        "Number of bytes read={0}, buffer should be populated with data now.", 
        byteCount);
});
```

## 主题（Subject）

到目前为止，本章已经探讨了各种返回 `IObservable<T>` 实现的工厂方法。不过还有另一种方法：`System.Reactive` 定义了各种实现 `IObservable<T>` 的类型，我们可以直接实例化这些类型。但我们如何确定这些类型产生的值呢？我们之所以能做到这一点，是因为这些类型也实现了 `IObserver<T>`，使我们能够向其中推送值，而我们推送的这些值将被观察者看到。

同时实现 `IObservable<T>` 和 `IObserver<T>` 的类型在 Rx 中被称为主题。有一个 `ISubject<T>` 来表示这一点。(它位于 `System.Reactive` NuGet 包中，与 `IObservable<T>` 和 `IObserver<T>` 不同，它们都内置于 .NET 运行时库中）。`ISubject<T>` 看起来像这样：

```c#
public interface ISubject<T> : ISubject<T, T>
{
}
```

原来还有一个两个参数的 `ISubject<TSource, TResult>`，以适应既是观察者又是可观察对象的实体可能以某种方式转换通过它流动的数据的情况，这意味着输入和输出类型不一定相同。下面是两个类型参数的定义：

```c#
public interface ISubject<in TSource, out TResult> : IObserver<TSource>, IObservable<TResult>
{
}
```

正如你所看到的，`ISubject` 接口没有定义任何自己的成员。它们只是继承自 `IObserver<T>` 和 `IObservable<T>`--这些接口只是直接表达了主体既是观察者又是可观察者这一事实。

但这有什么用呢？我们可以把 `IObserver<T>` 和 `IObservable<T>` 分别看作“消费者”和“发布者”接口。因此，主体既是消费者，也是发布者。数据既可以流入主题，也可以流出主题。

Rx 提供了一些主题实现，这些实现偶尔会对希望提供 `IObservable<T>` 的代码有用。虽然 `Observable.Create` 通常是实现这一功能的首选方法，但在一种重要的情况下，主题可能更有意义：如果您的代码发现了感兴趣的事件（例如，通过使用某些消息传递技术的客户端 API），并希望通过 `IObservable<T>` 使其可用，那么主题有时会提供比 `Observable.Create` 或自定义实现更方便的方法。

Rx 提供了几种主题类型。我们将从最简单易懂的开始。

### `Subject<T>`

`Subject<T>` 类型会立即将对其 `IObserver<T>` 方法的任何调用转发给当前订阅它的所有观察者。本示例展示了它的基本操作：

```c#
Subject<int> s = new();
s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));

s.OnNext(1);
s.OnNext(2);
s.OnNext(3);
```

我创建了一个 `Subject<int>`。我订阅了它两次，然后反复调用它的 `OnNext` 方法。结果如下，说明 `Subject<int>` 将每次 `OnNext` 调用都转发给了两个订阅者：

```
Sub1: 1
Sub2: 1
Sub1: 2
Sub2: 2
Sub1: 3
Sub2: 3
```

我们可以用这种方法将接收数据的 API 与 Rx 世界连接起来。您可以想象编写这样的程序：

```c#
public class MessageQueueToRx : IDisposable
{
    private readonly Subject<string> messages = new();

    public IObservable<string> Messages => messages;

    public void Run()
    {
        while (true)
        {
            // Receive a message from some hypothetical message queuing service
            string message = MqLibrary.ReceiveMessage();
            messages.OnNext(message);
        }
    }

    public void Dispose()
    {
        message.Dispose();
    }
}
```

将其修改为使用 `Observable.Create` 并不难。但是，如果您需要提供多个不同的 `IObservable<T>` 源，这种方法就会变得更加简单。想象一下，我们根据内容区分不同的消息类型，并通过不同的观察对象发布它们。如果我们仍想通过单个循环从队列中拉取消息，就很难用 `Observable.Create` 来安排。

`Subject<T>` 还会将对 `OnCompleted` 或 `OnError` 的调用分发给所有的订阅者。当然，Rx 的规则要求一旦你在 `IObserver<T>` 上调用了 `OnCompleted` 或 `OnError` 方法（而任何 `ISubject<T>` 都是 `IObserver<T>`，所以这个规则适用于 `Subject<T>`），你就不能再次在该观察者上调用`OnNext`、`OnError` 或 `OnCompleted` 方法。事实上，`Subject<T>` 会容忍违反这个规则的调用，它只是忽略它们，所以即使你的代码在内部没有完全遵守这些规则，你向外界呈现的 `IObservable<T>` 也会表现正确，因为 Rx 强制执行这一点。

`Subject<T>` 实现了 `IDisposable`。释放 `Subject<T>` 会使其进入一种状态，在这种状态下，如果你调用它的任何方法，它都会抛出异常。文档还将其描述为取消订阅所有观察者，但是由于已经 `Dispose` 的 `Subject<T>` 无论如何都无法再产生任何进一步的通知，这实际上并没有什么意义。（请注意，在`Dispose` 时它不会调用观察者的 `OnCompleted` 方法。）唯一的实际效果是，它内部用于跟踪观察者的字段会被重置为一个特殊的标识值，表示它已经被`Dispose`，这意味着“取消订阅”观察者的唯一外部可观察效果是，如果出于某种原因，你的代码在 `Dispose` 之后仍然保留了对 `Subject<T>` 的引用，那么它将不再为 GC 目的保持所有订阅者的可达性。如果一个 `Subject<T>` 在不再使用后仍然始终可达，那本身实际上是一个内存泄漏，但是 `Dispose` 至少可以限制其影响：只有 `Subject<T>` 本身仍然可达，而不是所有的订阅者。

`Subject<T>` 是最直接的主题，但还有其他更专业的主题。

### `ReplaySubject<T>`

`Subject<T>` 不会记忆任何内容：它会立即将接收到的值分配给订阅者。如果有新的订阅者出现，他们只能看到订阅后发生的事件。另一方面，`ReplaySubject<T>` 可以记住它所看到的每一个值。如果有新的主题出现，它将收到迄今为止发生的事件的完整历史记录。

这是前面 [`Subject<T>` 部分](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/03_CreatingObservableSequences.md#subjectt)第一个示例的变体。它创建的是 `ReplaySubject<int>` 而不是 `Subject<int>`。它不是立即订阅两次，而是先创建一个初始订阅，然后在发送了几个值后再创建第二个订阅。

```c#
ReplaySubject<int> s = new();
s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));

s.OnNext(1);
s.OnNext(2);

s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));

s.OnNext(3);
```

输出：

```
Sub1: 1
Sub1: 2
Sub2: 1
Sub2: 2
Sub1: 3
Sub2: 3
```

如您所料，我们最初只看到了 `Sub1` 的输出。但当我们第二次调用 subscribe 时，我们可以看到 `Sub2` 也收到了前两个值。然后，当我们报告第三个值时，两个子程序都看到了。如果这个示例使用的是 `Subject<int>`，我们就只能看到这样的输出：

```
Sub1: 1
Sub1: 2
Sub1: 3
Sub2: 3
```

这里有一个显而易见的潜在问题：如果 `ReplaySubject<T>` 会记住发布给它的每一个值，那么我们就不能将它用于无穷无尽的事件源，因为它最终会导致内存不足。

`ReplaySubject<T>` 提供的构造函数可以接受简单的缓存到期设置，从而限制内存消耗。其中一个选项是指定要记住的项目的最大数量。下面的示例创建了一个缓冲区大小为 2 的 `ReplaySubject<T>`：

```c#
ReplaySubject<int> s = new(2);
s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));

s.OnNext(1);
s.OnNext(2);
s.OnNext(3);

s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));

s.OnNext(4);
```

由于第二个订阅是在我们已经生成 3 个值后才出现的，因此它不再能看到所有值。它只会收到订阅前发布的最后两个值（当然，第一个订阅会继续看到所有值）：

```
Sub1: 1
Sub1: 2
Sub1: 3
Sub2: 2
Sub2: 3
Sub1: 4
Sub2: 4
```

或者，也可以通过向 `ReplaySubject<T>` 构造函数传递 `TimeSpan` 来指定基于时间的限制。

### `BehaviorSubject<T>`

与 `ReplaySubject<T>` 一样，`BehaviorSubject<T>` 也有一个内存，但它只记住一个值。然而，它与缓冲区大小为 1 的 `ReplaySubject<T>` 并不完全相同。`ReplaySubject<T>` 一开始的状态是其内存中什么都没有，而 `BehaviorSubject<T>` 则始终只记住一个项目。在我们第一次调用 `OnNext` 之前，这怎么能行呢？`BehaviorSubject<T>` 通过要求我们在构造它时提供初始值来实现这一点。

因此，您可以将 `BehaviorSubject<T>` 视为一个始终具有可用值的主体。如果您订阅了 `BehaviorSubject<T>`，它将立即产生一个值。(随后它可能会产生更多的值，但它总是会立即产生一个值）。同时，它还会通过一个名为 `Value` 的属性提供该值，因此您无需为了获取该值而向它订阅 `IObserver<T>`。

`BehaviorSubject<T>` 可以看作是一个可观察的属性。与普通属性一样，只要您提出要求，它就能立即提供一个值。不同的是，它可以在每次值发生变化时通知您。如果您使用的是 [ReactiveUI 框架](https://www.reactiveui.net/)（基于 Rx 的构建用户界面的框架），那么 `BehaviourSubject<T>` 作为视图模型（在底层领域模型和用户界面之间起中介作用的类型）中的属性的实现类型是有意义的。它具有类似于属性的行为，使您可以随时检索值，但它也提供更改通知，ReactiveUI 可以处理更改通知，以保持用户界面的最新状态。

在完成（completion）方面，这个比喻略有不足。如果你调用 `OnCompleted`，它会立即在所有观察者上调用 `OnCompleted`，如果有任何新的观察者订阅，它们也会立即收到完成通知，它不会先提供最后一个值。（因此，在这一点上它与缓冲区大小为 1 的 `ReplaySubject<T>` 又有所不同。）

同样，如果调用 `OnError`，所有当前的观察者都将收到 `OnError` 调用，而任何后续的订阅者也只会收到 `OnError` 调用。

### `AsyncSubject<T>`

`AsyncSubject<T>` 向所有观察者提供它收到的最终值。由于在调用 `OnCompleted` 之前它无法知道哪个是最终值，因此在调用 `OnCompleted` 或 `OnError` 方法之前，它不会调用任何订阅者的任何方法。(如果 `OnError` 被调用，它就会将此信息转发给所有当前和未来的订阅者）。我们经常会间接使用这个主题，因为它是 Rx 与 `await` 关键字集成的基础。(当你等待一个可观察序列时，`await` 返回的是源发出的最终值）。

如果在 `OnCompleted` 之前没有调用 `OnNext`，那么就没有最终值，因此它只会完成任何观察者，而不会提供值。

在本例中，由于序列从未完成，因此不会发布任何值。任何值都不会写入控制台。

```c#
AsyncSubject<string> subject = new();
subject.OnNext("a");
subject.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
subject.OnNext("b");
subject.OnNext("c");
```

在本例中，我们调用了 `OnCompleted` 方法，这样就会有一个最终值（'c'）供主体生成：

```c#
AsyncSubject<string> subject = new();

subject.OnNext("a");
subject.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
subject.OnNext("b");
subject.OnNext("c");
subject.OnCompleted();
subject.Subscribe(x => Console.WriteLine($"Sub2: {x}"));
```

输出：

```
Sub1: c
Sub2: c
```

如果您的应用程序启动时需要完成一些可能很慢的工作，而且只需完成一次，您可能会选择 `AsyncSubject<T>` 来提供工作结果。需要这些结果的代码可以订阅该主题。如果工作尚未完成，它们将在结果可用时立即收到。如果工作已经完成，它们将立即收到结果。

### 主题工厂

最后，值得注意的是，您还可以通过工厂方法创建主题。考虑到主题结合了 `IObservable<T>` 和 `IObserver<T>` 接口，似乎应该有一个工厂允许您自己将它们结合起来。`Subject.Create(IObserver<TSource>, IObservable<TResult>)` 工厂方法就是这样提供的。

```c#
// 从指定的观察者（用于向主体发布信息）和可观察者（用于订阅从主体发送的信息）中创建主体
public static ISubject<TSource, TResult> Create<TSource, TResult>(
    IObserver<TSource> observer, 
    IObservable<TResult> observable)
{...}
```

请注意，与刚才讨论的所有其他主题不同，这个主题创建的输入和输出之间没有内在联系。它只是将你提供的 `IObserver<TSource>` 和 `IObserver<TResult>` 实现封装在一个对象中。对主体的 `IObserver<TSource>` 方法的所有调用都将直接传递给您提供的观察者。如果您希望值出现在相应的 `IObservable<TResult>` 的订阅者中，这就需要您来实现了。这实际上是用最少的胶水将您提供的两个对象结合在一起。

主题（Subjects）提供了一种方便的方法来探索 Rx，并在某些生产场景中偶尔会有用，但并不推荐在大多数情况下使用它们。有关说明可以在[《使用指南》附录](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/C_UsageGuidelines.md)中找到。与使用主题不同，更倾向于使用本章前面展示的工厂方法。

## 总结

我们已经看过了创建序列的各种及时和延迟的方式。我们已经了解了如何使用各种工厂方法生成基于计时器的序列。我们还探讨了从其他同步和异步表示形式过渡的方式。

简要回顾：

- 工厂方法
  - Observable.Return
  - Observable.Empty
  - Observable.Never
  - Observable.Throw
  - Observable.Create
  - Observable.Defer
- 生成式方法
  - Observable.Range
  - Observable.Generate
  - Observable.Interval
  - Observable.Timer
- 适配器
  - Observable.Start
  - Observable.FromEventPattern
  - Task.ToObservable
  - Task<T>.ToObservable
  - IEnumerable<T>.ToObservable
  - Observable.FromAsyncPattern

创建可观测序列是我们实际应用 Rx 的第一步：创建序列，然后将其公开以供使用。既然我们已经掌握了如何创建可观测序列，我们就可以更详细地了解操作符，它们允许我们描述要应用的处理过程，从而建立更复杂的可观测序列。