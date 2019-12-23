# 深入理解 ValueTask

.NET Framework 4 里面的命名空间为 `System.Threading.Tasks`的 `Task` 类。这个类以及它派生的 `Task<TResult>` 早已成为编程的主要部分，在 C#5 中的异步编程模式当作介绍了 `async/await`。在这篇文章里，我会覆盖新的类 `ValueTask / ValueTask<TResult>`，介绍它们在通用的使用上降低内存消耗来提高异步性能，这是非常重要的。

## Task

Task 有多种用途，但是它的核心就是 “promise”，它表示最终完成的一些操作。你初始化一个操作，并返回给它一个 Task，它当操作完成的时候它会完成，这可能作为初始化操作的一部分同步发生（比如访问一个早就缓冲好了的缓冲区），也有能是异步的，在你完成这个任务时（比如访问一些还没有缓冲好的字节，但是很快就缓冲好了可以访问），或者是在你已经接收 Task 的时候异步完成（比如通过网络访问数据）。因为操作完成可能是异步的，所以你需要为结果等待它（但这经常违背异步编程的初衷）或者你必须提供一个回调函数来调用，当这个操作完成的时候。在 .NET 4 中，提供了如回调函数一样的来实现如 `Task.ContinueWith` 方法，它暴露通过传递一个委托的回调函数，这个函数在 Task 完成时触发：

```c#
SomeOperationAsync().ContinueWith(task =>{
    try {
        TResult result = task.Result;
        UseResult(result);
    } catch (Exception e) {
        HandleException(e);
    }
})
```

但是在 C# 5 以及 .NET Framwrok 4.5 中，`Task` 只需要 `await` 这样就能很简单的获取这个异步操作完成返回的结果，它生成的代码能够优化上述所有情况，无论操作是否同步完成，是否异步完成，还是在已经提供的回调异步完成，都可以正确地处理事情。

```c#
TResult result = await SomeOperationAsync();
UseResult(result);
```

`Task` 很灵活，并且有很多好处。例如你可以通过多个消费者并行等待多次。你可以存储一个到字典中，以便后面的任意数量的消费者等待，它允许为异步结果像缓存一样使用。如果场景需要，你可以阻塞一个等待完成。并且你可以在这些任务上编写和使用各种操作（就像组合器），例如 `WhenAny` 操作，它可以异步等待第一个操作完成。

然而，这种灵活性对于大多数情况下是不需要的：仅仅只是调用异步操作并且等待结果：

```c#
TResult result = await SomeOperationAsync();
UseResult(result);
```

如上用法，我们根本不需要多次等待，我们不需要处理并行等待，我们也不需要处理异步阻塞，我们更不需要去写组合器。我们只是简单的等待异步操作 promise 返回的结果。这就是全部，我们怎么写异步代码（例如 `TResult = SomeOperation();`），也能很自然而然的用 `async / await`。

进一步说，`Task` 会有潜在的副作用，在特定的场景中，这个例子被大量创建，并且高吞吐和高性能为关键概念：`Task` 是一个类。作为一个类，就是说任意操作创建一个 Task 都会分配一个对象，越来越多的对象都会被分配，所以 GC 操作也会越来越频繁，也就会消耗越来越多的资源，本来它应该是去做其他事的。

运行时和核心库能减轻大多数这种场景。举个例子，如果你写了如下代码：

```c#
public async Task WriteAsync(byte value)
{
    if(_bufferedCount == _buffer.Length)
    {
        await FlushAsync();
    }
    _buffer[_bufferedCount++] = value;
}
```

在常规的例子中，缓冲区有可用空间，并且操作是同步完成。当这样运行的时候，这里返回的 `Task` 没有任何特别之处，因为它不会返回值：这个返回 `Task` 就等价于返回一个 `void` 的同步方法。尽管如此，运行时能够简单缓存单个非泛型的 `Task` 以及对于所有的 `async Task` 同步完成的方法都能重复使用它（暴露的缓存的单例就像是 `Task.CompletedTask`）。例如你的代码可以这样：

```c#
public async Task<bool> MoveNextAsync()
{
    if(_bufferedCount == 0)
    {
        await FillBuffer();
    }
    return _bufferedCount > 0;
}
```

