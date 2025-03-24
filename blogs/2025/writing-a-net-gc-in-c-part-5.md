这是我们用 C# 编写 .NET 垃圾收集器之旅的第五部分。在前一篇文章中，我们已经开始为实现标记阶段奠定基础，并学习了如何遍历托管堆。接下来，我们需要学习如何遍历引用，以便标记可到达的对象，并推断出哪些对象可以被收集。

要遍历引用，我们需要两样东西：

- 根列表，即不可收集的引用（局部变量、静态字段、GC 句柄......）。它们是图遍历的起点。
- 从一个对象到另一个对象的引用列表。这就是本文的重点。

如果你错过了前面的部分，可以在这里找到：

- [第 1 部分](writing-a-net-gc-in-c-part-1.md)：简介和项目设置
- [第 2 部分](writing-a-net-gc-in-c-part-2.md)：实现最小 GC
- [第 3 部分](writing-a-net-gc-in-c-part-3.md)：使用 DAC 检查托管对象
- [第 4 部分](writing-a-net-gc-in-c-part-4.md)：遍历托管堆

## GCDesc

为了遍历引用，GC 需要能够找到从一个对象到另一个对象的引用。换句话说，它必须知道哪些字段可能包含引用，以及在内存中哪里可以找到它们。这些信息存储在一个名为 `GCDesc` 的结构中。

每个类型都有自己的 `GCDesc`。为了提高效率，它并不描述对象的完整布局，而只描述那些包含引用的部分。此外，它还指出了引用所在的内存范围，而不是每个字段的确切偏移量。举个实际例子，请看下面的对象布局：

```
+-----------------+
|  Field1 (ref)   |
+-----------------+
|  Field2 (ref)   |
+-----------------+
|  Field3         |
+-----------------+
|  Field4 (ref)   |
+-----------------+
```

假设所有字段长度均为 8 字节，除 `Field3` 外，其他字段均存储引用。`GCDesc` 中存储的信息将是引用所在的内存范围，例如 `[0, 16] [24, 32]`。由于存储引用的字段大小始终相同（指针的大小），这些信息足以推断出对象中存储了多少个引用以及它们的位置。

> 我们即将看到，`GCDesc` 实际上并不存储范围的起点和终点，而是存储起点和大小。

`GCDesc` 保存在类型的方法表旁边。或者更准确地说，就在它的前面。

```
                                         +-----------------+
                      Object             |                 |
                +-----------------+      |                 |
                | Object header   |      |  GCDesc         |
                +-----------------+      +-----------------+
Object ref ---> | MethodTable*    | ---> |  MethodTable    |
                +-----------------+      |                 |
                |  Field1         |      |                 |
                +-----------------+      |                 |
                |  Field2         |      |                 |
                +-----------------+      +-----------------+
                |  Field3         |
                +-----------------|
                |  Field4         |
                +-----------------+
```

## GCDesc 编码

