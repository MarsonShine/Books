# Net 异步模式

在此之前，Silverlight 只提供了Web访问的异步版本的 API。这里有一个下载网页和显示它的例子

```c#
private void DumpWebPage(Uri uri) {
    WebClient webClient = new WebClient();
    webClient.DownloadStringCompleted += OnDownloadStringCompleted;
    webClient.DownloadStringAsync(uri);
}

private void OnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
    Console.WriteLine(e.Result);
}
```

这种模式是 EAP 模式（Events-base Asynchronous Pattern）。处理的是单个同步方法的下载网页，期间是阻塞的直到下载完成，一个事件一个方法被使用。这个方法看起来就像是同步版本，只是它有一个 void 返回类型。事件指定了类型 EventArgs，它包含了接收的返回值。

我们在调用改方法之前立即注册该事件。当然，因为这是异步代码，方法会立即返回。然后，在将来的某个时刻，事件将会触发，然后我们就处理了。

这个模式使用起来很乱，尤其是由于你将原本很简单的顺序指令要分隔成两个方法。更重要的是（On top of that），实际上你注册一个事件，这会带来复杂性。如果你继续使用相同的实例来进行其他请求，你可能不希望原始事件任然被附加，处理程序也将重复执行。

另一个异步模式就包含了一个 IAsyncResult 接口。Dns 类就是个例子，它查看主机 IP 地址，BeginGetHostAddress。它也要求两个方法，一个叫 BeginMethodName，它代表操作的开始，还有一个叫 EndMethodName，它代表操作结束，在回调方法里面获取返回的值。

```c#
private void LookupHostName() {
    object unrelatedObject = "hello";
    Dns.BeginGetHostAddresses("oreilly.com", OnHostNameResolved, unrelatedObject);
}

private void OnHostNameResolved(IAsyncResult ar) {
    object unrelatedObject = ar.AsyncState;
    IPAddress[] addresses = Dns.EndGetHostAddresses(ar);
    //Do something with addresses
}
```

至少，这个设计没有遗留事件处理程序的问题。然而这还是会给 API 带来复杂性，用两个方法而不是一个方法，并且我发现它是不自然的。

这些异步模式都要求你分割超过两个方法。IAsyncResult 模式允许你第一个方法传递参数到第二个方法，就像上面我传 “hello” 。这种方式非常混乱，它要求你传递一些变量甚至你不需要它，并且还强制你从 object 强转。EAP 也支持传递对象，同样它是混乱的。

在异步模式下，在不同的方法之间传递上下文是通用的问题。我们下节会讲到 lambda 表达式是一个解决方法，并且你可以在之前提到的那些场景都能使用。

# 最简单的异步模式

最简单具有异步行为的代码，不使用 async，包括传递作为参数的回调函数到方法：

```c#
void GetHostAddress(string hostName,Action<IPAddress> callback)
```

我发现这比其他模式更易使用。

不同于上面分割的两个方法：

```c#
private void LookupHostName()
private void OnHostNameResolved(IAsyncResult ar)
```

就像我刚之前提到的，你可以使用匿名方法或者是 lambda 表达式作为 callback。这个对于允许你访问从第一部分的方法变量是非常有好处。

```c#
private void LookupHostNameByLambda() {
    int aUsefulVariable = 3;
    GetHostAddress("oreilly.com", address => {
        //Do something with address and aUsefulVariable
    });
}
```

尽管 lambda 经常有一点难读，如果你正在使用多个异步 APIs，你将要嵌套多个 lambda。你的代码变得非常快速的缩进，并且越来越难以使用。

这种简单的方法的缺点是不在将任何异常抛回给调用方。在使用这些模式当中，调用 EndMethodName 或者是取 Result 属性将会抛出异常，所以原来的代码能处理它。相反，他们可能出现在错误的地方，或者根本无法处理。

# Task

.net4.0出现了 TPL （Task Parallel Library），这里面最重要的类就是 Task，它代表正在做的操作。还有泛型版本 Task<T>，一旦这个操作完成，就会充当将来可以用的 T 类型的值。

