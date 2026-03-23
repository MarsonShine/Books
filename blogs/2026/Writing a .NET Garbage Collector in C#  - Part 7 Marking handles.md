# 用 C# 编写 .NET 垃圾回收器（第 7 部分）：标记句柄

[喜欢这篇文章吗？不妨看看 **《Pro .NET Memory Management》第二版**，进一步了解 .NET 垃圾回收器的内部机制！ ![Pro .NET Memory Management](https://minidump.net/images/progc.png)](https://amzn.to/42tg58c)

在本系列的上一篇文章里，我们已经开始实现垃圾回收器的标记阶段（mark phase）和清扫阶段（sweep phase）。我当时提到，根对象会分成三个桶：局部变量与线程静态存储、GC 句柄，以及终结队列。到目前为止，我们只扫描了第一个桶。接下来，就该轮到 GC 句柄了。

如果你需要先回顾一下上下文，可以先跳回前面的几篇文章：

* [第 1 部分：](https://minidump.net/2025-28-01-writing-a-net-gc-in-c-part-1/) 引言与项目搭建
* [第 2 部分：](https://minidump.net/writing-a-net-gc-in-c-part-2/) 实现一个最小可用的 GC
* [第 3 部分：](https://minidump.net/writing-a-net-gc-in-c-part-3/) 使用 DAC 检查托管对象
* [第 4 部分：](https://minidump.net/writing-a-net-gc-in-c-part-4/) 遍历托管堆
* [第 5 部分：](https://minidump.net/writing-a-net-gc-in-c-part-5/) 解析 GCDesc 以找出托管对象的引用
* [第 6 部分：](https://minidump.net/writing-a-net-gc-in-c-part-6/) 实现标记与清扫阶段

回到第 2 部分，当时我们用的是一个简单的定长数组来保存句柄，并且提到过迟早要回头重做。现在为了做标记，我们需要按类型取回句柄，所以正好是时候实现一套更像样的存储结构了。

## 重构句柄存储

我们的句柄存储需要满足几个要求：

1. 能够高效地按句柄类型取回句柄
2. 一旦分配，句柄的地址就绝不能再变化。我们当然可以用固定数组（pinned array），但我更倾向于直接用原生内存来强制保证这一点
3. 必须能容纳任意数量的句柄
4. 新句柄的分配必须是线程安全的

第一个要求很好满足：为每一种句柄类型分别建立一套独立的存储即可。至于后面两个要求，我们基本没得选：既然不能对存储区做重新分配（否则句柄在内存中的位置就会变化），那就只能采用基于“段”的方案——需要更多空间时，就追加新的段。

```csharp
public unsafe class GCHandleStore
{
    private readonly HandleSegmentList[] _lists;

    public GCHandleStore()
    {
        // ...

        // Allocate one list of segments per type of handle
        _lists = new HandleSegmentList[HandleTypeCount];

        for (int i = 0; i < HandleTypeCount; i++)
        {
            _lists[i] = new HandleSegmentList();
        }
    }

    ...
}
```

`HandleSegmentList` 负责管理一个由多个 `HandleSegment` 组成的链表。由于句柄可能在任意时刻被释放，我们需要一条空闲链表（freelist），以便快速找到段中的空位。这里我们直接把空闲链表的信息塞进句柄本身里。为了方便回忆，下面是句柄的结构：

```csharp
[StructLayout(LayoutKind.Sequential)]
public unsafe ref struct ObjectHandle
{
    public GCObject* Object;
    public nint ExtraInfo;
    public HandleType Type;

    public void Clear()
    {
        Object = null;
        ExtraInfo = 0;
    }
}
```

这个结构给了我们足够大的可操作空间。我们额外引入一个占位用的句柄类型 `HandleType.HNDTYPE_FREE`，并约定：只要某个句柄的类型是 Free，那么它的 `ExtraInfo` 字段就保存该段中下一个空闲槽位的索引（如果存在的话）。

```csharp
public unsafe class HandleSegment
{
    private readonly ObjectHandle* _buffer;
    private readonly int _capacity;
    private long _freeHead; // index of the first free slot, or -1

    public HandleSegment? Next;

    public HandleSegment(int capacity)
    {
        _capacity = capacity;
        _buffer = (ObjectHandle*)NativeMemory.AllocZeroed((nuint)capacity, (nuint)sizeof(ObjectHandle));

        // Build the freelist: each free slot's Type is HNDTYPE_FREE,
        // and ExtraInfo holds the index of the next free slot.
        for (int i = 0; i < capacity; i++)
        {
            _buffer[i].Type = HandleType.HNDTYPE_FREE;
            _buffer[i].ExtraInfo = i + 1;
        }

        // -1 indicates that there is no free slot after this one
        _buffer[capacity - 1].ExtraInfo = -1;
        _freeHead = 0;
    }
}
```

接着我们实现一个 `TryAllocate` 方法，用来尝试从段里拿到一个空槽位。由于它可能会被多个线程同时调用，因此必须保证线程安全：

```csharp
    /// <summary>
    /// Try to allocate a slot from this segment.
    /// Returns null if the segment is full.
    /// </summary>
    public ObjectHandle* TryAllocate()
    {
        while (true)
        {
            var head = Volatile.Read(ref _freeHead);

            if (head == -1)
            {
                return null;
            }

            var slot = _buffer + head;
            var next = (int)slot->ExtraInfo;

            if (Interlocked.CompareExchange(ref _freeHead, next, head) == head)
            {
                slot->Clear();
                return slot;
            }
        }
    }
```

这段代码的核心逻辑很直接：我们不断用原子 CAS（compare-and-swap）操作尝试替换空闲链表的头节点，直到成功为止；或者 `_freeHead` 变成 `-1`，说明最后一个空槽也已经被别人抢走了。

不过，如果把下面这个场景代入进去，就会发现这里其实存在一个微妙的竞态条件：

* 线程 T1 读到 `_freeHead = 5`，同时读到 `slot5->ExtraInfo = 6`
* 线程 T2 先弹出 5（此时头节点变成 6），再弹出 6（头节点变成 7），然后把 5 再压回去（头节点重新变成 5，但这时 `slot5->ExtraInfo = 7`）
* T1 的 CAS 成功了（因为头节点又“看起来”是 5），于是它把 `_freeHead` 设置为旧值 6，结果把节点 7 给丢了，整条链表也被破坏了

这就是著名的 [ABA 问题](https://en.wikipedia.org/wiki/ABA_problem)。常见的修复办法，是把一个“标签”（tag）和索引一起打包保存。这样就算头节点索引看起来没变，也能识别出它其实已经被弹出又放回过一次：

```csharp
    private static long Pack(int index, int tag) => ((long)tag << 32) | (uint)index;
    private static (int Index, int Tag) Unpack(long packed) => ((int)(uint)packed, (int)(packed >> 32));

    public ObjectHandle* TryAllocate()
    {
        while (true)
        {
            var packed = Volatile.Read(ref _freeHead);
            var (head, tag) = Unpack(packed);

            if (head == -1)
            {
                return null;
            }

            var slot = _buffer + head;
            var next = (int)slot->ExtraInfo;
            var newPacked = Pack(next, tag + 1);

            if (Interlocked.CompareExchange(ref _freeHead, newPacked, packed) == packed)
            {
                slot->Clear();
                return slot;
            }
        }
    }
```

`Free` 方法的思路与此类似：不断尝试把我们手里的句柄压回空闲链表头，直到成功为止：

```csharp
    /// <summary>
    /// Return a slot to the freelist.
    /// </summary>
    public void Free(ObjectHandle* handle)
    {
        handle->Object = null;
        handle->Type = HandleType.HNDTYPE_FREE;
        var index = (int)(handle - _buffer);

        while (true)
        {
            var packed = Volatile.Read(ref _freeHead);
            var (head, tag) = Unpack(packed);
            handle->ExtraInfo = head;
            var newPacked = Pack(index, tag + 1);

            if (Interlocked.CompareExchange(ref _freeHead, newPacked, packed) == packed)
            {
                return;
            }
        }
    }
```

回到 `HandleSegmentList`。这里我们保存了 `HandleSegment` 链表的头结点和尾结点：

```csharp
public unsafe class HandleSegmentList
{
    private const int SegmentCapacity = 256;

    private readonly HandleSegment _head;
    private HandleSegment _tail;
    private readonly Lock _growLock = new();

    public HandleSegmentList()
    {
        _head = new HandleSegment(SegmentCapacity);
        _tail = _head;
    }
}
```

`HandleSegmentList` 的分配逻辑会稍微复杂一点。首先，我们优先尝试在尾段中分配，因为这是最有可能成功的快路径：

```csharp
    public ObjectHandle* Allocate()
    {
        // Fast path: try the tail segment (most common case)
        var tail = Volatile.Read(ref _tail);
        var result = tail.TryAllocate();

        if (result != null)
        {
            return result;
        }
    }
```

如果尾段已经满了，那么我们就遍历整条链表去找空槽。也许还有更快的办法，不过眼下我们先假设段链表不会大到离谱：

```csharp
            // Scan all segments to find a free slot
            var segment = _head;

            while (segment != null)
            {
                result = segment.TryAllocate();

                if (result != null)
                {
                    return result;
                }

                segment = segment.Next;
            }
```

要是仍然找不到空位，那就只能新建一个段了。理论上这部分也可以做成无锁，但那样我们就必须先乐观地分配一块内存，万一竞争失败还得把它丢掉。在没有基准测试支撑之前，我倾向于认为这很可能得不偿失，所以这里直接上锁。

```csharp
            // Slow path: all segments are full, take a lock to grow
            lock (_growLock)
            {
                // If tail changed, another thread already grew the list
                if (tail != Volatile.Read(ref _tail))
                {
                    continue;
                }

                // Still the same tail, append a new segment
                var newSegment = new HandleSegment(SegmentCapacity);

                // Allocate the handle *before* publishing the segment
                // Otherwise there is a small risk that the segment gets full before we allocate
                var handle = newSegment.TryAllocate()!;

                Volatile.Write(ref tail.Next, newSegment);
                Volatile.Write(ref _tail, newSegment);

                return handle;
            }
```

整套逻辑被包在一个 `while` 循环里，是为了处理一种情况：我们在等待 `_growLock` 的过程中输给了别的线程。

```csharp
    /// <summary>
    /// Allocate a new handle in this list. Grows by adding a new segment if needed.
    /// </summary>
    public ObjectHandle* Allocate()
    {
        while (true)
        {
            // Fast path: try the tail segment (most common case)
            var tail = Volatile.Read(ref _tail);
            var result = tail.TryAllocate();

            if (result != null)
            {
                return result;
            }

            // Scan all segments to find a free slot
            var segment = _head;

            while (segment != null)
            {
                result = segment.TryAllocate();

                if (result != null)
                {
                    return result;
                }

                segment = segment.Next;
            }

            // Slow path: all segments are full, take a lock to grow
            lock (_growLock)
            {
                // If tail changed, another thread already grew the list
                if (tail != Volatile.Read(ref _tail))
                {
                    continue;
                }

                // Still the same tail, append a new segment
                var newSegment = new HandleSegment(SegmentCapacity);

                // Allocate the handle *before* publishing the segment
                // Otherwise there is a small risk that the segment gets full before we allocate
                var handle = newSegment.TryAllocate()!;

                Volatile.Write(ref tail.Next, newSegment);
                Volatile.Write(ref _tail, newSegment);

                return handle;
            }
        }
    }
```

`Free` 方法就简单多了。因为我们从来不会把段从链表里移除，所以只需要逐段检查，找到句柄归属的那个段即可：

```csharp
    /// <summary>
    /// Free a handle that belongs to one of this list's segments.
    /// </summary>
    public void Free(ObjectHandle* handle)
    {
        var segment = FindSegment(handle);
        segment?.Free(handle);
    }

    public HandleSegment? FindSegment(ObjectHandle* handle)
    {
        var segment = _head;

        while (segment != null)
        {
            if (segment.ContainsHandle(handle))
            {
                return segment;
            }

            segment = segment.Next;
        }

        return null;
    }
```

而 `HandleSegment.ContainsHandle` 做的事情也很单纯：检查给定句柄地址是否落在本段对应的那块原生内存范围内。

```csharp
    public bool ContainsHandle(ObjectHandle* handle)
    {
        return handle >= _buffer && handle < _buffer + _capacity;
    }
```

最后，我们还要在 `GCHandleStore` 上实现一个 `EnumerateHandlesOfType` 方法，让它能够从对应的存储里枚举某些类型的所有已分配句柄。出于篇幅考虑，这里我把枚举器的实现省略掉了；如果你感兴趣，可以直接去看 [GitHub 仓库里的代码](https://github.com/kevingosse/ManagedDotnetGC/blob/6b9bbe09ea923b4fe18ce926afee7b04f0538c82/ManagedDotnetGC/GCHandleStore.cs#L130)。

```csharp
    public HandlesEnumerable EnumerateHandlesOfType(ReadOnlySpan<HandleType> handleTypes) => new(this, handleTypes);
```

## 扫描句柄

很好，现在我们终于可以回到标记阶段本身了。在[第 6 部分](https://minidump.net/writing-a-net-gc-in-c-part-6/)结束时，我们的 `MarkPhase` 方法大概是这样的：

```csharp
    private void MarkPhase()
    {
        ScanContext scanContext = new();
        scanContext.promotion = true;
        scanContext._unused1 = GCHandle.ToIntPtr(_handle);

        var scanRootsCallback = (delegate* unmanaged<GCObject**, ScanContext*, uint, void>)&ScanRootsCallback;
        _gcToClr.GcScanRoots((IntPtr)scanRootsCallback, 2, 2, &scanContext);
    }
```

在 `GcScanRoots` 扫描完执行引擎提供的根之后，我们还需要再扫一遍强句柄（strong handles）。.NET 里当然还有很多稀奇古怪的句柄类型，不过好消息是，它们大多这些年已经逐步废弃了。眼下我们真正需要关心的，其实只有 `HNDTYPE_STRONG`（强句柄）和 `HNDTYPE_PINNED`（固定句柄）。

```csharp
    private void MarkPhase()
    {
        ScanContext scanContext = new();
        scanContext.promotion = true;
        scanContext._unused1 = GCHandle.ToIntPtr(_handle);

        var scanRootsCallback = (delegate* unmanaged<GCObject**, ScanContext*, uint, void>)&ScanRootsCallback;
        _gcToClr.GcScanRoots((IntPtr)scanRootsCallback, 2, 2, &scanContext);

        ScanHandles();
    }


    private void ScanHandles()
    {
        ScanContext scanContext = default;

        foreach (var handle in _gcHandleManager.Store.EnumerateHandlesOfType([HandleType.HNDTYPE_STRONG, HandleType.HNDTYPE_PINNED]))
        {
            var obj = handle->Object;
            if (obj != null)
            {
                ScanRoots(obj, &scanContext, default);
            }
        }
    }
```

这部分很直白。真正更阴险的，是另一种句柄：dependent handle（依赖句柄）。依赖句柄既可以通过 `DependentHandle` 使用，也可以通过 `ConditionalWeakTable` 使用。它允许我们在两个对象之间建立一种“反向依赖”。

举个例子，设有两个对象 `A` 和 `B`。如果 `A` 引用了 `B`（`A --> B`），那么只要 `A` 还活着，`B` 就不会被回收。如果我们改用依赖句柄（`new DependentHandle(A, B)`），那么即便 `A` 并没有直接引用 `B`，也能达到同样的效果。

这个机制很方便。例如，你想给一个并不归自己所有的对象外挂一些属性时，就可以用依赖句柄来实现：这样一来，只要目标对象被回收，你附着在它上的那部分数据也会自动释放。

那为什么依赖句柄会让 GC 头疼呢？因为 `A` 明明没有直接引用 `B`，却仍然能让 `B` 活下来。于是，GC 必须先完成一次常规的堆标记，然后才能再去扫描依赖句柄：

```csharp
    private void MarkPhase()
    {
        ScanContext scanContext = new();
        scanContext.promotion = true;
        scanContext._unused1 = GCHandle.ToIntPtr(_handle);

        var scanRootsCallback = (delegate* unmanaged<GCObject**, ScanContext*, uint, void>)&ScanRootsCallback;
        _gcToClr.GcScanRoots((IntPtr)scanRootsCallback, 2, 2, &scanContext);

        ScanHandles();
        ScanDependentHandles();
    }

    private void ScanDependentHandles()
    {
        ScanContext scanContext = default;

        foreach (var handle in _gcHandleManager.Store.EnumerateHandlesOfType([HandleType.HNDTYPE_DEPENDENT]))
        {
            // Target: primary
            // Dependent: secondary
            var primary = handle->Object;
            var secondary = (GCObject*)handle->ExtraInfo;

            if (primary == null || secondary == null)
            {
                continue;
            }

            if (primary->IsMarked() && !secondary->IsMarked())
            {
                ScanRoots(secondary, &scanContext, default);
            }
        }
    }
```

但这样仍然不够。考虑下面这张对象图：

```csharp
A ··> B --> C ··> D
```

这里，`A` 通过依赖句柄（用 `··>` 表示）让 `B` 保活；`B` 又通过普通引用（`-->`）让 `C` 保活；而 `C` 再通过另一个依赖句柄让 `D` 保活。假设 `A` 本身是某种根。

标记阶段开始时，我们只能先找到 `A`。随后扫描依赖句柄，发现 `A` 让 `B` 活着，于是继续标记，进而找到 `C`。到这里都没问题。

但此时 `C` 已经被标记了，这又意味着我们得再扫描一轮依赖句柄，才能把 `D` 也找出来。以此类推。

因此，要想正确处理依赖句柄，我们就必须不断循环扫描，直到这一轮里不再出现新的可标记对象：

```csharp
    private void ScanDependentHandles()
    {
        bool markedObjects;
        ScanContext scanContext = default;

        do
        {
            markedObjects = false;

            foreach (var handle in _gcHandleManager.Store.EnumerateHandlesOfType([HandleType.HNDTYPE_DEPENDENT]))
            {
                // Target: primary
                // Dependent: secondary
                var primary = handle->Object;
                var secondary = (GCObject*)handle->ExtraInfo;

                if (primary == null || secondary == null)
                {
                    continue;
                }

                if (primary->IsMarked() && !secondary->IsMarked())
                {
                    ScanRoots(secondary, &scanContext, default);
                    markedObjects = true;
                }
            }
        }
        while (markedObjects);
    }
```

事实上，这部分在 .NET GC 里本来就是[一个很明显的性能瓶颈](https://github.com/dotnet/runtime/issues/44683)，所以依赖句柄一定要慎用。举个很具体的例子：我曾经在 Visual Studio 启动时观察到一次长达 800ms 的卡顿，根因就是 GC。继续往下追以后，我发现那次回收过程中，GC 有 **60% 的时间都耗在扫描依赖句柄上**！

[![](https://minidump.net/images/2026-02-10-writing-a-net-gc-in-c-part-7-2.png)](https://minidump.net/images/2026-02-10-writing-a-net-gc-in-c-part-7-2.png)

ReSharper 和 Rider 在开启 Solution-Wide Analysis 时也遇到过同样的问题，所以后来我们专门重构了相关逻辑，尽量降低对依赖句柄的依赖。

## 清理句柄

还有最后一件事。完成堆标记以后，我们必须把所有指向“未被标记对象”的弱引用和依赖句柄失效掉。这一步是在清扫阶段完成的：

```csharp
    private void ClearHandles()
    {
        foreach (var handle in _gcHandleManager.Store.EnumerateHandlesOfType(
            [HandleType.HNDTYPE_WEAK_SHORT, HandleType.HNDTYPE_WEAK_LONG, HandleType.HNDTYPE_DEPENDENT]))
        {
            var obj = handle->Object;

            if (obj != null && !obj->IsMarked())
            {
                handle->Clear();
            }
        }
    }
```

目前我们还没有区分短弱引用（short weak reference）和长弱引用（long weak reference）。二者的区别在于：当对象还在等待终结（finalization）时，长弱引用仍然会继续保留，以便对象在某些情况下被“复活”。由于我们现在还没支持终结机制，所以暂时不用操心这部分。

这就是本篇的全部内容了。到这里为止，我们的 GC 已经能够正确管理弱引用（在不考虑终结的前提下）以及依赖句柄，这一点也可以通过[这些简单测试](https://github.com/kevingosse/ManagedDotnetGC/blob/master/TestApp/Tests/WeakReferenceTest.cs)验证。

下一篇，我们会来看内部指针（interior pointer）——在开始实现终结机制之前，这是最后一个需要补上的关键拼图。

一如既往，完整代码仍然可以在 [GitHub](https://github.com/kevingosse/ManagedDotnetGC/) 上找到。

[喜欢这篇文章吗？不妨看看 **《Pro .NET Memory Management》第二版**，进一步了解 .NET 垃圾回收器的内部机制！ ![Pro .NET Memory Management](https://minidump.net/images/progc.png)](https://amzn.to/42tg58c)

## 作者信息

Kevin Gosse

## 原文链接

https://minidump.net/writing-a-net-gc-in-c-part-7/
