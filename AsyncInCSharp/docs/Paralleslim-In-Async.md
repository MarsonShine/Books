# 并行异步

异步为使用现代机器并行提供了一个很好的机会。语言特性试之前的结构化编程变得更容易。

在最开始，我们写了最简单的多个耗时操作，比如网络请求，并发处理。可以使用 **Task.WhenAll** 一样，异步代码对于这种操作非常高效——不涉及本地计算。那么当涉及到本地计算的时候，异步本身没有帮助。除非资源异步结束，这样你写的代码都同步运行到调用线程上。

## 等待和锁

引入并行的最好的方式就是在不同线程是调度工作。**Task.Run** 会让这些变得更容易，它返回一个 Task，我们可以像其他任何长时间耗时的操作对待它一样。但是使用多线程的话就会引入一些风险，关于在内存中访问公共变量是不安全的。

传统的解决方法就是在你使用异步的时候用 lock 关键字，但是这样会更加复杂。我们前面讲了，await 关键字是不能被 lock，所以这样的话就没有办法在当你正在等待这个方法执行完毕的时候阻止代码运行冲突。事实上，最好去避免存储任何关于横跨 await 关键的资源。异步整个关键点就是当资源正在等待的时候会被释放，并且作为程序员，我们要意识到在那个时候任何事都可能发生。

```c#
lock (sync) {
    //准备异步操作
}
int myNum = await AlexsAMethodAsync();
lock (sync) {
   //使用异步的结果 
}
```

一个有意义的例子就是 UI 线程。只有一个 UI 线程，所以在这种情况下，这个线程的行为就像是同步 lock 一样。你只要知道你的代码是只运行在 UI 线程就行了，同一时刻只运行一行代码。但是同样，在等待时候还是任何事都会发生。如果你点击一个按钮，申请一个网络请求操作，他们在你的代码正在等待的时候去点击其他按钮。这正好在 UI 应用程序是有意义的：UI 需要及时响应用户接下来要做的任何请求，甚至这个操作是危险的。

但是对于异步，针对这点我们可以在程序中选择用其他方式来替代。我们必须学会要在安全的地方 await，并期待这世界里面的状态都能在改变后恢复。有时，这就是说要再做一次看似毫无意义的检查，看看是否继续往下处理。

```c#
if (DataInValid()) {
    Data d = await GetNewData();
    if(DataInvalid()) {
        SetNewData(d);
    }
}
```

## 参与者（Actors）

我说过 UI 线程可以简单的比作 lock，因为它只有一个。实际上，一个比较好的说法就是它是一个参与者（actor）。一个线程就是一个参与者，它的职责就是负责指定一些数据的值，并且其他线程可能也会访问这个数据。 这种情况下，只有 UI 线程才能访问这个数据并显示在 UI 上。也就是说在 UI 的代码中是更容易安全维护，只有一个地方可能会发生变化（异常），也就是 await 处。

大体上，你可以在组件上构建一个程序，它一个线程上操作并且查看数据。这就是行为编程。它能够使你运用电脑的并行能力，让每一个参与者都能运用到不同的电脑内核。这对一般编程来说是非常高效的，不同的组件有不同的状态，都需要各自安全的维护。

> 还有其他的技术可以，比如数据流编程，这对嵌入式并发编程非常有效。有大量的计算，但是彼此独立，都能各自并发运行。很明显这种情况是非常符合 Actors 的选择的。

首先，使用 actors 编程就好想使用锁编程一样。他们都有共享的概念，在同一个线程上运行去访问共同一个数据。但不同的是，其中一个线程可能永远不会在多个 Actors 中。当一个线程在另一个 Actor 运行的时候，这个线程不在占据了这个资源，它必须进行异步调用。当这个调用正在等待的时候，就会释放去做其他的事。

在处理共享内存方面，Actors 是一种内部的比使用锁要更加可拓展的编程方式。多核访问单个内存地址是一个模型。它变得越来越脱离现实情况。如果你曾经使用过锁，你就会知道死锁以及条件竞争的痛苦，它很容易在有所的代码中发生。

## C# 使用 Actors

NAct 库会使 Actors 编程更加容易，当然，你可以使用手动编写 [Actors](https://www.nuget.org/packages/NAct) 编程风格。NAct 充分使用了 C# 异步特性，它把普通的对象转变成 actors，会让他们的调用移动到自己的线程中去。**它通过包装对象的代理来实现这一点，并将其转换为 actor。**

我们来看看例子。假设我实现了一个密码服务（cryptography service）使用了伪随机数来加密数据流。这里有两种敏感计算任务，我想让他们并行运行：

- 生成随机数
- 使用生面生成的随机数来加密数据流

我们来看下实现一个随机数生成器。NAct 提供了一个借口给我们实现，然后 NAct 将会创建一个代理：

```c#
public interface IRndGenerator : IActor
{
	Task<int> GetNextNumber();
}
```

这个借口必须要实现 IActor，它只是一个空的标记接口。这个接口所有的方法必须返回可兼容异步的类型：

- void
- Task
- Task<T>

然后，我们要实现这个生成器类：

```c#
public class RndGenerator : IRndGenerator {
    private readonly Random _random;
    private const int RANDOM_SEED = 1000;
    public RndGenerator() {
        _random = new Random(RANDOM_SEED);
    }
    public async Task<int> GetNextNumber() {
        //生成一个随机数...要缓慢
        return _random.Next();
    }
}
```

这里没有什么让人惊讶的。这只是个普通的类。为了使用它，必须要构造它，把它给到 NAct 去包装，生成一个 actor。

```c#
IRndGenerator rndActor = ActorWrapper.WrapActor(new RndGenerator());
Task<int> nextTask = rndActor.GetNextNumber();
foreach (var chunk in stream)
{
    int rndNum = await nextTask;
    // Get started on the next number
    nextTask = rndActor.GetNextNumber();
    // Use rndNum to encode chunk - slow
    ...
}
```

在这个迭代中，我们都等待一个随机数的生成，在这个缓慢的工作完成之前，然后才开始处理下一个。因为 **rndActor** 是一个 actor，NAct 马上会返回一个 Task，并在 **RndGenerator** 的线程中执行生成。现在，有两个计算在并发处理，能更好的利用 CPU。异步语言特性使这种以前很难的编程风格变得更容易。

这里不详细介绍 NAct 如何使用了，但是我希望我给你展示的 actor 模型的使用是很简单的。其他的特性，如在正确的线程触发事件，以及在空闲的参与者之间更智能化的共享线程，意味着他可以拓展到真实的系统。