C#5.0 异步特征使用了 Task 拓展，这个我们稍后讨论。然而，不使用 async 关键字，你可以使用 Task 以及 Task<T> 来编写异步程序。现在，你要开始一个操作，它将返回 Task<T> 类型实例，然后使用 ContinuWith 方法来注册你的回调函数。

```c#
private void LookupHostNameByTask() {
    Task<IPAddress[]> iPAddressesPromis = Dns.GetHostAddressesAsync("oreilly.com");
    iPAddresses.ContinueWith(_ => {
        IPAddress[] iPAddresses = iPAddressesPromis.Result;
        //do something with address
    });
}
```

Task 的优势很明显，在 Dns 它只有一个方法，能让 API 更加清晰。所有异步相关的行为逻辑都在 Task 类里调用，而不需要在每个异步方法中重复。它还能做更重要的事，比如处理异常和同步上下文（SynchronizationContexts）。这些在第八章会有讨论，如何更加实用化在指定的线程运行回调函数（例如 UI 线程）。

更重要的是，Task 在处理异步操作时，给我们提供了一个通用抽象基本的方式。我们可以使用这种可组合的与Task一起工作，它还提供了许多情况下有用的一些行为。这将来第七章讨论。

# 手动异步的问题

就如我们所见，有很多种方式来实现异步问题。他们之间很相似。但是你也应该看到他们共同的缺点。你将要写的程序会分成两个方法：主方法和回调方法。使用匿名方法和 lambda 回调能减轻这种问题，但是你的代码会变得难以阅读。

此外还有另一个问题，我们讨论了调用匿名方法，但是你无法知道你是不是还需要另一个的匿名方法，如果你遇到的是匿名方法循环调用呢？你只有递归这一种优化项，它要比一般的循环难读懂。

```c#
private void LookupHostNames(string[] hostNames) {
    LookupHostNamesHelper(hostNames, 0);
}

private void LookupHostNamesHelper(string[] hostNames, int i) {
    Task<IPAddress[]> iPAddressesPromise = Dns.GetHostAddressesAsync(hostNames[i]);
    iPAddressesPromise.ContinueWith(_ => {
        IPAddress[] iPAddresses = iPAddressesPromise.Result;
        //do something with address
        if (i + 1 < hostNames.Length) {
            LookupHostNamesHelper(hostNames, i + 1);
        }
    });
}
```

以这种编程风格来手写异步编程的话会引起另一个问题。如果你在写异步代码，然后想用在你的程序里面其他地方，那么你就必须要提供异步的API。如果使用这种异步API，那么你就要为一个方法写两边（一个同步，一个异步），这看起来令人困惑和混乱。并且异步代码是连续不断的，所以你也不能只在异步 API 处理，还要在调用源，这样会使得原来的程序一团糟。

# 转换到手写异步代码例子

之前我们有讨论 WPF UI 框架应用程序下在网页是缓慢的，因为它在下载页面的过程中系统是未响应的。那么这节我们用手动异步技术来改造一下。

第一步 发现异步版本的 Task API（WebClient.DownloadData）。就如下面一下，WebClient 使用了 EAP（Event-base Asynchronous Pattern），所以我们可以注册一个事件回调，然后开始下载

```c#
private void AddFavicon(string domain) {
    WebClient webclient = new WebClient();
    webclient.DownloadDataCompleted += OnWebClientOnDownloadDataCompleted;
    webclient.DownloadDataAsync(new Uri("http://" + domain + "/favicon.ico"));
}

private void OnWebClientOnDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) {
    var bytes = e.Result; //favicon 字节流
    Console.WriteLine(Convert.ToBase64String(bytes));
}
```

当然，我们的逻辑被拆分到两个方法里。我这里不倾向于使用 lambda EAP 是因为代码在开始下载之前调用，它看起来想是同步的（不可读）。

这个版本的代码例子，你运行它就会发现系统不仅可以保持相应，下载的图片也渐渐显示出来。同时我们也引入了一个新的问题，因为这所有的下载操作都是在一开始触发的，图片的顺序取决于下载的快慢而不是请求图片的顺序。如果你细心检查的话就会发现这个问题，我建议是修复这个问题，这涉及到由循环转化为递归。其他有效的解决方法也都可以。