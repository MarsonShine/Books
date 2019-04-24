正如我之前所说，所有的异步编程都是跟线程有关的。在 C# 中，那就意味着我们需要理解 .NET 我们的代码在程序中到底运行在哪个线程中，在发生长时间操作的地方它到底做了什么。

# 在第一个 await 之前

在你写的每个异步方法里面，有一些代码在你触发关键字 await 之前发生。同样的， 在等待的表达式中也有一些代码。

代码总是运行在调用它的线程里。在第一个 **await** 之前它是不关心任何事的。

> 这里又一个公共的关于 async 的错误概念。async 从不会在后台线程调度的你方法。只有一种方式来运行任务，那就是想 **Task.Run**。

在 UI 应用程序的例子中，也就是说在 await 之前，代码都是运行在 UI 线程中的。否则，在 Web 应用程序的话，它就是运行在 ASP.NET 的工作线程中。

通常，你可能作为等待的表达式运行其他的异步方法在第一次 **await** 的行，它也必须运行在调用它的线程。那就是说调用的线程将会继续深入你的应用程序执行你的代码，直到所有的方法都返回 Task。这样做的方法可能是一个框架方法，或者是使用 TaskCompletionSource 构造的 Task 方法。这个方法是你程序中异步方法的资源——所有的异步方法都只是传播异步。

这运行的代码在第一个真正到达异步之前将非常广泛，并且在 UI 应用程序中，这些代码都会运行在 UI 线程中，这时 UI 会变为未响应。幸运的是，代码不会运行太长时间，但是要记住只是使用 async 是不会保证你的 UI 是相应的。如果觉得它慢，需要性能分析，找出他耗时的地方在哪。

# 异步操作期间

做异步操作的具体是哪个线程呢？

这很难回答。这是异步代码，典型的操作就是一个网络请求操作，这里没有线程被锁住来等待这个操作的完成。

> 当然，如果你使用异步等待计算，例如 Task.Run，线程池会用一个闲置的线程来执行计算并且变得繁忙。

这里正有一个线程等待网络请求完成，但是它在其他所有网络请求是共享的。它在 Windows IO 完成端口被调用。当网络请求完成时，操作系统中的一个中断处理程序会增加一个 job 到IO 完成端口 的队列中。执行 1000 个网络请求，所有的请求都会开始，并且作为响应到达，他们会在单个 IO 完成端口中依次处理。

> 理论上，通常有几个 IO 完成端口处理程序的线程，来利用多个CPU内核。然而，线程的数目是相同的，无论你有 10 个网络请求还是 1000 个。

# 同步上下文

.NET Framework 提供一个同步上下文（SynchronizationContext），它能指定类型的线程运行代码。在 .NET 中有各种上下文，比如最重要的还是 UI 线程上下文在 WinForm 和 WPF 应用程序中。

**SynchronizationContext** 实例自己不会任何有用的事，所有做实际的事情的是它的子类。它的静态成员能让你读取和控制当前的 **SynchronizationContext**。SynchronizationContext 是当前线程的一个属性。你正在运行的代码在任何时刻都会运行在特定的线程之中，你应该能获取当前的 SynchronizationContext 并存储它。然后，你可以使用它指定你开始的线程来运行代码。所有的这些都应该不需要知道这个线程就是你开始的那个线程，只要你使用 **SynchronizationContext**，你就能回过头获取它。

SynchronizationContext 有个重要的方法是 **Post**，他能使一个委托能在正确的线程中运行。

有些 SynchronizationContext 封装了单个线程，比如 UI 线程。这些封装成为特定的线程——比如线程池，它能选择池中任何线程来传递委托。在那个线程上运行的代码实际不会有改变，但是只用于监控，就像 ASP.NET 中的 SynchronizationContext。

# 等待同步上下文

我们知道在代码运行第一个 **await** 之前是运行在调用它的线程上的，但是当你的方法在使用 await 之后又做了什么呢？

