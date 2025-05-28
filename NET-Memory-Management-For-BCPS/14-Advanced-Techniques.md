本章是前一章的延续，将介绍.NET中更高级的技术。请注意，阅读前一章（特别是关于引用类型、引用返回和 `ref` 结构体的内容）对理解本章非常有帮助。

本章内容与当前.NET编程趋势（尤其是注重性能优化的方向）紧密契合——通过极致压榨CPU时钟周期和堆内存分配，使托管框架和应用运行得更快。越来越多的库及其API正在通过更高效的Span和管道技术进行“Span化”和“管道化”改造。希望本章的讲解能帮助您适应这个现代化的.NET世界。

# Span与Memory

在C#中，我们可以通过多种方式分配连续内存：常规堆分配数组、固定缓冲区、`stackalloc` 或非托管内存。如果能用统一的方式表示所有这些情况，同时不引入比普通数组更多的开销，将会非常便利。此外，这类内存经常需要被“切片”——仅将部分内存提供给其他方法处理。而所有这些操作都应该在不产生堆分配的前提下完成——堆分配正是高性能.NET代码的头号敌人。这些需求催生了 `Span<T>`。

## Span<T>

.NET Core 2.1引入了新的泛型类型 `Span<T>`。它是值类型（ref struct），因此本身不会产生分配。它具有返回引用的索引器，可以像数组一样使用。更重要的是，它设计用于高效切片——子范围由另一个 `Span<T>` ref 结构体表示，同样无需任何内存分配。

代码清单14-1展示了典型的 `Span<T>` 使用场景。无论 `UseSpan` 方法最后使用哪个 span 实例（代表不同类型的内存），都可以通过 `Span<T>` 公开的 `Length` 属性和索引器成员像数组一样使用。注意 `UseSpan` 被标记为 `unsafe` 是因为指针使用，而非 `Span<T>` 本身。

代码清单14-1. 典型 `Span<T>` 使用场景

```csharp
unsafe public static void UseSpan() 
{
    var array = new int[64];
    Span<int> span1 = new Span<int>(array);
    Span<int> span2 = new Span<int>(array, start: 8, length: 4);
    Span<int> span3 = span1.Slice(0, 4);
    Span<int> span4 = stackalloc[] { 1, 2, 3, 4, 5 };
    Span<int> span5 = span4.Slice(0, 2);
    void* memory = NativeMemory.Alloc(64);
    Span<byte> span6 = new Span<byte>(memory, 64);
    
    var span = span1; // 也可以是span2, span3等
    for (int i = 0; i < span.Length; i++)
        Console.WriteLine(span[i]);
        
    NativeMemory.Free(memory);
}
```

并非所有内存都应被修改，因此还提供了对应的 `ReadOnlySpan<T>` 类型来表示只读内存。典型应用包括处理字符串数据——字符串不可变，若用 `Span<T>` 表示会破坏这个特性。字符串的 `AsSpan` 扩展方法返回的是 `ReadOnlySpan<char>`。当然，也可以主动用此类型表示常规数据的只读视图（见代码清单14-2）。

代码清单14-2. 典型 `ReadOnlySpan<T>` 使用场景

```csharp
public static void UseReadOnlySpan() 
{
    var array = new int[64];
    ReadOnlySpan<int> span1 = new ReadOnlySpan<int>(array);
    ReadOnlySpan<int> span2 = new Span<int>(array);
    
    string str = "Hello world";
    ReadOnlySpan<char> span3 = str.AsSpan();
    ReadOnlySpan<char> span4 = str.AsSpan(start: 6, length: 5);
}
```

虽然初看可能并不惊艳，但这个类型在许多应用中具有革命性意义。首先，它能显著简化某些API。试想一个整数解析例程需要处理各种内存类型时（见代码清单14-3），API接口会因支持所有使用场景而快速膨胀。而通过 `Span<char>` 可以简化为单一方法（见代码清单14-4）。

代码清单14-3  存在问题的整型解析API

```c#
int Parse(string input);
int Parse(string input, int startIndex, int length);
unsafe int Parse(char* input, int length);
unsafe int Parse(char* input, int startIndex, int length);
```

