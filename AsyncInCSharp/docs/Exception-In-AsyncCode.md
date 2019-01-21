# 异步异常

在异步代码中，异常是在各自工作在他们自己的调用栈上的，每个方法调用回来，通过他们到达 try catch 并捕捉它们，或者离开这段代码。在异步代码中，特别是在一个方法在等待完成被恢复之后，当前调用栈就与程序员的意图关系不大了，主要是包含框架恢复异步方法的逻辑。异常将在调用的代码中可能被捕获，并且捕捉的堆栈跟踪根本没有用处，所以 C# 改变异常行为会更加有用。

> 你可以在调试器中看到调用栈信息

## 在 Task 返回的方法中的异常

很多异步方法你都会返回一个 Task 或是 Task<T>。这些方法是链式的，它们每个都在等在下一个完成，异步方法的调用栈与同步代码的调用栈很相似。C# 努力使异步方法的异常行为与同步方法的异常行为一样。实际上，**try...catch** 里的异步代码会捕捉异常抛出的异常。

```c#
async Task Catcher() {
    try {
        await Thrower();
    } catch(AlexsException) {
        //发生异常将运行这部分代码
    }
}
async Task Thrower() {
    await Task.Delay(100);
    throw new AlexsException();
}
```

> 直到运行到第一个 await，同步调用栈以及链式异步调用的堆栈都是相同的。此时异常的行为任然会因为一致性而改变，但是所需的改变要小的多。

为此，C# 会捕捉你异步方法中的所有异常。当发生异常时，会返回一个 Task 给调用者，异常信息就在里面。这个 Task 就是 Faulted。如果一个方法正在等待的这个 Task 失败了，这个方法就会由正在等待的 Task 抛出的异常恢复。

异步方法中抛出的异常对象与 await 这个方法后的 Task 中抛出的异常信息对象是相同的。向上传播调用栈时，会继续收集堆栈跟踪，把它新增到已经存在的这个堆栈跟踪中。你甚至尝试重新抛出异常时，会比较惊讶。举个例子，在手写异步代码中，它在 .NET Exception 类型中，作为一个新的特性。

这里有一个两个链式异步方法的堆栈跟踪信息。我自己的代码会高亮显示：

System.NullReferenceException: Object reference not set to an instance of an object.
   at **FaviconBrowser.MainWindow.<GetFavicon>dc.MoveNext() in**
**MainWindow.xaml.cs:line 74**
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at 
System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(T
ask task)
   at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
   **at FaviconBrowser.MainWindow.<GetButton_OnClick>d0.MoveNext() in**
**MainWindow.xaml.cs:line 41**
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.<ThrowAsync>b__0(Object 
state)
   at ... Framework methods

要注意 **MoveNext** 已经被编译器转化了，这方面可以看第十四章。在我的每行里有少许的框架方法，但是我任然可以得到我自己调用引起的异常结果这样的效果。

## 未被捕获的异常

同步代码和异步代码一个重要的不同就是被调用的方法抛出的异常。在异步方法中，它是在 await 处抛出的而不是方法实际抛出的地方。显然，如果你调用方法和 await 分开的话，就会出现捕获不到异常的情况，例如下面这段代码

```c#
Task task = Thrower();//never throws exceptions;
try {
    await task;
} catch (Exception e) {
    //Exception will reach here;
}
```

异步方法是很容易忘掉 **await** ，特别是返回一个非泛型的 Task，因为你无需这个方法返回任何值。这就相当于使用空的 catch 块来捕获所有异常并且忽略它们。这是不对的，因为它会导致程序中无效的状态，就好像 bug 发生他们的 bug 引起的一样。一定要总是等待调用异步方法，来避免浪费太多时间来调试。

> 这种忽略异常的行为实际上从 .NET 版本异步方法之前改变过来的。如果你期望异常在你的 TPL 代码重新抛出在线程释放的时候，那么在 .net4.5 应该不会太久就会支持这个功能的。

## 异步 void 方法异常

异步方法返回 void 是不能 await 的，所以他们在异常方面是与众不同的。我们不喜欢它们的异常总是未捕捉的。相反，任何异常离开 async void 方法都在会调用他的线程中重新抛出异常：

1. 如果异步方法被调用的时候存在 SynchronizationContext，则异常被 **Posted**
2. 如果不是，则在线程池抛出异常

在大多数情况，它们最后都将会结束处理，除非一个未处理的异常处理被附加到合适的事件中。那这可能不是你想要的，这也是你应该只写异步 void 方法以便内部代码调用的一个理由，或者你能保证它不会抛出异常。

## 只一次触发（Fire And Forget）

这种情况非常罕见，你无需关心方法是否成功，也不关心等待它的是不是很复杂。这种情况下，我的建议是任然是返回一个 Task，但是传递 Task 到这个方法的目的是处理异常。下面这个拓展方法对于我来说就非常好：

```c#
public static void ForgetSafety(this Task task) {
    task.ContinuWith(HandleException);
}
```

HandleException 可以处理发生的所有异常，并把它记录到日志系统。就像第七章 “写自己的组合器” 讲的一样。

## AggregateException 以及 WhenAll

在异步的世界里，我们必须要处理这么一个场景，同步世界不可能在发生的事。一个方法可能一次抛出多个异常。这是可能的，举个例子，当你使用 **Task.WhenAll** 并 await 一组操作完成的时候。在他们之中有一些失败了，没一个失败是最重要的，或是第一个错误，换句话说就是你都要知道这些错误。

WhenAll 只是一个公共的机制来产生多个异常；其实还有很多其他方式并发运行多个操作。支持多个异常被直接构建到 **Task** 中。而不是直接包含一个 Exception，Task 失败时，会有一个 **AggregateException**。它包含了所有发生异常的集合。

因为要支持直接构建到 Task 中，在被封装到 Task 之前，当一个异常封装到异步方法中，AggregateException 就会创建，并且实际发生的异常会作为一个内部异常添加进去。所以大多数情况下，AggregateException 只包含一个内部异常，但是 WhenAll 针对多异常会创建 AggregateException。

> 这都将发生，无论异常是否在第一次 await 。Exceptions 在第一次 await 之前将更早的同步抛出，但是那样就会使他们靠近调用方法的地方而不是调用 await 的地方，这样就不会一致了。

另一方面，当在 await 的时候重新抛出，我们需要妥协。await 应该重新抛出与在异步方法中抛出的原始异常类型相同，而不是 AggregateException。所以别无选择，只能抛出第一个内部异常。但是你捕捉它之后，你就可以用 Task 直接获取到这个 AggregateException 以及这些异常列表。

```c#
Task<Image[]> allTask = Task.WhenAll(tasks);
try
{
    await allTask;
}
catch
{
    foreach (Exception ex in allTask.Exception.InnerExceptions)
    {
        // Do something with exception
    }
}
```