实际上，大多数时候，它还是运行在调用它的线程的，尽管实际上调用的线程或许已经完成了其他事。这对于程序员来说非常简单了。

C# 使用 SynchronizationContext 来完成这个。我们之前提到的 “上下文” 章节，当你 await Task，当前的 SynchronizationContext 被存储在这个等待的方法中。然后当方法被恢复的时候，**await** 会捕捉到 SynchronizationContext 使用 **Post** 来恢复（继续）这个方法。

警告：方法能在不同的线程中在它开始的地方恢复，如果：

- 只有一个 SynchronizationContext，有多个线程，就像线程池。
- SynchronizationContext 只有一个，不会发生线程切换
- 当 await 到达时，没有当前的 SynchronizationContext，例如控制台程序
- 可以配置 Task 不使用 SynchronizationContext 来恢复

幸运的是对于 UI 应用程序，在相同的线程恢复是非常重要的，那些都不会应用，所以你能在 await 之后安全的维护你的 UI。

# 异步操作生命周期

让我们来看之前下载网站发 fivacon 的例子，计算那个代码运行在那个线程。我写了这两个异步方法：

```c#
async void GetButton_OnClick(...)
async Task<Image> GetFaviconsAsync(...)
```

**GetButton_OnClick** 时间函数调用 **GetFaciconsAsync**，再按顺序调用 **WebClient.DownloadDataTaskAsync**。下面是一个运行的方法事件队列图

![1547609596726](C:\Users\Administrator\AppData\Roaming\Typora\typora-user-images\1547609596726.png)

1. 用户点击按钮，GetButton_OnClick 事件处理程序入队列。

2. UI 线程首先执行 GetButton_Onclick 一部分的代码，包括调用的 GetFaviconAsync。

3. UI 线程在 GetFaviconAsync 中继续，并且执行它的前半部分（await 之前），包括调用 DownloadDataTaskAsync

4. UI 线程在 DownloadDataTaskAsync 中继续，开始下载并返回一个 Task。

5. UI 线程离开 DownloadDataTaskAsync，并且在 GetFaviconAsync 到达 await。

6. 当前线程上下文（SynchronizationContext）被捕捉——它是 UI 线程

7. GetFaviconAsync 在 await 的时候是阻塞的，DownloadDataTaskAsync 返回的 Task 被告知在完成后恢复它（使用捕捉的同步上下文）

8. UI 线程离开 GetFaviconAsync，它也返回一个 Task，并且在 GetButton_OnClick 中到达 await。

9. 同样的，GetButton_OnClick 在 await 时候是阻塞的

10. UI 线程离开 GetButton_OnClick，并被释放来处理其他用户操作。

    > 在这个时候，我们正在等待 icon 下载。这里需要花费几秒。注意到 UI 线程是释放了来处理其他用户操作，并且 IO 完成端口线程还没有涉及。在操作期间线程阻塞的线程数量是0。

11. 下载完成，IO 完成端口中对逻辑排队以 DownloadDataTaskAsync 中处理它。

12. IO 完成端口线程设置 从 DownloadDataTaskAsync 返回的Task 为完成。

13. IO 完成端口线程在 Task 中运行代码来处理完成，它调用在捕捉的 SynchronizationContext 上调用 Post（UI 线程）来继续。

14.  IO 完成端口线程释放以在其他 IO 上工作

15. UI 线程发现 **Posted** 指令并且恢复了 GetFaviconAsync，继续执行它的后半部分，知道结束。

16. UI 线程离开 GetFaviconAsync 时，它将 GetFaviconAsync 返回的 Task 设置为完成。

17. 这个时候，当前的 SynchronizationContext 跟捕获的上下文是相同的，不需要 Post，UI 线程同步处理

    > 这套逻辑在 WPF 是不适用的，因为 WPF 经常创建新的 SynchronizationContext 对象。尽管他们是等价的，这使得 TPL 认为它需要再一次 Post。

