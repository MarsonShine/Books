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

一旦开始运行调用 **DownloadStringTaskAsync** 这样一个长时间的操作，它将给我们一个非常简单的方式去执行多个异步并发操作。我们可以开始多个 Task 操作，然后一起等待。

```c#

```

> 上面这种等待多个 Task 的操作是危险的，因为它们有可能会发生异常。如果这些操作抛出了一个异常，第一个 await Task 就会传播这个异常，也就是说第二个 Task 永远等不到结束。它观察不到异常，这取决于 .NET 版本以及设置，也许会丢失，也许会重新在一个线程抛出异常从而让进程失败。

# 异步方法返回类型

这里有 async 标记的方法返回的三个类型

- void
- Task
- Task<T>

在异步方法通常返回时还没有完成，除此之外不能返回任何类型。需要指出的是，一个异步方法将等一个长时间的操作时，就以为它需要快速返回，但是在未来的某个时刻重新占用继续往下执行。也就是说当方法返回时，这个返回值没有意义。只有在 await 之后结果值才会有用。

> 这里要区分一下方法返回的类型，举个例子，Task<string>——结果类型是程序实际想给调用者的，在这个例子中就是 string。在没有 async 的方法中，方法返回的类型与结果类型通常是一样的，这跟 async 标记的方法是不同的。

显然，void 是异步情况下返回类型的合理选择。一个 async void 标记的方法是一个 “一触发就不管” 的异步操作。调用者不需要等到它的任何结果，也不需要知道这个操作完成与否或是它（该操作）是否成功。当你知道没有调用者需要知道这个操作何时完成或者是否成功的时候你应该是用 void。事实上，void 用的非常少。使用 async void 大体上都是出于异步代码和其他代码的边界情况，例如 UI 事件必须返回 void。

async 方法返回 Task 允许调用者等待这个操作完成，一旦发生了异步操作异常，并且可以传播它。当不需要结果值时，一个 async Task 方法就要比 async void 方法要好得多，因为它允许调用者可以等待他，更容易的处理这些异常。

最后，async 方法返回 Task<T> ，例如 Task<string> 用在当这个操作有一个返回值的时候。

# 异步，方法签名和接口

**async** 关键出现在方法声明附近，就像 **public** 或 **static** 关键字一样。尽管如此，在重写其他方法，实现接口或被调用方面，async 并不是方法签名的一部分。

async 关键字只有在它应用的方法上的编译是有影响的，不像其他应在方法上的关键字，它改变方法与外部世界交互。因为这点，围绕着重写方法和实现接口规则完全忽略了 async 关键字。

```c#
public class BaseClass {
    public virtual async Task<int> MethodAsync() {
        return await Task.FromResult(default(int));
    }
}

class SubClass : BaseClass {
    public override Task<int> MethodAsync() {
        return new Task<int>(() => 1);
    }
}
```

接口不能在方法声明中使用 async。要记住是没有 async 标记的方法，使用 return 语句取决于方法返回的类型：

1. void methods

​	return 语句必须是 **return;** 或是没有 return。

2. 返回泛型类型的方法

​	返回的必须是一个 T 类型的表达式（比如 `return 5+x`） 并且必须存在于方法的最后运行路径。

方法标记了 async，应用于下面两个规则：

1. void 方法和返回 Task 方法

   return 语句必须是 **return;** 或是可选的

2. 方法返回 Task<T>

   return 语句必须有一个 T 类型的表达式存在于方法内所有代码之后。

在 async 方法中，方法返回的类型是不同于在返回语句中的类型表达式。方法在给到调用者之前，编译器会把你的值转化封装到返回的 Task<T> 中。当然，在现实中，Task<T> 是立即创建的，只有在一个长时间操作结束后才会填充到你的结果值。

# 异步方法具有传染性

就像我们看到的，最好的方式就是使用异步API返回的 Task 是在异步方法中等待它。只有这么做，你的方法就会指定返回一个 Task。而作为受益方，调用这个方法等待你的 Task 完成肯定不会被阻塞，所以你的调用者也将等待你。

这里有一个例子，我已经写了这个方法来获取字符串的长度，并且异步返回。

```c#
private async Task<int> GetPageSizeAsync(string url) {
    WebClient webClient = new WebClient();
    string page = await webClient.DownloadStringTaskAsync(url);
    return page.Length;
}
```

就这样，我们可以写链式异步方法，每个方法等待下一个方法。async 是一种连续不断的程序模型，容易遍布整个代码库中。但是这样反而异步方法更容易写了，我想这些都不是问题。

# 异步匿名委托和Lambda

原始方法能被标记为 async，匿名方法的两种形式也可以是异步的。语法看起来就跟平常的方法一样。下面展示了写一个匿名异步委托：

```c#
Func<Task<int>> getNumberAsync = async delegate { return 3;};
```

这里是异步 lambda 表达式

```c#

```

相同规则都可以应用到原来的异步方法中。你可以使用它们让代码保持整洁，捕获闭包，方式在非异步代码中完全一样。