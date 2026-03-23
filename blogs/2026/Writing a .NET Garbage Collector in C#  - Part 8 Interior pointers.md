# 用 C# 编写 .NET 垃圾回收器（第 8 部分）：内部指针

[喜欢这篇文章吗？别犹豫，去看看 **Pro .NET Memory Management** 第 2 版，进一步了解 .NET 垃圾回收器的内部机制！ ![Pro .NET Memory Management](https://minidump.net/images/progc.png)](https://amzn.to/42tg58c)

在前两篇文章里，我们一直在看如何实现垃圾回收器的标记阶段（mark phase）。我故意暂时跳过了一个话题：内部指针（interior pointer）。

如果你还记得，我们的 `ScanRoots` 方法长这样：

```csharp
private void ScanRoots(GCObject* root, ScanContext* context, GcCallFlags flags)
{
    if ((IntPtr)root == 0)
    {
        return;
    }

    if (flags.HasFlag(GcCallFlags.GC_CALL_INTERIOR))
    {
        // TODO
        return;
    }

    _markStack.Push((IntPtr)root);

    while (_markStack.Count > 0)
    {
        var ptr = _markStack.Pop();
        var o = (GCObject*)ptr;

        if (o->IsMarked())
        {
            continue;
        }

        o->EnumerateObjectReferences(_markStack.Push);
        o->Mark();
    }
}
```

今天我们就来填上这个 TODO，你很快就会明白，为什么这个主题值得单独写一篇。

如果你需要快速回顾一下前文，不妨先跳回这几篇文章：

* [第 1 部分：](https://minidump.net/2025-28-01-writing-a-net-gc-in-c-part-1/) 介绍与项目搭建
* [第 2 部分：](https://minidump.net/writing-a-net-gc-in-c-part-2/) 实现一个最小可用的 GC
* [第 3 部分：](https://minidump.net/writing-a-net-gc-in-c-part-3/) 使用 DAC 检查托管对象
* [第 4 部分：](https://minidump.net/writing-a-net-gc-in-c-part-4/) 遍历托管堆
* [第 5 部分：](https://minidump.net/writing-a-net-gc-in-c-part-5/) 解析 GCDesc 以找出托管对象的引用
* [第 6 部分：](https://minidump.net/writing-a-net-gc-in-c-part-6/) 实现标记阶段和清扫阶段（sweep phase）
* [第 7 部分：](https://minidump.net/writing-a-net-gc-in-c-part-7/) 标记句柄（handle）

## 什么是内部指针？

顾名思义，内部指针就是指向对象内部的指针。通常，引用会指向对象的起始位置。更准确地说，是指向对象头之后的位置：

```csharp
var obj = new MyObject();
```

```
                     MyObject
                +-----------------+  
                | Object header   |  
                +-----------------+  
       obj ---> | MethodTable*    | 
                +-----------------+ 
                |  Field1         | 
                +-----------------+ 
                |  Field2         | 
                +-----------------+ 
                |  Field3         |
                +-----------------|
                |  Field4         |
                +-----------------+
```

而内部指针会指向对象内部的某个位置：

```csharp
var obj = new MyObject();
ref var ptr = ref obj.Field2;
```

```
                     MyObject  
                +-----------------+  
                | Object header   |  
                +-----------------+  
       obj ---> | MethodTable*    | 
                +-----------------+ 
                |  Field1         | 
                +-----------------+ 
       ptr ---> |  Field2         | 
                +-----------------+ 
                |  Field3         |
                +-----------------|
                |  Field4         |
                +-----------------+
```

直接让内部指针指向对象字段，这种情况其实并不常见；但在现代高性能 C# 代码中，获取底层数组某个元素的引用却越来越普遍。比如，假设有一个用来存放计数器的字典：

```csharp
private Dictionary<string, int> _counters = new();

public void Increment(string key)
{
    _counters.TryGetValue(key, out var value);
    value++;
    _counters[key] = value;
}
```

我们得先取出值（如果存在），把它加一，再写回去。这样会做两次字典查找。我们也可以改成这样：

```csharp
private Dictionary<string, int> _counters = new();

public void Increment(string key)
{
    ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_counters, key, out _);
    value++;
}
```

`CollectionsMarshal.GetValueRefOrAddDefault` 会返回底层数组中该项的一个引用，这样我们就可以直接修改它，从而省掉一次查找。

不过，还有一个更简单也更常见的用例：span。

```csharp
long[] array = [1, 2, 3, 4];
var span = array.AsSpan();
var slice = span.Slice(2);
```

在底层实现上，span 就是靠着持有一个直接指向数组内部的引用来工作的：

```
                      long[]         
                +-----------------+  
                | Object header   |  
                +-----------------+  
     array ---> | MethodTable*    | 
                +-----------------+ 
                |  Length         | 
                +-----------------+ 
                |  Padding        | 
                +-----------------+ 
      span ---> |  1              |
                +-----------------|
                |  2              |
                +-----------------|
     slice ---> |  3              |
                +-----------------|
                |  4              |
                +-----------------+
```

总之，到这里你应该已经明白了：引用并不一定指向对象的起始位置。但这为什么会成为一个问题呢？

## 从 GC 的视角看内部指针

内部指针和普通引用一样，也必须保证对象存活。比如下面这段代码：

```csharp
public static void Test()
{
    long[] array = [1, 2, 3, 4];
    var span = array.AsSpan().Slice(2);
    DoStuff(span);
}

private static void DoStuff(ReadOnlySpan<long> span)
{
    // Do something with the span
}
```

当 `DoStuff` 被调用时，JIT 已经不再认为 `array` 这个引用仍然存活，因此它已经可以被垃圾回收了。span 以及背后的内部指针必须像普通引用一样让对象保持存活，否则我们最终可能会读到无效内存。

GC 在标记对象时，至少需要读取方法表（method-table）指针，原因有两个：

* 我们需要读取紧挨着方法表存放的 GCDesc，用它来找出对象对外引用的偏移。关于这一点，可以回顾一下[第 5 部分](https://minidump.net/writing-a-net-gc-in-c-part-5/)。
* 如[第 6 部分](https://minidump.net/writing-a-net-gc-in-c-part-6/)所述，我们会使用方法表指针的最低有效位来给对象打标记。

对于普通引用，这对 GC 来说很简单，因为它拿到的就是方法表指针所在的地址。可对于内部指针，它必须想办法往回找到对象的起始位置。

```
                |                 |
                |                 |
                |                 |
                |       ???       |
                |                 |
                |                 |
                +-----------------|
       ptr ---> |  3              |
                +-----------------|
                |  4              |
                +-----------------+
```

从原理上说，向后读取内存并没有问题，问题在于：什么时候该停？GC 怎么知道自己已经找到了对象的开头？一个很诱人的方案是：只要看到某个值指向一个合法的方法表，就把它当成对象起点。但这种做法很容易出现误判。比如下面这段代码：

```csharp
public class MyObject
{
    public nint Handle;
}

var obj = new MyObject { Handle = typeof(string).TypeHandle.Value };
```

`TypeHandle.Value` 就是方法表的地址，因此指向 `MyObject` 内部的指针会把这个启发式规则骗过去。你也许会说，这个场景太人为构造了，在真实应用里几乎不会出现；但即便只是一个普通的哈希码，也有可能恰好和某个方法表地址发生碰撞。更关键的是，这个问题从根本上无解：无论你设计出多复杂的模式来识别对象起点，都始终存在一个非零概率，使同样的模式出现在随机数据里。

那 GC 还能怎么办？如果没有额外上下文，唯一安全的办法就是从内存段（segment）的起始位置一路[遍历托管堆](https://minidump.net/writing-a-net-gc-in-c-part-4/)，直到给定地址为止，以此确定对象起点。但从性能角度看，这当然完全不能接受：每次需要标记一个内部指针时，我们不可能都把整段内存扫一遍（.NET GC 里一个基础 region 有 4 MB，在引入 region 之前甚至可达 4 GB！）。

我们需要一个技巧，避免每次都把整个段扫描一遍。.NET GC 用的、而我们也准备实现的这个技巧，叫作砖块表（brick table）。

## 砖块表的设计

核心思路很简单：建立一张对象在内存中位置的映射表，这样就能快速判断某个地址属于哪个对象。砖块表有很多不同的实现方式，而每一种实现都是精度与内存占用之间的权衡。为了更直观地理解这种权衡，我们来看几个实际例子。

先假设我们有一张穷举式的砖块表。它记录堆上每个对象的位置。其一种实现方式是位图（bitmap）：每一位对应 1 字节内存；如果某一位被置 1，就表示有对象从那个字节开始。对于一个 1 GB 的堆，这张砖块表需要 128 MB 内存，浪费相当惊人！好在我们还能进一步优化：所有对象都会按指针大小对齐。因此在 x64 上，每个对象的起始地址一定是 8 的倍数。考虑到这一点后，砖块表中的每一位就可以覆盖 8 个字节，而不会漏掉任何对象。这样内存占用就变成 128 / 8 = 16 MB。在某些场景下也许可以接受，但依然不算小。

想把它继续缩小，就得接受砖块表不再是穷举的：它不再直接给出内部指针所指对象的精确地址，而是给出附近某个对象的地址，然后我们再稍微遍历一下堆，找到正确的对象。举例来说，如果每一位覆盖 64 字节，那么砖块表就能缩小到 2 MB。不过这时位图方案的局限也暴露出来了：既然我们需要知道对象到底从哪里开始，那就只能标记那些恰好落在 64 字节边界上的对象，这会极大削弱砖块表的效果。如果你不太容易脑补出来，可以看下面这个示意图：

```
----------------------------------------
        |        |        |        |
----------------------------------------
        0        0        0        0
```

每一条竖线 `|` 都表示一个 64 字节边界，并映射到位图中的一个 bit。如果一个对象（`*`）恰好分配在边界起点，我们就可以把对应的 bit 置 1，表示它的位置：

```
----------------------------------------
        |*       |        |        |
----------------------------------------
        1        0        0        0
```

因此，当我们扫描砖块表时，只要看到某个 bit 被置位，就知道有对象位于那块 64 字节内存的起点。

但如果对象位于中间某个位置：

```
----------------------------------------
        |     *  |        |        |
----------------------------------------
        0        0        0        0
```

那我们就没法标记这个 bit。否则，当我们扫描位图、看到某个 bit 被置位时，就只能知道对象位于这 64 字节范围内的_某个位置_，却无法知道它究竟从哪里开始。问题于是又回到了原点：我们到底该怎么识别对象的起始位置？

归根结底，位图方案在试图穷举映射对象位置时表现不错；但一旦我们为了节省内存而降低精度，它就撑不住了。

一个扩展性更好的做法，是把砖块表实现成“偏移量数组”。数组中的每个元素都表示某段内存中第一个对象的偏移（当然，也可以像后面那样存最后一个对象的偏移）。如果把它实现成字节数组，那么每个元素就能表示 255 字节的内存范围（不是 256，因为值 `0` 被保留用来表示“这个范围里没有对象”）。假设砖块表开头是这样：`[0x54, 0x0, 0x22, 0x23]`。解码之后，我们就知道：

* 地址 `0xFF*0 + 0x54 = 0x54` 处有一个对象
* 范围 `[0xFF*1, 0xFF*2) = [0xFF, 0x1FE)` 中没有对象
* 地址 `0xFF*2 + 0x22 = 0x220` 处有一个对象
* 地址 `0xFF*3 + 0x23 = 0x320` 处有一个对象

有了这些信息，如果我们遇到一个指向 `0x300` 的内部指针，就知道 `0x220` 处有一个对象，于是可以从那里开始遍历托管堆。

这种实现里，砖块表的每 1 个字节对应 255 字节内存，因此要覆盖 1 GB 堆需要 4 MB。再考虑到对象像前面说的那样按 8 字节对齐，那么砖块表中的每个字节实际上可以覆盖 `255*8 = 2040` 字节内存。这样覆盖 1 GB 堆只需要大约 500 KB。这个效率就相当高了！

在这张砖块表上执行查找时，可以使用下面的算法：

* 给定一个地址，先算出砖块表中哪个字节覆盖了这个地址（也就是用地址除以 `255*8`）
* 读取该字节。如果它不是 0，就说明我们已经拿到了这个范围内第一个对象的地址，可以从那里开始遍历托管堆
* 如果它是 0，就继续读取砖块表中的前一个表项，并重复这个过程，直到找到一个非 0 表项，或者退回到开头（这就意味着我们必须遍历整个段）

为了避免最后这一步，也就是避免向后扫描数量不定的表项，.NET GC 还做了另一个优化：它把砖块表项改成了有符号 `short`。正数表示对象的偏移量，负数表示我们需要在砖块表中向前跳多远，才能找到一个正值表项。于是算法就变成了：

* 给定一个地址，先算出砖块表中哪个表项覆盖了这个地址
* 读取该表项。如果它大于 0，就说明我们已经拿到了这个范围内第一个对象的地址，可以从那里开始遍历托管堆
* 如果它是负数，就把砖块索引加上这个（负）值，然后重复查找。比如我们一开始落在第 9 个表项上，读到的值是 -4，那么就跳回第 5 个表项，而且一定能在那里看到一个非 0 值

## 实现

在我的 GC 里，我决定暂时不实现 .NET GC 的那个优化，而是让砖块表中的每个字节覆盖 `255*8 = 2040` 字节内存。我把砖块表分配在每个段的起始位置。我的段大小是 4 MB，所以每张砖块表大约只占 2 KB。与此同时，我还决定记录这个范围内“最后一个对象”的偏移，而不是第一个对象的偏移；否则砖块表的第一个字节就浪费了（因为段里的第一个对象一定会从段起始位置分配）。我不打算详细解释那段代码，因为它基本都是枯燥的计算，讲了也没太多附加价值。如果你好奇，可以直接去 [GitHub](https://github.com/kevingosse/ManagedDotnetGC/blob/master/ManagedDotnetGC/Segment.cs) 看源码。

我的 `Segment` 对象对外暴露了两个 API：`void MarkObject(IntPtr addr)` 用来在砖块表里标记某个地址；`IntPtr FindClosestObjectBelow(IntPtr addr)` 则会在给定地址下方返回一个已知且距离最近的对象地址。接下来，我们就用它来处理内部指针：

```csharp
private void ScanRoots(GCObject* root, ScanContext* context, GcCallFlags flags)
{
    if ((IntPtr)root == 0)
    {
        return;
    }

    if (flags.HasFlag(GcCallFlags.GC_CALL_INTERIOR))
    {
        // Find the segment containing the interior pointer
        var segment = _segmentManager.FindSegmentContaining((nint)root);

        if (segment.IsNull)
        {
            Write($"  No segment found for interior pointer {(IntPtr)root:x2}");
            return;
        }

        var objectStartPtr = segment.FindClosestObjectBelow((IntPtr)root);
        bool found = false;

        // Walk the heap starting from objectStartPtr,
        // until we find the object pointed at by the interior pointer
        foreach (var ptr in WalkHeapObjects(objectStartPtr, (IntPtr)root))
        {
            var o = (GCObject*)ptr;
            var size = o->ComputeSize();

            // Is the interior pointer within this object?
            if ((IntPtr)o <= (IntPtr)root && (IntPtr)root < (IntPtr)o + (nint)size)
            {
                root = o;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Write($"  No object found for interior pointer {(IntPtr)root:x2}");
            return;
        }
    }

    _markStack.Push((IntPtr)root);

    while (_markStack.Count > 0)
    {
        var ptr = _markStack.Pop();
        var o = (GCObject*)ptr;

        if (o->IsMarked())
        {
            continue;
        }

        var segment = _segmentManager.FindSegmentContaining((nint)o);

        if (segment.IsNull)
        {
            continue;
        }

        o->EnumerateObjectReferences(_markStack.Push);
        o->Mark();
        segment.MarkObject((IntPtr)o); // Update the brick table
    }
}
```

可以看到，`ScanRoots` 方法里有两处变化：

* 现在我们已经能正确处理内部指针：先借助砖块表拿到目标地址下方某个对象的地址，再沿着堆向前遍历，直到找到真正对应的对象
* 当我们标记一个对象时，也会顺手把它的位置写入砖块表

目前，这个 GC 只会清扫内存，并[把死亡对象替换成一个特殊的空闲对象](https://minidump.net/writing-a-net-gc-in-c-part-6/)，这个对象和普通对象一样可以被遍历。这意味着目前我们还不需要从砖块表里清除对象标记，但等将来实现能够复用这些空闲空间的分配器时，这件事就得重新考虑了。

另外要注意，第一次 GC 时砖块表还是空的。这意味着那时候扫描内部指针的代价会特别高。为了降低这个成本，我会在标记阶段发现对象时顺便更新砖块表，让它逐步填充起来。但我们还能做得更多：线程在分配对象时，大多可以借助各自的[分配上下文](https://minidump.net/writing-a-net-gc-in-c-part-2/)独立完成；不过当它们需要新的分配上下文时，仍然会调用 GC 的 `Alloc` API。[我们可以在那时更新砖块表](https://github.com/kevingosse/ManagedDotnetGC/blob/500772ac383195177d74c779e554e8861a28f24e/ManagedDotnetGC/GCHeap.cs#L200)。

做到这一步之后，我们的 GC 终于能够解析内部指针了！正如你看到的，它比一开始想象的要难处理得多。而且 GC 在扫描它们时必须尽可能高效，这一点非常重要，因为内部指针通常出现在那些对性能极其敏感、经过高度优化的代码里。

你可能已经注意到了：因为引入了砖块表，我们的 GC 现在需要频繁查找某个地址到底属于哪个段（这由 `_segmentManager.FindSegmentContaining` 方法完成）。而这个方法目前效率很低——它只是线性遍历段列表，直到找到正确的那个——所以很快就会成为瓶颈。出于这个原因，再加上我们到目前为止完全忽略了冻结段（frozen segment）这个话题，下一篇文章里我们将彻底重新思考 GC 的内存分配方式。

[喜欢这篇文章吗？别犹豫，去看看 **Pro .NET Memory Management** 第 2 版，进一步了解 .NET 垃圾回收器的内部机制！ ![Pro .NET Memory Management](https://minidump.net/images/progc.png)](https://amzn.to/42tg58c)

## 作者信息
Kevin Gosse

## 原文链接
https://minidump.net/writing-a-net-gc-in-c-part-8/