代码清单14-4  使用 `Span<T>` 简化的整型解析API

```c#
int Parse(ReadOnlySpan input);
```

得益于 `Span<T>` 能够表示各种形式的连续值集合（如数组、字符串、非托管数组指针等），它可以极大简化API设计，无需创建大量重载方法，也无需强制用户创建不必要的副本（以使数据适配API要求）。

其次，`Span<T>` 极大简化了高性能代码的编写，例如可以安全地使用 `stackalloc`（如代码清单14-1所示）。但最重要的是其切片能力，允许对较小的内存块进行操作（例如解析时），在代码中传递这些切片而不会产生开销。稍后您将看到它是如何实现高效切片的。

C#编译器还能智能地处理 `Span<T>` 包装数据的生命周期。因此，从方法返回包装托管数组的 `Span<T>` 是完全可行的（因为数组生命周期超出方法范围，见代码清单14-5中的 `ReturnArrayAsSpan` 方法），但不允许返回局部栈数据（因为方法结束时这些数据会被丢弃，见代码清单14-5中非法的 `ReturnStackallocAsSpan` 方法）。在处理非托管内存时需要特别注意，因为必须记得显式释放内存（见代码清单14-5中的 `ReturnNativeAsSpan` 方法，其中分配的内存从未被释放）。

代码清单14-5  返回 `Span<T>` 的三种示例

```c#
public Span<int> ReturnArrayAsSpan()
{
    var array = new int[64];
    return array.AsSpan();
}

public Span<int> ReturnStackallocAsSpan()
{
    Span span = stackalloc[] { 1, 2, 3, 4, 5 };
    // 编译错误CS8352: 无法在此上下文中使用局部变量'span'，因为它可能将引用变量暴露到声明范围之外
    return span;
}

public unsafe Span<int> ReturnNativeAsSpan()
{
    IntPtr memory = Marshal.AllocHGlobal(64);
    return new Span(memory.ToPointer(), 8);
}
```

### 使用示例

让我们看几个 `Span<T>` 的使用示例。自本书第一版以来，`Span<T>` 已在.NET生态系统中广泛应用，形成了许多成熟的设计模式。

Kestrel 服务器（用于托管ASP.NET Core Web应用程序）很好地利用了大数据切片能力。代码清单14-6展示了 `KestrelHttpServer` 早期实现中 `HttpParser`类的部分代码。可以看到，传入的HTTP请求通过 `Span<T>` 切片逐行解析：首先每行作为独立切片传入 `ParseRequestLine` 方法，随后该行中的每个关键部分（如HTTP路径或查询）又被切片为独立的 `Span<T>` 实例并传递给 `OnStartLine` 方法。这种方式避免了调用 `string.Substring` 时发生的内存复制，且由于 `Span<T>` 是栈上分配的，全程不会产生堆分配。

`OnStartLine` 方法进一步使用传入的 `Span<T>` 实例实现业务逻辑。同样地，切片后的HTTP头信息也在 `HttpParser` 类中以相同方式解析。

代码清单14-6 KestrelHttpServer中 `HttpParser` 类的代码片段

```csharp
public unsafe bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined) {
    var span = buffer.First.Span;
    var lineIndex = span.IndexOf(ByteLF);
    if (lineIndex >= 0) {
        consumed = buffer.GetPosition(lineIndex + 1, consumed);
        span = span.Slice(0, lineIndex + 1);
    }
    // 固定内存并解析切片
    fixed (byte* data = &MemoryMarshal.GetReference(span)) {
        ParseRequestLine(handler, data, span.Length);
    }
}

private unsafe void ParseRequestLine(TRequestHandler handler, byte* data, int length) {
    int offset;
    // 获取方法并设置偏移量
    var method = HttpUtilities.GetKnownMethod(data, length, out offset);
    // 获取路径切片
    var pathBuffer = new Span<byte>(data + pathStart, offset - pathStart);
    // 获取查询参数切片
    var targetBuffer = new Span<byte>(data + pathStart, offset - pathStart);
    var query = new Span<byte>(data + queryStart, offset - queryStart);
    handler.OnStartLine(method, httpVersion, targetBuffer, pathBuffer, query, customMethod, pathEncoded);
}
```

