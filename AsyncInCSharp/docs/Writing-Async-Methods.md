# 编写异步代码

现在我们知道了异步代码是有多好了，但是它很难写，对吗？现在就让我们来看看 C# 5.0 async 关键字。方法当用 **async** 标记时，要么方法体内就要包含 **await** 关键字。

```c#
private async void DumpWebPageAsync(string uri){
    WebClient client = new WebClient();
    string page = await client.DownloadStringTaskAsync(uri);
    Console.WriteLine(page);
}
```

**await** 转换了后面的方法体，让它在下载期间处于等待，并当下载完成后返回。这种转换能让方法变为异步的。在这节，我们将分析这种异步方法。

# 转化 Favicon Example 为异步方法

现在我们来修改之前写的下载网站 logo 并显示的例子，改用为 async await 模式。如果可以，那么打开你原来版本的代码例子去尝试加上 **async** 和 **await**。

**AddFavicon** 这个方法很重要，它下载图标，然后加载到 UI。我想让这个方法变为异步的，所以在用户下载期间的行为，UI 要能自由响应。第一步就是要加 **async** 关键字到方法体上。它跟 static 关键字一样出现在方法签名中。

然后我们需要使用 **await** 等待下载。在 C# 语法中，**await** 充当一个一元运算符，就像 ! 非操作符，或者是类型转换操作符。它位于表达式的左边，代表这个异步等待该表达式。

最后，必须要把调用 **DownloadData** 的方法改为异步版本的方法 **DownloadDataAsync**。

> async 方法并不是自动异步的。async 方法只是让异步方法变得更容易。它们开始异步运行，知道它们在调用的方法体内 await 等待。只有这么做时，它们才是异步的。有时候，一个 async 方法是同步运行时，它就不需要 await。

```c#
private async void AddFavicon(string uri){
    WebClient client = new WebClient();
    byte[] bytes = await client.DownloadDataAsync("http://" + domain + "/favicon.ico");
    Image imageControl = MakeImageControl(bytes);
    m_WrapPanel.Children.Add(imageControl);
}
```

来比较一下这两个版本的代码。async 标记的方法更像同步版本的代码。没有额外的方法，在相同的结构中就只有一些额外一点点代码。然而，这比之前的 [手动编写异步代码](Some-Asynchronous-Patterns-Used-In-Net.md)” 异步要表现得更好。

# Task 和 await

我们在方法 client.DownloadStringTaskAsync 下的 await 表达式初打个断点。

```c#
Task<string> DownloadStringTaskAsync(string address)
```

它返回 Task<string>，Task 代表一个正在做的操作，泛型子类 Task<T> 代表在未来的某个时刻这个操作完成返回的类型。

Task 和 Task<T> 都表示一个异步操作，当操作完成之后返回调用处继续执行。你可以用 Task 实例的 ContinuWith 手动的使用它，ContinueWith 传递一个委托，当操作执行完毕后回调触发。你可以用 await 来执行你剩下的异步方法。

如果你应用了 await Task<T>，就代表这个表达式是可等待的，整个表达式的类型是 T。就意味着你可以把等待的结果分配给变量以及在余下的方法继续用这个结果，就像我们看到的那个方法一样。当你等待一个非泛型的 Task 时，它就变得是可等待的，并且它不会返回任何值，也不需要分配给任何变量，就像在调用一个 **void** 方法。也就是说，Task 他不承诺返回值，仅仅只是代表一个行为操作。

`await smtpClient.SendMailAsync(mailMessage)`

这里是不会把一个 await 表达式分割的，所以我们可以直接访问 Task，或者做其他事，在等待这个 Task 完成之前。

```c#
Task<string> myTask = webClient.DownloadStringTaskAsync(uri);
string page = await myTask;
```

要完全理解这个是非常重要的。**DownloadStringTaskAsync** 在第一行执行。它在当前线程开始同步执行，并且一旦开始下载，它仍会在当前线程返回一个类型为 Task<string>。只有当我们 await 返回的 Task<string>，编译器就会做相应的事情。只要是在异步方法中使用了 **await**，过程都是如此。

一旦开始运行调用 **DownloadStringTaskAsync** 这样一个长时间的操作，它将给我们一个非常简单的方式去执行多个异步并发操作。