18. UI 线程恢复 GetButton_OnClick，继续执行后面的部分知道结束。

这很复杂，但是我认为这指出来的每个步骤都值得看。注意到通过 UI 线程运行我的每行代码。IO 完成端口线程的运行时间只够向 UI 线程发布一条指令，它运行我的两个方法的后半部分。

# 不使用同步上下文

每个 SynchronizationContext 的实现都会以不同的方式执行的 Post。大多数情况它们都是相对代价昂贵的。为了避免消耗，.NET 在 Task 完成的时候捕捉当前同步上下文时，也会选择不使用 Post。发生这种情况的时候，你可以在调试中看看，就会发现调用栈将会颠倒【upside-down】（忽略框架代码）。从程序员角度来看，其中最深层次的方法被其他方法调用，最后在它完成时调用其他方法。

当 **SynchronizationContext** 不同的时候（实则为线程上下文发生切换），调用它的 Post 消耗是非常大的。在性能敏感的代码中或是在库中，你不关心你使用的是哪个线程，你可能选择不为性能损失买单。[*In performance-critical code, or in library code where you don’t care which thread you*
*use, you might choose not to pay that performance penalty*]

这个时候你可以等待之前调用在 Task 上的 ConfigureAwait。那样的话，你大可不必在完成时恢复 Post 回来到原始的线程同步上下文。

```c#
byte[] bytes = await client.DownloadDataTaskAsync(url).ConfigureAwait(false);
```

尽管 **ConfigureAwait** 可能不会如你期望的那样做。它在 .NET 中原本被设计为一个提示，提示你不用关心你恢复的方法是那个线程上的，它不是一个严格的指令。它具体做什么取决于你正在等待完成的 Task。如果这个线程不重要，也许它取自于线程池，它也应该继续执行的代码。但是如果它对于某些情况来说是很重要，那么 .NET 将优先释放它来做其他事，并且你的方法将会在线程池中恢复。.NET 使用当前同步上下文的线程来判断它是否重要。

# 与同步代码交互

你可能已经在处理已经存在的一个应用程序，并且当你用 TAP 写新代码时，你需要与旧系统的代码沟通。当你这么做的时候，你经常不得不抛弃一些异步优势与同步代码交互，但是它还是值得计划为将来用异步模式风格来写新代码来在某个时刻切换。

从异步到同步的方式也是非常简单。如果你阻塞 API，你只用 **Task.Run** 在线程池上运行并等待它即可。你虽然用了一个线程，但是这是不可避免的。

```c#
var result = Task.Run(() => MyOldMethod());
```

从同步代码到异步代码，或者实现一个同步 API 的也是很容易的，但是有隐患。Task 有一个 **Result** 属性，它在等待 **Task** 完成的时候是阻塞的。你可以在 await 的地方使用它，但是你的方法不能被标记为 **async** 或者是返回的是 **Task**。同样，一个线程被浪费了。这次调用线程是为了阻塞。

```c#
var result = AlexsMethodAsync().Result;
```

提醒一下，当你使用来自于只有一个线程的同步上下文的时候，这个技术是行不通的，就如同 UI 线程一样。想一下，请求 UI 线程的时候，UI 线程在做什么。它是阻塞的，它正在等 AlexsMethodAsync 的任务完成。AlexsMethodAsync 很有可能调用了其他 TAP 方法，并且也在等待它。当操作完成了，捕捉到了同步上下文（UI 线程）用 Post 指令来恢复 AlexsMethodAsync。但是 UI 线程将永远不会接受这个消息，因为它任然处于等待状态。发生死锁了。幸运的是，这个错误会经常导致死锁的发生，所以调试起来并不困难。

你可以通过启动异步代码之前移动到线程池来避免死锁问题，以至于 **SynchronizationContext** 被捕获时是线程池的，而不是 UI 线程的。尽管代码会变得丑陋。最好花时间调用异步代码。

```c#
var result = Task.Run(() => AlexsMethodAsync()).Result;
```

