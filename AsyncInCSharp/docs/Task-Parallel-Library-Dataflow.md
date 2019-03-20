# TPL 数据流

另一个让我们很容易使用并发编程的工具类库就是 TPL Dataflow。在这个模式，你可以在需要的数据入口指定特定场景的操作，并且系统会自动并行这些操作。你可以在 [Nuget](https://www.nuget.org/packages/Microsoft.Tpl.Dataflow) 上获取这个库。

> 当大多数程序决定关键性能的是数据转换时，数据流编程（Dataflow Programing）就非常有用了。你可以完全使用数据转换以及actors（见上章） ，其中有一个 actors 有个很重的计算负载，那么你就可以使用 dataflow 来并行。

TPL Dataflow 的概念是把数据分成一块接着一块传递。新建一个数据流网络，你要实现两个接口来把这些块（block）串在一起：

**ISourceBlock<T>**

​	你可以请求一个 T 类型的消息

**ITargetBlock<T>**

​	你提供的消息对象

ISourceBlock<T> 提供了一个 **LinkTo** 方法，它把 ITargetBlock 串在一起，所有被处理的消息都通过 ISourceBlock<T> 传递到 ITargetBlock<T>。大多数 blocks 都实现了这两个接口，也许不同的只是参数类型，以至于他们的消息一个消费另一个产生。

你也可以自己实现这些接口，更常用的还是使用内键的块，例如：

**ActionBlock<T>** 

​	当你要构造一个 ActionBlock<T> 时，你需要往构造函数里面传递一个委托，并为后面的每个消息都执行这个委托。ActionBlock<T> 只实现了 ITargetBlock<T>。

**TransformBlock<TIn,TOut>**

​	同样，你要传递一个委托给构造函数，但是这次的委托是一个返回值的函数。这个值会作为消息传递给下一个块。TransformBlock<TIn,TOut> 实现了 ITargetBlock<TIn> 以及 ISourceBlock<TOut>。它是并行版本的 LINQ 的 Select 。

**JoinBlock<T1,T2,...>**

​	它连接多个输入流到单个输出流元组。

还有很多其他内建的 block，并且有了它们，你可以实现任何传输带风格的计算。开箱即用（Out of the box），这些并行的块充当管道，但是每块只会一次处理一个消息。如果大部分你的这些块话费的时间是相同的，那这是好的，但是如果其中一个阶段要比剩下的都要慢，那么你可以配置 ActionBlock<T> 以及 TransformBlock<TIn,TOut> 在单个块内部并发工作，分割自己到很多相同的块中并共享工作，这样是非常高效的。

TPL dataflow 能通过 async 来改进，因为委托传递给 ActionBlock<T> 和 TransformBlock<TIn,TOut>，他们都能够 async，并且各自返回 Task 或者 Task<T>。当那些委托涉及到长时间远程操作时，这非常重要，那些操作都能够并行运行而不会浪费线程。此外，当从外部与数据流块交互时，以异步的方式来做是很有用的，因此为了方便，在 ITargetBlock<T> 拓展了 TAP 模式的异步方法，比如 SendAsync。