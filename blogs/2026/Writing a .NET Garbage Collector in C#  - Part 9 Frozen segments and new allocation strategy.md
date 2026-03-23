# 用 C# 编写 .NET 垃圾回收器（第 9 部分）：冻结段与新的分配策略

[喜欢这篇文章吗？不妨看看 **Pro .NET Memory Management** 第二版，进一步了解 .NET 垃圾回收器的内部机制！ ![Pro .NET Memory Management](https://minidump.net/images/progc.png)](https://amzn.to/42tg58c)

自己动手写 GC 的一大好处，是能通过亲身踩坑，更好地理解 .NET GC 某些设计决策背后的原因。当然，为了写《Pro .NET Memory Management》第二版，我花了两年时间研究 GC 的源代码，但源码更多教会我“它是怎么做的”，而不是“为什么要这么做”。

冻结段就是这样一个例子。一年前，我写过一篇[文章](https://minidump.net/exploring-frozen-segments/)，解释如何把对象分配到冻结段中。那时我知道，要想允许在冻结段里分配对象，必须调用 `RegisterFrozenSegment` 把这些冻结段注册给 GC。但我并不明白**为什么**必须这样做，也不知道如果你真的不这么做，会发生什么。

正如我当时在那篇文章评论区里回复别人时写的：

[![](https://minidump.net/images/2026-03-17-writing-a-net-gc-in-c-part-9-1.png)](https://minidump.net/images/2026-03-17-writing-a-net-gc-in-c-part-9-1.png)

不过现在我明白了，你很快也会明白。

如果你在继续之前想先回顾一下，不妨先读一遍或重读前面的文章：

*   [第 1 部分：](https://minidump.net/2025-28-01-writing-a-net-gc-in-c-part-1/) 引言与项目搭建
*   [第 2 部分：](https://minidump.net/writing-a-net-gc-in-c-part-2/) 实现一个最小可用的 GC
*   [第 3 部分：](https://minidump.net/writing-a-net-gc-in-c-part-3/) 使用 DAC 检查托管对象
*   [第 4 部分：](https://minidump.net/writing-a-net-gc-in-c-part-4/) 遍历托管堆
*   [第 5 部分：](https://minidump.net/writing-a-net-gc-in-c-part-5/) 解码 GCDesc 以找出托管对象中的引用
*   [第 6 部分：](https://minidump.net/writing-a-net-gc-in-c-part-6/) 实现标记阶段和清扫阶段
*   [第 7 部分：](https://minidump.net/writing-a-net-gc-in-c-part-7/) 标记句柄
*   [第 8 部分：](https://minidump.net/writing-a-net-gc-in-c-part-8/) 内部指针

## NonGC 堆

首先，我们到底在说什么？GC 允许把托管对象分配到一种位于非托管内存中的段里，这种段就叫**冻结段**。所有冻结段合在一起，构成了 NonGC 堆。

它的意义是什么？NonGC 堆的设计目标，是存放那些“永生”的对象：它们不会被回收，也不引用其他对象（或者至少不引用那些并非永生的对象）。在垃圾回收期间扫描这些对象纯属浪费资源，因为它们既不会被收集，也不会让其他对象保持存活。NonGC 堆就是告诉 GC：这些对象可以忽略。

从 .NET 8 开始，JIT 已经知道 NonGC 堆的存在，并利用它做了一些优化。比如，字符串字面量会存放在 NonGC 堆中，这样 JIT 在编译函数时就可以把这些对象的地址直接嵌进汇编代码里，省掉一层间接访问。官方文档里给出了几个例子：[官方文档](https://github.com/dotnet/runtime/blob/main/docs/design/features/NonGC-Heap.md)。

## 冻结段与 GC

GC API 声明了几个 GC 必须实现、用来处理冻结段的函数：`RegisterFrozenSegment`、`UnregisterFrozenSegment`、`UpdateFrozenSegment` 和 `IsInFrozenSegment`。`RegisterFrozenSegment` 和 `UnregisterFrozenSegment` 顾名思义。`UpdateFrozenSegment` 用来告诉 GC：当前冻结段的哪一部分已经填入了对象。`IsInFrozenSegment` 主要由 JIT 调用，用来判断给定对象是否由 GC 管理。

我第一次实现自己的 GC 时，这些方法只是非常基础的一版：

```
partial class GCHeap
{
    private readonly ConcurrentDictionary<nint, StrongBox<(nint start, nint end)>> _frozenSegments = new();
    private int _frozenSegmentIndex;

    public unsafe nint RegisterFrozenSegment(segment_info* pseginfo)
    {
        var handle = Interlocked.Increment(ref _frozenSegmentIndex);
        _frozenSegments.TryAdd(handle, new((pseginfo->pvMem + pseginfo->ibFirstObject, pseginfo->pvMem + pseginfo->ibAllocated)));

        return handle;
    }

    public void UnregisterFrozenSegment(nint seg)
    {
        _frozenSegments.TryRemove(seg, out _);
    }

    public unsafe bool IsInFrozenSegment(GCObject* obj)
    {
        foreach (var segment in _frozenSegments.Values)
        {
            if ((nint)obj >= segment.Value.start && (nint)obj < segment.Value.end)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateFrozenSegment(nint seg, nint allocated, nint committed)
    {
        if (_frozenSegments.TryGetValue(seg, out var segment))
        {
            segment.Value.end = allocated;
        }
    }
}
```

它把冻结段记在一个 `ConcurrentDictionary` 里，仅仅是为了能够回答所有调用 `IsInFrozenSegment` 的地方。除此之外，什么也没做。毕竟，冻结段的意义不就是让 GC 忽略它们吗？当时我一直觉得这件事很奇怪：运行时难道不能自己维护这些信息吗？为什么要把这份记账工作委托给 GC？

然而，后来我发现下面这个简单程序会崩溃：

```
string str = "Hello";

InnerFunction(str);

static void InnerFunction(object obj)
{
    GC.Collect();
    Console.WriteLine(obj is string);
}
```

为什么？

要理解这一点，我们先回顾一下自己的 GC 是怎么工作的。在标记阶段，我们会枚举根对象，并沿着引用图向下遍历，找出所有可达对象。当发现对象时，我们会[通过把方法表指针的最低有效位设为 1](https://minidump.net/writing-a-net-gc-in-c-part-6/) 来给它打标记。之后在清扫阶段，我们会遍历堆，对每个对象执行以下逻辑：

*   如果对象没有被标记，就把它当作空闲空间
*   如果对象已被标记，就清除标记，恢复原始的方法表指针

听起来这套逻辑应该没问题，而我最初测试时也确实如此。不过，我们来看一下前面那个例子里到底发生了什么：

*   `Hello` 是字符串字面量，由运行时分配在冻结段里
*   这个字符串字面量的引用被传给了 `InnerFunction` 方法
*   在 `InnerFunction` 内部，触发了一次垃圾回收
*   在标记阶段，GC 枚举根对象并找到 `obj`。它会给冻结段里的对象打标记，从而修改其方法表指针
*   在清扫阶段，GC 会遍历堆，并给所有仍然存活的对象取消标记。**但是，这个字符串字面量位于托管堆之外，因此 GC 在遍历时根本找不到它**
*   垃圾回收结束，运行时继续执行。`obj is string` 会检查该对象的方法表，而由于方法表指针已经无效，于是触发崩溃

这显然不妙。该怎么修？一种直接的办法是：在清扫阶段结束时，除了遍历托管堆，还额外遍历冻结段，把其中对象的标记去掉：

```
private void UnmarkFrozenSegments()
{
   foreach (var segment in _frozenSegments.Values)
   {
       foreach (var obj in WalkHeapObjects(segment.Value.start, segment.Value.end))
       {
           var o = (GCObject*)obj;
           o->Unmark();
       }
   }
}
```

有了这个简单修复，程序就不再崩溃了。不过，这意味着 GC 也得像处理“普通”托管段一样遍历冻结段，这就抵消了它们存在的一部分意义。从理论上说，冻结段的一个主要好处恰恰是 GC 可以忽略它们。我们能做得更好吗？

如果我们不想在后面再给冻结段里的对象取消标记，那一开始就不应该给它们打标记。我们可以在标记阶段加一个额外判断：

```
private void ScanRoots(GCObject* obj, ScanContext* context, GcCallFlags flags)
{
    // ... (cut for brevity)

    _markStack.Push((IntPtr)obj);

    while (_markStack.Count > 0)
    {
        var ptr = _markStack.Pop();
        var o = (GCObject*)ptr;

        if (o->IsMarked())
        {
            continue;
        }

        // Only mark the object if it's in the managed heap
        if (!IsInFrozenSegment(o))
        {
          o->EnumerateObjectReferences(_markStack.Push);
          o->Mark();
        }
    }
}
```

理论上这能工作，但这个权衡并不好：`IsInFrozenSegment` 需要枚举所有冻结段，才能判断给定地址是否落在其中某一个里。如果对每个被引用对象都调用一次，这个开销太大了。我们可以用区间树把复杂度降到 `O(log n)`，但这依然过高。另一种办法是像[上一篇文章](https://minidump.net/writing-a-net-gc-in-c-part-8/)里那样，用位图在 `O(1)` 时间内判断某个地址是否属于冻结段。但因为冻结段不由 GC 管理，它们可能分配在内存中的任何位置，大小也完全不固定。这样一来，我们的位图就必须覆盖整个地址空间，白白浪费大量内存。

或者，我们可以把问题倒过来想。

## 如果我有无限内存会怎样？

正如前面提到的，要高效判断一个地址是否属于冻结段，其实很难，因为冻结段的分配位置并不受我们控制。不过，托管段分配在哪儿，却完全由我们决定。所以，与其回答“它是不是属于冻结段？”，不如改成回答“它是不是**不**属于托管段？”。

要理解接下来要做什么，我们得先回顾一些内存管理的基础概念。你有没有试过查看一个简单的 .NET 控制台程序究竟会占用多少内存？

```
Console.WriteLine("Hello, World!");

var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

Console.WriteLine($"Private memory: {currentProcess.PrivateMemorySize64 / 1024 / 1024} MB");
Console.WriteLine($"Virtual memory: {currentProcess.VirtualMemorySize64 / 1024 / 1024} MB");
```

[![](https://minidump.net/images/2026-03-17-writing-a-net-gc-in-c-part-9-2.png)](https://minidump.net/images/2026-03-17-writing-a-net-gc-in-c-part-9-2.png)

`6 MB` 的私有内存，对一个简单的 Hello World 来说也许有点多，但对托管语言而言不算离谱。可虚拟内存是……_等等……这有几位数？_ 你是说 _2,297,939 MB_？

[![](https://minidump.net/images/2026-03-17-writing-a-net-gc-in-c-part-9-3.jpg)](https://minidump.net/images/2026-03-17-writing-a-net-gc-in-c-part-9-3.jpg)

当然，这里面有个门道：这几乎全都是**保留**的内存。

你大概已经知道，在现代操作系统上（这里的“现代”指至少 90 年代中期以来的系统），每个进程都会拥有自己的地址空间。如果进程 A 往地址 `0x1000` 写入一个值，进程 B 在自己的 `0x1000` 位置看到的会是完全不同的内容。每个进程拿到的都是**虚拟**内存，操作系统会按需把它映射到真实的物理内存上。这个过程分两步：

*   首先，进程会**保留**一段内存。它是在告诉操作系统：“我打算使用这个范围内的内存。”这时候操作系统其实不会做太多事，只是把这件事记到内部的某种簿记结构里
*   然后，进程会**提交**内存。这时它真的要往这段已保留的内存里写入数据了，所以操作系统必须把这段地址范围映射到实际的物理内存

这也正是我们在控制台程序里看到的情况：它保留了 2 TB 的内存（虚拟内存），但只提交了 6 MB（私有内存）。既然最终只会用到其中一小部分，为什么还要预先保留这么多内存？答案就是：为了精确解决我们前面提到的那个问题。

在 64 位环境下，地址空间大得惊人，与应用真正需要的规模相比，几乎可以视为无限。而“无限减去一大块”，仍然近似于无限。因此，这里的诀窍就是：一开始先保留一大块内存，大到足以覆盖任何一个“正常规模”应用的需求；等真正用到时，再按需提交其中的一部分。这样一来，我们就能确保所有托管内存都落在一个定义明确的地址范围内；反过来说，凡是不在这个范围里的东西，就一定是非托管内存。

有了这个策略，要判断一个对象是否**可能**属于冻结段，就只需要做一次上下界比较。当然，这只在 64 位上成立。32 位地址空间总共只有 4 GB，其中还得留出一部分给原生分配使用。在 .NET 垃圾回收器里，仅仅是“如何保留内存”这一策略的实现代码，就复杂得出人意料。GC 会尽最大努力把所有分配都放进一块连续的保留地址空间里；如果失败了，还准备了很多后备方案；最坏情况下，它会退回到前一部分描述的做法：在垃圾回收结束时遍历冻结段，把对象取消标记。至于我们这个自定义 GC，就不打算处理这些复杂情况了。从这一刻起，它将正式只支持 64 位运行时。

读到这里，你可能会觉得，冻结段似乎是 .NET GC 之所以努力把所有分配都塞进一整块连续保留地址空间中的主要原因。其实并不止如此，背后还有其他几项动机，比如把所有对象限制在一个紧凑的地址范围内，从而缩小 `card table` 的尺寸。之所以我们最先撞上冻结段这个问题，只是因为我恰好选择了这样的实现顺序。

## 新的分配策略

说了这么多，是时候真正实现新的分配策略了。到目前为止，我们的 GC 一直依赖 [`NativeMemory.Alloc`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativememory.alloc?view=net-10.0&WT.mc_id=DT-MVP-5003493)。它的内部实现绑定的是 C 语言的 `malloc` API，因此我们的分配很可能会和其他组件发起的原生分配交错在一起。现在我们要把它替换成自己的 `NativeAllocator`。它的工作方式是：启动时立刻保留 2 TB 内存，然后按需提交其中的一小块来满足分配请求。这两步都调用 Windows 的 [`VirtualAlloc`](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc?WT.mc_id=DT-MVP-5003493) API（Linux 上则是 [`mmap`](https://man7.org/linux/man-pages/man2/mmap.2.html)）。理论上，如果整整 2 TB 都被耗尽了，我们就应该退化到“地址空间碎片化”模式，但这个玩具项目我就不折腾了：_2 TB 对任何人来说都应该够用了_。

在构造函数里，我们会立即保留这块内存，从而得到所有托管内存都必须落入的上下界：

```
internal partial class NativeAllocator : IDisposable
{
    private const uint MEM_COMMIT = 0x00001000;
    private const uint MEM_RESERVE = 0x00002000;
    private const uint MEM_DECOMMIT = 0x00004000;
    private const uint MEM_RELEASE = 0x00008000;

    private const uint PAGE_READWRITE = 0x04;

    public NativeAllocator(long size)
    {
        _lowestAddress = VirtualAlloc(IntPtr.Zero, (UIntPtr)size, MEM_RESERVE, PAGE_READWRITE);

        if (_lowestAddress == IntPtr.Zero)
        {
            throw new OutOfMemoryException("Failed to reserve memory");
        }

        _highestAddress = _lowestAddress + (nint)size;
        _nextFreeAddress = _lowestAddress;
    }

    public nint LowestAddress => _lowestAddress;
    public nint HighestAddress => _highestAddress;

    public void Dispose()
    {
        if (_lowestAddress != IntPtr.Zero)
        {
            VirtualFree(_lowestAddress, UIntPtr.Zero, MEM_RELEASE);
            _lowestAddress = IntPtr.Zero;
            _highestAddress = IntPtr.Zero;
        }
    }
}
```

分配代码要稍微复杂一些，原因有两个：它必须是线程安全的，而且我们只能按操作系统页大小的整数倍来提交内存：

```
private static readonly int PageSize = Environment.SystemPageSize;

public nint Allocate(nint size)
{
    if (size <= 0)
    {
        return IntPtr.Zero;
    }

    var alignedSize = (size + (PageSize - 1)) & ~(nint)(PageSize - 1);
    nint address;

    while (true)
    {
        address = Volatile.Read(ref _nextFreeAddress);
        var end = address + alignedSize;

        if (end > HighestAddress)
        {
            throw new OutOfMemoryException("Not enough memory to allocate");
        }

        if (Interlocked.CompareExchange(ref _nextFreeAddress, end, address) == address)
        {
            break;
        }
    }

    var result = VirtualAlloc(address, (UIntPtr)alignedSize, MEM_COMMIT, PAGE_READWRITE);

    if (result == IntPtr.Zero)
    {
        throw new OutOfMemoryException("VirtualAlloc failed to commit memory");
    }

    return address;
}
```

释放内存就简单多了（至少目前如此）：

```
public void Free(nint address, nint size)
{
    if (address == IntPtr.Zero || _lowestAddress == IntPtr.Zero)
    {
        return;
    }

    if (address < _lowestAddress || address >= _highestAddress)
    {
        throw new InvalidOperationException($"Address {address:x2} is out of reserved range");
    }

    var alignedSize = (size + (PageSize - 1)) & ~(nint)(PageSize - 1);

    if (!VirtualFree(address, (UIntPtr)alignedSize, MEM_DECOMMIT))
    {
        throw new InvalidOperationException("VirtualFree failed to decommit memory");
    }
}
```

就是这样！现在我们可以公开一个 API，用来判断某个地址是否落在 `NativeAllocator` 持有的范围内：

```
public bool IsInRange(nint ptr) => ptr >= LowestAddress && ptr < HighestAddress;
```

然后直接把它接入标记阶段。它足够便宜，可以对每个对象都调用：

```
private void ScanRoots(GCObject* obj, ScanContext* context, GcCallFlags flags)
{
    // ... (cut for brevity)

    _markStack.Push((IntPtr)obj);

    while (_markStack.Count > 0)
    {
        var ptr = _markStack.Pop();
        var o = (GCObject*)ptr;

        if (o->IsMarked())
        {
            continue;
        }

        // Only mark the object if it's in the managed heap
        if (_nativeAllocator.IsInRange((nint)o))
        {
          o->EnumerateObjectReferences(_markStack.Push);
          o->Mark();
        }
    }
}
```

有了这个改动，我们就不必在垃圾回收结束时再去遍历冻结段、给其中对象取消标记了，因为这些对象从一开始就不会被标记。

当然，目前这个分配器还非常简单，因为我们的 GC 还不会重用已经释放的地址区间。等到未来开始回收并重用空间时，就必须换成一个功能完整的分配器：它要有空闲链表来跟踪空闲内存块，还要有按大小分桶的机制来避免碎片化。说白了，本质上就是重新实现一遍 `malloc`。

和往常一样，完整代码可以在 [GitHub](https://github.com/kevingosse/ManagedDotnetGC/) 上找到。

[喜欢这篇文章吗？不妨看看 **Pro .NET Memory Management** 第二版，进一步了解 .NET 垃圾回收器的内部机制！ ![Pro .NET Memory Management](https://minidump.net/images/progc.png)](https://amzn.to/42tg58c)

## 作者信息
Kevin Gosse

## 原文链接
https://minidump.net/writing-a-net-gc-in-c-part-9/
