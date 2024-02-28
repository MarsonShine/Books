# 错误处理操作符

异常时有发生。有些异常本质上是可以避免的，只是因为我们代码中的错误才会发生。例如，如果我们将 CLR 置于必须引发 `DivideByZeroException` 的境地，那么我们就做错了。但是有很多异常是无法通过防御性编码来避免的。例如，与 I/O 或网络故障有关的异常（如 `FileNotFoundException` 或 `TimeoutException`）可能是由代码无法控制的环境因素引起的。在这种情况下，我们需要优雅地处理异常。处理方式取决于具体情况。向用户提供某种错误信息可能比较合适；在某些情况下，记录错误可能是更合适的应对方式。如果故障可能是短暂的，我们可以尝试重试失败的操作来恢复。

`IObserver<T>` 接口定义了 `OnError` 方法，以便源可以报告错误，但由于该方法会终止序列，因此无法直接确定下一步该怎么做。不过，Rx 提供的操作符提供了多种错误处理机制。

## Catch

Rx 定义了一个 `Catch` 操作符。这个名字让人特意联想到 C# 的 `try/catch` 语法，因为它可以让你以类似于正常执行代码时出现的异常的方式处理来自 Rx 源的错误。它有两种不同的工作方式。您可以只提供一个函数，Rx 会将错误传递给该函数，而该函数可以返回一个 `IObservable<T>`，此时 `Catch` 将从该函数而非原始源转发项目。或者，你可以不传递函数，而只提供一个或多个附加序列，每次当前序列失败后，`Catch` 就会转到下一个序列。

### 提取异常

`Catch` 有一个重载功能，当源产生错误时，可以提供一个处理程序供调用：

```c#
public static IObservable<TSource> Catch<TSource, TException>(
    this IObservable<TSource> source, 
    Func<TException, IObservable<TSource>> handler) 
    where TException : Exception
```

这在概念上与 C# 的 `catch` 代码块非常相似：我们可以编写代码来查看异常，然后决定如何继续处理。与 `catch` 代码块一样，我们可以决定对哪种异常感兴趣。例如，我们可能知道源有时会产生超时异常（`TimeoutException`），在这种情况下，我们可能只想返回一个空序列，而不是错误：

```c#
IObservable<int> result = source.Catch<int, TimeoutException>(_ => Observable.Empty<int>());
```

只有当异常属于指定的类型（或由其派生的类型）时，`Catch` 才会调用我们的函数。如果序列以无法转换为 `TimeoutException` 的异常终止，那么错误将不会被捕获，并将流向订阅者。

此示例返回 `Observable.Empty<int>()`。这在概念上类似于 C# 中的“吞”异常，即选择不采取任何行动。这可能是对预期异常的合理响应，但对基本异常类型来说，这样做通常不是个好主意。

最后一个示例忽略了输入，因为它只对异常类型感兴趣。不过，我们可以自由地检查异常，并对 `Catch` 中应出现的内容做出更精细的决定：

```c#
IObservable<string> result = source.Catch(
    (FileNotFoundException x) => x.FileName == "settings.txt"
        ? Observable.Return(DefaultSettings) : Observable.Throw<string>(x));
```