通常情况下，我们期望会有一些数据被缓冲，在这个例子中，这个方法检查 `_bufferedCount`，检验值大于 0，并返回 true；只有当前缓冲区域没有缓冲数据时，它才需要执行可能是异步完成的操作。由于这里是 `Boolean` 存在两个可能的结果（`true` 和 `false`），这里可能只有两个对象 `Task<bool>`，它需要表示所有可能的结果值，所以运行时会缓存两个对象，以及简单返回一个 `Task<bool>` 的缓存对象，它的结果值为 `true` 来避免必要的分配。只有当操作异步完成时，这个方法需要分配一个新的 `Task<bool>`，因为在它知道这个操作的结果之前，它需要将对象返回给调用者，并且还要必须有一个唯一的对象，当操作完成的时候将它存进去。

运行时为其他的类型很好的维护一个很小的缓存，但是用它来存储所有是行不通的。例如下面方法：

```c#
public async Task<int> ReadNextByteAsync()
{
    if(_bufferedCount == 0)
    {
        await FillBuffer();
    }
    if(_bufferedCount == 0) {
        return -1;
    }
    _bufferedCount--;
    return _buffer[_pisition++];
}
```

也经常会同步完成。但是不像 `Boolean` 那个例子，这个方法返回一个 `Int32` 的值，它大约有 40 亿中可能的值，并且为所有的情况缓存一个 `Task<int>`，将会消耗可能数以百计的千兆字节内存。运行时为 `Task<int>` 负责维护一个小的缓存，但是只有很小部分结果的值有用到，例如如果是同步完成的（数据缓存到缓存区），返回值如 4，它最后使用了缓存 task，但是如果这个操作是同步完成返回结果值如 42，它最后将分配一个新的 `Task<int>`，就类似调用 `Task.FromResult(42)`。

很多库实现了尝试通过维护它们自己的缓存来降低这个特性。例如 .NET Framwork 4.5 的 `MemoryStream.ReadAsync` 重载函数总是同步完成的，因为它只是从内存中读数据。`ReadAsync` 返回一个 `Task<int>`，这个 `Int32` 结果值表示读的字节数。`ReadAsync` 经常用在循环中，表示每次调用请求的字节数，`ReadAsync` 能够完全满足这个请求。因此，通常情况下的请求重复调用 `ReadAsync` 来同步返回一个 `Task<int>`，其结果与之前的调用相同。因此，`MemoryStream` 维护单个 task 的缓存，它成功返回最后一个 task。然后再接着调用，如果这个新的结果与缓存的 `Task<int>` 匹配，它只返回已经缓存的。否则，它会使用 `Task.FromResult` 来创建一个新的，存储到新的缓存 task 并返回。

即使如此，还有很多案例，这些操作同步完成并且强制分配一个 `Task<TResult>` 返回。

## `ValueTask<TResult>` 和同步完成

所有的这些都引发 .NET CORE 2.0 引入了一个新类型，可用于之前的 .NET 版本 ，在`System.Threading.Tasks.Extensions` Nuget 包中：`ValueTask<TResult>`。

`ValueTask<TResult>` 在 .NET Core 2.0 作为结构体引入的，它是 `TResult` 或 `Task<TResult>` 包装器。也就是说它能从异步方法返回并且如果这个方法同步成功完成，不需要分配任何内存：我们只是简单的初始化这个 `ValueTask<TResult>` 结构体，它返回 `TResult`。只有当这个方法异步完成时，`Task<Result>` 才需要被分配，通过 被创建的`ValueTask<TResult>` 来包装这个实力对象（为了最小化的大小的 `ValueTask<TResult>` 以及优化成功路径，一个异步方法它出现故障，并且出现未处理的异常，它还是会分配一个 `Task<TResult>` 对象，所以 `ValueTask<TResult>`能简单的包装 `Task<TResult>` 而不是必须添加额外的字段来存储异常信息）。

于是，像 `MemoryStream.ReadAsync` 这个方法，它返回一个 `ValueTask<int>`，它没有缓存的概念，并且能像下面代码一样编码：

```c#
public override ValueTask<int> ReadAsync(byte[] buffer, int offset, int count)
{
    try {
        int butyRead = Read(buffer, offset, count);
        return new ValueTask<int>(bytesRead);
    }
    catch (Exception e)
    {
        return new ValueTask<int>(Task.FromException<int>(e));
    }
}
```

## ValueTask<TResult> 和 异步完成

为了写一个异步方法不需要为结果类型占用额外的分配的情况下完成同步，是一个巨大的胜利。这就是为什么我们把 `ValueTask<TResult>` 添加到 .NET Core 2.0 的原因以及为什么我们期望去使用的新的方法返回 `ValueTask<TResult>` 而不是 `Task<TResult>`。例如，当我们添加新的 `ReadAsync` 重载函数到 `Stream` 类中是为了能够传递给 `Memory<byte>` 而不是 `byte[]`，我们使它返回的类型是 `ValueTask<TResult>`。这样，Stream（它提供 `ReadAsync` 同步完成方法）和之前的 `MemoryStream` 的例子一样，使用这个签名（ValueTask）能够减少内存分配。

