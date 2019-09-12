# 深入理解 ValueTask

.NET Framework 4 里面的命名空间为 `System.Threading.Tasks`的 `Task` 类。这个类以及它派生的 `Task<TResult>` 早已成为编程的主要部分，在 C#5 中的异步编程模式当作介绍了 `async/await`。在这篇文章里，我会覆盖新的类 `ValueTask / ValueTask<TResult>`，介绍它们在通用的使用上降低内存消耗来提高异步性能，这是非常重要的。

## Task

Task 有多种用途，但是它的核心就是 “promise”，它表示最终完成的一些操作。你初始化一个操作，并返回给它一个 Task，它当操作完成的时候它会完成，这可能作为初始化操作的一部分同步发生（比如访问一个早就缓冲好了的缓冲区），也有能是异步的，在你完成这个任务时（比如访问一些还没有缓冲好的字节，但是很快就缓冲好了可以访问），或者是在你已经接收 Task 的时候异步完成（比如通过网络访问数据）。因为操作完成可能是异步的，所以你需要为结果等待它（但这经常违背异步编程的初衷）或者你必须提供一个回调函数来调用，当这个操作完成的时候。在 .NET 4 中，提供了如回调函数一样的来实现如 `Task.ContinueWith` 方法，它暴露通过传递一个委托的回调函数，这个函数在 Task 完成时触发：

```c#
SomeOperationAsync().ContinueWith(task =>{
    try {
        TResult result = task.Result;
        UseResult(result);
    }catch (Exception e) {
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

//TODO