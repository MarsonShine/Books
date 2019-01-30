# 深入理解异步编译器转换机

Async 在 C# 编译器中是通过 .NET Framework 库的基类来实现。在运行时，它是不需要做任何改变来支持 async。也就是说 **await** 通过转换来实现的，这个转换在早期 C# 版本中是可以自己编写的。我们使用反编译工具如 .NET Reflector 来看下它生成的代码。

理解生成的代码对调试，性能分析以及其他异步代码分析都是非常有帮助的，还非常有趣。

## 存根（Stub）方法

 异步方法被存根方法替换了。当你调用 async 方法发生的第一件事就是运行存根方法。让我们来看下简单的异步例子：

```c#
public async Task<int> AlexsMethod()
{
    int foo = 3;
    await Task.Delay(500);
    return foo;
}
```

编译器就会生成对应的存根方法，就像下面：

```c#
public Task<int> AlexsMethod()
{
    <AlexsMethod>d__0 stateMechine = new <AlexsMethod>d__0();
    stateMechine.<>4__this = this;
    stateMechine.<>t__builder = AsyncTaskMethodBuilder<int>.Create();
    stateMechine<>.t__builder.Start<<AlexMethod>d_0>(ref statemechine);
    return stateMechine.<>t__builder.Task;
}
```

我手动的修改了方法中的变量，使之更容易看懂。就像我之前说的 “Async，Method Signatures，以及接口” 22 页面一样，在外面使用 **async** 关键字没有副作用。这就变得很明显了，生成的存根方法的签名总是与原始 async 方法相同，但是没有标记 async 关键字。

你应该注意到在存根方法都不在原始代码中。大多数存根方法由初始化变量结构组成，并调用 **<AlexsMethod>d__0**>。这个结构体是状态机，所有重活累活都是它完成的。存根方法调用了一个 **Start** 方法，然后返回一个 **Task**。为了理解其中发生的细节，我们需要去看看这个状态机结构里面的内容。

## 状态机结构

编译器生成一个结构，它作为一个状态机并包含了所有我的原始代码。这样做是为了有一个对象能够代表这个方法的状态，当运行到达 **await** 的时候，它能被存储下来。要记住是当我们到达 await 的时候，关于我们在方法中所处的位置的一切都会被记住，因此，当方法被恢复时，它也可以被恢复。

当你的方法暂停的时候，编译器会存储每一个本地变量，尽管这个过程是可行的，但是这将会生成大量的代码。一个好的改变方式就是你方法中所有的局部变量都更改为这个类型的成员变量，这样我们就能存储这个类型的实例，并且所有的本地变量都会自动保存完好。这正是这个状态机的作用。

> 状态机是结构体而不是引用类的主要是因为性能。因为当异步方法同步完成，它就不需要分配到堆。不幸的是，它成为结构体我们很难进行推断。

状态机作为一个内部结构被生成，它包含了异步方法。这样就能很容易知道它是从哪个方法生成的，但是主要还是它能够访问你类型的私有成员信息。

来看一下生成的状态机结构 <AlexsMethod>d__0 内容：

```c#
public int <>1__state;
public int <foo>5__1;
public AlexsClass <>4__this;
public AsyncTaskMethodBuilder<int> <>t__builder;
public object <>t__stack;
public TaskAwaiter <>u_$awaiter2;
```

> 所有的变量都有尖括号（angle brackets）。这是编译器生成时候标记的。这在其他编译器生成的代码来说是非常重要的，它必须要与用户的代码共存，由于尖括号的存在，这些变量在 C# 里面不会被使用（也就是说我们平常在写代码时，尖括号用作变量是不被允许的）。

首先，状态变量 <>1__state，它是在我们的代码运行到 **await** 时存储内容的地方。在我们到达 **await** 之前，这个状态变量的值是 -1。原始方法中的每个 await 都会被编号，当方法暂停等待时，这个等待回复的编号就会被存储到状态变量中。

接着就是 **<foo>5__1** 它是存储原始变量 foo 的值。我们马上就会看到所有访问我的 foo 变量都会替换成访问这个 <foo>5__1成员变量。

然后再来看 <>4__this。这个只是在状态机里表现的是非静态的异步方法以及这个方法所属的对象。从某种程度上，你可以把 this 想象成在方法中的其他局部变量，当你访问相同对象的其他成员时，这个变量恰好被隐式引用。在异步被转化之后，它需要被显式引用及存储，因为我的代码被移动了，已经从原始对象转变成了状态机结构了。

**AsyncTaskMethodBuilder** 是帮助类，它包含所有关于那些状态机共享的逻辑。它做什么？创建 Task 通过存根方法来返回。实际上，这个处理过程就跟 **TaskCompletionSource** 类似，它创建一个可以操作的 Task 返回，它能晚点完成。与 TaskCompletionSource 不同的是它（AsyncTaskMethodBuilder）是优化一步方法的，并且使用了一些技巧（tricks）比如是结构体而不是类来提高性能。

> 返回 void 的 async method 使用了 AsyncVoidMethodBuilder，当 async methods 返回 Task<T> 时会使用泛型版本—— AsyncTaskMethodBuilder<T>。

堆栈变量，<>t__stack 是用来 awaits 的，它作为一个大的表达式部分。.NET IL 是基于堆栈的语言，而如此复杂的表达式是通过小的指令构建的，它的值维护在堆栈中。当 await 是在这些复杂的表达式之中的时候，在堆栈当前的值会被放置在这个堆栈变量中，如果有多个值的，这个变量就是 Tuple。

最后，TaskAwaiter 是个临时存储的变量，存储帮助 await 关键字来当 Task 完成时，来注册通知。

## MoveNext 方法

状态机经常会调用 MoveNext 方法，它是你的原始代码的最后都会调用的方法。当方法第一次运行以及当我们从 await 恢复的时候，这个方法都会被调用。甚至是最简单的异步方法，也是非常复杂，所以我们讲试着解释这个场景下的每一步的转换。我也会掉过一些无关键要的细节，所以下面这些描述在很多地方都不完全准确。

> 原始方法会调用 MoveNext，这与 C# 早先版本的迭代器中的 MoveNext 很类似。在方法中是使用 yield return 关键字来实现 IEnumerable 的。那里使用的状态机系统中使用的与异步状态机类似，尽管更简单。

### Your Code

第一步就是拷贝你的代码到 MoveNext 方法中。要记住任何访问这些变量都需要更改指针在状态机中生成的新的成员变量。在 await 处，我们将会留下一个空白，并在稍后填充它。

```c#
<foo>5__1 = 3;
Task k = Task.Delay(500);
Logic to wait t gose here
return <foo>5__1;
```

### 转换完成时返回

原始代码中每个 return 片段都需要被转成代码，Task 完成时会通过一个存根方法来返回。事实上，MoveNext 返回 void，我们返回 foo；甚至返回一个无效的。

```c#
<>t__builder.SetResult(<foo>5__1);
return;
```

