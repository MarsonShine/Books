# 垃圾回收机制

在托管进程中的垃圾回收有两种模式

1. 本机内存堆（Native Heap），由 VirtualAlloc 这个 Windows API 分配的，是 OS 和 CLR 使用的，用于 **非托管代码所需的内存**，如 Windows API，操作系统数据结构以及 CLR 数据等。
2. 托管堆（Managed Heap），这个就是我们最常用的，用于所有 .NET 所有对象的内存分配，也被称为 **GC 堆**，因为其中的对象都要受垃圾回收机制的控制。

## 托管堆

托管堆又分两种

1. 小对象堆
2. 大对象堆，> 85000 字节的都是 LOH（大对象堆）

它们都有各自的内存段，并且都可能有多个内存段。小对象堆一般分为三代，分别是 0，1，2 代。其中 0，1 总是被分配在同一个内存段中的。在初始化阶段，各个阶段的内存分布情况如下图

![](asserts/gc-init.png)

内存地址从左到右是由小变大的。从图中也能看出 Gen 0 与 Gen 1 是分配在同一个内存段的。第 2 代和第 1 代堆只有占开始的一小部分。因为在初始化的时候它们都还是空的。

如果在应用程序运行过程中，不断的在分配对象内存。如果这个对象内存的大小是小于 85000 字节的，那么默认就会被分配到第 0 代，以及**紧挨着当前已用的内存空间往后分配**。因此整个分配过程与速度都是非常快的。但是一旦快速分配失败（一直内部分配机制，前面提到的紧挨着内存后分配的机制），对象就可能在第 0 代的任意位置存储。所以这就必然导致会浪费内存（内存碎片化），**一旦超过第 0 代所属的内存段边界，那么就会触发垃圾回收机制了**。

每次 GC 都会把第 0 代没有被引用的对象回收掉。并且都会把它提升一代。即第 0 代变为第 1 代，第 1 代升为第 2 代。在发生垃圾回收是，会触发 “碎片整理”（也叫内存压缩），是一种移动对象到新的位置，其目的是为了内存对象的连续性，提高访问效率（CPU 缓存）。在几次垃圾回收，并且没有做 “碎片整理” 的内存堆分布图由上图就会变为以下这样

![](asserts/gc-after.png)

对象的位置没有移动过，但是每个代的内存堆的边界发生了改变。

每一代内存发生压缩移动时，消耗的代价很大。因为 GC 要查找所有对象是否已经被回收（没有被引用），找到然后要把她们指向新的位置（终结队列），并还可能会暂停所有托管线程。所以垃圾回收会在 “划算” 时才会进行碎片整理。

> 这里提到的 “划算” 适时的进行垃圾回收，内部采用了类似标记法来临时标记某些对象已被 “释放” ，然后在某个时刻一次性把这些做了标记的对象全部回收并内存压缩。

那么垃圾回收器是如何知道哪些对象有没有被引用呢？

实际上是通过一种树形结构来一层层引用往下找得知的。有一点需要注意：有些对象可能没有受到 GC 根对象的引用，但如果是位于第 2 代内存堆中，那么第 0 代回收是不会清理这些对象的，必须得等到 Full GC 才会被清理。

如果第 0 代堆即将占满一个内存段，垃圾回收也无法通过内存压缩移动内存对齐来获取足够的空间内存，这个时候 GC 就会再分配一个新的内存段，容纳第 1 代和 第 0 代，老的内存段将会变为第 2 代堆。老的第 0 代所有对象都会被放入新的第  1 代堆中，老的 1 代堆同理将提升为第 2 代堆。内存分配图如下

![](asserts/gc-03.png)

## 垃圾回收的阶段

1. 挂起（Suspension）—— 在垃圾回收之前，所有托管线程都要被强制中止。
2. 标记（Mark）—— 从 GC 根节点开始，沿着所有对象引用遍历并标记
3. 碎片整理（Compact）—— 将对象重新紧挨着排放并更新引用，减少内存碎片化。在小对象堆中是按需进行的，无法控制。大对象堆碎片整理不会自动进行。
4. 恢复（Resume）—— 托管线程恢复

在标记阶段，代回收时并不会遍历内存堆中的所有对象，而是只遍历需要回收的部分即可。比如第 0 代回收只涉及第 0 代内存堆中的对象，第 1 代回收涉及第 1 代和第 0 代的内存堆对象。第 2 代回收和完全回收则是需要遍历内存堆中所有存活的对象。所以第 2 代和完全回收的代价非常大。

## 工作站模式

工作站模式（WorkStation），所有的 GC 都发生在触发垃圾回收的线程中，它们的优先级是相同的。这种模式适用于简单应用，交互不复杂的应用的。

## 服务器模式

服务器模式下，GC 会为每个逻辑处理器分配独立的内存堆。每个处理器堆都包含一个小对象堆和一个 LOH。从应用程序角度上看，只有一个逻辑内存堆，所以对象引用会在多个堆之间交叉进行的（这些引用公用相同的虚拟地址空间）。

服务器模式主要有以下好处：

1. 垃圾回收可以并行，每个垃圾回收线程负责回收一个内存堆。这可以明显提高效率
2. 在某些情况下， 内存分配的速度也会快，特被是对 LOH 而言，因为会在所有的内存堆中同时进行。

## 服务器垃圾回收前发出通知机制

1. 调用 GC.RegisterForFullGCNotification，参数是两个阈值，一个是第 2 代内存堆的阈值，一个是 LOH 的阈值。
2. 调用 GC.WaitForFullGCApproach 方法来轮训（Poll）垃圾回收状态，可以一直等待下去或者一个超时值。
3. 如果 WaitForFullGCApproach 方法返回 Success，就将程序转入可接收完全垃圾回收的状态（比如切断发往本机的请求）
4. 调用 GC.Collect 方法手动强制执行一次完全垃圾回收。
5. 调用 GC.WaitForFullGCComplete（仍然可指定一个超时值）等待完全垃圾回收的完成。
6. 重新开启请求
7. 如果不想再收到完全垃圾回收的通知，可以调用 GC.CancelFullGCNotification 方法。