## DataFlow基础

TPL 数据流（dataflow）库的功能很强大，可用用来创建网格（mesh）和管道（pipleline），并通过它们是以异步的形式发送数据。数据流具有很强的 “声明式变成” 风格。通常要完整的定义网格，才能开始处理数据，最终让网格成为一个让数据流通的体系架构。

每个网格由各种互相链接的数据流块（block）构成。独立的块比较简单，只负责数据处理中某个单独的步骤。当块处理完它的数据后，就会把数据传递给它链接的块。

我们首先 Nuget 上下载一个包：.netframework 下的为 Microsoft.Tpl.Dataflow；.NET Core 下的为 System.Threading.Task.Dataflow。

### 链接数据流块

问题：创建网格时，需要把数据流块互相链接起来

TPL 数据流提供的块只有一些基本的成员，很多实用的方法都是拓展方法，我们来看 LinkTo 方法

```c#
var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
var substractBlock = new TransformBlock<int, int>(item => item - 2);
//建立链接后，从multiplyBlock出来的数据自动进入substractBlock。
multiplyBlock.LinkTo(substractBlock);
```

默认情况下，互相链接的数据流只传递数据，不传递完成情况（或异常信息）。如果数据流是线形的（例如管道），一般情况下要传递完成情况，这个时候我们通过在链接中设置 PropagateCompletion 属性：

```C#
var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
var substractBlock = new TransformBlock<int, int>(item => item - 2);
var options = new DataFlowLinkOptions { PropagateCompletion = true};
multiplyBlock.LinkTo(substractBlock, options);
multiplyBlock.Complete();
await substractBlock.Completion;
```

一旦建立链接，数据就会自动从源块传递到目标块。如果设置了 PropagateCompletion，完成情况也会传递数据。在管道的每个节点上，当出错的信息传递给下一块时，就会封在 AggregateException 内，如果完成情况的管道很长，错误信息就存储在多个 AggragateException ，这样就可以对错误进行处理了。

链接数据流块方式有很多种，在网格中主要包含 分叉、链接、甚至循环。大多数情况分叉足够了。

利用 `DataflowLinkOptions` 可以对链接设置多个不同的参数。另外可以在 LinkTo 方法中设置断言，形成一个数据通行的过滤器。数据被过滤也不会删除，会考虑通过其他链接通过。如果所有链接都无法连通，则会留到当前块内。

### 数据流块的异常处理

数据流块发生了异常，就会进入故障状态，就会删除里面数据并拒绝接收新的数据。该数据流块不会产生新的数据。下面这代码，第一个数据发生了异常，就会进入异常状态，并删除第二个数拒绝新的数据进来。

```c#
public static async Task HandleException(){
    var block = new TransformBlock<int, int> (item => {
        if (item == 1){
            throw new InvalidOperationException ("invalid operation");
        } 
    });
    block.Post (1);
    block.Post (2);
}
```

在运行上述代码的时候我们发现，我们无法捕捉异常，没有 try,catch 肯定无法捕捉，那么是否加上 try，catch 就可以了呢？

我们调用数据流块的 Completion：`await block.Completion` 。只有这样才能正确捕捉异常，进入 catch 代码段。Completion 返回的也是一个 Task，一旦数据流执行完成，这个任务也就完成了。如果数据流出错，这个任务也出错。

```c#
public static async Task HandleExceptionInTransformBlock () {
    try {
        var block = new TransformBlock<int, int> (item => {
                if (item == 1)
                    throw new InvalidOperationException ("invalid operation");
                return item * 2;
            });
        block.Post (1);
        block.Post (2);
        await block.Completion; //等待这个数据链全部完成
    } catch (InvalidOperationException) {
        //catching
        Console.Error.WriteLine ("InvalidOperationException");
    }
}
```

在这个基础上，我们在构造数据块链接起来的时候，给它传递一个 `PropagateCompletion = true`，就意味着要把错误信息传递给下一个数据流块，数据流块接收到错误信息后，即使这个错误已经是 AggregateException ，也会继续封装在 AggregateException 中。所以这个时候我们就用 AggregateException.Flatten 方法可以简化错误处理过程。

```c#
public static async Task HandleExceptionTransformBlockWithPropagateCompletion () {
    try {
        var multiplyBlock = new TransformBlock<int, int> (item => {
                if (item == 1) {
                    throw new InvalidOperationException ("invalid operation");
                }
                return item * 2;
            });
        var substractBlock = new TransformBlock<int, int> (item => item - 2);
        multiplyBlock.LinkTo (substractBlock,
            new DataflowLinkOptions { PropagateCompletion = true });
        multiplyBlock.Post (1);
        multiplyBlock.Post (2);
        await substractBlock.Completion;
    } catch (AggregateException ae) {
        Console.WriteLine ("AggregateException 错误次数：" + ae.InnerExceptions.Count);
    }
}	
```

