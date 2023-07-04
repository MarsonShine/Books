# 矢量化简介 Vector128 和 Vector256

**矢量化（vectorization）**是将算法从每次迭代对单个值进行操作转换为每次迭代对一组值（向量）进行操作的艺术。它可以以**增加代码复杂性为代价大大提高性能。**

在最近的版本中，.NET 引入了许多用于矢量化的新 API。其中绝大多数是特定于硬件的，因此它们要求用户提供每个处理器体系结构（例如 x86、x64、Arm64、WASM 或其他平台）的实现，并可以选择对执行代码的硬件使用最佳指令。

.NET 7 为 `Vector64<T>` 、 `Vector128<T>` 和 `Vector256<T>` 引入了一组新的 API，用于编写与硬件无关的跨平台矢量化代码。同样，.NET 8 引入了 `Vector512<T>` 。本文档旨在向您介绍新的 API 并提供一组最佳实践。

## 代码架构

`Vector128<T>` 是支持矢量化的所有平台的“公分母（common denominator）”（预计情况始终如此）。它表示包含类型 `T` 元素的 128 位向量。

`T` 被限制为特定的基元类型：

- `byte` and `sbyte` (8 位).
  `byte` 和 `sbyte`（8 位）。
- `short` and `ushort` (16 位).
  `short` 和 `ushort`（16 位）。
- `int`, `uint` and `float` (32 位).
  `int` 、 `uint` 和 `float`（32 位）。
- `long`, `ulong` and `double` (64 位).
  `long` 、 `ulong` 和 `double`（64 位）。
- `nint` and `nuint` (32 或 64 位, 具体取决于体系架构, 在 .NET 7+ 中可用)
  `nint` 和 `nuint`（32 或 64 位，具体取决于体系结构，在 .NET 7+ 中可用）

.NET 8 引入了一个 `Vector128<T>.IsSupported` ，指示给定的 `T` 是否支持矢量化，以帮助确定每个运行时（包括来自泛型上下文）的工作。

单个 `Vector128` 操作允许您操作：16（s）字节、8（u）短整型、4（u）整数/浮点数或 2（u）长整型/双精度数。

```
------------------------------128-bits---------------------------
|             64                |               64              |
-----------------------------------------------------------------
|      32       |      32       |      32       |      32       |
----------------------------------------------------------------|
|  16   |  16   |  16   |  16   |  16   |  16   |  16   |  16   |
-----------------------------------------------------------------
| 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 |
-----------------------------------------------------------------
```

`Vector256<T>` 是 `Vector128<T>` 的两倍，所以当它被硬件加速、数据足够大、并且基准测试证明它提供了更好的性能时，你应该考虑使用它而不是 `Vector128<T>` 。对代码进行基准测试可能很重要，因为并非所有平台都以相同的方式处理较大的向量。

例如，x86/x64 上的 `Vector256<T>` 通常被视为 `2x Vector128<T>` 而不是 `1x Vector256<T>` ，其中每个 `Vector128<T>` 被视为一个“通道（lane）”。对于大多数操作，这不会提供任何其他注意事项，它们仅对向量的单个元素进行操作。但是，某些操作可能会“交叉通道（cross lanes）”，例如随机操作或成对操作，并且可能需要额外的开销来处理。

例如，考虑 `Add(Vector128<float> lhs, Vector128<float> rhs)` ，您最终有效地执行（伪代码）：

```
result[0] = lhs[0] + rhs[0];
result[1] = lhs[1] + rhs[1];
result[2] = lhs[2] + rhs[2];
result[3] = lhs[3] + rhs[3];
```

使用这种算法，我们拥有什么大小的向量并不重要，因为我们访问的是输入向量的相同索引，而且一次只能访问一个。因此，无论我们有 `Vector128<T>` 还是 `Vector256<T>` 或 `Vector512<T>` ，它们的运行方式都是一样的。

您可能会注意到，如果扩展到在单个 256 位向量上运行，此算法将改变行为（注意 `result[2]` 现在是 `lhs[4] + lhs[6]` 而不是 `rhs[0] + rhs[1]` ）：

```
// process left
result[0] = lhs[0] + lhs[1];
result[1] = lhs[2] + lhs[3];
result[2] = lhs[4] + lhs[5];
result[3] = lhs[6] + lhs[7];
// process right
result[4] = rhs[0] + rhs[1];
result[5] = rhs[2] + rhs[3];
result[6] = rhs[4] + rhs[5];
result[7] = rhs[6] + rhs[7];
```

由于此行为会更改，因此 x86/x64 平台选择将操作视为 `2x Vector128<float>` 输入，从而为您提供：

```
// process lower left
result[0] = lhs[0] + lhs[1];
result[1] = lhs[2] + lhs[3];
// process lower right
result[2] = rhs[0] + rhs[1];
result[3] = rhs[2] + rhs[3];
// process upper left
result[4] = lhs[4] + lhs[5];
result[5] = lhs[6] + lhs[7];
// process upper right
result[6] = rhs[4] + rhs[5];
result[7] = rhs[6] + rhs[7];
```

