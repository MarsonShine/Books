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

## 未注意的异常

