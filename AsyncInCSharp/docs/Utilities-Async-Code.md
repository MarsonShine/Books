基于任务的异步模式旨在简化创建处理任务的实用程序。因为所有的 TAP 方法都会返给你一个 Task，任何我们写的 TAP方法，它的行为在其他的任务都能再次使用。这节，我们来关注下 Task 的实用性，包括以下几点：

- 方法看起来像 TAP 方法，但是具有有用的特殊行为，而不是异步方法调用本身
- 组合，那些处理 Task 的那些方法，会生成新的有用的基于 Task 的任务
- 取消和显示进度条在异步操作期间的工具

当这些实用性的东西存在的时候，它是非常有用的，你们自己很容易的去实现他们，这样你在未来就需要熟悉一些 .NET Framwork 没提供的工具。

# 延迟一段时间

大多数情况下简单的耗时长的操作，你也许想要尽可能的在一段时间内不要做任何事。在同步的世界里这就等价于 **Thread.Sleep**。事实上你可以在 Task.Run 中结合使用 Thread.Sleep：

```c#
await Task.Run(() => Thread.Sleep(100));
```

但是这个简单的方法纯属浪费。一个线程被用来在某个时间段阻塞，这也太浪费了。.NET 已经有一种方式在某个时间段之后调用你的代码，它不需要任何线程，它就是 **System.Threading.Timer**。设置一个 Timer（触发器）会更加有效，然后使用 TaskCompletionSource 来创建 Task，当 **Timer** 触发的时候我们就能获得它完成的信息：

```c#
public static Task Delay(int millis) {
    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
    Timer timer = new Timer(_ => tcs.SetResult(null), null, millis, Timeout.Infinite);
    tcs.Task.ContinueWith(delegate { timer.Dispose(); });
    return tcs.Task;
}
```

当然，这对于 .NET Framwork 提供的这个工具是非常有用的。它就是 **Task.Delay**，并且 .NET Framwork 提供的要比我写的更加有用，健壮和高效。

# 等待任务集合

之前的章节 “任务和等待” 有提到过，它非常容易并行的运行多个异步操作通过依次调用他们，然后依次等待他们。我们也将会在第九章发现，我们等待每一个我们开始的任务，它是很重要的，否则异常就会丢失。

解决方案就是使用 **Task.WhenAll**，它是非常实用的，它能将多个 Task 并产生一个聚合的 Task，一旦所有的 Task 完成时，这个聚合的 Task 就会完成。这里有一个最简单的 WhenAll 的代码，它提供泛型的重载：

```c#
Task WhenAll(IEnumerable<Task> tasks)
```

使用 WhenAll 和 等待多个 tasks 之间不同的关键就是 **WhenAll** 让异常抛出的时候会得到一个对的行为。从这点来看，你应该经常是用 WhenAll。

泛型版本的 **WhenAll** 还提供数组，它包含你给的各个 Task 的结果。这是很方便的，但不是必要的，因为你还是可以访问原来的 Tasks，所以你可以使用 **Result** 属性已知道它们必须要完成。

我们来重新回顾一下图标下载的那个案例。我们现在有一个要调用 **async void** 的方法来依次下载每一个图标。然后那个方法会把下载下来的图标在程序中显示。并行的下载，这个方法非常高效，但是有两个问题：

- 图标在程序中出现，依赖于下载完成的顺序。
- 因为每个图片下载都独立与 **async void** 方法当中，在 UI 线程抛出异常来封装它，这样做是非常困难的

所以让我们来重构这个方法，让遍历所有的图标本身是异步的。也就是说我们能把这些异步操作当做一组行为来控制。我们从重构之后这一刻开始，我们开始依次处理每个图片：

```c#
private async void GetButton_OnClick(object sender, RoutedEventArgs e){
    foreach (string domain in s_Domains){
        Image image = await GetFavicon(domain);
        AddAFavicon(image);
    }
}
```

现在我们修复并行下载图标这个问题，也可以有序的显示。我们一开始就把通过调用 **GetFavicon** 的所有下载以及把 Tasks 存储到集合中。

```c#
List<Task<Image>> tasks = new List<Task<Image>>();
foreach(string domain in s_Domains){
    tasks.Add(GetFavicon(domain));
}
```

或者如果你喜欢 LINQ 的话更好：

```c#
IEnumerable<Task<Image>> tasks = s_Domains.Select(GetFavicon);
//IEnumerable Filter 是惰性的，ToList 表示任务开始运行
tasks = tasks.ToList();
```

一旦我们有了一组任务，我们可以把这组任务给到 **WhenAll** 以及当所有任务完成是，它会给我一个 task，并带上所有的结果。

```c#
Task<Image[]> allTask = Task.WhenAll(tasks)
```

然后，我们剩下要做的就是 **await** 所有的 task 以及使用结果集合

```c#
Image[] images = await AllTask;
foreach(Image eachImage in images){
    AddFavicon(eachImage);
}
```

所以，我们能成功的写一些代码，那些远离复杂的并行逻辑，而只需要少许行的代码就行了。最后在 WhenAll 下能得到所有结果。

# 等待任务集合中的任意一个

其它通用的你可能需要的工具就是，处理多个任务时，只需要等待第一个完成就行了。这可能是因为你正在请求一个稀有的资源并且其中尽早的返回使用。