### 断开链接

如果我们在已经运行的数据流块链接中要修改数据流结构，该如何解决？这是一种高级应用，很少用到。

数据链块是可以随时随地建立/断开链接。**数据会在网格中自由传递，不会受此影响**。建立和断开链接时，线程都是**线程安全**的。

在创建数据流块之间的链接时，保留 LinkTo 方法返回的 IDisposeable 接口。想断开链接就释放它即可。

```c#
public static void ReleaseBlock () {
    var multiplyBlock = new TransformBlock<int, int> (item => item * 2);
    var substractBlock = new TransformBlock<int, int> (item => item - 2);
    IDisposable link = multiplyBlock.LinkTo (substractBlock);
    multiplyBlock.Post (1);
    multiplyBlock.Post (2);
    //断开数据流块之间的链接
    link.Dispose();
}
```

### 限制流量

需要在数据网格中分叉，数据流量能在各个分支之间平衡。

默认情况下，数据流块生成数据后，会检查每个链接，会把数据传递给每个数据流块。同样的，默认情况下，每个数据流块都会维护一个输入缓存区，在数据流块处理数据之前，接收任意的数据。

有分叉时，一个数据流块被分成了两个目标快，这样就有问题了：第一个目标快不停的缓冲，那么另一个就永远没有机会得到数据。我们可以在构建数据流块链接的时候，传递一个BoundedCapacity，来限制目标快的流量。

```c#
var sourceBlock = new BufferBlock<int>();
var options = new DataflowBlockOptions { BoundedCapacity = 1 };
var targetBlockA = new BufferBlock<int>(options);
var targetBlockB = new BufferBlock<int>(options);
sourceBlock.LinkTo(targetBlockA);
sourceBlock.LinkTo(targetBlockB);
```

> 限流可以用于分叉的负载平衡，但也可以用于任何行为的流中。例如，用 I/O 操作的数据填充网剧网格时，可以设置数据流块的 BoundedCapacity 属性。这样在网格来不及处理数据时，就不会读取过多的 I/O 数据，网格也不会缓冲所有数据。

### 数据流块的并行处理

因为在数据流块里面每个数据都是独立执行的，所以完全具备并行的要求。如果某个特定的数据执行消耗的时间很长，那么就得设置 MaxDegreeOfParallelism 参数，使数据流块在数据输入时采取并行的方式。默认情况 MaxDegreeOfParallelism  = 1，所以一个数据流块只能处理一个输入数据。

MaxDegreeOfParallelism 可以设置为 DataflowBlockOptions.Unbounded 或者大于1的数值

```c#
public static async Task BlockParallelSlim (bool isParalllel) {
    var multiplyBlock = new TransformBlock<int, int> (
            item => {
                Thread.Sleep (1000 + (100 * item));
                return item * 2;
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = isParalllel?ExecutionDataflowBlockOptions.Unbounded : 1 });
    var substractBlock = new TransformBlock<int, int> (
            item => item - 2);
    multiplyBlock.LinkTo (substractBlock);
    multiplyBlock.Post (10);
    multiplyBlock.Post (11);
    multiplyBlock.Post (12);
    multiplyBlock.Post (13);
    await multiplyBlock.Completion;
}
```

### 创建自定义数据流块

希望能够抽取具有同样逻辑的数据流块在自定义数据流块复用，用来处理更复杂逻辑的数据流块链接

用 `DataflowBlock.Encapsulate` 封装数据流块中任何具有单一输入和输出块的部分。Encapsulate 会利用这两段创建一个单独的数据流块。开发者得自己负责完成数据的传递以及完成情况。

```c#
public static IPropagatorBlock<int, int> CreateCustomBlock () {
    var multiplyBlock = new TransformBlock<int, int> (
            item => item * 2);
    var addBlock = new TransformBlock<int, int> (item => item + 2);
    var divideBlock = new TransformBlock<int, int> (item => item / 2);
    var flowCompletion = new DataflowLinkOptions { PropagateCompletion = true };
    multiplyBlock.LinkTo(addBlock,flowCompletion);
    addBlock.LinkTo(divideBlock,flowCompletion);
    return DataflowBlock.Encapsulate(multiplyBlock,addBlock);
}
```

