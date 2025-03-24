这已经是我们用 C# 编写 .NET 垃圾收集器之旅的第四部分。这一次，我们将学习如何行走托管堆，这是实现垃圾收集器标记阶段的先决条件。如果您还没有阅读过这些内容，或者需要复习一下，可以在这里找到之前的部分：

- 第 1 部分：简介和项目设置
- 第 2 部分：实现最小 GC
- 第 3 部分：使用 DAC 检查托管对象

## 漫步堆

那么它是关于什么的呢？我们正在实施跟踪式垃圾回收器，这意味着我们将跟踪引用链来确定对象的可达性（而不是其他形式的自动内存管理，如引用计数）。遍历引用树将告诉我们哪些对象仍在使用中，并通过排除法告诉我们哪些对象不在使用中。我们可以安全地收集这些对象。但要 “排除 ”对象，我们需要了解所有对象，这就是为什么我们需要能够遍历堆。

这不是我第一次走查托管堆，我强烈建议你阅读这篇文章，作为本文的补充。

遍历堆的诀窍在于认识到对象正在形成一种链表，至少只要它们是连续的（稍后会有详细介绍）。从一个对象开始，我们可以通过计算它的大小找到下一个对象，然后将其添加到当前地址，如此反复，直到堆的尽头。

从大小的角度来看，有两种对象：固定大小的对象和可变大小的对象。绝大多数对象都是固定大小的，我们只需读取其方法表中的 `BaseSize` 字段，就能得到它们的大小。数组和字符串的长度是可变的，因此计算它们的大小要复杂一些。我们必须读取它们的长度（位于方法表指针之后），然后乘以存储在方法表 `ComponentSize` 字段中的每个元素的大小。结果必须与基本大小相加，才能得到对象的总大小。

有了这些信息，我们就可以编写一个 `ComputeSize` 方法，该方法接收指向托管对象的指针并返回其大小。我们将在本文稍后部分使用该方法：

```c++
private static unsafe uint ComputeSize(GCObject* obj)
{
    var methodTable = obj->MethodTable;

    if (!methodTable->HasComponentSize)
    {
        // Fixed-size object
        return methodTable->BaseSize;
    }

    // Variable-size object
    return methodTable->BaseSize + obj->Length * methodTable->ComponentSize;
}
```

`GCObject` 结构已经在[第二部分](writing-a-net-gc-in-c-part-2.md)已经定于：

```c#
[StructLayout(LayoutKind.Sequential)]
public unsafe struct GCObject
{
    public MethodTable* MethodTable;
    public uint Length;
}
```