另一个经典案例是 `System.Private.CoreLib` 程序集中定义的内部 `ValueStringBuilder` 结构体。顾名思义，这是 `StringBuilder` 的值类型版本，提供可变的字符串操作功能。

如代码清单14-7所示，它使用 `Span<char>` 作为内部存储，使其具有存储无关性——初始存储可以是 `stackalloc` 分配的栈内存、原生内存或堆分配的数组。通过返回引用类型的索引器，可以高效访问单个字符。

代码清单14-7 内部 `ValueStringBuilder` 类的代码片段

```csharp
internal ref struct ValueStringBuilder {
    private char[] _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public ValueStringBuilder(Span<char> initialBuffer) {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }
    public ref char this[int index] => ref _chars[index];
}
```

私有字段 `_pos` 作为游标指示已使用的字符数。通过代码清单14-8中的 `AsSpan` 方法组，可以轻松通过切片返回当前内容（无需任何内存分配）。

代码清单14-8 内部 `ValueStringBuilder` 类的切片功能

```csharp
public ReadOnlySpan<char> AsSpan() => _chars.Slice(0, _pos);
public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);
public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);
```

当需要真正的字符串时，可通过代码清单14-9中的堆分配 `ToString` 方法实现。注意此时会调用 `Dispose` 方法（后文说明），表示该实例已被消费。

代码清单14-9 内部 `ValueStringBuilder` 类的字符串转换功能

```csharp
public override string ToString() {
    string result = _chars.Slice(0, _pos).ToString();
    Dispose();
    return result;
}
```

如代码清单14-10所示，向此类构建器追加内容只需在当前游标位置设置字符（追加字符串时设置多个字符）。当初始`Span<char>`空间不足时，会从`ArrayPool<char>`租用更大数组（参见`Grow`方法）。由于`Span<char>`的存储无关性，只需将新数组赋给同一字段即可。

