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