然而，工作在高吞吐的服务时，我们还是要考虑尽可能的减少分配，也就是说要考虑减少以及移除异步完成相关的内存分配。

对于 `await` 模式，对于所有的异步完成的操作，我们都需要能够去处理返回表示完成事件的操作的对象：调用者必须能够传递当操作完成时要被调用的回调函数以及要求有一个唯一对象能够被重用，这需要有一个唯一的对象在堆上，它能够作为特定操作的管道。但是，这并不以为这一旦这个操作完成所有关于这个对象都能被重用。如果这个对象能够被重用，那么这个 API 维护一个或多个这样对象的缓存，并且为序列化操作重用，这意思就是说不能使用相同对象到多次异步操作，但是对于非并发访问是可以重用的。

在 .NET Core 2.1，`ValueTask<TResult>` 增强功能支持池化和重用。而不只是包装 `TResult` 或 `Task<TResult>`，y引入了一个新的接口，`IValueTaskSource<TResult>`，增强 `ValueTask<TResult>` 能够包装的很好。`IValueTaskSource<TResult>` 提供必要的核心的支持，以类似于 `Task<TResult>` 的方式来表示 `ValueTask<TResult>` 的异步操作：

```c#
public interface IValueTaskSource<out TResult>
{
	ValueTaskSourceStatus GetStatus(short token);
	void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOmCompletedFlags flags);
	TResult GetResult(short token);
}
```

`GetStatus` 用来满足像 `ValueTask<TResult>.Completed` 等属性，返回一个表示异步操作是否正在执行中还是是否完成还是怎么样（成功或失败）。`OnCompleted` 是被 `ValueTask<TResult>` 的可等待者用于当操作完成时，从 `await` 中挂起的回调函数继续运行。`GetResult` 用于获取操作的结果，就像操作完成后，等待着能够获得 `TResult` 或传播可能发生的所有异常。

绝大多数开发者不需要去看这个接口：方法简单返回一个 `ValueTask<TResult>`，它被构造去包装这个接口的实例，消费者并不知情（consumer is none-the-wiser）。这个接口主要就是让开发者关注性能的 API 能够避免内存分配。

在 .NET Core 2.1 有一些这样的 API。最值得注意的是 `Socket.ReceiveAsync` 和 `Socket.SendAsync`，有新增的重载，例如

```c#
public ValueTask<int> ReceiveAsync(Momory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default);
```

这个重载返回 `ValueTask<int>`。如果这个操作同步 完成，它能构造一个 `ValueTask<int>` 并得到一个合适的结果。

```c#
int result = ...;
return new ValueTask<int>(result);
```

`Socket` 实现了维护一个用于接收和一个用来发送的池对象，这样每次每个完成的对象不超过一个，这些重载函数都是 0 分配的，甚至是它们完成了异步操作。然后 `NetworkStream` 就出现了。举个例子，在 .NET Core 2.1 中 `Stream` 暴露这样一个方法：

```c#
public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, cancellationToken cancellationToken);
```

这个复写方法 `NetworkStream`。`NetworkStream.ReadAsync` 只是委托给 `Socket.ReceiveAsync`，所以从 `Socket` 转成 `NetworkStream`，并且 `NetworkStream.ReadAsync` 是高效的，无分配的。

## 非泛型 ValueTask

当 .NET Core 2.0 引入 `ValueTask<TResult>` ，它纯碎是为了优化同步完成的情况下，为了避免分配一个 `Task<TResult>` 存储可用的 `TResult`。这也就是说非泛型的 `ValueTask` 是不必要的：对于同步完成的情况，从 `Task` 返回的方法返回 `Task.CompletedTask` 单例，并且为 `async Task` 方法在运行时隐式的返回。

随着异步方法零开销的实现，非泛型 `ValueTask` 变得再次重要起来。因此，在 .NET Core 2.1 中，我们也引入了非泛型的 `ValueTask` 和 `IValueTaskSource`。它们提供泛型的副本版本，相同方式使用，在 void 类型使用。

## IValueTaskSource / IValueTaskSource<T> 实现

大多数开发者不需要实现这些接口。它们也不是那么容易实现的。如果你需要这么做，在 .NET Core 2.1 有一些内部实现作为参考。例如：