代码清单14-10 内部 `ValueStringBuilder` 类的追加逻辑

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void Append(char c) {
    int pos = _pos;
    if (pos < _chars.Length) {
        _chars[pos] = c;
        _pos = pos + 1;
    } else {
        GrowAndAppend(c);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
private void GrowAndAppend(char c) {
    Grow(1);
    Append(c);
}

[MethodImpl(MethodImplOptions.NoInlining)]
private void Grow(int additionalCapacityBeyondPos) {
    int minimumLength = (int)Math.Max((uint)(_pos + additionalCapacityBeyondPos), 
        Math.Min((uint)(_chars.Length * 2), 2147483591u));
    char[] array = ArrayPool<char>.Shared.Rent(minimumLength);
    _chars.Slice(0, _pos).CopyTo(array);
    char[] arrayToReturnToPool = _arrayToReturnToPool;
    _chars = (_arrayToReturnToPool = array);
    if (arrayToReturnToPool != null) {
        ArrayPool<char>.Shared.Return(arrayToReturnToPool);
    }
}
```

从数组池中获取的数组应当归还给池。这一操作由 `Dispose` 方法处理（见代码清单14-11）。请注意，虽然该方法命名为 `Dispose`，但 `ValueStringBuilder` 并未实现 `IDisposable` 接口——因为 ref struct 无法实现接口！因此无法使用 `using` 块包装实例，必须显式调用 `Dispose` 方法。

代码清单14-11. `ValueStringBuilder`类的内部片段（释放逻辑）

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void Dispose()
{
    char[] toReturn = _arrayToReturnToPool;
    this = default; // 安全措施：避免在错误地再次追加内容时使用已归还的数组
    if (toReturn != null) // 若原始stackalloc缓冲区足够大则无需操作
    {
        ArrayPool<char>.Shared.Return(toReturn);
    }
}
```

使用 `ValueStringBuilder` 非常简单。只需准备初始存储空间（如小型 `stackalloc` 缓冲区）并传递给其构造函数（见代码清单14-12）。

代码清单14-12. `ValueStringBuilder` 使用示例

```csharp
public string UseValueStringBuilder()
{
    Span<char> initialBuffer = stackalloc char[40];
    var builder = new ValueStringBuilder(initialBuffer);
    
    // 使用builder.Append(...)的逻辑
    string result = builder.ToString();
    builder.Dispose();
    return result;
}
```

`ValueStringBuilder` 是综合运用多种现代技术的绝佳范例：ref struct、ref 返回、Span<T>、ArrayPool<T> 以及最常用的stackalloc。通过阅读其[源码](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs)（可在.NET运行时 GitHub 仓库找到）能有效掌握这些技术。

> .NET代码库中还有个非常相似的 [ValueListBuilder](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ValueListBuilder.cs) 结构体，建议您阅读其实现！

受 `Span<T>` 灵活性的吸引，您可能会想出如代码清单14-13所示的简洁方案：小于特定阈值时使用 `stackalloc` 分配缓冲区，较大时则使用 `ArrayPool`。虽然这段代码看起来优雅且能通过编译，但存在严重缺陷——无法将数组归还给池（无法从 `Span<T>` 实例还原原始数组）！

代码清单14-13. 尝试提供条件性本地缓冲区分配的简洁方案（不可行）

```csharp
private const int StackAllocSafeThreshold = 128;
public void UseSpanNotWisely(int size)
{
    Span<int> span = size < StackAllocSafeThreshold 
        ? stackalloc int[size] 
        : ArrayPool<int>.Shared.Rent(size);
    for (int i = 0; i < size; ++i)
        Console.WriteLine(span[i]);
    // ArrayPool<int>.Shared.Return(??); // 无法归还数组
}
```

前文展示的 `ValueStringBuilder` 解决的正是类似问题（还额外实现了本地缓冲区的可扩展性）。若您尝试实现类似代码清单14-13的功能，将会遇到C#的语言限制。例如在非 `unsafe` 上下文中，不能将 `stackalloc` 结果赋值给已定义的变量（只能在初始化时赋值）。因此这种方案需要额外代码，会变得冗长且不够优雅（见代码清单14-14）。不过在.NET基础库中仍可能见到这类代码，因为它确实能实现所需功能（虽然需要借助 `unsafe` 指针操作）。

代码清单14-14. 使用 `unsafe` 的有效实现方案

```csharp
public unsafe void UseSpanWisely(int size)
{
    Span<int> span;
    int[] array = null;
    if (size < StackAllocSafeThreshold)
    {
        int* ptr = stackalloc int[size];
        span = new Span<int>(ptr, size);
    }
    else
    {
        array = ArrayPool<int>.Shared.Rent(size);
        span = array;
    }
    for (int i = 0; i < size; ++i)
        Console.WriteLine(span[i]);
    if (array != null)
        ArrayPool<int>.Shared.Return(array);
}
```

自C# 11起，对 `Span<T>` 变量作用域的限制有所放宽。使用 `unsafe` 关键字时，编译器允许 span 逃逸作用域，并会显示警告而非错误（如代码清单14-15所示）。这些变更对早期C#版本也适用。

代码清单14-15. 更简洁的条件性本地缓冲区分配方案

```csharp
public unsafe void UseSpanWiselyAndConcisely(int size)
{
    Span<int> span;
    int[] array = null;
    if (size < StackAllocSafeThreshold)
    {
        span = stackalloc int[size]; // 警告CS9080：在此上下文中使用变量'span'可能导致引用变量逃逸其声明作用域
    }
    else
    {
        array = ArrayPool<int>.Shared.Rent(size);
        span = array;
    }
    for (int i = 0; i < size; ++i)
        Console.WriteLine(span[i]);
    if (array != null)
        ArrayPool<int>.Shared.Return(array);
}
```

`Span<T>` 的另一个典型应用是通过 `"字符串".AsSpan().Slice(...)` 方法实现非分配性子字符串操作。这是在解析字符串时避免昂贵 `string.Substring` 调用的绝佳方案。

### 探秘 Span<T>

在了解了 `Span<T>` 的各种用法示例后，我们来深入探讨其实现原理。虽然初看并不明显，但其内部实现相当复杂，涉及诸多CLR底层机制的关键设计。因此我们将逐步详细解析 `Span<T>` 背后的设计决策——作为当前.NET生态变革的核心，理解这些设计至关重要。

出于性能考虑，最好使用结构体（避免堆分配）。由于它可能表示栈分配的内存（如 `stackalloc`），其本身绝不能出现在堆上（否则可能超出其包装内存的生命周期）。即使不考虑性能因素，也必须使用栈分配的结构体并确保其不会被装箱（这是第一个设计难点）。

- 作为内存区域的表示，它需要存储两个信息：指针（地址）和长度。
- `Span<T>` 可能表示托管数组的子区间（例如切片操作），因此指针可能指向托管对象内部——如果您联想到“内部指针”（interior pointer）就对了！实际上最理想的指针类型就是托管指针（可指向对象内部）。但您可能记得托管指针仅允许用于局部变量、参数和返回值，不能作为字段——即使是结构体字段也不行，因为结构体可能被装箱（第三个设计难点）。

这些要点构成了 `Span<T>` 最核心的设计考量。进一步分析，若能满足以下两个条件，上述三个难题将迎刃而解：

- 存在仅能栈分配的类型——这样就能安全存储栈地址，且默认单线程使用无需考虑线程安全问题
- 允许在 `Span<T>` 字段中使用托管指针——这样就能以安全方式访问任意内存类型

若您没有跳过第13章，可能已经意识到我们描述的正是...ref 结构体！这类 byref-like 类型完美契合需求（事实上它们就是为 `Span<T>` 而引入的）。更妙的是，byref-like 类型无需运行时修改，主要工作由C#编译器完成，其IL代码与当前.NET和.NET Framework 完全向后兼容。因此第一个条件已满足。

第二个条件更为苛刻。有了 byref-like 类型后，自然会想到 byref-like 实例字段——由于二者的限制特性相似，托管指针理应能作为 byref-like 类型的成员字段。遗憾的是，当前C#和CIL都不支持此类字段，必须修改运行时。为此专门为 `Span<T>` 引入了新的内在类型（runtime实现）来表示这类 byref-like 实例字段。因此第二个条件仅在.NET Core 2.1及更高版本中才能满足。

当第二个条件不满足时，可通过变通方案实现（稍后展示）。这导致存在两种 `Span<T>` 实现版本：

- “慢速版”：运行在.NET Framework和.NET Core 2.1之前的兼容版本，无需运行时修改。由于向后兼容风险，.NET Framework 可能永远不会支持新特性。
- “快速版”：利用.NET Core 2.1引入的 byref-like 实例字段支持的版本。

不必过分关注“快慢”命名——两者都很快，尽管“慢速版”比后者慢一倍。清单14-16的基准测试及14-17的结果清晰表明：

-  NET 8.0的“快速版” `Span<T>` 性能优于常规.NET数组。
-  .NET Framework的“慢速版” `Span<T>` 慢约50%。

但要注意，这个精心设计的基准测试仅聚焦索引器数据访问。实际应用场景的性能差异会更小。

清单14-16. 使用 `Span` 进行访问时间的简单基准测试（“.NET Framework版”为慢速实现，“.NET Core版”为快速实现），并与常规数组进行对比

```c#
public class SpanBenchmark
{
    private byte[] array;
    [GlobalSetup]
    public void Setup()
    {
        array = new byte[128];
        for (int i = 0; i < 128; ++i)
        array[i] = (byte)i;
    }
    [Benchmark]
    public int SpanAccess() 
    {
        var span = new Span<byte>(this.array);
        int result = 0;
        for (int i = 0; i < 128; ++i) 
        {
            result += span[i];
        }
        return result;
    }

    [Benchmark]
    public int ArrayAccess() 
    {
        int result = 0;
        for (int i = 0; i < 128; ++i) 
        {
            result += this.array[i];
        }
        return result;
    }
}
```

清单14-17. 来自清单14-16的BenchmarkDotNet测试结果

| 方法        | 运行平台           | 平均耗时 | 误差     | 内存分配 |
| ----------- | ------------------ | -------- | -------- | -------- |
| SpanAccess  | .NET 8.0           | 50.66 ns | 0.386 ns | -        |
| ArrayAccess | .NET 8.0           | 63.23 ns | 0.540 ns | -        |
| SpanAccess  | .NET Framework 4.8 | 96.56 ns | 1.813 ns | -        |
| ArrayAccess | .NET Framework 4.8 | 66.49 ns | 1.092 ns | -        |

接下来我们将详细剖析两个版本的实现，重点关注从托管/非托管内存的构造过程以及索引器实现。

> 后续代码清单会频繁使用 `Unsafe` 类。这个通用类提供内存和指针的底层操作（本章稍后简要介绍）。示例中的用法非常直观——主要用于类型转换和简单指针运算。

### 慢速 Span

“慢速 Span”无法使用类似 byref 的字段。为了模拟作为字段的内部指针，它需要同时存储对象引用和对象内部的偏移量（见代码清单 14-18）。保留对象引用可避免产生 GC 漏洞——当包裹在 `Span<T>` 中时，需要通过该引用来保持对象可达性。`Span<T>` 还会存储长度信息。

代码清单 14-18　.NET Framework 中“慢速” `Span<T>` 的声明

```csharp
public readonly ref partial struct Span<T> 
{ 
    private readonly Pinnable<T> _pinnable; 
    private readonly IntPtr _byteOffset; 
    private readonly int _length; 
}
// 这个类存在的唯一目的，就是让任意对象都能通过非安全转换来获取用户数据起始位置的引用
[StructLayout(LayoutKind.Sequential)]
internal sealed class Pinnable<T> 
{ 
    public T Data; 
}
```

那么从托管数据和非托管数据构造 `Span<T>` 的过程是怎样的呢？包裹托管数组的操作非常直观（见代码清单 14-19）。这里会存储数组的完整引用（确保 GC 能发现并避免回收该数组），以及数组数据起始位置的偏移量（即 `ArrayAdjustment` 实际返回的值），在数组切片情况下还会进行适当的偏移调整。

代码清单 14-19　从托管数组构造“慢速” Span

```csharp
public Span(T[] array) 
{ 
    _length = array.Length; 
    _pinnable = Unsafe.As<Pinnable<T>>(array); 
    _byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment; 
}
public Span(T[] array, int start, int length) 
{ 
    _length = length; 
    _pinnable = Unsafe.As<Pinnable<T>>(array); 
    _byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add<T>(start); // Add 方法执行指针运算
}
```

包裹非托管内存的操作更为简单，因为不需要关心对象引用问题（见代码清单 14-20）。只需保存长度和地址信息。

代码清单 14-20　从非托管内存构造“慢速” Span

```csharp
public unsafe Span(void* pointer, int length) 
{ 
    _length = length; 
    _pinnable = null; 
    _byteOffset = new IntPtr(pointer); 
}
```

“慢速 Span”的索引器需要进行更多计算——对于托管数组，它需要在对象地址基础上添加数据起始的字节偏移量，以及指定索引处元素的字节偏移量（见代码清单 14-21）。

代码清单 14-21 “慢速” Span 的索引器实现

```csharp
public ref T this[int index]
{
    get 
    { 
        if (_pinnable == null) 
            unsafe 
            { 
                return ref Unsafe.Add<T>(ref Unsafe.AsRef<T>(_byteOffset.ToPointer()), index); 
            } 
        else 
            return ref Unsafe.Add<T>(ref Unsafe.AddByteOffset<T>(ref _pinnable.Data, _byteOffset), index); 
    } 
}
```

> 若想深入研究“慢速” Span 的源代码，建议反编译  `System.Memory` nuget 包进行分析。

### 快速 Span

“快速 Span”利用了运行时对类 byref 字段的支持。得益于 byref 字段，这一版 `Span<T>` 的实现更为简洁。无论托管还是非托管数据都由 byref 字段持有（见代码清单 14-22）。由于垃圾回收器（GC）支持托管（内部）指针，当 `Span<T>` 仍在使用时，相关托管对象不会被回收的风险。

代码清单 14-22　从托管和非托管内存构造“快速”Span

```c#
public Span(T[] array)
{
    _reference = ref MemoryMarshal.GetArrayDataReference(array);
    _length = array.Length;
}
public Span(T[] array, int start, int length)
{
    _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /*强制零扩展*/);
    _length = length;
}
public unsafe Span(void* pointer, int length)
{
    _reference = ref *(T*)pointer;
    _length = length;
}
```

此外，内存访问变得极其简单，只需非常快速的指针运算即可（见代码清单 14-23）——这使得性能可与常规数组媲美。

代码清单 14-23 “快速”Span的索引器实现

```c#
public ref T this[int index]
{

    [Intrinsic]
    get { 
        return ref Unsafe.Add(ref _reference, (nint)(uint)index); 
    }
}
```

性能差异的另一来源是.NET Core中JIT编译器的改进。特别是它在消除边界检查（例如循环中的检查）方面表现更优。另一个区别在于“快速”Span体积更小，因此按值传递时的开销更低。

从GC开销的角度来看，“慢速”与“快速”Span其实截然相反。“慢速版”直接持有对象引用（当包装托管对象时），因此遍历速度更快；而“快速版”包含需要GC遍历扫描的内部指针，解引用速度稍慢。但实践中这种差异微不足道，很难想象存在大量存活 `Span<T>` 的应用会因此产生显著影响。

> 自C# 11起，ref结构体(ref struct)中已可使用ref字段。但更通用的 byref-like字段（类中也可使用）呢？这类可能引入堆到堆内部指针的特性不太可能被加入。如前所述，与运行时解析它们的开销相比，其带来的收益实在有限。

> **慢速 Span VS 快速 Span 实质上的区别**
>
> **byref 字段**是 .NET 运行时级别的特性，允许在结构体中直接存储**对内存位置的引用**，而不是存储对象引用+偏移量的组合。
>
> ```c#
> // 概念对比：
> // 传统字段：存储"值"或"对象引用"
> public struct TraditionalStruct
> {
>     private object _objectRef;    // 存储对象引用
>     private int _value;          // 存储值
> }
> 
> // byref 字段：直接存储"内存位置的引用"
> public ref struct ByRefStruct  
> {
>     private ref T _reference;    // 直接指向内存中的某个 T 实例
> }
> ```
>
> 慢速 Span 是通过三个属性来存储对象：
>
> ```c#
> public readonly ref partial struct SlowSpan<T> 
> { 
>     private readonly Pinnable<T> _pinnable;  // 8字节：对象引用
>     private readonly IntPtr _byteOffset;     // 8字节：偏移量
>     private readonly int _length;            // 4字节：长度
> }
> ```
>
> 这里面就需要 20字节（+ 4字节对齐填充 = 24字节）。在访问元素时，需要三步：1. 获取 _pinnable 引用。2.计算位置。3. 解引用（UnSafe）。
>
> 而快速 Span 直接通过 byref 存储对象内存位置的引用：
>
> ```c#
> public readonly ref partial struct FastSpan&lt;T&gt;
> {
>     private ref readonly T _reference;   // 8字节：直接的内存引用
>     private readonly int _length;        // 4字节：长度  
> }
> ```
>
> 这里只需要 12 字节 + 4字节的对齐填充 = 16字节。在访问元素时，只需要直接使用 _reference 和指针位置计算。
>
> 在汇编指令上也可以看出明显区别：
>
> ```
> ; 慢速 Span (伪汇编)
> mov rdx, [rax+8]        ; 访问 _pinnable.Data  
> mov rcx, [rbp-16]       ; 加载 _byteOffset
> add rdx, rcx            ; 计算基地址
> lea rax, [rdx+2*4]      ; 计算最终地址 (index * sizeof(T))
> ```
>
> ```
> ; 快速 Span (伪汇编) 
> mov rax, [rbp-8]        ; 直接加载 _reference
> lea rax, [rax+2*4]      ; 计算最终地址 (index * sizeof(T))
> ```

