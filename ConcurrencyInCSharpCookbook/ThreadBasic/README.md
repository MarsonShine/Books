### 处理async void方法的异常

最好不要从async void 方法传递出异常。如果必须要用到async void 方法，可以考虑把所有代码 try 块中，直接处理异常。

**一个异常从async void 传递出来时，会在 SynchronizationContext 中引发出来。**处理async void 方法的异常还可以通过SynchronizationContext 。当async void 方法启动时，SynchronizationContext 就处于激活状态。如果系统环境支持 SynchronizationContext，就可以通过全局范围内处理这些顶层的异常。例如ASP.NET 有 Application_Error。

通过控制 SynchronizationContext，也可以处理async void 传递出来的异常，但是自己编写 SynchronizationContext 工作量太大，所以我们可以使用已经编写好的内库 Nito.AsyncEx 中的 AsyncContext，其中 async 方法不返回 Task，但是AsyncContext也能在async void 方法中起作用。

```c#
static int Main(string[] args){
    try{
        return AsyncContext.Run(() => MainAsync(args));
    } catch(Exception ex){
        Console.Error.WriteLine(ex);
        return -1;
    }
}

static async Task<int> MainAsync(string[] args){
    ...	//异步代码
}
```

要注意，如果你一定要实现自己的 SynchronizationContext，一定不要在已经有 SynchronizationContext 的线程上（比如 UI 或 ASP.NET request 线程）安装这个类，也不要在线程池线程上放 SynchronizationContext。属于你的线程有控制台住线程，还有你自己创建的所有线程。