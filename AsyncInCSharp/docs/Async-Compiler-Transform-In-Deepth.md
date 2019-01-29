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

> 