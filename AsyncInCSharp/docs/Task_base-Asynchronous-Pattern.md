基于任务的异步模式（TAP）是微软推荐 Task 写异步的模式。StephenToub 在微软并行编程开发团队中写了一篇文档，很值得阅读。

这个模式当使用 **async** 产生的方法，是能够让 APIs 能够使用 **await**，它经常使用 Task。这节，将聚焦于这个模式，以及如何用它工作的技术。

# TAP 是什么

我们假设我们已经知道了如何为异步代码设计一个好的方法签名：

- 它应该参数比较少，或者没有。尽可能的便面使用 **ref** 和 **out**。
- 应该要有返回类型，它才是有意义的，作为方法里面的表达式的结果值
- 它需要有一个名称来解释这些方法的行为，而不需要额外的代码
- 常见或期望的失败应该成为返回类型的一部分，当一个未知的错误发生要抛出来的时候

这是一个设计好的同步方法，它是 Dns 类的一个方法：

```c#
public static IPHostEntry(string hostNameOrAddress)
```

基于你已有的编写同步方法技能，在设计一个异步方法时，TAP 给了相同的指导方案。他们是：

- 它应该有作为同步方法相同的参数，绝对不能使用 **ref** 和 **out** 参数。
- 它应该返回 Task 或 Task<T>，这是取决于对应的同步方法返回什么类型。在未来的某个时间 task 完成时，要提供结果值给方法。
- 它应该被命名为 NameAsync，其中 Name 等于同步方法的名称。
- 在方法使用过程中发生了错误，要从方法中直接抛出。任何异常都应该在 Task 中。

下面是一个好的 TAP 模式的异步方法：

```c#
public static Task<IPHostEntry> IPHostEntryAsync(string hostNameOrAddress)
```

它们可能明显处于完成转台，但是当我们在 “.NET 中的异步模式” 看到的，这是 .NET 使用的第三种正式的异步模式，并且我确定其他人肯定用无数种非正式的方式来写异步代码。 

TAP 异步方法的关键就是返回一个 **Task**，它封装了一个长时间运行的，承诺在未来的某个时刻完成的操作。没有这个，上个异步模式就需要去增加额外的参数，或者增加额外的方法或事件到接口来支持回调机制。**Task** 包含了需要支持回调的指令，它不会污染你的详细代码。

还有一个好处就是，因为异步代码的回调机制是在 **Task* 中的，所以他们不需要在其他异步地方多次声明。反过来，这个机制就意味着做事情更加复杂和有力以及高拓展性，就像恢复上下文一样，包括同步上下文。它也提供了公共的 API 来处理异步操作，编译器的特征（async await）让异步比其他的模式更加合理。

# 使用 Task 进行密集型操作

有时，一个耗时长的操作不会使任何网络或访问硬盘；仅仅只是因为复杂的计算，需要大量处理器事件来完成。当然，我们不能期望捆绑在一个线程上就能够这么做，就好像网络请求一样。但是在程序中，我们可以避免 UI 冻结。为了达到这个目的，我们必须返回一个 UI 线程来处理其他事件，并且使用一个不同的线程来进行长时间的计算。

**Task** 提供了一个简单的方式处理这个，你可以处理像其他的 **Task** 一样使用 await，当它计算完成时更新 UI：

```c#
Task t = Task.Run(() => MyLongComputation(a,b))
```

这是工作在后台线程的一种非常简单的方式。

例如，如果你需要更多的控制线程的计算，或是线程在队列中如何排队，Task 有一个静态属性叫 **Factory**，类型为 **TaskFactory**。它有一个 **StartNew** 方法，有多个重载来控制你执行的计算：

```c#
Task t = Task.Factory.StartNew(() => MyLongComputation(a,b),
								cancellationToken,
								TaskCreationOptions.LongRunning,
								taskShceduler);
```

如果你正在写一个类库，它包含大量的密集型计算方法，你可能就需要提供异步版本的方法来调用 Task.Run 在你的后台线程中工作。这不是个好建议，因为你的 API 的调用者比你更知道关于应用程序的线程要求。例如，在 web 应用程序，使用线程池已经没有收益了；只有一件事是可以优化的，那就是线程的数量。**Task.Run** 非常容易调用，如果需要的话，就让他们自己去做吧。

# 创建 Puppet 任务

TAP 很容易用，所以你将很自然的想用提供它在你的所有的 API 方法。当你使用其他 TAP 的 APIs，我们已经知道使用异步方法怎样去做了。但是当一个耗时长的操作并没有 TAP 的API 又怎么办呢？也许它是使用其他异步模式的 API。可能你无法使用这个 API，但是你可以做一些手动异步操作来完成。

TaskCompletionSource<T> 就能很好的使用它。它可以新建一个 Puppet Task。你可以让 Task 在你想要完成的时间段来完成，并且你可以通过给一个异常在你想要的时间段让它失败。

让我们来看一个例子。你得封装下面这个方法显示给用户：

```C#
Task<bool> GetUserPermission()
```

这个提示是你写的一个自定义对话框，它询问用户是否同意。因为在你的应用程序中，这种许可在很多地方都需要，所以使它更容易的调用是非常重要的。这是完美的地方使用异步方法，因为你想释放 UI 线程来显示对话框。但是它甚至不接近那些网络请求或是长时间运行的操作的传统异步方法。这里，我们正在等待用户，然后我们来看下方法体：

```c#
private Task<bool> GetUserPermission() {
    //创建一个 TaskCompletionSource，让我们可以返回一个任我操控的 Task
    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
    //新建窗口
    PermissionDialog dialog = new PermissionDialog();
    //当用户完成对话框时，Task 使用 SetResult 来表示完成
    dialog.Closed += delegate { tcs.SetResult(dialog.PermissionGranted); };
    dialog.Show();
    //返回一个“傀儡” Task，它还没有完成。
    return tcs.Task;
}
```

注意这个方法并没有标记 **async**；我们手动创建的 Task，所以我们不需要编译器生成一个给我们。**TaskCompletionSource<bool>** 创建一个 **Task**，并且使它能做一个属性返回。我们能在在 **TaskCompletionSource** 稍后使用 **SetResult**  方法中使 Task 完成。

因为根据我们依据的 TAP，我们的调用者只是等待用户的许可。调用非常简洁。

```c#
if(await GetUserPermission()){
    ...
}
```

一个令人烦恼的就是，**TaskCompletionSource** 没有非泛型的版本。但是，由于 Task<T> 是 Task 的子类，你可以使用 Task<T> 的地方使用 Task。反过来就是说你可以使用 TaskCompletionSource<T>，并且通过它的属性 **Task**  返回Task<T> ，它（返回的Task<T>）是一个有效的 Task。我倾向于使用 **TaskCompletionSource<object>** 以及 调用 **SetResult(null) **来完成它。你可以根据需要创建一个非泛型的 **TaskCompletionSource**，在泛型的基础之上。