这最终会保留行为，并使从 `128-bit` 过渡到 `256-bit` 或更高变得更加容易，因为您实际上只是再次**循环展开（unrolling the loop）**。但是，这确实意味着，如果您需要真正同时执行涉及上车道和下车道的任何操作，则某些算法可能需要额外的处理。此处确切的额外费用取决于正在执行的操作、底层硬件支持的内容以及稍后将更详细介绍的其他几个因素。

## 检查硬件加速

若要检查给定的向量大小是否经过硬件加速，请在相关的非泛型向量类上使用 `IsHardwareAccelerated` 属性。例如，`Vector128.IsHardwareAccelerated` 或 `Vector256.IsHardwareAccelerated`。请注意，即使加速了矢量大小，仍可能有一些操作不是硬件加速的；例如，浮点除法可以在某些硬件上加速，而整数除法则不能。

输入的大小也很重要。它至少需要具有单个向量的大小才能执行矢量化代码路径（有一些高级技巧可以让您对较小的输入进行操作，但我们不会在这里描述它们）。`Count` 属性（例如 `Vector128<T>.Count` 或 `Vector256<T>.Count` ）返回单个向量中给定类型 T 的元素数。

当 `Vector256` 加速时，`Vector128` 通常也会加速，但不能保证这一点。最佳做法是始终显式检查 `IsHardwareAccelerated`。您可能想缓存 `IsHardwareAccelerated` 和 `Count` 属性中的值，但这不是必需的，也不建议这样做。实时编译器将 `IsHardwareAccelerated` 和 `Count` 转换为常量，无需调用任何方法即可检索信息。

### 示例代码结构

```c#
void CodeStructure(ReadOnlySpan<byte> buffer)
{
    if (Vector256.IsHardwareAccelerated && buffer.Length >= Vector256<byte>.Count)
    {
        // Vector256 code path
    }
    else if (Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<byte>.Count)
    {
        // Vector128 code path
    }
    else
    {
        // non-vectorized && small inputs code path
    }
}
```

为了减少小输入的比较次数，我们可以通过以下方式重新排列它：

```c#
void OptimalCodeStructure(ReadOnlySpan<byte> buffer)
{
    if (!Vector128.IsHardwareAccelerated || buffer.Length < Vector128<byte>.Count)
    { 
        // scalar code path
    } 
    else if (!Vector256.IsHardwareAccelerated || buffer.Length < Vector256<byte>.Count)
    { 
        // Vector128 code path
    } 
    else
    { 
        // Vector256 code path
    }
}
```

两种向量类型提供相同的功能，**但 arm64 硬件不支持 `Vector256`**，因此为了简单起见，我们将在所有示例中使用 `Vector128` 。显示的所有示例还假设小端架构和/或不需要处理字节序。`BitConverter.IsLittleEndian` 可用于（并由 JIT 转换为常数）用于需要考虑字节序的算法。

根据这些假设，文档中显示的所有示例都假定它们作为以下 `if` 块的一部分执行：

```c#
else if (Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<byte>.Count)
{
    // Vector128 code path
}
```

### 测试

这样的代码结构要求我们测试所有可能的代码路径：

- `Vector256` 加速：
  - 输入足够大，可以从 `Vector256` 的矢量化中受益。
  - 输入不够大，无法从 `Vector256` 的矢量化中受益，但它可以从 `Vector128` 的矢量化中受益。
  - 输入太小，无法从任何类型的矢量化中受益。
- `Vector128` 加速：
  - 输入足够大，可以从 `Vector128` 的矢量化中受益。
  - 输入太小，无法从任何类型的矢量化中受益。
- `Vector128` 和 `Vector256` 都不会加速。

可以根据大小实现涵盖某些方案的测试，但不可能在单元测试级别切换硬件加速。在启动 .NET 进程之前，可以使用环境变量对其进行控制：

- 当 `DOTNET_EnableAVX2` 设置为 `0` 时，`Vector256.IsHardwareAccelerated` 返回 `false`。
- 当 `DOTNET_EnableHWIntrinsic` 设置为 `0` 时，不仅提到的两个 API 都返回 `false`，3 和 `Vector.IsHardwareAccelerated` 也返回 `false`。

假设我们在支持 `Vector256` 的 `x64` 机器上运行测试，我们需要编写涵盖所有大小场景的测试，并使用以下命令运行它们：

- 无自定义设置
- `DOTNET_EnableAVX2=0`
- `DOTNET_EnableHWIntrinsic=0`

另一种方法是在足够多的硬件变体上运行测试以覆盖所有路径。

### 基准测试

所有这些复杂性都需要得到回报。我们需要对代码进行基准测试，以验证投资是否有益。我们可以通过 [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) 做到这一点。

#### 自定义配置

可以定义一个配置，指示工具为所有三种方案运行基准：

