# 内存模型

在谈论线程同步相关的知识之前，我们得先知道 “内存模型” 是什么。

内存模型是系统定义的规则，编译器会根据它对读写操作进行**重排序**。处理器根据它对跨线程读写进行重排序。并且内存模型是无法修改。

## 数据格式

内存模型的数据格式被分为两种

1. 强类型：严格模式下，禁止编译器和硬件进行优化。
2. 若类型：允许编译器和处理器自由的重新排列读和写指令。

不同的系统平台会用不同的内存模型，有些比较严格（比如 x86/x64 架构），而 arm 架构比较宽松。但是另一方面，ARM 它不仅允许 JIT 编译器以正确的顺序发出机器指令，还允许使用特殊的指令来确保处理器本身不会以违反 CLR 内存模型的方式来重排序。

不同处理器架构对程序编码也有明显影响，特别是当在线程同步的某些 bug 情况下。因为在 ARM 下，JIT 编译器要比在 x86/x64 下对读写指令的重排序更加自由，所以关于线程同步的 bug 在后者可能无法察觉，但是转到 ARM 下就能看到。

## volatile

关键字 Volatile 就是设置系统编译器是否对这些读写指令进行重新排序，加上 Volatile 关键字就是表明要严格按照顺序执行指令。

除了使用关键字 Volatile 之外，还可以将共享数据放入 lock 关键字内或者是 interlocked 块中。所有的同步方法都会创建一道内存屏障（Memory Barrier）。所有在同步指令之前读取的数据在屏障之后都不允许重新排序。所有在屏障之前写入的数据都不允许重排序。这样数据的更新对所有 CPU 都是可见的。（CLR via C# 中将此过程简称为，易失共享变量要在第一个读，最后一个写）。

## 双检索 —— volatile

先看下面的 “双检索” 的实现

```c#
private bool isComplete = false;
private object asyncObject = new object();

// 以下是错误实现
private void Complete()
{
  if(!isComplete)
  {
    lock (syncObject)
    {
      if (!isComplete) {
        DoCompletionWork();
        isComplete = true;
      }
    }
  }
}
```

由于前面提到的内存模型，系统允许编译器对读写指令重排序，所以 isComplete 变量更新的顺序是不可控的，即某个线程将它设为 true 之后，其他线程仍然会看到 false。更糟糕的是，对 `isComplete = true` 这个写指令有可能被提前到 DoCompletionWork() 之前。

那么如何修正问题呢？只要在 IsComplete 加关键字 volatile 即可。

```c#
private volatile bool isComplete = false;
```

要记住：

**volatile 不是用来提高性能的，而是为了保证正确性的，它不会明显降低或提高性能**