`GCDesc` 的编码有点[复杂](https://github.com/dotnet/runtime/blob/main/src/coreclr/gc/gcdesc.h)。第一个字段是 `GCDescSeries` 的个数（注意：GCDesc 在内存中是向后增长的，因此从方法表开始向后，这是第一个字段）。大多数情况下，计数是正数，但对于值类型数组，计数将是负数，这表示编码不同。

### GCDescSeries 计数 > 0

计数为正数时，表示后面的 `GCDescSeries` 数量。每个 `GCDescSeries` 都是一对值，描述了存储引用的内存范围：

```c#
[StructLayout(LayoutKind.Sequential)]
internal struct GCDescSeries
{
    public nint Size;
    public nint Offset;

    public void Deconstruct(out nint size, out nint offset)
    {
        size = Size;
        offset = Offset;
    }
}
```

因此，计数2的 `GCDesc` 结构看起来就像：

```
+-----------------+
|  Size2          |
+-----------------+
|  Offset2        |
+-----------------+
|  Size1          |
+-----------------+
|  Offset1        |
+-----------------+
|  Count          |
+-----------------+
|  MethodTable    |
|                 |
|                 |
|                 |
|                 |
+-----------------+
```

`GCDescSeries` 的偏移量相对于对象的起始位置（请记住，对象的起始位置是方法表指针，而不是头部）。`GCDescSeries` 的大小非常奇怪，因为对象的基本大小已经从中减去，所以我们必须将其加回来。举个实际例子，请看这个对象：

```
+-----------------+
| Object header   |
+-----------------+
| MethodTable*    |
+-----------------+
|  Field1 (ref)   |
+-----------------+
```

对于 64 位进程，假设 `Field1` 是引用，那么 `GCDesc` 将包含一个偏移量为 8、大小为 -16 的 `GCDescSeries`（范围的实际大小为 8，但对象的基本大小为 24，因此值编码为 `8 - 24 = -16`）。大小的单位是字节，因此我们必须用它除以指针的大小，才能得到引用的次数。

将所有这些综合起来，我们就可以开始编写一个方法来查找对象中的引用，目前只处理正向情况：

```c#
    private static unsafe void EnumerateObjectReferences(GCObject* obj, Action<IntPtr> callback)
    {
        if (!obj->MethodTable->ContainsGCPointers)
        {
            return;
        }

        var mt = (nint*)obj->MethodTable;
        var objectSize = obj->ComputeSize();

        var seriesCount = mt[-1];

        Console.WriteLine($"Found {seriesCount} series");

        if (seriesCount > 0)
        {
            var series = (GCDescSeries*)(mt - 1);

            for (int i = 1; i <= seriesCount; i++)
            {
                var (seriesSize, seriesOffset) = series[-i];
                seriesSize += (int)objectSize;

                Console.WriteLine($"Series {i}: size={seriesSize}, offset={seriesOffset}");

                var rangeStart = (nint*)((nint)obj + seriesOffset);
                var nbRefsInRange = seriesSize / IntPtr.Size;

                for (int j = 0; j < nbRefsInRange; j++)
                {
                    // Found a reference
                    callback(rangeStart[j]);
                }
            }
        }
        else
        {
            // TODO
        }
    }
```

`GCObject` 和 `MethodTable` 已在 [第四部分](writing-a-net-gc-in-c-part-4.md)定义，`ComputeSize` 实现。

### 测试

我们可以在 “经典”.NET 应用程序中测试这段代码，它比 GC 更容易调试。为此，我们创建一个新的控制台应用程序，并从 GC 项目中复制 `GCObject` 和 `MethodTable` 定义。我们添加一个辅助程序来获取托管对象的地址：

```c#
internal static unsafe class Utils
{    
    public static nint GetAddress<T>(T obj) => (nint)(*(T**)&obj);
}
```

现在我们可以测试 `EnumerateObjectReferences` 方法了：

```c#
class Program
{
    static void Main(string[] args)
    {
        var obj = new ObjectWithReferences();
        var address = Utils.GetAddress(obj);
        EnumerateObjectReferences((GCObject*)address, ptr =>
        {
            Console.WriteLine($"Found reference to {ptr:x2}");
        });
    }
}

internal class ObjectWithReferences
{
    public object Field1 = new();
    public object Field2 = new();
}
```

当然，这很不安全，如果 GC 决定在执行过程中移动对象，就会导致测试失败，但这对我们的简单测试来说已经足够了。这应该会显示如下内容：

```
Found 1 series
Series 1: size=16, offset=8
Found reference to 1cf8e40b508
Found reference to 1cf8e40b520
```

一个对象有多个连续区域（series）怎么办？要想拥有多个连续区域，我们在设计对象时必须确保引用不是连续的。你可能想写：

```c#
internal class ObjectWithMultipleSeries
{
    public object Field1 = new();
    public long Field2;
    public object Field3 = new();
}
```

但输出结果仍将显示一个连续区域：

```
Found 1 series
Series 1: size=16, offset=8
Found reference to 1766ec0b510
Found reference to 1766ec0b528
```

事实证明，**JIT 是允许对字段重新排序的**，而且它总是试图在对象的开头将引用分组，因此最终只有一个连续区域。那么，也许我们可以使用具有固定布局的结构：

```c#
[StructLayout(LayoutKind.Sequential)]
internal struct ObjectWithMultipleSeries()
{
    public object Field1 = new();
    public long Field2;
    public object Field3 = new();
}
```

> 请注意这个微妙的主构造函数：`internal struct ObjectWithMultipleSeries()`。只编写 `internal struct ObjectWithMultipleSeries` 将是非法的，需要一个显式构造函数来使用字段初始化器：
>
> ```c#
> [StructLayout(LayoutKind.Sequential)]
> internal struct ObjectWithMultipleSeries
> {
>     public object Field1 = new();
>     public long Field2;
>     public object Field3 = new();
> 
>     public ObjectWithMultipleSeries()
>     {
>     }
> }
> ```

因为我们使用的是结构体，所以必须装箱它以更新代码，否则就找不到方法表：

```c#
class Program
{
    static void Main(string[] args)
    {
        // Explicit cast to object to box the struct
        var obj = (object)new ObjectWithReferences();
        var address = Utils.GetAddress(obj);
        EnumerateObjectReferences((GCObject*)address, ptr =>
        {
            Console.WriteLine($"Found reference to {ptr:x2}");
        });
    }
}
```

执行之后，我们仍然看到只有一个连续区域

```
Found 1 series
Series 1: size=16, offset=8
Found reference to 259b6c0b4e8
Found reference to 259b6c0b500
```

我们之前添加的 `[StructLayout(LayoutKind.Sequential)]` 又是怎么回事呢？事实证明，当结构包含引用时，JIT 会忽略 `StructLayout`，并自动切换回 `LayoutKind.Auto`。那怎么可能有多个序列呢？至少有两种情况可能发生这种情况。第一种是嵌套结构体（JIT 不会在结构体之间重新排列字段）。

```c#
internal struct NestedStruct()
{
    public object NestedField1 = new();
    public long NestedField2;
}

internal class ObjectWithMultipleSeries
{
    public long Field1;
    public NestedStruct Field2 = new();
    public object Field3 = new();
}
```

其中的布局是这样的（注意，引用 `Field3` 被移到了对象的开头，然后按顺序排列值类型）：

```
+----------------------+
|  Field3 (ref)        |
+----------------------+
|  Field1              |
+----------------------+
|  NestedField1 (ref)  |
+----------------------+
|  NestedField2        |
+----------------------+
```

将会显示：

```
Found 2 series
Series 1: size=8, offset=8
Found reference to 18201c0b560
Series 2: size=8, offset=24
Found reference to 18201c0b548
```

第二种情况是继承（子类不能改变基类的布局）。

```c#
internal class BaseClass
{
    public object BaseField1 = new();
    public long BaseField2;
}

internal class ObjectWithMultipleSeries : BaseClass
{
    public object Field1 = new();
}
```

它的布局如下：

```
+--------------------+
|  BaseField1 (ref)  |
+--------------------+
|  BaseField2        |
+--------------------+
|  Field1 (ref)      |
+--------------------+
```

好极了！我们还可以检查代码是否正确处理了引用类型数组：

```c#
class Program
{
    static void Main(string[] args)
    {
        ObjectWithMultipleSeries[] obj = [new(), new(), new()];
        var address = Utils.GetAddress(obj);
        EnumerateObjectReferences((GCObject*)address, ptr =>
        {
            Console.WriteLine($"Found reference to {ptr:x2}");
        });
    }
}
```

### GCDescSeries count < 0

还有一种情况需要处理：当 `GCDescSeries` 数为负数时，表示数组是值类型。这种情况比较特殊，因为只有在这种情况下，数列的个数才取决于数组中元素的个数（这种情况不会发生在引用类型的数组中，因为如前所述，这些数组只包含对象的引用，因此是由单个数列组成的）。

例如，如果我们使用前面定义的 `NestedStruct` 类型制作一个数组，其布局将是：

```
+----------------------+
|  NestedField1 (ref)  |
+----------------------+
|  NestedField2        |
+----------------------+
|  NestedField1 (ref)  |
+----------------------+
|  NestedField2        |
+----------------------+
|  NestedField1 (ref)  |
+----------------------+
|  NestedField2        |
+----------------------+
```

在这种情况下，`GCDesc` 会切换到不同的编码。当计数为负数时，计数的绝对值表示后面的 `ValSerieItem` 的数量，前面是一个偏移量（再次提醒，我们是在逆向增长，所以“前面”实际上意味着它在内存中的位置更高）。`ValSerieItem` 包含两个字段：范围内指针的个数，以及跳过查找下一个范围的字节数。每个字段的大小都是指针大小的一半（整个 `ValSerieItem` 适合放在一个指针中）。

如果计数为 -2，`GCDesc` 将如下所示：

```
+-----------------+
|  ValSerieItem   |
+-----------------+
|  ValSerieItem   |
+-----------------+
|  Offset         |
+-----------------+
|  Count          |
+-----------------+
|  MethodTable    |
|                 |
|                 |
|                 |
|                 |
+-----------------+
```

在 C# 中表示 `ValSerieItem` 非常麻烦，因为没有内置类型来存储半个指针。相反，我们可以使用单个 `nint` 作为底层存储，然后使用位掩码来提取值：

```c#
internal struct ValSerieItem(nint value)
{
    public uint Nptrs => IntPtr.Size == 4 ? (ushort)(value & 0xFFFF) : (uint)(value & 0xFFFFFFFF);

    public uint Skip => IntPtr.Size == 4 ? (ushort)((value >> 16) & 0xFFFF) : (uint)(((long)value >> 32) & 0xFFFFFFFF);
}
```

要查找值类型数组中的引用，我们必须从偏移量开始，然后为每个 `ValSerieItem` 读取给定数量的指针并跳转跳过字节。数组中有多少个元素，就必须重复多少次。结果代码如下

```c#
    private static unsafe void EnumerateObjectReferences(GCObject* obj, Action<IntPtr> callback)
    {
        if (!obj->MethodTable->ContainsGCPointers)
        {
            return;
        }

        var mt = (nint*)obj->MethodTable;
        var objectSize = obj->ComputeSize();

        var seriesCount = mt[-1];

        Console.WriteLine($"Found {seriesCount} series");

        if (seriesCount > 0)
        {
            // ...
        }
        else
        {
            var offset = mt[-2];
            var valSeries = (ValSerieItem*)(mt - 2) - 1;

            Console.WriteLine($"Offset {offset}");

            // Start at the offset
            var ptr = (nint*)((nint)obj + offset);

            // Retrieve the length of the array
            var length = obj->Length;

            // Repeat the loop for each element in the array
            for (int item = 0; item < length; item++)
            {
                for (int i = 0; i > seriesCount; i--)
                {
                    // i is negative, so this is going backwards
                    var valSerieItem = valSeries + i;

                    Console.WriteLine($"ValSerieItem: nptrs={valSerieItem->Nptrs}, skip={valSerieItem->Skip}");

                    // Read valSerieItem->Nptrs pointers
                    for (int j = 0; j < valSerieItem->Nptrs; j++)
                    {
                        callback(*ptr);
                        ptr++;
                    }

                    // Skip valSerieItem->Skip bytes
                    ptr = (nint*)((nint)ptr + valSerieItem->Skip);
                }
            }
        }
    }
```

如果使用由三个 `NestedStruct` 组成的数组运行代码，我们会得到

```
Found -1 series
Offset 16
ValSerieItem: nptrs=1, skip=8
Found reference to 1de5c80b530
ValSerieItem: nptrs=1, skip=8
Found reference to 1de5c80b548
ValSerieItem: nptrs=1, skip=8
Found reference to 1de5c80b560
```

## 总结

我们已经实现了 GC 的另一个缺失部分：在对象中查找引用的能力，这对于遍历引用图至关重要。在此过程中，我们发现了 .NET 中对象布局的一些微妙之处。在下一篇文章中，我们将最终能够构建一个可触及对象的图，但会有一些限制。