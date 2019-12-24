# 异步流（AsyncStream）

原文地址：[https://github.com/dotnet/roslyn/blob/master/docs/features/async-streams.md](https://github.com/dotnet/roslyn/blob/master/docs/features/async-streams.md)

> 注意：以下内容最好能根据反编译工具查看异步流相关类生成的代码效果最佳

异步流是可枚举类（Enumerable）的异步变体，它会在遍历下一个元素的时候（Next）会涉及异步操作。只要继承自 IAsyncEnumerable<T> 就能实现。

首先我们来看下这些在 .netcore3.0 新增的异步流 API

```c#
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }
    public interface IAsyncEnumerator<out T> : IAsyncDisposable
    {
        T Current { get; }

        ValueTask<bool> MoveNextAsync();
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
```

当有异步流时，你可以使用异步形式的 `foreach` 代码段迭代可枚举的类：`await foreach(var item in asyncStream){...}`。`await foreach` 语句与 `foreach` 语句是一样的，只是它使用了 `IAsyncEnumerable` 代替 `Enumerable`，在每次迭代都会去执行调用 `await MoveNextAsync()`，并且这些枚举器的释放都是异步的。

同样，当你有可异步释放的对象时，你就可以通过 `using` 语句使用并异步的处理它：`await using(var response = asyncDisposable){...}`。`await using` 跟 平常用的 `using` 是一样的，就如异步流对象一样，只是用了 `IAsyncDisposable` 代替 `IDisposable` ，`DisposeAsync()` 代替方法 `Dispose()`。

使用者也可以自己手动实现这些接口，或者使用编译器的高级特性从使用着（程序员）定义的方法（调用一个异步迭代器方法）自动生成状态机。异步迭代器方法特征如下：

1. 是异步的（标记 async）
2. 返回 IAsyncEnumerable<T> 或 IAsyncEnumerator<T> 类型。
3. 要使用 await 语法（await 表达式，如 await foreach 或是 await using 语句）以及 yield 语句（yield return，yield break）。

举个例子：

```c#
async IAsyncEnumerable<int> GetValuesFromServer()
{
    while (true)
    {
        IEnumerable<int> batch = await GetNextBatch();
        if (batch == null) yield break;

        foreach (int item in batch)
        {
            yield return item;
        }
    }
}
```

就像在迭代方法里一样，这里有几个限制，在 yield 声明语句出现的附近：

- 对于一个 yield 语句（或者是其他任意形式）出现在 try finally 语句中会导致编译期错误。
- 对于一个 yield 语句出现在 try 语句包裹的任何地方包括任何 catch 块都会导致编译器错误。

## await using 语句的详细设计

异步 using 与 常规 using 是一样低（lowered）的，只是用 DisposeAsync() 代替 Dispose() 方法。

要注意，基于模式查找 DisposeAsync 绑定到一个实例方法，它能够在被没有参数下被调用。

拓展方法是不可用在异步流上的，DisposeAsync 的结果必须是可等待的。

## await foreach 语句的详细设计

await foreach 就如常规 foreach 一样，只是：

- GetEnumerator() 被替换程了 GetAsyncEnumerator()
- MoveNext() 被替换成了 await MoveNextAsync()
- Dispose() 被替换成了 DisposeAsync()

注意，基于模式查找的 `GetAsyncEnumerator`，`MoveNextAsync` 以及 `DisposeAsync` 绑定到一个没有参数就能被调用的实例方法。拓展方法无效。`MoveNextAsync` 以及 `DisposeAsync` 同样必须是可等待的。释放清理 `await foreach` 不包含回掉函数来检查这个接口的实现。

异步 foreach 迭代是不允许集合类型为 dynamic 类型，因为没有等价的异步非泛型 IEnumerable 接口。

包装器类型能传递非默认值（查看 api `.WithCancellation(CancellationToken)` 拓展方法），因此允许消费者控制异步流的取消。异步流的生产者也能够使用取消令牌通过在自定义类写 IAsyncEnumerator<T> GetAsyncEnumerator(CancellatonToken) 异步迭代器方法。

```c#
E e = ((C)(x)).GetAsyncEnumerator(default);
try
{
    while(await e.MoveNextAsync())
    {
        V v = (V)(T)e.Current; -OR-  (D1 d1, ...) = (V)(T)e.Current;
        //body
    }
}
finally
{
    await e.DisposeAsync();
}
```



## 异步迭代器方法的详细设计

异步迭代器方法被初始化的状态机的启动（kick-off）方法替换。它不会在开始时就运行状态机（不像常规的 async 方法的启动方法）。这个启动方法的方法被标记为 `AsyncIteratorStateMachineAttribute`。

对于可枚举的异步迭代器方法的状态机首先就要实现 `IAsyncEnumerable<T>` 以及 `IAsyncEnumerator<T>`。对于可枚举异步迭代器，它只需要实现 `IAsyncEnumerator<T>`。它与状态机生成异步方法是相似的。它包含生成器（builder）以及等待者（awaiter）字段，用于在后台运行状态机（当 await 在异步迭代器到达的时候）。它也能捕捉参数值（如果有）或者当前对象 `this` （如果需要）。

> 状态机运行过程可详见 《async in c#》。
>
> 这里也提供我尝试理解翻译的地址 
>
> <https://github.com/MarsonShine/Books/blob/master/AsyncInCSharp/docs/Async-Compiler-Transform-In-Deepth.md>

但是对于新的异步内容，这里添加了新的状态：

- 值到结束的状态（promise of a value-or-end），
- 被 yielded 出的当前值类型 T，
- 一个 int 类型值，它能捕捉创建它的线程 id，
- 一个 bool 表示，标识 "dispose mode"，
- 一个 CancellationTokenSource 的组合令牌（in enumerables）

状态机主要的方法是 `MoveNext()`。它能被通过 `MoveNextAsync()` 获得运行，或者从 `await` 方法中作为一个后台继续等待启动（continuation）。

value-or-end 状态从 `MoveNextAsync` 返回。下面提到的都能满足：

- true（当伴随着后台运行状态机执行后，值变的可用）
- false（如果 end 到达）
- 异常。这个状态（promise）被 `ManualResetValueTaskSourceCore<bool>` 实现（它是可重用的，并且无需再分配的方式来生成和实现 `ValueTask<bool>` 或者 `ValueTask` 实例）。关于这些类的更多细节请访问 [https://blogs.msdn.microsoft.com/dotnet/2018/11/07/understanding-the-whys-whats-and-whens-of-valuetask/](https://blogs.msdn.microsoft.com/dotnet/2018/11/07/understanding-the-whys-whats-and-whens-of-valuetask/)

与常规的异步方法的状态机相比，在异步迭代器方法的 `MoveNext()` 增加如下逻辑：

- 支持处理 `yield return` 语句，它保存了当前值并实现 结果 true 的状态（promise with result true）
- 支持处理 `yield break` 语句，它设置释放模式为 on，并跳转到封闭的（enclosing）finally 或 exit 块。
- 执行分配给 finally 块（当正在释放的时候）
- 退出方法，会释放 `CancellationTokenSource` （如果存在）并且实现结果为 false 的状态
- 捕捉异时时，它会释放 `CancellationTokenSource` （如果存在）并且在状态里设置异常信息

这反映在实现中，它将异步方法的降低机制（lowering machinery）拓展如下：

1. 处理 yield return 以及 yield break 语句（查看 `VisitYieldReturnStatement` 方法以及 `VisitYieldBreakStatement` 方法到 `AsyncIteratorMethodToStateMachineRewriter`）
2. 处理 try 语句（查看方法 `AsyncIteratorMethodToStateMachineRewriter` 里的 `VisitTryStatement` 和 `VisitExtractedFinallyBlock` 方法）
3. 对自己的 promise 产生额外的状态和逻辑（查看 `AsyncIteratorRewriter` 方法，它产生其他的成员变量：`MoveNextAsync`，`Currenct`，`DisposeAsync` 以及一些支持可重置的 `ValueTask` 行为的成员，也就是 `GetResult`，`SetStatus`，`OnCompleted`）

```c#
ValueTask<bool> MoveNextAsync()
{
    if(state == StateMachineStates.FinishedStateMachine)
    {
        return default(ValueTask<bool>);
    }
    ValueOrEndPromise.Reset();
    var inst = this;
    builder.Start(ref inst);
    var version = valueOrEndPromise.Version;
    if(valueOrEndPromise.GetStatus(version) == ValueTaskSourceStatus.Succeeded)
    {
        return new ValueTask<bool>(valueOrEndPromise.GetResult(version));
    }
    return new ValueTask<bool>(this,version);//注意，这里用到了状态机的 IValueTaskSource 实现
}
```

```c#
T Current => current;
```

异步迭代器方法的启动方法和状态机的初始化都遵循常规迭代器方法。尤其是 `GetAsyncEnumerator()` 方法就像 `GetEnumerator()` 只是它设置初始化状态为 StateMachineStates.NotStartedStateMachine (-1)：

```c#
IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
{
    {StateMachineType} result;
    if(initialThreadId == /*托管线程id*/ && state == StateMachineStates.FinishedStateMachine)
    {
        state = InitialState;	//-3
        disposeMode = false;
        result = this;
    }
    else
    {
        result = new {StateMachineType}(InitialState);
    }
    /*复制每个参数代理，或者在每个参数在被标记为[EnumeratorCancellation]的情况下和 GetAsyncEnumerator 的 token 参数结合使用*/
}
```

对于被标记为 [EnumeratorCancellation] 的参数，GetEnumerator 通过结合两个可用的 token 初始化：

```c#
if(this.parameterProxy.Equals(default))
{
    result.parameter = token;
}
else if(token.Equals(this.parameterProxy) || token.Equals(default))
{
    result.paramter = this.parameterProxy;
}
else
{
    result.combinedTokens = CancellationTokenSource.CreatedLinkedTokenSource(this.parameterProxy,token);
    result.parameter = combinedTokens.Token;
}
```

对于线程 id 的检查，可查看 [https://github.com/dotnet/corefx/issues/3481](https://github.com/dotnet/corefx/issues/3481)

类似地，启动方法与常规迭代器方法非常相似：

```c#
{
    {StateMachineType} result = new {StateMachineType}(StateMachineStatus.FinishedStateMachine);//-2
    /* 保存参数到参数代理中 */
    return result;
}
```

## Disposal

迭代器和异步迭代器方法都需要处理，因为他们执行的步骤都被调用者控制，它可以选择在得到所有的元素之前释放迭代器。例如，`foreach(...){if(...) break;}`。相比之下，异步方法会持续自动运行直到结束。在调用者角度来说，它们不会在执行过程中挂起（暂停），所以它们不需要被释放。

总之，异步迭代器的释放基于四个设计元素：

- `yield return`（当恢复至 dispose 模式时，跳转到 finally ）
- `yield break`（进入 dispose 模式并且跳转到封闭的 finally 块）
- `finally`（`finally` 后我们会跳转到下一个）
- `DisposeAsync`（进入 dispose 模式并恢复执行）

异步迭代器方法的调用者应该在当一个方法完成或被 `yield return` 挂起时，只调用 `DisposeAsync()`。`DisposeAsync` 会在状态机("dispose mode")设置一个标识以及（如果这个方法没有完成）从当前状态恢复执行。状态机能够在被给定的状态恢复执行（甚至那些分配到 try 中的）。在 dispose 模式下恢复执行时，它会直接跳转到 `finally`。

`finally` 块只在 await 表达式下可能包含暂停以及恢复。由于在 `yield return` 上的限制（上面描述的），dispose 模式不会运行进入 `yield return`。一旦 finally 块完成，在 dispose 模式中执行跳转到下一个 finally 块，或是在到达了方法的最顶层结尾。

到达 `yield break` （或是方法结束）会设置 dispose 模式标识以及跳转到 finally 块。当我们返回控制权给调用者时（通过到达方法尾部来完成 promise false）所有的处理都会被完成以及状态机也处于完成状态。所以 `DisposeAsync()` 不需要做其他事了。

从给定的 finally 块来看处理，可以执行块中的代码：

- 正常执行（比如在 try 块中的代码之后）
- 在 try 块中触发异常（它将执行必要的 finally 块以及在 Finished 状态中终止方法）
- 调用 `DisposeAsync()`（它在 dispose 模式恢复执行并且跳转到 finally 块）
- yield break（进入 dispose 模式并跳转到 finally 块）
- dispose 模式，后面跟着嵌套 finally

a yield return is lowered as：

```c#
_current = expression;
_state = <next_state>;
goto <exprReturnTrueable>;// 它执行了 _valueOrEndPromise.SetResult(true);return;
//从 state = <next_state> 恢复,将执行到这个标签
<next_state_label>:;
this.state = cachedState = NotStartedStateMachine;
if(disposeModel)/* 跳转到 finally 或退出*/
```

a yield break is lowered as：

```c#
disposeModel = true;
/* 跳转到 finally 或退出 */
```

```c#
ValueTask IAsyncDisposable.DisposeAsync()
{
    if(state >= StateMachineStates.NotStartedStateMachine /* -1 */)
    {
        throw new NotSupportedException();
    }
    if(state == StateMachineStates.FinishedStateMachine /* -2 */)
    {
        return default;
    }
    disposeModel = true;
    _valueOrEndPromise.Reset();
    var inst = this;
    _builder.Start(ref inst);
    return new ValueTask(this,_valueOrEndPromise.Version);//注意，这里用到了状态机的 IValueTaskSource 实现
}
```

### **与通常提取 finally 的对比**

当 finally 块内不含 await 表达式，try/catch is lowered as:

```c#
try
{
    ...
    finallyEntryLabel:
}
finally
{
    ...
}
if(disposeMode) /* 跳转 finally 或退出 */
```

当 finally 块内包含 await 表达式时，在异步重写之前被提取（通过 AsyncExeptionHandlerRewriter）。

在这种情况下，我们将知道：

```c#
try
{
    ...
    goto finallyEntryLabel:
}
catch(Exception e)
{
    ... save exception ...
}
finallyEntryLabel:
{
    ... 从 finally 的原始代码和异常的附加处理 
}
```

这两种情况下，我们将在 finally 逻辑块后添加 `if(disposeMode) /* 跳转到 finally 或退出 */`

### 状态值和转换

可枚举从状态 -2 开始。调用 GetAsyncEnumerator 来设置状态为 -3，或返回一个新的枚举器（状态也为 -3）。

从这里开始，MoveNext 将会做：

- 到达方法的最后（-2，我们已经做完并释放）
- 到达 `yield break`（状态不变，dispose mode = true）
- 到达 `yield return`（-N，从 -4 递减）
- 到达 `await`（N，从 0 递增）

从暂停状态 N 或 -N，MoveNext 将恢复执行(-1)。但是如果暂停 yield return（-N），你也能调用 DisposeAsync，它在 dispose mode 中恢复执行（-1）。

当在 dispose mode 下，MoveNext 会继续暂停（N）和恢复（-1）直到到达方法结束（-2）。

从状态 -1 或 N 调用 DisposeAsync 的结果未指明。编译器在这种情况下会生成 `throw new NotSupportedException`。

```
   DisposeAsync                              await
 +------------------------+             +------------------------> N
 |                        |             |                          |
 v   GetAsyncEnumerator   |             |        resuming          |
-2 --------------------> -3 --------> -1 <-------------------------+    Dispose mode = false
 ^                                   |  |                          |
 |         done and disposed         |  |      yield return        |
 +-----------------------------------+  +-----------------------> -N
 |        or exception thrown        |                             |
 |                                   |                             |
 |                             yield |                             |
 |                             break |           DisposeAsync      |
 |                                   |  +--------------------------+
 |                                   |  |
 |                                   |  |
 |         done and disposed         v  v    suspension (await)
 +----------------------------------- -1 ------------------------> N
                                        ^                          |    Dispose mode = true
                                        |         resuming         |
                                        +--------------------------+
```




> 注意：A `yield return` is lowered as 我实在是不知道 is lowerd as 到底是什么意思，单个单词都会，拼在一起怎么读都不顺

[参考链接]: https://blog.stephencleary.com/2013/11/there-is-no-thread.html