- [AwaitableSocketAsyncEventArgs](https://github.com/dotnet/corefx/blob/61f51e6b2b26271de205eb8a14236afef482971b/src/System.Net.Sockets/src/System/Net/Sockets/Socket.Tasks.cs#L808)
- [AsyncOperation](https://github.com/dotnet/corefx/blob/89ab1e83a7e00d869e1580151e24f01226acaf3f/src/System.Threading.Channels/src/System/Threading/Channels/AsyncOperation.cs#L37)<TResult>
- [DefaultPipeReader](https://github.com/dotnet/corefx/blob/a10890f4ffe0fadf090c922578ba0e606ebdd16c/src/System.IO.Pipelines/src/System/IO/Pipelines/Pipe.DefaultPipeReader.cs#L16)

为了让开发者想做的更加简单，在 .NET Core 3.0 中，我们计划引入所有封装这些逻辑到 `ManualResetValueTaskSource<TResult>` 类中去，这是一个结构体，能被封装到另一个对象中，这个对象实现了 `IValueTaskSource<TResult>` 以及/或者 `IValueTaskSource`，这个包装类简单的将大部分实现委托给结构体即可。要了解更多相关的问题，详见 dotnet/corefx 仓库中的 [issues](https://github.com/dotnet/corefx/issues/32664)。

## ValueTasks 有效的消费模式

从表面上来看，`ValueTask` 和 `ValueTask<TResult>` 要比 `Task` 和 `Task<TResult>` 更加有限。没错，这个方法主要的消费就是简单的等待它们。

但是，因为 `ValueTask` 和 `ValueTask<TResult>` 可能封装可重用的对象，因此与 `Task` 和 `Task<TResult>` 相比，如果有人偏离期望的路径而只是等待它们，它们的消耗实际上受到了很大的限制。一般的，像下面的操作永远不会执行在 `ValueTask / ValueTask<TResult>` 上：

- 等待 `ValueTask / ValueTask<TResult>` 多次。底层对象可能已经回收了并被其他操作使用。与之对比，`Task / Task<TResult>` 将永不会把从完成状态转成未完成状态，所以你能根据需要等待多次，并每次总是能得到相同的结果。
- 并发等待 `ValueTask / ValueTask<TResult>`。底层对象期望一次只在从单个消费者的回调函数执行，如果同时等待它很容易发生条件争用以及微妙的程序错误。这也是上述操作具体的例子：“等待 `ValueTask / ValueTask<TResult>` 多次。”，相反，`Task / Task<TResult>` 支持任何数量并发的等待。
- 当操作还没完成时调用 `.GetAwaiter().GetResult()`。`IValueTaskSource / IValueTaskSource<TResult>` 的实现在操作还没完成之前是不需要支持阻塞的，并且很可能不会，这样的操作本质上就是条件争用，不太可能按照调用者的意图调用。相反，`Task / Task<TResult>` 能够这样做，阻塞调用者一直到任务完成。

如果你在使用 `ValueTask / ValueTask<TResult>` 以及你需要去做上述提到的，你应该使用它的 `.AsTask()` 方法获得 `Task / Task<TResult>`，然后方法会返回一个 Task 对象。在那之后，你就不能再次使用 `ValueTask / ValueTask<TResult>`。

简而言之：对于 `ValueTask / ValueTask<TResult>`，你应该要么直接 `await` （可选 `.ConfigureAwait(false)`）要么调用直接调用 `AsTask()`，并且不会再次使用它了。

```c#
// 给定一个返回 ValueTask<int> 的方法
public ValueTask<int> SomeValueTaskReturningMethodAsync();
...
// GOOD
int result = await SomeValueTaskReturningMethodAsync();
// GOOD
int result = await SomeValueTaskReturningMethodAsync().ConfigureAwait(false);
// GOOD
Task<int> t = SomeValueTaskReturningMethodAsync().AsTask();
// WARNING
ValueTask<int> vt = SomeValueTaskReturningMethodAsync();
... // 存储实例至本地变量会使它更加可能会被滥用
	// 但是这样写还是 ok 的
// BAD：等待多次
ValueTask<int> vt = SomeValueTaskReturningMethodAsync();
int result = await vt;
int result2 = await vt;
// BAD: 并发等待（并且根据定义，多次等待）
ValueTask<int> vt = SomeValueTaskReturningMethodAsync();
Task.Run(async () => await vt);
Task.Run(async () => await vt);
// BAD: 在不知何时完成时使用 GetAwaiter().GetResult() 
ValueTask<int> vt = SomeValueTaskReturningMethodAsync();
int result = vt.GetAwaiter().GetResult();
```

还有一个高级模式开发者可以选择使用，在自己衡量以及能找到它提供的好处才使用它。

特别的，`ValueTask / ValueTask<TResult>` 提供了一些属性，他们能表明当前操作的状态，例如如果操作还没完成， `IsCompleted` 属性返回 `false` ，以及如果完成则返回 `true`（意思是不会长时间运行以及可能成功完成或相反），如果只有在成功完成时（企图等待它或访问非抛出来的异常的结果）`IsCompletedSuccessfully` 属性返回 `true` 。对于开发者所想的所有热路径，举个例子：开发者想要避免一些额外的开销，而这些开销只在一些必要的径上才会有，可以在执行这些操作之前检查这些属性，这些操作实际上使用 `ValueTask / ValueTask<TResult>`，如 `.await,.AsTask()`。例如，在 .NET Core 2.1 中 `SocketsHttpHandler` 的实现，代码对连接读操作，它返回 `ValueTask<int>`。如果操作同步完成，那么我们无需担心这个操作是否能被取消。但是如果是异步完成的，那么在正在运行时，我们想要取消操作，那么这个取消请求将会关闭连接。这个代码是非常常用的，并且分析显示它只会有一点点不同，这个代码本质上结构如下：

```c#
int bytesRead;
{
    ValueTask<int> readTask = _connection.ReadAsync(buffer);
    if(readTask.IsCompletedSuccessfully)
    {
        bytesRead = readTask.Result;
    }
    else
    {
        using(_connection.RegisterCancellation())
        {
            bytesRead = await readTask;
        }
    }
}
```

这种模式是可接受的，因为 `ValueTask<int>` 是不会在调用 `.Result` 或 `await` 之后再次使用的。

## 是否每个新的异步 API 都应返回 ValueTask / ValueTask<TResult> ?

不！默认的选择任然还是 `Task / Task<TResult>`。

正如上面强调的，`Task / Task<TResult>` 要比 `ValueTask / ValueTask<TResult>` 更容易正确使用，除非性能影响要大于可用性影响，`Task / Task<TResult>` 任然是优先考虑的。返回 `ValueTask<TResult>` 取代 `Task<TResult>` 会有一些较小的开销，例如在微基准测试中，等待 `Task<TResult>` 要比等待 `ValueTask<TResult>` 快，所以如果你要使用缓存任务（如你返回 `Task / Task<bool>` 的 API），在性能方面，坚持使用 `Task / Task<TResult>` 可能会更好。`ValueTask / ValueTask<TResult>` 也是多字相同大小的，在他们等待的时候，它们的字段存储在一个正在调用异步方法的状态机中，它们会在相同的状态机中消耗更多的空间。

然而，`ValueTask / ValueTask<TResult>` 有时也是更好的选择，a）你期望在你的 API 中只用直接 `await` 他们，b）在你的 API 避免相关的分配开销是重要的，c）无论你是否期望同步完成是通用情况，你都能够有效的将对象池用于异步完成。当添加 `abstract,virtual,interface` 方法时，你也需要考虑这些场景将会在复写/实现中存在。

## ValueTask 和 ValueTask<TResult> 的下一步是什么？

对于 .NET 核心库，我们讲会继续看到新的 API 返回 `Task / ValueTask<TResult>`，但是我们也能看到在合适的地方返回 `ValueTask / ValueTask<TResult>` 的 API。据其中一个关键的例子，计划在 .NET Core 3.0 提供新的 `IAsyncEnuerator `支持。`IEnumerator<T>` 暴露了一个返回 `bool` 的`MoveNext` 方法，并且异步 `IAsyncEnumerator<T>` 提供了 `MoveNextAsync` 方法。当我们初始化开始设计这个特性的时候，我们想过 `MoveNextAsync` 返回 `Task<bool>`，这样能够非常高效对比通用的 `MoveNextAsync` 同步完成的情况。但是，考虑到我们期望的异步枚举影响是很广泛的，并且它们都是基于接口，会有很多不同的实现（其中一些可能非常关注性能和内存分配），考虑到绝大多数的消费者都将通过 `await fearch` 语言支持，我们将 `MoveNextAsync` 改成返回类型为 `ValueTask<bool>`。这样就允许在同步完成场景下更快，也能优化实现可重用对象能够使异步完成更加减少分配。实际上，当实现异步迭代器时，C# 编译器就会利用这点能让异步迭代器尽可能降低分配。

原文地址：https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/