这个工作的工具就是 **Task.WhenAny**。也有泛型的版本，有很多重载，但是只有这个是非常有趣的：

```c#
Task<Task<T>> WhenAny(IEnumerable<Task<T>> tasks)
```

WhenAny 的方面签名要比 WhenAll 要稍微难懂一点，并且这是有道理的。当发生异常时，WhenAny 是一个需要小心使用的工具。如果你想找出你程序当中所有的异常，你就需要确保每个 task 是被等待的，否则异常就会丢失。简单的使用 **WhenAny** 并且忘记其他的 Task 就等价于捕获了所有的异常并忽略它们，这是坏的实践方式并且它会以微妙的 bugs 以及无效的状态的形式出现。

WhenAny 返回的类型是 Task<Task<T>>。也就是说在你等待它之后，你会获得一个 Task<T>。这个 Task<T> 是那些原始的 tasks 中最新完成的那个，就是说当你获得它的时候就已经完成了。它给你一个 Task 而不是具体的 T 类型的结果的原因就是，你要知道你第一个完成的原始任务是什么，所以你可以取消以及等待其他所有的任务。

```c#
Task<Task<Image>> anyTask = Task.WhenAny(tasks);
Task<Image> winner = await anyTask;
Image image = await winner;//这总是同步完成的
AddAFavicon(image)
foreach(Task<Image> eachTask in tasks){
    if(eachTask != winner){
        await eachTask;
    }
}
```

使用胜者（最新成功返回的 task）会更新 UI 这是没有损伤的，但是之后，你应该等待其它所有的 Task。要期望他们全部都要成功，这段额外的代码对于你的程序来说没有任何坏处。但是，如果其中一个失败了，就意味着你可以发现它，并能修复这个 bug。

# 创建你自己的组合器

我们调用 **WhenAll** 以及 **WhenAny **异步组合器。当它们返回 tasks 时，他们自己本来就不是异步方法，但是以有用的方式结合其他 task。如果需要，你也可以写自己的组合器，有一个能重用的并行行为的画板，你可以运用在你所喜欢的地方。

来写一个组合器的例子。假设，我们在 task 上要加一个超时设置。尽管我们可以很容易的从头开始写，它可以作为一个很好的例子使用 **Delay** 以及 **WhenAny**。一般情况下，组合器最容易用 async 来实现，在像这个例子，当然有一些细节你是不需要的。

```c#
private static async Task<T> WithTimeout<T>(Task<T> task,int timeout){
    Task delayTask = Task.Delay(timeout);
    Task firstToFinish = await Task.WhenAny(task, delayTask);
    if(firstToFinish == delayTask){
        //延迟任务先完成 - 说明超时 当做处理异常处理
        task.ContinueWith(HandleException);
        throw new TimeoutException();
    }
    return await task;
}
```

我的技术点是用 Task 创建一个延时任务，它将在超时后发生。然后我用 WhenAny 来选择消费在原始的 Task 以及延时的 Task 这两个两个 Task 谁先完成。这个例子就是看谁先完成，延时任务完成则抛出异常，否则就是返回结果。

要注意，我在这个例子中我很小心这个延时异常，当延时发生的时候。我用 **ContinuWith** 捕捉了原始任务的继续任务，它用来出异常或是返回结构之后的后续任务。我知道延时是不会抛出异常的，所以我根本无需处理它。HandleException 方法可以像下面一样这么实现：

```c#
private static void HandleException<T>(Task<T> task)
{
    if (task.Exception != null)
    {
        logging.LogException(task.Exception);
    }
}
```

显然，这里做什么明显是取决于你处理异常的策略。通过 **ContinuWith** 捕获异常，我就能确保原始 task 何时才能完成，可能在以后的某个时间点，运行了异常检查。重要的是，这不会影响主程序的运行，因为它早就做了准备当超时的时候它要做什么。

# 取消异步操作

对比 Task 类，TAP 通过 CancellationToken 类型是可以取消的。按照约定，任何 TAP 方法都取消，都有一个带有 **CancellatonToken** 参数的方法重载。举个在 .Framework 中的 **DbCommand** 类型的例子，它查询数据库的方法是异步的。最简单的重载是无参的 ExecuteNonQueryAsync。

```c#
Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken token);
```

我们来看下我们怎么调用取消取消异步任务的。首先，我们需要 **CancellationTokenSource** 类，它会生成 **CancellationToken** 并且控制它们，它跟 **TaskCompletionSource** 生成 Task 并控制它是一样的。下面这段代码是未完成的，但是我会展示一些你需要的技术给你：

```c#
CancellationTokenSource cts = new CancellationTokenSource();
cancelButton.Click += delegate { cts.Cancel(); };
int result = await dbCommand.ExecuteNonQueryAsync(cts.Token);
```

当你调用 **CancellationTokenSource.Cancel** 方法时，**CancellationToken** 转变为取消状态。当它发生取消时，它可能会注册一个委托来调用，但是实际上，一个更简单的方法就是去轮询检查你的方法是否调用了取消，这样更加有效。如果你在异步方法中轮询，并且 **CancellationToken** 是可用的，你就应该在循环迭代中调用 ThrowIfCancellationRequested：

```c#
foreach (var x in thingsToProcess)
{
	cancellationToken.ThrowIfCancellationRequested();
    // Process x ...
}
```