这为一个特定文件提供了特殊处理方法，但除此之外会重新抛出异常。在这里返回 `Observable.Throw<T>(x)`（其中 `x` 是原始异常）在概念上类似于在 `catch` 代码块中写 `throw`。(在 C# 中，`throw`; 和 `throw x`; 之间有一个重要的区别，因为它改变了捕获异常上下文的方式，但在 Rx 中，`OnError` 并不捕获堆栈跟踪，因此没有类似的区别）。

当然，你也可以抛出完全不同的异常。你可以返回任何你喜欢的 `IObservable<T>`，只要它的元素类型与源相同。

### Fallback

`Catch` 的其他重载提供的行为区分度较低：您可以提供一个或多个额外的序列，当当前源失败时，异常将被忽略，`Catch` 将直接转到下一个序列。由于你永远不会知道异常是什么，因此这种机制让你无法知道发生的异常是你预料到的，还是完全出乎意料的，所以你通常会想避免使用这种形式。但为了完整起见，这里介绍一下如何使用它：

```c#
IObservable<string> settings = settingsSource1.Catch(settingsSource2);
```

该表单只提供一个回退。还有一个静态的 `Observable.Catch` 方法，它接受一个参数数组，因此你可以传递任意数量的来源。这与前面的示例完全相同：

```c#
IObservable<string> settings = Observable.Catch(settingsSource1, settingsSource2);
```

还有一个接受 `IEnumerable<IObservable<T>>` 的重载。

如果任何一个源结束时没有报告异常，`Catch` 也会立即报告完成，并且不会订阅任何后续源。如果最后一个源报告了异常，`Catch` 将没有其他源可以依靠，因此在这种情况下它不会捕获异常。它会将最后一个异常转发给它的订阅者。

## Finally

与 C# 中的 `finally` 代码块类似，Rx 使我们能够在序列完成时执行某些代码，而不管它是完成还是失败。Rx 增加了在 `catch/finally` 中没有完全对应的第三种完成模式：订阅者可能会在源完成之前取消订阅。(这在概念上类似于使用 `break` 提前结束 `foreach`)。`Finally` 扩展方法接受一个 `Action` 作为参数。无论调用的是 `OnCompleted` 还是 `OnError`，当序列终止时都将调用该 `Action`。如果订阅在完成前被处理掉，它也会调用该操作。

```c#
public static IObservable<TSource> Finally<TSource>(
    this IObservable<TSource> source, 
    Action finallyAction)
{
    ...
}
```

在这个示例中，我们有一个完成的序列。我们提供了一个动作，并看到它在我们的 `OnCompleted` 处理程序之后被调用。

```c#
var source = new Subject<int>();
IObservable<int> result = source.Finally(() => Console.WriteLine("Finally action ran"));
result.Dump("Finally");
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);
source.OnCompleted();
```

输出：

```
Finally-->1
Finally-->2
Finally-->3
Finally completed
Finally action ran
```

源序列也可能因异常而终止。在这种情况下，异常会被发送到订阅者的 `OnError`（我们会在控制台输出中看到），然后我们提供给 `Finally` 的委托就会被执行。

或者，我们也可以将订阅处理掉。在下一个示例中，我们看到即使序列没有完成，`Finally` 操作也会被调用。

```c#
var source = new Subject<int>();
var result = source.Finally(() => Console.WriteLine("Finally"));
var subscription = result.Subscribe(
    Console.WriteLine,
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);
subscription.Dispose();
```

输出：

```
1
2
3
Finally
```

请注意，如果订阅者的 `OnError` 抛出异常，并且如果源调用 `OnNext` 时没有 `try/catch` 块，CLR 的未处理异常报告机制就会启动，在某些情况下，这可能会导致应用程序在 `Finally` 操作符有机会调用回调之前关闭。我们可以用下面的代码来创建这种情况：

```c#
var source = new Subject<int>();
var result = source.Finally(() => Console.WriteLine("Finally"));
result.Subscribe(
    Console.WriteLine,
    // Console.WriteLine,
    () => Console.WriteLine("Completed"));
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);

// 导致应用程序宕机。最后可能无法调用操作。
source.OnError(new Exception("Fail"));
```

如果不使用 `try/catch` 将其封装，而是直接从程序的入口点运行，可能会显示 `Finally` 消息，也可能不会显示，因为在异常一直到达堆栈顶端而未被捕获的情况下，异常处理会有微妙的不同。(奇怪的是，程序通常会运行，但如果连接了调试器，程序通常会在不运行 `Finally` 回调的情况下退出）。

这主要是好奇心作祟：ASP.NET Core 或 WPF 等应用程序框架通常会安装自己的堆栈顶层异常处理程序，而且无论如何，你都不应该订阅一个明知会调用 `OnError` 而不提供错误回调的源。之所以会出现这个问题，是因为这里使用的基于委托的 `Subscribe` 重载提供了一个 `IObserver<T>` 实现，该实现会抛出 `OnError`。不过，如果你正在构建控制台应用程序以尝试 Rx 的行为，你很可能会遇到这个问题。在实践中，`Finally` 会在更正常的情况下做正确的事情。(但无论如何，都不应该从 `OnError` 处理程序中抛出异常）。

## Using

`Using` 工厂方法允许将资源的生命周期与可观察序列的生命周期绑定。该方法需要两个回调：一个用于创建一次性资源，另一个用于提供序列。这样就可以对所有内容进行惰性评估。当代码在此方法返回的 `IObservable<T>` 上调用 `Subscribe` 时，这些回调将被调用。

```c#
public static IObservable<TSource> Using<TSource, TResource>(
    Func<TResource> resourceFactory, 
    Func<TResource, IObservable<TSource>> observableFactory) 
    where TResource : IDisposable
{
    ...
}
```

当序列通过 `OnCompleted` 或 `OnError` 终止或当订阅被取消时，资源将被释放。

## OnErrorResumeNext

本节的标题就让老 VB 开发人员不寒而栗！(对于那些不熟悉这种阴暗语言特性的人来说，VB 语言允许您指示它忽略执行过程中出现的任何错误，并在任何失败后继续执行下一条语句）。在 Rx 中，有一种名为 `OnErrorResumeNext` 的扩展方法，其语义与同名的 VB keywords/statement 类似。该扩展方法允许用另一个序列继续一个序列，而不管第一个序列是优雅地完成还是由于错误。

这与 `Catch` 的第二种形式（如 [Fallback](#Fallback) 中所述）非常相似。不同的是，在 `Catch` 中，如果任何源序列结束时没有报错，`Catch` 将不会转到下一个序列。`OnErrorResumeNext` 将转发所有输入产生的所有元素，因此它与 `Concat` 类似，只是忽略所有错误。

正如在 VB 中，`OnErrorResumeNext` 关键字最好不要用于抛出代码以外的任何其他用途，在 Rx 中也应谨慎使用。它会悄无声息地吞噬异常，使程序处于未知状态。一般来说，这会增加代码的维护和调试难度。(这同样适用于 `Catch` 的 fallback 形式）。

## Retry

如果您希望您的序列遇到可预测的故障，您可能想重试。例如，如果您在云环境中运行，操作偶尔会因不明原因而失败是很常见的。云平台通常会因运行原因定期搬迁服务，这意味着操作失败的情况并不罕见--你可能在云提供商决定将服务搬迁到不同的计算节点之前向服务发出请求，但如果你立即重试，完全相同的操作就会成功（因为重试的请求会被路由到新节点）。Rx 的 `Retry` 扩展方法提供了在失败后重试指定次数或直到成功的功能。如果源报错，它就会重新订阅源。

本示例使用简单重载，它会在出现任何异常时重试。

```c#
public static void RetrySample<T>(IObservable<T> source)
{
    source.Retry().Subscribe(t => Console.WriteLine(t)); // Will always retry
    Console.ReadKey();
}
```

给定一个产生值 0、1 和 2 的源，然后调用 `OnError`，输出将是数字 0、1、2 无休止地重复。这个输出将永远持续下去，因为这个示例永远不会取消订阅，如果你不告诉它，`Retry` 将永远重试。

我们可以指定重试的最大次数。在下一个示例中，我们只重试一次，因此第二次订阅时发布的错误会传递到最后一次订阅。请注意，我们告诉 `Retry` 的最大尝试次数，因此如果我们希望它重试一次，就需要传递 2 的值，即初始尝试加一次重试。

```c#
source.Retry(2).Dump("Retry(2)"); 
```

输出：

```c#
Retry(2)-->0
Retry(2)-->1
Retry(2)-->2
Retry(2)-->0
Retry(2)-->1
Retry(2)-->2
Retry(2) failed-->Test Exception
```

在使用无限重复重载时，应适当小心。显然，如果底层序列出现持续性问题，就可能陷入无限循环。另外，需要注意的是，没有重载允许你指定要重试的异常类型。

Rx 还提供了 `RetryWhen` 方法。它类似于我们查看的第一个 `Catch` 重载：它不会无差别地处理所有异常，而是允许你提供代码来决定如何处理异常。它的工作方式略有不同：它不会每次出现错误时都调用此回调函数，而是一次调用并传入一个 `IObservable<Exception>`，通过它提供所有的异常，而回调函数返回一个称为信号 Observable 的 `IObservable<T>`。T 可以是任何类型，因为此信号观察者可能返回的值将被忽略：重要的是哪个 `IObserver<T>` 方法被调用。

如果在接收异常时，信号观察者调用了 `OnError`，`RetryWhen` 将不会重试，并向其订阅者报告同样的错误。另一方面，如果信号可观察对象调用 `OnCompleted`，`RetryWhen` 也不会重试，而是在不报告错误的情况下完成。但如果信号可观察对象调用 `OnNext`，则会导致 `RetryWhen` 通过重新订阅源进行重试。

应用程序通常需要超越简单 `OnError` 处理程序的异常管理逻辑。Rx 提供的异常处理操作符与我们在 C# 中习惯使用的操作符类似，你可以用它来编写复杂而健壮的查询。在本章中，我们将介绍 Rx 的高级错误处理和更多资源管理功能。