```c#
static void Main(string[] args)
{
    Job enough = Job.Default
        .WithWarmupCount(1)
        .WithIterationTime(TimeInterval.FromSeconds(0.25))
        .WithMaxIterationCount(20);

    IConfig config = DefaultConfig.Instance
        .HideColumns(Column.EnvironmentVariables, Column.RatioSD, Column.Error)
        .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig
            (exportGithubMarkdown: true, printInstructionAddresses: false)))
        .AddJob(enough.WithEnvironmentVariable("DOTNET_EnableHWIntrinsic", "0").WithId("Scalar").AsBaseline());

    if (Vector256.IsHardwareAccelerated)
    {
        config = config
            .AddJob(enough.WithId("Vector256"))
            .AddJob(enough.WithEnvironmentVariable("DOTNET_EnableAVX2", "0").WithId("Vector128"));

    }
    else if (Vector128.IsHardwareAccelerated)
    {
        config = config.AddJob(enough.WithId("Vector128"));
    }

    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
        .Run(args, config);
}
```

注意：该配置定义了一个[反汇编器](https://adamsitnik.com/Disassembly-Diagnoser/)，它以 GitHub markdown 格式导出反汇编（在 x64 和 arm64、Windows 和 Linux 上都支持）。在处理需要检查生成的汇编代码的高性能代码时，它通常是一种非常宝贵的工具。

#### 内存对齐

我们可以通过使用 [NativeMemory.AlignedAlloc](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.nativememory.alignedalloc) 来分配对齐的非托管内存。

```c#
public unsafe class Benchmarks
{
    private void* _pointer;

    [Params(6, 32, 1024)] // test various sizes
    public uint Size;

    [GlobalSetup]
    public void Setup()
    {
        _pointer = NativeMemory.AlignedAlloc(byteCount: Size * sizeof(int), alignment: 32);
        NativeMemory.Clear(_pointer, byteCount: Size * sizeof(int)); // ensure it's all zeros, so 1 is never found
    }

    [Benchmark]
    public bool Contains()
    {
        ReadOnlySpan<int> buffer = new (_pointer, (int)Size);
        return buffer.Contains(1);
    }

    [GlobalCleanup]
    public void Cleanup() => NativeMemory.AlignedFree(_pointer);
}
```

示例结果（请注意摘要中打印的 AVX2、AVX 和 SSE4.2 信息）：

```
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1413/22H2/2022Update/SunValley2)
AMD Ryzen Threadripper PRO 3945WX 12-Cores, 1 CPU, 24 logical and 12 physical cores
.NET SDK=8.0.100-alpha.1.22558.1
  [Host]    : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
  Scalar    : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT
  Vector128 : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX
  Vector256 : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
```

```
|   Method |       Job | Size |       Mean |    StdDev | Ratio | Code Size |
|--------- |---------- |----- |-----------:|----------:|------:|----------:|
| Contains |    Scalar | 1024 | 143.844 ns | 0.6234 ns |  1.00 |     206 B |
| Contains | Vector128 | 1024 | 104.544 ns | 1.2792 ns |  0.73 |     335 B |
| Contains | Vector256 | 1024 |  55.769 ns | 0.6720 ns |  0.39 |     391 B |
```

注意：如您所见，即使是像 [Contains](https://learn.microsoft.com/dotnet/api/system.memoryextensions.contains) 这样简单的方法也没有观察到完美的性能提升：x8 表示 `Vector256`（256/32）和 `x4 表示 1`（128/32）。要了解原因，我们需要使用一个分析器来提供有关 CPU 指令级别的信息，根据硬件的不同，这可能是[英特尔 VTune](https://www.intel.com/content/www/us/en/developer/tools/oneapi/vtune-profiler.html) 或 [amd uprof](https://www.intel.com/content/www/us/en/developer/tools/oneapi/vtune-profiler.html)。

结果应该非常稳定（平面分布），但另一方面，我们正在衡量最佳情况方案的性能（输入很大，对齐，并且搜索其全部内容，因为从未找到该值）。

解释基准测试设计指南超出了本文档的范围，但我们有一个[专门的文档](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md#benchmarks-are-not-unit-tests)。长话短说，您应该对生产环境的所有现实方案进行基准测试，以便您的客户实际上可以从您的改进中受益。

**内存随机化（Memory randomization）**

另一种方法是启用内存随机化。在每次迭代之前，工具将分配随机大小的对象，使它们保持活动状态并重新运行应分配实际内存的设置。

您可以在[此处](https://github.com/dotnet/BenchmarkDotNet/pull/1587)阅读有关它的更多信息。它需要了解什么是分布以及如何阅读它。这也超出了本文档的范围，但一本关于统计的书，如 [Pro .NET 基准测试](https://aakinshin.net/prodotnetbenchmarking/)可以帮助你很好地理解这个主题。

无论您打算如何对代码进行基准测试，您都需要记住，输入越大，您从矢量化中受益就越多。如果您的代码使用较小的缓冲区，性能甚至可能会变差。

## 循环

要使用大于单个向量的输入，通常需要遍历整个输入。这应该分为两部分：

- 一次对多个值进行操作的矢量化循环
- 余数的处理

示例：我们的输入是一个十个整数的缓冲区，假设 `Vector128` 被加速，我们在第一次循环迭代中处理前四个值，在第二次迭代中处理接下来的四个值，然后我们停止，因为只剩下两个。根据我们如何处理其余部分，我们区分两种方法。

### 标量余数处理

想象一下，我们要计算给定缓冲区中所有数字的总和。我们绝对希望每个元素只添加一次，没有重复。这就是为什么在第一个循环中，我们在一次迭代中添加四个（128 位 / 32 位）整数。在第二个循环中，我们处理剩余的值。

```c#
int Sum(Span<int> buffer)
{
    Debug.Assert(Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<int>.Count);

    // 初始和是零，所以我们需要一个所有元素都初始化为零的向量。
    Vector128<int> sum = Vector128<int>.Zero;

    // 我们需要获得缓冲区中第一个值的引用，它将在后面用于从内存加载向量。
    ref int searchSpace = ref MemoryMarshal.GetReference(buffer);
    // 还有一个偏移量，这将被矢量和标量循环所使用。
    nuint elementOffset = 0;
    // 以及最后一个有效的偏移量，我们可以从中加载数值
    nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
    for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector128<int>.Count)
    {
        // 我们从给定的偏移量加载一个向量。
        Vector128<int> loaded = Vector128.LoadUnsafe(ref searchSpace, elementOffset);
        // We add 4 integers at a time:
        sum += loaded;
    }

    // 我们将向量中的所有4个整数相加到一个中去
    int result = Vector128.Sum(sum);

    // 并处理剩余的元素，以非矢量的方式：
    while (elementOffset < (nuint)buffer.Length)
    {
        result += buffer[(int)elementOffset];
        elementOffset++;
    }

    return result;
}
```

注意：使用 `ref MemoryMarshal.GetReference(span)` 而不是 `ref span[0]`，使用 `ref MemoryMarshal.GetArrayDataReference(array)` 而不是 `ref array[0]` 来处理空缓冲区场景（会抛出 `IndexOutOfRangeException`）。如果缓冲区为空，这些方法将返回对存储第 0 个元素的位置的引用。此类引用可能为空，也可能不为空。你可以使用它进行固定，但绝不能取消引用它。

注意： `GetReference` 方法有一个重载，它接受 `ReadOnlySpan` 并返回可变引用。请谨慎使用！要获取 `readonly` 引用，您可以使用 ReadOnlySpan.GetPinnableReference 或只执行以下操作：

```c#
ref readonly T searchSpace = ref MemoryMarshal.GetReference(buffer);
```

注意：请记住，`Vector128.Sum` 是静态方法。`Vectior128<T>` 和 `Vector256<T>` 同时提供实例方法和静态方法（像 `+` 这样的运算符只是 C# 中的静态方法）。 `Vector128` 和 `Vector256` 是仅具有静态方法的非泛型静态类。在搜索方法时了解它们的存在很重要。

### 矢量化余数处理

有一些场景和高级技术可以允许矢量化余数处理，而不是诉诸上面所示的非矢量化方法。一些算法可以使用回溯方法来加载另一个向量的元素，并屏蔽已经处理过的元素。对于幂等算法，最好只是回溯并处理最后一个向量，根据需要对元素重复操作。

在下面的示例中，我们需要检查给定的缓冲区是否包含特定数字;多次处理值是完全可以接受的。缓冲区包含六个 32 位整数，`Vector128` 表示加速，一次可以处理四个整数。在第一次循环迭代中，我们处理前四个元素。在第二次（也是最后一次）迭代中，我们需要处理剩余的两个元素。由于余数小于 `Vector128` 并且我们没有改变输入，因此我们对包含最后四个元素的 `Vector128` 执行矢量化操作。

```c#
bool Contains(Span<int> buffer, int searched)
{
    Debug.Assert(Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<int>.Count);

    Vector128<int> loaded;
    // 我们需要一个矢量来存储搜索到的值。
    Vector128<int> values = Vector128.Create(searched);

    ref int searchSpace = ref MemoryMarshal.GetReference(buffer);
    nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
    nuint elementOffset = 0;
    for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector128<int>.Count)
    {
        loaded = Vector128.LoadUnsafe(ref searchSpace, elementOffset);
        // 将加载的向量与搜索到的值向量进行比较
        if (Vector128.Equals(loaded, values) != Vector128<int>.Zero)
        {
            return true; // 如果发现有差异，返回true
        }
    }

    // 如果还有任何元素，则处理搜索空间中的最后一个向量
    if (elementOffset != (uint)buffer.Length)
    {
        loaded = Vector128.LoadUnsafe(ref searchSpace, oneVectorAwayFromEnd);
        if (Vector128.Equals(loaded, values) != Vector128<int>.Zero)
        {
            return true;
        }
    }

    return false;
}
```

`Vector128.Create(value)` 创建一个新向量，其中所有元素初始化为指定值。所以 `Vector128<int>.Zero` 相当于 `Vector128.Create(0)`。

`Vector128.Equals(Vector128 left, Vector128 right)` 比较两个向量并返回一个向量，其中每个元素要么是全位集，要么是零，具体取决于 left 和 right 中的相应元素是否相等。如果比较结果不为零，则表示至少有一个匹配项。

### 访问冲突测试（Access violation（AV）testing）

以无效方式处理其余部分可能会导致不确定且难以诊断的问题。

让我们看一下下面的代码：

```c#
nuint elementOffset = 0;
while (elementOffset < (nuint)buffer.Length)
{
    loaded = Vector128.LoadUnsafe(ref searchSpace, elementOffset); // BUG!

    elementOffset += (nuint)Vector128<int>.Count;
}
```

对于六个整数的缓冲区，循环将执行多少次？两次！第一次它将加载前四个元素，但第二次它将加载缓冲区之后内存的随机内容！

编写检测该问题的测试很困难，但并非不可能。.NET 团队使用名为 [BoundedMemory](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/TestUtilities/System/Buffers/BoundedMemory.Creation.cs) 的帮助程序实用工具，该实用工具分配一个内存区域，该区域紧跟在有害（`MEM_NOACCESS`）页之前或后面紧跟。尝试读取紧接在内存之前或之后的内存会导致 `AccessViolationException`。

## 加载和存储矢量

### 加载

`Vector128` 和 `Vector256` 都提供了至少五种从内存加载它们的方法：

```c#
public static class Vector128
{
    public static Vector128<T> Load<T>(T* source) where T : unmanaged
    public static Vector128<T> LoadAligned<T>(T* source) where T : unmanaged
    public static Vector128<T> LoadAlignedNonTemporal<T>(T* source) where T : unmanaged
    public static Vector128<T> LoadUnsafe<T>(ref T source) where T : struct
    public static Vector128<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct
}
```

前三个重载需要指向源的指针。为了能够以安全的方式使用指向托管缓冲区的指针，需要首先固定缓冲区。这是因为 GC 无法跟踪非托管指针。它需要帮助来确保它在使用它时不会移动内存，因为指针会静默地变为无效。这里棘手的部分是正确执行指针算术：

```c#
unsafe int UnmanagedPointersSum(Span<int> buffer)
{
    fixed (int* pBuffer = buffer) // 固定非托管内容，防止因 GC 移动了内存地址
    {
        int* pEnd = pBuffer + buffer.Length;
        int* pOneVectorFromEnd = pEnd - Vector128<int>.Count;
        int* pCurrent = pBuffer;

        Vector128<int> sum = Vector128<int>.Zero;

        while (pCurrent <= pOneVectorFromEnd)
        {
            sum += Vector128.Load(pCurrent);

            pCurrent += Vector128<int>.Count;
        }

        int result = Vector128.Sum(sum);

        while (pCurrent < pEnd)
        {
            result += *pCurrent;

            pCurrent++;
        }

        return result;
    }
}
```

`LoadAligned` 和 `LoadAlignedNonTemporal` 要求输入对齐。对齐的读取和写入应该稍微快一些，但使用它们的代价是增加复杂性。“非临时（NonTemporal）”意味着允许（但不是必需）硬件绕过缓存。非临时读取在处理大量数据时提供了加速，因为它避免了用永远不会再次使用的值重复填充缓存。

> “非临时（NonTemporal）”指的是数据不被设计用于 CPU 缓存，因为数据只被处理一次，很少被再次重复使用，因此不需要缓存。非临时读取在处理大量数据时提供了加速，因为它避免了用永远不会再次使用的值重复填充缓存。非临时读取允许硬件绕过缓存，直接从内存读取数据。

目前，.NET 仅公开一个用于分配非托管对齐内存的 API：[NativeMemory.AlignedAlloc](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.nativememory.alignedalloc)。将来，我们可能会提供一个专用的 API 来分配托管、对齐和固定的内存缓冲区。

创建对齐缓冲区的替代方法（我们并不总是控制输入）是固定缓冲区，找到第一个对齐的地址，处理未对齐的元素，然后开始对齐循环，然后处理其余元素。在我们的代码中添加这种复杂性可能并不总是值得的，需要通过各种硬件上的适当基准测试来证明。

第四种方法只需要托管引用（`ref T source`）。我们不需要固定缓冲区（GC 正在跟踪托管引用并在内存移动时更新它们），但它仍然需要我们正确处理托管指针算法：

```c#
int ManagedReferencesSum(int[] buffer)
{
    Debug.Assert(Vector128.IsHardwareAccelerated && buffer.Length >= Vector128<int>.Count);

    ref int current = ref MemoryMarshal.GetArrayDataReference(buffer);
    ref int end = ref Unsafe.Add(ref current, buffer.Length);
    ref int oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<int>.Count);

    Vector128<int> sum = Vector128<int>.Zero;

    while (!Unsafe.IsAddressGreaterThan(ref current, ref oneVectorAwayFromEnd))
    {
        sum += Vector128.LoadUnsafe(ref current);

        current = ref Unsafe.Add(ref current, Vector128<int>.Count);
    }

    int result = Vector128.Sum(sum);

    while (Unsafe.IsAddressLessThan(ref current, ref end))
    {
        result += current;

        current = ref Unsafe.Add(ref current, 1);
    }

    return result;
}
```

注意：`Unsafe` 不会公开名为 `IsLessThanOrEqualTo` 的方法，因此我们使用否定 `Unsafe.IsAddressGreaterThan` 来达到预期效果。

**指针算法总是会出错，即使您是一位经验丰富的工程师，并且从 .NET 架构师那里获得了非常详细的代码审查**。在 [#73768](https://github.com/dotnet/runtime/pull/73768) 中引入了 GC 孔。代码看起来很简单：

```c#
ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

do
{
    equals = Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace));
    if (equals == Vector128<TValue>.Zero)
    {
        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
        continue;
    }

    return ...;
}
while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));
```

这是 `LastIndexOf` 实现的一部分，我们从缓冲区的末尾迭代到开头。在循环的最后一次迭代中，`currentSearchSpace` 可能成为指向缓冲区开始前的未知内存的指针：

```c#
currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
```

直到 GC 在那之后立即启动，在内存中移动对象，更新所有有效的托管引用并恢复执行，这很好，运行以下条件：

```c#
while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));
```

这可能返回 true，因为 `currentSearchSpace` 无效且未更新。如果您对更多详细信息感兴趣，可以查看 [issue](https://github.com/dotnet/runtime/issues/75792#issuecomment-1249973858) 和[PR](https://github.com/dotnet/runtime/pull/75857)。

这就是为什么**我们建议使用采用托管引用和元素偏移量的重载。它不需要固定或执行任何指针算术。它仍然需要小心，因为通过不正确的偏移会导致GC孔。**

```c#
public static Vector128<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct
```

**我们唯一需要记住的是执行无符号整数算术时潜在的 `nuint` 溢出。**

```c#
Span<int> buffer = new int[2] { 1, 2 };
nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
Console.WriteLine(oneVectorAwayFromEnd);
```

你能猜出结果吗？对于 64 位进程，它是 `FFFFFFFFFFFFFFFE` （ `18446744073709551614` 的十六进制表示）！这就是为什么在进行类似计算之前需要始终检查缓冲区的长度！

### 存储

与加载类似，`Vector128` 和 `Vector256` 都提供了至少五种将它们存储在内存中的方法：

```c#
public static class Vector128
{
    public static void Store<T>(this Vector128<T> source, T* destination) where T : unmanaged
    public static void StoreAligned<T>(this Vector128<T> source, T* destination) where T : unmanaged
    public static void StoreAlignedNonTemporal<T>(this Vector128<T> source, T* destination) where T : unmanaged
    public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination) where T : struct
    public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset) where T : struct
}
```

出于加载所描述的原因，我们建议使用采用**托管引用**和**元素偏移**的重载：

```c#
public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset) where T : struct
```

注意：当从一个缓冲区加载值并将它们存储到另一个缓冲区时，我们需要考虑它们是否重叠。[MemoryExtensions.Overlap](https://learn.microsoft.com/dotnet/api/system.memoryextensions.overlaps#system-memoryextensions-overlaps-1(system-readonlyspan((-0))-system-readonlyspan((-0)))) 是用于执行此操作的 API。

### 转换

如前所述，`Vector128<T>` 和 `Vector256<T>` 被约束为一组特定的基元类型。目前，`char` 不是其中之一，但这并不意味着我们不能使用新的 API 实现矢量化文本操作。对于相同大小的基元类型（以及不包含引用的值类型），**强制转换**是解决方案。

[Unsafe.As<TFrom,TTo>](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.unsafe.as#system-runtime-compilerservices-unsafe-as-2(-0@)) 可用于获取对受支持类型的引用：

```c#
void CastingReferences(Span<char> buffer)
{
    ref char charSearchSpace = ref MemoryMarshal.GetReference(buffer);
    ref short searchSpace = ref Unsafe.As<char, short>(ref charSearchSpace);
    // 现在开始我们就可以使用 Vector128<short> 或 Vector256<short>
}
```

或者 [MemoryMarshal.Cast](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.memorymarshal.cast#system-runtime-interopservices-memorymarshal-cast-2(system-readonlyspan((-0))))，它将一个基元类型的跨度强制转换为另一个基元类型的跨度：

```c#
void CastingSpans(Span<char> chars)
{
    Span<short> shorts = MemoryMarshal.Cast<char, short>(chars);
}
```

还可以从非托管指针获取托管引用：

```c#
void PointerToReference(char* pUtf16Buffer, byte* pAsciiBuffer)
{
    // of the same type:
    ref byte asciiBuffer = ref *pAsciiBuffer;
    // of different types:
    ref ushort utf16Buffer = ref *(ushort*)pUtf16Buffer;
}
```

仅当已知托管引用已固定时，才安全地将托管引用转换为指针。如果不是，则在获得指针后的那一刻，它可能无效。

## 心态

矢量化现实世界的算法在开始时似乎很复杂。软件工程师如何处理复杂的问题？我们把它们分解成子问题，直到这些问题变得足够简单，可以直接解决。

让我们实现一个矢量化方法，用于检查给定的字节缓冲区是否仅由有效的 ASCII 字符组成，以查看如何解决类似的问题。

### 边缘情况

在我们开始实现之前，让我们列出 `IsAcii(ReadOnlySpan<byte> buffer)` 方法的所有边缘情况（理想情况下编写测试）：

- 它不需要抛出任何参数异常，因为 `ReadOnlySpan` 是 `struct` ，它永远不会是 `null` 或无效。
- 对于空缓冲区，它应返回 `true` 。
- 它应检测整个缓冲区中的无效字符，而不考虑缓冲区的长度或其长度是否是矢量宽度的偶数倍。
- 它不应读取不属于提供的缓冲区的任何字节。

### 标量解决方案

一旦我们知道了所有边缘情况，我们就需要了解我们的问题并找到标量解决方案。

ASCII 字符是介于 `0` 到 `127`（含）之间的值。这意味着我们可以通过搜索大于 `127` 的值来找到无效的 ASCII 字节。如果我们将 `byte`（无符号，范围从 0 到 255）视为 `sbyte`（有符号，范围从 -128 到 127），则执行“小于零”检查的问题。

0-127 范围的二进制表示形式如下：

```
00000000
01111111
^
最高有效位
```

当我们查看它时，我们可以意识到另一种方法是检查最高有效位是否等于 `1` 。对于标量版本，我们可以执行逻辑 AND：

```c#
bool IsValidAscii(byte c) => (c & 0b1000_0000) == 0;
```

### 矢量化解决方案

另一个步骤是矢量化我们的标量解决方案，并根据数据选择最佳方法。

如果我们重用前面几节中介绍的循环之一，我们只需要实现一个接受 `Vector128<byte>` 并返回 `bool` 的方法，并且执行与我们的标量方法完全相同的事情，但针对的是向量而不是单个值：

```c#
[MethodImpl(MethodImplOptions.AggressiveInlining)]
bool IsValidAscii(Vector128<byte> vector)
{
    // to perform "> 127" check we can use GreaterThanAny method:
    return !Vector128.GreaterThanAny(vector, Vector128.Create((byte)127))
    // to perform "< 0" check, we need to use AsSByte and LessThanAny methods:
    return !Vector128.LessThanAny(vector.AsSByte(), Vector128<sbyte>.Zero)
    // to perform an AND operation, we need to use & operator
    return (vector & Vector128.Create((byte)0b_1000_0000)) == Vector128<byte>.Zero;
    // we can also just use ExtractMostSignificantBits method:
    return vector.ExtractMostSignificantBits() == 0;
}
```

我们还可以使用特定于硬件的说明（如果可用）：

```c#
if (Sse41.IsSupported)
{
    return Sse41.TestZ(vector, Vector128.Create((byte)0b_1000_0000));
}
else if (AdvSimd.Arm64.IsSupported)
{
    Vector128<byte> maxBytes = AdvSimd.Arm64.MaxPairwise(vector, vector);
    return (maxBytes.AsUInt64().ToScalar() & 0x8080808080808080) == 0;
}
```

对所有可用的解决方案进行基准测试，并选择最适合我们的解决方案。

```c#
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1413/22H2/2022Update/SunValley2)
AMD Ryzen Threadripper PRO 3945WX 12-Cores, 1 CPU, 24 logical and 12 physical cores
.NET SDK=8.0.100-alpha.1.22558.1
  [Host]   : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
```

```
|                     Method | Size |      Mean | Ratio | Code Size |
|--------------------------- |----- |----------:|------:|----------:|
|                     Scalar | 1024 | 252.13 ns |  1.00 |      69 B |
|             GreaterThanAny | 1024 |  32.49 ns |  0.13 |     178 B |
|                LessThanAny | 1024 |  29.33 ns |  0.12 |     146 B |
|                        And | 1024 |  26.13 ns |  0.10 |     138 B |
|                      TestZ | 1024 |  27.26 ns |  0.11 |     129 B |
| ExtractMostSignificantBits | 1024 |  27.33 ns |  0.11 |     141 B |
```

即使是这样一个简单的问题也可以用至少 5 种不同的方式解决，并且每种方式在不同的硬件上的表现都明显不同。使用复杂的特定于硬件的指令并不总是提供最佳性能，因此使用新的 `Vector128` 和 `Vector256` API，我们不需要成为汇编语言专家来编写快速的矢量化代码。

## 工具链

`Vector128`、`Vector128<T>`、`Vector256` 和 `Vector256<T>` 公开了大量 API。我们受到时间的限制，所以我们不会用例子来描述所有这些。相反，我们将它们分组到类别中，以便您大致了解它们的功能。不需要记住这些方法中的每一个都在做什么，但重要的是要记住它们允许的操作类型，并在需要时检查详细信息。

注意：当它们无法在给定平台上进行矢量化时执行，所有这些方法都有“软件回退”。

### 构造

每个向量类型都提供一个 `Create` 方法，该方法接受单个值并返回一个向量，其中所有元素都初始化为此值。

```c#
public static Vector128<T> Create<T>(T value) where T : struct
```

`CreateScalar` 将第一个元素初始化为指定值，其余元素初始化为零。

```c#
public static Vector128<int> CreateScalar(int value)
```

`CreateScalarUnsafe` 类似，但其余元素未初始化。太危险了！

我们还有一个重载，允许指定给定向量中的每个值：

```c#
public static Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
```

最后但并非最不重要的一点是，我们有一个接受缓冲区的 `Create` 重载。它创建一个向量，其元素设置为缓冲区的前 `VectorXYZ<T>.Count` 个元素。不建议在循环中使用它，其中应使用 `Load` 种方法（为了性能）。

```c#
public static Vector128<T> Create<T>(ReadOnlySpan<T> values) where T : struct
```

要在另一个方向上执行复制，我们可以使用 `CopyTo` 扩展方法之一：

```c#
public static void CopyTo<T>(this Vector128<T> vector, Span<T> destination) where T : struct
```

### 位操作

所有特定于大小的矢量类型都为常见位操作提供一组 API。

`BitwiseAnd` 计算两个向量的按位与， `BitwiseOr` 计算两个向量的按位或。它们都可以通过使用相应的运算符（`&` 和 `|`）来表示。`Xor` 也是如此，可以用 `^` 运算符和 `Negate`(`~`) 表示。

注意：在可能的情况下，应首选运算符，因为它有助于避免运算符优先级的错误，并且可以提高可读性。

```c#
public static Vector128<T> BitwiseAnd<T>(Vector128<T> left, Vector128<T> right) where T : struct => left & right;
public static Vector128<T> BitwiseOr<T>(Vector128<T> left, Vector128<T> right) where T : struct => left | right;
public static Vector128<T> Xor<T>(Vector128<T> left, Vector128<T> right) => left ^ right;
public static Vector128<T> Negate<T>(Vector128<T> vector) => ~vector;
```

`AndNot` 计算给定向量的按位与和另一个向量的 1 补码。

```c#
public static Vector128<T> AndNot<T>(Vector128<T> left, Vector128<T> right) => left & ~right;
```

`ShiftLeft` 将矢量的每个元素移位指定的位数。`ShiftRightArithmetic` 执行带符号的右移，`ShiftRightLogical` 执行无符号的移位：

```c#
public static Vector128<sbyte> ShiftLeft(Vector128<sbyte> vector, int shiftCount) => vector << shiftCount;
public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> vector, int shiftCount) => vector >> shiftCount;
public static Vector128<byte> ShiftRightLogical(Vector128<byte> vector, int shiftCount) => vector >>> shiftCount;
```

### 相等

`EqualsAll` 比较两个向量以确定是否所有元素相等。`EqualsAny` 比较两个向量以确定是否有任何元素相等。

```c#
public static bool EqualsAll<T>(Vector128<T> left, Vector128<T> right) where T : struct => left == right;
public static bool EqualsAny<T>(Vector128<T> left, Vector128<T> right) where T : struct
```

`Equals` 比较两个向量以确定它们在每个元素上是否相等。它返回一个向量，其元素为全位集或零，具体取决于 `left` 和 `right` 参数中的相应元素是否相等。

```c#
public static Vector128<T> Equals<T>(Vector128<T> left, Vector128<T> right) where T : struct
```

我们如何计算第一场比赛的索引？让我们仔细看看以下相等性检查的结果：

```c#
Vector128<int> left = Vector128.Create(1, 2, 3, 4);
Vector128<int> right = Vector128.Create(0, 0, 3, 0);
Vector128<int> equals = Vector128.Equals(left, right);
Console.WriteLine(equals);
```

```
<0, 0, -1, 0>
```

`-1` 只是 `0xFFFFFFFF`（全位集）。我们可以用 `GetElement` 来获取第一个非零元素。

```c#
public static T GetElement<T>(this Vector128<T> vector, int index) where T : struct
```

但这不是最佳解决方案。相反，我们应该提取最有效的位：

```c#
uint mostSignificantBits = equals.ExtractMostSignificantBits();
Console.WriteLine(Convert.ToString(mostSignificantBits, 2).PadLeft(32, '0'));
```

```
00000000000000000000000000000100
```

并使用 [BitOperations.TrailingZeroCount](https://learn.microsoft.com/dotnet/api/system.numerics.bitoperations.trailingzerocount) 或 [uint.TrailingZeroCount](https://learn.microsoft.com/dotnet/api/system.uint32.trailingzerocount)（在 .NET 7 中引入）以获取尾随零计数。

要计算最后一个索引，我们应该使用 [BitOperations.LeadingZeroCount](https://learn.microsoft.com/dotnet/api/system.numerics.bitoperations.leadingzerocount) 或 [uint.LeadingZeroCount](https://learn.microsoft.com/dotnet/api/system.uint32.leadingzerocount)（在 .NET 7 中引入）。但是返回值需要从 31 中减去（一个 `uint` 中的 32 位，从 0 开始索引）。

如果我们使用从内存加载的缓冲区（例如：在缓冲区中搜索给定字符的最后一个索引），则两个结果都将相对于提供给用于从缓冲区加载向量的 `Load` 方法的 `elementOffset` 。

```c#
int ComputeLastIndex<T>(nint elementOffset, Vector128<T> equals) where T : struct
{
    uint mostSignificantBits = equals.ExtractMostSignificantBits();

    int index = 31 - BitOperations.LeadingZeroCount(mostSignificantBits); // 31 = 32 (bits in UInt32) - 1 (indexing from zero)

    return (int)elementOffset + index;
}
```

如果我们使用仅接受托管引用的 `Load` 重载，我们可以使用 `Unsafe.ByteOffset(ref T, ref T)` 来计算元素偏移量。

```c#
unsafe int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector128<T> equals) where T : struct
{
    int elementOffset = (int)Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(T);

    uint mostSignificantBits = equals.ExtractMostSignificantBits();
    int index = BitOperations.TrailingZeroCount(mostSignificantBits);
    
    return elementOffset + index;
}
```