对于 `MethodTable` 结构体，我们可以直接复制 [BCL 中某些方法所使用的那个](https://source.dot.net/#System.Private.CoreLib/src/System/Runtime/CompilerServices/RuntimeHelpers.CoreCLR.cs,3a48c25c1e3ec333)，这样能为我们节省不少时间。

现在我们可以编写一个方法来遍历内存的一部分，同样假设对象是连续存储的。我们使用[第 3 部分](writing-a-net-gc-in-c-part-3.md)中的 `DacManager` 来获取对象的名称：

```c#
private void TraverseHeap(nint start, nint end)
{
    var ptr = start + IntPtr.Size;

    while (ptr < end)
    {
        var obj = (GCObject*)ptr;
        var name = _dacManager?.GetObjectName(new(ptr));

        Write($"{ptr:x2} - {name}");

        var alignment = sizeof(nint) - 1;
        ptr += ((nint)ComputeSize(obj) + alignment) & ~alignment;
    }
}
```

这里需要注意两点。首先，正如过去多次提到的那样，对托管对象的引用并不指向对象的实际起始位置。我们需要将 `IntPtr.Size` 加到起始位置，以跳过头部并找到方法表指针。头部的大小包含在对象的基本大小中，因此循环的下一次迭代将指向对象的正确部分。其次，对象在指针边界上对齐，所以我们必须在处理每个对象后确保对齐正确。

既然我们已经知道如何遍历一段内存，接下来我们还得弄清楚可能包含对象的所有内存范围。

## 管理分配上下文

目前，我们的垃圾回收器有一个非常简单的分配策略：每当一个线程请求新的分配上下文时，我们都会使用 `NativeMemory.AllocZeroed` 分配一个新的 32KB 内存块（这实际上等同于 C 语言中的 `calloc` 调用）。我们需要跟踪这些内存块以便能够遍历堆。这可以通过一个简单的线程安全列表来实现：

```c#
private ConcurrentQueue<(IntPtr start, IntPtr end)> _allocationContexts = new();
```

`Alloc` 方法中，我们添加新的上下文至集合中：

```c#
    public GCObject* Alloc(ref gc_alloc_context acontext, nint size, GC_ALLOC_FLAGS flags)
    {
        var result = acontext.alloc_ptr;
        var advance = result + size;

        if (advance <= acontext.alloc_limit)
        {
            acontext.alloc_ptr = advance;
            return (GCObject*)result;
        }

        var growthSize = Math.Max(size, 32 * 1024) + IntPtr.Size;
        var newPages = (IntPtr)NativeMemory.AllocZeroed((nuint)growthSize);

        // NEW: we now keep track of the allocation context
        _allocationContexts.Enqueue((newPages, newPages + growthSize));

        var allocationStart = newPages + IntPtr.Size;
        acontext.alloc_ptr = allocationStart + size;
        acontext.alloc_limit = newPages + growthSize;

        return (GCObject*)allocationStart;
    }
```

现在我们可以编写一个遍历整个堆的方法。它会枚举所有分配上下文，并调用我们之前编写的 `TraverseHeap(nint start, nint end)` 方法：

```c#
private void TraverseHeap()
{
    foreach (var (start, end) in _allocationContexts)
    {
    	TraverseHeap(start, end);
    }
}
```

我们还需要对 `TraverseHeap(nint start, nint end)` 方法做一个小的补充：因为分配上下文可能还没满，我们需要在发现空方法表指针时中断循环，空方法表指针表示分配上下文已用部分的结束：

```c#
private void TraverseHeap(nint start, nint end)
{
    var ptr = start + IntPtr.Size;

    while (ptr < end)
    {
        var obj = (GCObject*)ptr;

        if (obj->MethodTable == null)
        {
            // We reached the end of used part of the allocation context
            break;
        }

        var name = _dacManager?.GetObjectName(new(ptr));

        Write($"{ptr:x2} - {name}");

        var alignment = sizeof(nint) - 1;
        ptr += ((nint)ComputeSize(obj) + alignment) & ~alignment;
    }
}
```

最后一步是将堆遍历逻辑插入 `GarbageCollect` 方法。在遍历堆之前，我们先暂停托管线程，然后再恢复它们。严格来说，这并不是必须的，因为我们并没有移动或释放对象，但我们肯定会在将来的某个时候这样做：

```c#
    public HResult GarbageCollect(int generation, bool low_memory_p, int mode)
    {
        Write("GarbageCollect");

        _gcToClr.SuspendEE(SUSPEND_REASON.SUSPEND_FOR_GC);

        _gcHandleManager.Store.DumpHandles(_dacManager);
        TraverseHeap();

        _gcToClr.RestartEE(finishedGC: true);

        return HResult.S_OK;
    }
```

如果我们在测试程序中进行尝试，就能看到堆的内容与预期相符：

![](https://minidump.net/images/2025-02-18-writing-a-net-gc-in-c-part-4-1.png)

这种分配策略在简单的应用中肯定行得通，如果能对它进行基准测试，看看它与其他解决方案相比如何，那将会非常有趣。归根结底，垃圾回收器必须在多个因素之间做出妥协，我们已经介绍了其中几个因素。第一个因素是分配上下文的大小。每个线程都有自己的分配上下文，如果分配的上下文越大，就意味着线程可以自行执行更多分配，从而减少 CPU 开销。另一方面，如果分配上下文较小，我们就可以通过工作集更准确地反映实际需要多少内存，从而减少应用程序的内存使用量。

.NET GC 采用了一种策略来平衡这两方面的考虑：分配上下文较小（8KB），同时降低分配新上下文的成本。**具体做法是预先分配一大块内存，然后根据需要将其中的一部分分配到分配上下文中。这些内存块被称为 “段”**。

## 切换到分段

为了准确理解分段是如何工作的，我们将在 GC 中实现它们。首先要做的是实际声明分段。我们使用一个简单的类来跟踪它们的边界：

```c#
public unsafe class Segment
{
    public IntPtr Start;
    public IntPtr Current;
    public IntPtr End;

    public Segment(nint size)
    {
        Start = (IntPtr)NativeMemory.AllocZeroed((nuint)size);
        Current = Start;
        End = Start + size;
    }
}
```

实际上，.NET GC 并不像我们那样一次性分配所有内存。它会保留内存块，然后根据需要逐步提交。但在我们的简单实现中，我们并不打算这样做。然后，我们添加几个字段来跟踪分段，并在初始化时分配第一个字段：

```c#
    private const int AllocationContextSize = 32 * 1024;
    private const int SegmentSize = AllocationContextSize * 128;

    private List<Segment> _segments = new();
    private Segment _activeSegment;

    public HResult Initialize()
    {
        // ...

        _activeSegment = new(SegmentSize);
        _segments.Add(_activeSegment);
    }
```

分段的优点之一是无需跟踪分配上下文，因此我们可以删除之前添加的 `_allocationContexts` 集合。相反，我们可以直接走段。不过，这也有一个问题。我们的走堆策略基于这样一个假设，即对象是连续存储的，它们之间没有任何间隙。如果分配上下文指向一个共同的段，我们就打破了这一不变性。

![](https://minidump.net/images/2025-02-18-writing-a-net-gc-in-c-part-4-2.png)

一种解决方案是更新我们的堆遍历逻辑，逐个字节扫描空隙，直到找到下一个对象。这种方法可行（只要我们保持空闲空间为零），但效率也非常低。取而代之的是，我们将在垃圾回收开始时添加一个步骤，将段恢复到可遍历状态。为此，我们将在每个分配上下文的已用部分末尾分配一个虚拟的可变大小对象，以指示间隙的大小。我们的 `TraverseHeap` 方法会将其视为普通对象，并直接跳过它。

![](https://minidump.net/images/2025-02-18-writing-a-net-gc-in-c-part-4-3.png)

现在我们有了一个策略，可以更新 `Alloc` 方法以使用分段。逻辑会比以前复杂得多。有 4 种情况需要考虑：

- 分配上下文中仍有足够的空间：在这种情况下，什么都不会改变，我们像以前一样分配对象，并撞击 `alloc_ptr`。
- 分配上下文中没有足够空间，但当前分段中有足够空间：我们为线程分配一个新的分配上下文。如果可能，我们会分配一个完全大小的分配上下文，否则我们会分配剩余的空间。
- 分配上下文中没有足够空间，且当前分段中也没有足够空间：我们分配一个新的分段，然后处理前一种情况。
- 对象太大，无法容纳在一个分段中：我们分配一个新的分段，其大小不符合标准，只够存储该对象，同时给线程一个空的分配上下文（以便线程下次请求新的分配上下文）。

此外，我们还必须确保分配上下文有足够的空间来存储虚拟对象。在 .NET 中，对象的最小大小为 3 个指针，因此在 64 位模式下为 24 字节，这对于虚拟对象来说也是一样的。假设我们分配了 128 字节的分配上下文，并在其中存储了一个 120 字节的对象：我们将没有多余的空间来存储虚拟对象，最终会出现 8 字节的空隙，无法填补。因此，我们要确保分配上下文至少比要分配的对象大 3 个指针，并减少 `alloc_limit` 以防止线程使用额外的空间。

将所有这些因素考虑到 `Alloc` 方法中，我们可以得到：

```c#
    private static int SizeOfObject = sizeof(nint) * 3;

    public GCObject* Alloc(ref gc_alloc_context acontext, nint size, GC_ALLOC_FLAGS flags)
    {
        var result = acontext.alloc_ptr;
        var advance = result + size;

        if (advance <= acontext.alloc_limit)
        {
            // There is enough room left in the allocation context
            acontext.alloc_ptr = advance;
            return (GCObject*)result;
        }

        // We need to allocate a new allocation context
        var minimumSize = size + SizeOfObject;

        if (minimumSize > SegmentSize)
        {
            // We need a dedicated segment for this allocation
            var segment = new Segment(size);
            segment.Current = segment.End;

            lock (_segments)
            {
                _segments.Add(segment);
            }

            acontext.alloc_ptr = 0;
            acontext.alloc_limit = 0;

            return (GCObject*)(segment.Start + IntPtr.Size);
        }

        lock (_segments)
        {
            if (_activeSegment.Current + minimumSize >= _activeSegment.End)
            {
                // The active segment is full, allocate a new one
                _activeSegment = new Segment(SegmentSize);
                _segments.Add(_activeSegment);
            }

            var desiredSize = Math.Min(Math.Max(minimumSize, AllocationContextSize), _activeSegment.End - _activeSegment.Current);

            result = _activeSegment.Current + IntPtr.Size;
            _activeSegment.Current += desiredSize;

            acontext.alloc_ptr = result + size;
            acontext.alloc_limit = _activeSegment.Current - IntPtr.Size * 2;

            return (GCObject*)result;
        }
    }
```

请注意，我们只通过 `IntPtr.Size * 2` 来减少 `alloc_limit`。我们不需要 3，因为如[第二部分](writing-a-net-gc-in-c-part-2.md)所述，分配已经被 `IntPtr.Size` 移位了。

另外，请注意使用锁定来保持段的状态一致。减少锁定争用的一个好办法是拥有多个堆，每个堆都有自己的分段，并将每个线程亲和到给定的堆上。在服务器模式下，.NET GC 采用的就是这种策略，我们可能会在将来进行尝试。

为虚拟对象留出空间固然很好，但我们仍然必须在某些时候对其进行实际分配。每当分配上下文被丢弃时，我们都要这样做：

- 在 `Alloc` 方法中，当我们为线程分配一个新的分配上下文时。
- 当线程死亡时，在这种情况下，它将调用 `IGCHeap.FixAllocContext` 方法，给我们一个清理的机会。

虚拟对象的方法表由执行引擎提供。我们在构造函数中使用 `IGCToCLR.GetFreeObjectMethodTable` 方法获取它：

```c#
    private MethodTable* _freeObjectMethodTable;

    public GCHeap(IGCToCLRInvoker gcToClr)
    {
        _freeObjectMethodTable = (MethodTable*)gcToClr.GetFreeObjectMethodTable();
        
        // ...
    }
```

> 虚拟对象的 “正式 ”名称是 “Free object（自由对象）”，因此该方法也叫 “自由对象”。

我们执行 `FixAllocContext` 方法来分配虚拟对象，它将填充分配上下文中未使用的空间：

```c#
    public unsafe void FixAllocContext(gc_alloc_context* acontext, void* arg, void* heap)
    {
        FixAllocContext(ref Unsafe.AsRef<gc_alloc_context>(acontext));
    }

    private void FixAllocContext(ref gc_alloc_context acontext)
    {
        if (acontext.alloc_ptr == 0)
        {
            return;
        }

        AllocateFreeObject(acontext.alloc_ptr, (uint)(acontext.alloc_limit - acontext.alloc_ptr));
    }

    private void AllocateFreeObject(nint address, uint length)
    {
        var freeObject = (GCObject*)address;
        freeObject->MethodTable = _freeObjectMethodTable;
        freeObject->Length = length;
    }
```

我们不会忘记从 `Alloc` 调用它：

```c#
    private static int SizeOfObject = sizeof(nint) * 3;

    public GCObject* Alloc(ref gc_alloc_context acontext, nint size, GC_ALLOC_FLAGS flags)
    {
        var result = acontext.alloc_ptr;
        var advance = result + size;

        if (advance <= acontext.alloc_limit)
        {
            // There is enough room left in the allocation context
            acontext.alloc_ptr = advance;
            return (GCObject*)result;
        }

        // We need to allocate a new allocation context
        FixAllocContext(ref acontext);

        var minimumSize = size + SizeOfObject;

        // ...
    }
```

我们就快成功了。你可能已经注意到一个问题：我们使用 `FixAllocContext` 分配了废弃分配上下文中的虚拟对象，但垃圾回收时仍在使用的分配上下文怎么办？诀窍在于使用 `IGCToCLR.GcEnumAllocContexts` 枚举所有分配上下文，然后在每个上下文上调用 `FixAllocContext`：

```c#
    public HResult GarbageCollect(int generation, bool low_memory_p, int mode)
    {
        Write("GarbageCollect");

        _gcToClr.SuspendEE(SUSPEND_REASON.SUSPEND_FOR_GC);

        _gcHandleManager.Store.DumpHandles(_dacManager);

        var callback = (delegate* unmanaged<gc_alloc_context*, IntPtr, void>)&EnumAllocContextCallback;
        _gcToClr.GcEnumAllocContexts((IntPtr)callback, GCHandle.ToIntPtr(_handle));

        TraverseHeap();

        _gcToClr.RestartEE(finishedGC: true);

        return HResult.S_OK;
    }

    [UnmanagedCallersOnly]
    private static void EnumAllocContextCallback(gc_alloc_context* acontext, IntPtr arg)
    {
        var handle = GCHandle.FromIntPtr(arg);
        var gcHeap = (GCHeap)handle.Target!;
        gcHeap.FixAllocContext(ref Unsafe.AsRef<gc_alloc_context>(acontext));
    }
```

`GcEnumAllocContexts` 期望为每个分配上下文调用一个回调。由于将从本地代码中调用，回调必须是静态的，并用 `[UnmanagedCallersOnly]` 修饰。为了从静态方法中获取 `GCHeap` 类的实例，我们使用一个 `GCHandle` 作为参数传递给回调。`GCHandle` 在构造函数中创建，并将在应用程序的整个生命周期中重复使用。

```c#
    private GCHandle _handle;

    public GCHeap(IGCToCLRInvoker gcToClr)
    {
        _handle = GCHandle.Alloc(this);
        
        // ...
    }
```

最后，我们更新了 `TraverseHeap` 方法： 现在，`TraverseHeap()` 需要枚举段而不是分配上下文，`TraverseHeap(nint start, nint end)` 不再需要检查空值（因为整个段已被虚拟对象填满）。在我们的调试代码中，遇到虚对象时会显示 “Free”（就像在 WinDbg 中看到的那样）。

```c#
    private void TraverseHeap()
    {
        foreach (var segment in _segments)
        {
            TraverseHeap(segment.Start, segment.Current);
        }
    }

    private void TraverseHeap(nint start, nint end)
    {
        var ptr = start + IntPtr.Size;

        while (ptr < end)
        {
            var obj = (GCObject*)ptr;

            var name = obj->MethodTable == _freeObjectMethodTable
                ? "Free"
                : _dacManager?.GetObjectName(new(ptr));

            Write($"{ptr:x2} - {name}");

            var alignment = sizeof(nint) - 1;
            ptr += ((nint)ComputeSize(obj) + alignment) & ~alignment;
        }
    }
```

如果我们运行应用程序，就会发现堆已被正确遍历，虚对象填补了空白：

![](https://minidump.net/images/2025-02-18-writing-a-net-gc-in-c-part-4-4.png)

## 结论

我们现在有了一个可以正常工作的垃圾回收器，它使用段来管理分配上下文。我们模仿了 .NET GC 使用虚拟 “空闲 ”对象来保持分段处于可行走状态的方式。现在，我们已经能够列出所有对象，下一步将是找出哪些对象仍然可用。