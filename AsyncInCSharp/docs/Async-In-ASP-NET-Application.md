# ASP.NET 应用程序中的异步

.NET 开发者多数都开发 web 应用程序。异步给你 web 服务代码带来了性能上新的可能，所以这章我们聚焦于如何在 web 应用程序中写使用异步。

## Web 服务器异步代码的优势

在 web 服务器，一个请求的响应性与在 UI 中的响应性是不相同的。相反，web 服务器的性能是通过吞吐量（throughput）、延迟（latency）以及一致性来衡量的。

在高负载的 web 服务器下，异步代码要求比同步代码用更少的线程去做相同的事。每个线程都有很大的内存开销（over-head），并且服务器的瓶颈经常就是内存的容量。当内存不足时，就会经常发生垃圾回收（GC），这样通常要花时间做更多的事（GC 回收，内存对齐等）。如果所使用的内存无法填充到物理内存中，则必须将其分页到磁盘中，如果这样，当再次使用这个内存的时候就会非常缓慢。

ASP.NET 从 2.0 开始，编写异步 web 服务器是非常有必要的，但是没有通过语言的支持是很难实现的。大多数人们去选择增加更多的服务器去做负载均衡来获得效益。在 C#5.0 以及 .NET 4.5，这变得非常容易了，这是非常值得每个人利用这个效益优势。

## 在 ASP.NET MVC4 使用异步

在 ASP.NET MVC 4 极其以上，当运行在 .NET4.5 及其以上版本时，对于 TAP 有了完整的支持，所以我们能够使用异步方法。对于 MVC 异步重要的地方就是控制器了。你在控制器的方法中能简单的使用异步方法返回一个 Task<ActionResult>。

```c#
public class HomeController : Controller
{
    public async Task<ActionResult> Index()
    {
        ViewBag.Message = await GetMessageAsync();
        return View();
    }
}
```

事实上这取决于，你想提供长时间运行的请求一个异步的API供你调用。许多对象关系映射（ORMs）还不支持异步调用，但是 .NET framework 下的 SqlConnection 已经支持了。

## 在老版本的 MVC 使用异步

MVC4 之前，控制器也支持异步，但是不是基于 TAP 的，并且它涉及到了更多其他的（如 AsyncController）。这里有一种方式来适配一个 MVC4 TAP 风格的控制器方法来使用在老版本的 MVC。

```c#
public class HomeController : AsyncController
{
    public void IndexAsync()
    {
        AsyncManager.OutstandingOperations.Increment();
        Task<ActionResult> task = IndexTaskAsync();
        task.ContinuWith(_ =>
        {
            AsyncManager.Parameters["result"] = task.Result;
            AsyncManager.OutstandingOperations.Decrement();
        })
    }
    public ActionResult IndexCompleted(ActionResult result)
    {
        return result;
    }
    private async Task<ActionResult> IndexTaskAsync(){
        。。。
    }
}
```

首先，控制器必须继承 AsyncController 而不是 Controller，它能够使用异步模式。这个模式就意味着每个异步方法都要求有两个方法，一个命名 **ActionAsync**，另一个叫 **ActionCompleted**。**AsyncManager** 控制异步请求的生命周期。当 **OutStandingOperations** 缩减到 0 就会传递结果到 **ActionCompleted** 方法，并将名为 **Parameters** 的字典将结果传递进去。

因为我想保持简单一些，所以我忽视了异常。总的来说，代码非常丑陋，但是只有这样做，你才能想平常那样使用异步。

## WebForms 使用异步

ASP.NET 标准以及 Web Form 都没有一个版本来分开 .NET 框架在 .NET4.5 上运行，ASP.NET 支持在你的页面中标记 `async void`方法，例如 Page_Load 方法：

```c#
protected async void Page_Load(object object, EventArg event)
{
    Title = await GetTitleAsync();
}
```

你也许发现这是一个奇怪的实现，ASP.NET 是怎么知道 `async void` 是何时完成的？返回一个 Task 将会更有意义，这样 ASP.NET 就能在页面渲染之前等待它，就像 MVC4 一样。然而，我想这大概是往后兼容的原因吧，他要求方法返回 **void**。而不是 ASP.NET 使用特定的同步上下文（SynchronizationContext）追踪异步操作，当它完成时才继续。

要小心在 ASP.NET 的同步上下文中运行异步代码的时候，因为它是单线程的。如果你在等待一个 Task 时阻塞了（例如你调用了 Task.Result），那么就会发生死锁，由于更深层次的等待，将不会通过 SynchronizationContext 恢复。