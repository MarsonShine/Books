向量与硬件内在函数支持
===
---

# 引言
CoreCLR 运行时支持多种类型的硬件内在函数（hardware intrinsics），以及多种编译使用这些内在函数的代码的方式。该支持会因目标处理器不同而有所差异，而生成的代码也取决于 JIT 编译器的调用方式。本文档描述了运行时中内在函数的各种行为，并在最后给出对在运行时与库（libraries）部分工作的开发者的影响。

# 缩略语与定义
| 缩略语 | 定义
| --- | --- |
| AOT | Ahead of time（提前编译）。在本文档中，指在进程启动之前编译代码，并将其保存到文件中以供后续使用。

# 内在函数 API
大多数硬件内在函数的支持都与各类 Vector API 的使用相关。运行时支持 4 个主要的 API 面：

- 固定长度的浮点向量：`Vector2`、`Vector3`、`Vector4`。这些向量类型表示由不同长度的 float 组成的结构体。出于类型布局、ABI 以及互操作等目的，它们在表示上与包含相同数量 float 的结构体完全一致。这些向量类型的操作在所有架构与平台上都受支持，不过某些架构可能会对某些操作进行优化。
- 可变长度的 `Vector<T>`：表示运行时决定长度的向量数据。在任意给定进程中，`Vector<T>` 的长度在所有方法里都相同，但该长度可能因机器不同或进程启动时读取的环境变量设置不同而变化。类型参数 `T` 可为以下类型（`System.Byte`、`System.SByte`、`System.Int16`、`System.UInt16`、`System.Int32`、`System.UInt32`、`System.Int64`、`System.UInt64`、`System.Single`、`System.Double`），从而允许在向量中使用整数或 double 数据。`Vector<T>` 的长度与对齐在编译时对开发者而言是未知的（但可在运行时通过 `Vector<T>.Count` API 获取），并且 `Vector<T>` 不能出现在任何互操作签名中。这些向量类型的操作在所有架构与平台上都受支持，不过如果 `Vector<T>.IsHardwareAccelerated` API 返回 true，则某些架构可能会优化某些操作。
- `Vector64<T>`、`Vector128<T>`、`Vector256<T>`、`Vector512<T>` 表示固定大小的向量，和 C++ 中可用的固定大小向量非常相似。这些结构可用于任何可运行的代码，但除创建之外，直接在这些类型上受支持的功能很少。它们主要用于特定处理器的硬件内在函数 API。
- 特定处理器的硬件内在函数 API，例如 `System.Runtime.Intrinsics.X86.Ssse3`。这些 API 直接映射到某条指令或短小的指令序列，并且仅在特定硬件指令集上可用。只有在硬件支持该指令时，这些 API 才可使用。关于其设计，参见 https://github.com/dotnet/designs/blob/master/accepted/2018/platform-intrinsics.md。

# 如何使用内在函数 API

内在函数 API 有 3 种使用模型。

1. 使用 `Vector2`、`Vector3`、`Vector4` 与 `Vector<T>`。对这些类型来说，总是可以安全地直接使用。JIT 会尽其所能为相关逻辑生成尽可能优化的代码，并且会无条件这么做。
2. 使用 `Vector64<T>`、`Vector128<T>`、`Vector256<T>` 与 `Vector512<T>`。这些类型可以无条件使用，但只有在同时使用平台相关的硬件内在函数 API 时，才真正有用。
3. 使用平台内在函数 API。所有对这些 API 的使用都应当包裹在相应类型的 `IsSupported` 检查中。随后，在 `IsSupported` 检查内部即可使用平台特定 API。如果使用了多个指令集，那么应用开发者必须为每个使用到的指令集写对应的检查。

# 使用硬件内在函数对代码生成方式的影响

硬件内在函数会对代码生成（codegen）产生巨大影响，而这些硬件内在函数的 codegen 又取决于编译代码时目标机器可用的 ISA。

如果代码在运行时由 JIT 以即时编译（just-in-time）的方式编译，那么 JIT 会基于当前处理器的 ISA 生成它能生成的最佳代码。对硬件内在函数的这种使用与 JIT 的分层编译（tier）无关。`MethodImplOptions.AggressiveOptimization` 可用于跳过 tier 0 的编译，并始终为该方法生成 tier 1 代码。此外，运行时当前的策略是：`MethodImplOptions.AggressiveOptimization` 也可用于绕过将代码编译为 R2R 代码，尽管未来可能会改变。

对于 AOT 编译，情况要复杂得多。这是由于我们的 AOT 编译模型遵循以下原则。

1. AOT 编译在任何情况下都不得改变代码的语义行为（除了性能变化）。
2. 一旦生成了 AOT 代码，就应当使用它，除非有压倒性的理由避免使用。
3. 必须让误用 AOT 编译工具以违反原则 1 变得极其困难。

## Crossgen2 下硬件内在函数使用模型
编译器已知两组指令集。
- baseline 指令集：默认是 x86-64-v2（SSE、SSE2、SSE3、SSSE3、SSE4.1、SSE4.2、POPCNT），但可通过编译器选项调整。
- optimistic 指令集：默认是（AES、GFNI、SHA、WAITPKG、X86SERIALIZE）。

代码会使用 optimistic 指令集来驱动编译，但凡是使用了超出 baseline 指令集的任何指令集，都会被记录下来；如果尝试使用超出 optimistic 集合的指令集，并且这种尝试在语义上会产生影响，则同样会记录。如果 baseline 指令集包含 `Avx2`，则 `Vector<T>` 的大小与特性就是已知的。其他关于 ABI 的决策也可能被编码进去。例如，很可能 `Vector256<T>` 与 `Vector512<T>` 的 ABI 会因是否支持 `Avx` 而变化。

- 任何使用了 `Vector<T>` 的代码，除非 `Vector<T>` 的大小已知，否则不会被 AOT 编译。
- 在 Linux 或 Mac 机器上，任何将 `Vector256<T>` 或 `Vector512<T>` 作为参数传递的代码，除非已知 `Avx` 指令集是否受支持，否则不会被 AOT 编译。
- 需要比 optimistic 所支持的硬件能力更强的非平台内在函数，不会利用那项能力。`MethodImplOptions.AggressiveOptimization` 可用于禁用这种“次优”代码的编译。
- 利用了 optimistic 集合中指令集的代码，不会在只支持 baseline 指令集的机器上使用。
- 尝试使用 optimistic 集合之外指令集的代码，会生成在支持该指令集的机器上也不会被使用的代码。

#### 由这些规则导致的特性
- 在 optimistic 指令集范围内使用平台内在函数的代码，会生成良好的代码。
- 依赖于不在 baseline 或 optimistic 范围内的平台内在函数的代码，如果运行在确实支持该指令集的硬件上，会带来运行时 JIT 与启动时间方面的担忧。
- `Vector<T>` 代码会带来运行时 JIT 与启动时间方面的担忧，除非提升 baseline 使其包含 `Avx2`。

#### 平台内在函数使用的代码评审规则
- 代码库中任何对平台内在函数的使用，都**应该**用对其关联的 IsSupported 属性的调用进行包裹。该包裹可以在同一个使用硬件内在函数的函数里完成，但只要程序员能够控制所有进入某个使用硬件内在函数的函数的入口点，也并非必须在同一函数中包裹。
- 如果应用开发者非常关心启动性能，应避免使用超出 Sse42 的内在函数，或使用调整了 baseline 指令集支持的 Crossgen。

### 针对 System.Private.CoreLib.dll 的 Crossgen2 规则调整
由于已知 System.Private.CoreLib.dll 会按照下文针对其编写的代码评审规则接受评审，因此可以放宽规则“尝试使用 optimistic 集合之外指令集的代码，会生成在支持该指令集的机器上也不会被使用的代码”。这样做会允许在这些情况下生成非最优代码；但通过代码评审与分析器（analyzers）的约束，这些生成的逻辑仍能正确工作。

#### System.Private.CoreLib.dll 中代码的评审与分析器规则
- 代码库中任何对平台内在函数的使用都**必须**包裹在对其关联 IsSupported 属性的调用中。该包裹**必须**在使用硬件内在函数的同一函数中完成，或使用平台内在函数的函数必须使用 `CompExactlyDependsOn` 属性来表明该函数会无条件调用来自某类型的某些平台内在函数。
- 在单个使用平台内在函数的函数内，除非标记了 `CompExactlyDependsOn` 属性，否则其行为必须在 IsSupported 返回 true 与 false 时完全一致。这样可以让 R2R 编译器在较低的内在函数支持集合下进行编译，同时在存在分层编译时仍然期望该函数的行为不变。
- 过度使用内在函数可能会由于额外的 JIT 而导致启动性能问题，或者由于次优 codegen 而无法达到期望的性能。为解决这一问题，我们未来可能会改变编译规则，以便在启用适当的平台内在函数后再编译标记了 `CompExactlyDependsOn` 的方法。

在构建 `System.Private.CoreLib` 时，分析器会检查 `IsSupported` 属性与 `CompExactlyDependsOn` 属性的正确用法。该分析器要求所有 `IsSupported` 的使用符合少数几种特定模式。这些模式可通过 if 语句或三元运算符实现。

支持的条件检查包括

1. 用简单 if 语句检查 IsSupported 标志并包裹用法
```
if (PlatformIntrinsicType.IsSupported)
{
    PlatformIntrinsicType.IntrinsicMethod();
}
```

2. if 语句检查某个平台内在函数类型（该类型蕴含了所使用内在函数必然受支持）

```
if (Avx2.X64.IsSupported)
{
    Avx2.IntrinsicMethod();
}
```

3. 嵌套 if：外层条件由一组用 OR 连接的、互斥条件的 IsSupported 检查构成；内层检查是 else 分支，其中排除某些检查的适用。

```
if (Avx2.IsSupported || ArmBase.IsSupported)
{
    if (Avx2.IsSupported)
    {
        // Do something
    }
    else
    {
        ArmBase.IntrinsicMethod();
    }
}
```

4. 在一个用 `CompExactlyDependsOn`（较低级属性）标记的方法中，允许对更高级 CPU 特性的 IsSupported 做显式检查。但若如此做，整个函数的行为必须在该 CPU 特性启用与否时保持相同。分析器会将这种用法识别为警告，以便检查在 helper 方法中使用 IsSupported 是否遵循“语义完全等价”的规则。

```
[CompExactlyDependsOn(typeof(Sse41))]
int DoSomethingHelper()
{
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else clause is semantically equivalent
    if (Avx2.IsSupported)
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
    {
        Avx2.IntrinsicThatDoesTheSameThingAsSse41IntrinsicAndSse41.Intrinsic2();
    }
    else
    {
        Sse41.Intrinsic();
        Sse41.Intrinsic2();
    }
}
```

- NOTE: 如果 helper 需要在不同指令集启用时既要被使用、又要表现出不同的行为，那么正确的逻辑要求将 `CompExactlyDependsOn` 属性传播到所有调用方，使得任何调用方都不可能在错误的行为假设下被编译。参见 `Vector128.ShuffleUnsafe` 方法及其各种用法。


`CompExactlyDependsOn` 的行为是：一个方法可以应用 1 个或多个该属性。如果属性指定的任一类型，其关联的 `IsSupported` 属性在运行时不会有不变的结果，那么该方法在 R2R 编译期间将不会被编译或内联到其他函数中。如果没有任何被这样描述的类型会让 `IsSupported` 返回 true，则该方法在 R2R 编译期间同样不会被编译或内联到其他函数中。

5. 除了直接使用 IsSupported 属性来启用/禁用内在函数支持外，还可以使用如下风格的简单静态属性来减少代码重复。

```
static bool IsVectorizationSupported => Avx2.IsSupported || PackedSimd.IsSupported

public void SomePublicApi()
{
    if (IsVectorizationSupported)
        SomeVectorizationHelper();
    else
    {
        // Non-Vectorized implementation
    }
}

[CompExactlyDependsOn(typeof(Avx2))]
[CompExactlyDependsOn(typeof(PackedSimd))]
private void SomeVectorizationHelper()
{
}
```

#### System.Private.Corelib 中的非确定性内在函数

System.Private Corelib 中暴露的某些 API 在不同硬件上是有意保持非确定性的：它们只保证在同一进程范围内的确定性。为支持这类 API，JIT 定义了 `Compiler::BlockNonDeterministicIntrinsics(bool mustExpand)`，应当用它来帮助在诸如 ReadyToRun 的场景下阻止这类 API 的展开。此外，这类 API 应当递归地调用自身，以便间接调用（例如通过委托、函数指针、反射等）也能计算出相同的结果。

一个这类非确定性 API 的例子是 `System.Single` 与 `System.Double` 上暴露的 `ConvertToIntegerNative` API。这些 API 使用底层硬件上最快的机制将源值转换为目标整数类型。之所以存在，是因为 IEEE 754 规范在输入无法装入输出时（例如将 `float.MaxValue` 转成 `int`）将转换行为定义为未指定，从而不同硬件在这些边界情况上历史上会表现出不同的行为。它们面向的是：开发者不需要特别关心边界情况处理、但默认 cast 运算符为了规范化结果带来的性能开销又过高的场景。

另一个例子是各类 `*Estimate` API，例如 `float.ReciprocalSqrtEstimate`。这些 API 允许用户以一定精度损失为代价换取更快的结果，而该精度误差会随输入以及指令在何种硬件上执行而变化。

# JIT 中用于生成正确代码以处理不同指令集支持的机制

JIT 会接收一些标志，用于指示哪些指令集可用；同时它还可以访问新的 jit interface API：`notifyInstructionSetUsage(isa, bool supportBehaviorRequired)`。

notifyInstructionSetUsage API 用于通知 AOT 编译器基础设施：只有当运行时环境与该布尔参数所表示的情况完全一致时，生成的代码才可以执行。例如，若使用 `notifyInstructionSetUsage(Avx, false)`，则生成的代码在 `Avx` 指令集可用时不得被使用。类似地，`notifyInstructionSetUsage(Avx, true)` 表示只有在 `Avx` 指令集可用时，该代码才可以被使用。

虽然存在上述 API，但并不期望 JIT 中的一般用途代码会直接使用它。一般来说，JIT 代码应当使用多种不同的 API 来理解当前可用的硬件指令集支持。

| Api | 使用说明 | 精确行为
| --- | --- | --- |
|`compExactlyDependsOn(isa)`| 当决定“使用或不使用某指令集”会影响生成代码的语义时使用。绝不应在 assert 中使用。 | 返回某指令集是否受支持。并使用该计算结果调用 notifyInstructionSetUsage。
|`compOpportunisticallyDependsOn(isa)`| 当做“机会性”决策（使用或不使用某指令集）时使用：该指令集使用属于“锦上添花的优化机会”，但在 false 可能改变程序语义时不要使用。绝不应在 assert 中使用。 | 返回某指令集是否受支持。如果该指令集受支持，则调用 notifyInstructionSetUsage。
|`compIsaSupportedDebugOnly(isa)` | 用于断言某指令集是否受支持 | 返回某指令集是否受支持。不报告任何信息。仅在 debug 构建中可用。
|`getVectorTByteLength()` | 用于获取一个 `Vector<T>` 值的大小。 | 确定 `Vector<T>` 类型的大小。如果在某架构上其大小可能随规则变化，则使用 `compExactlyDependsOn` 来进行查询，以确保编译时与运行时的大小一致。
|`getMaxVectorByteLength()`| 获取本次编译中 SIMD 类型可能使用的最大字节数。 | 查询受支持的指令集集合，并确定支持的最大 SIMD 类型。使用 `compOpportunisticallyDependsOn` 来执行查询，以便只记录所需的最大尺寸。

> ### 什么是硬件内在函数（Intrinsics）？
>
> 在 .NET Core 3.0 之后，C# 允许开发者直接调用 CPU 的底层指令（如 Intel 的 SSE, AVX2，ARM 的 NEON）。这些 API 直接映射到单条汇编指令，性能极高。 文档将这些 API 分为四层：
>
> - **傻瓜兼容层 (`Vector2/3/4`)：** 永远安全，所有 CPU 都能跑。
> - **自适应层 (`Vector<T>`)：** 长度是动态的。在支持 AVX2 的机器上它是 256 位（放 8 个 int），在老机器上它是 128 位。
> - **硬性尺寸层 (`Vector128<T>`, `Vector256<T>`)：** 长度写死，作为传递数据的容器。
> - **硬核底层 (`X86.Avx2.Add()`)：** 直接绑定 CPU 架构。**必须**包在 `if (Avx2.IsSupported)` 里面用，否则在不支持的 CPU 上跑会直接触发硬件异常（非法指令崩溃）。
>
> ### 核心冲突：JIT vs. AOT (Crossgen2)
>
> 这篇文档有大半篇幅在解决一个问题：**AOT 编译时的 CPU，和最终运行时的 CPU，可能不是同一个！**
>
> - **如果是 JIT（即时编译）：** 没问题。JIT 运行在用户的机器上，它知道当前 CPU 支持 AVX2，就会直接把 `Vector<T>` 编译成 256 位的 AVX2 指令。
> - **如果是 AOT（提前编译打包）：** 麻烦大了。你在开发机上用 AOT 编了一个包含 AVX2 指令的 EXE 发给客户，客户用的是十年前的老赛扬 CPU，程序一跑就崩了。更可怕的是，如果使用了 `Vector<T>`，它的长度在 AOT 编译时根本不知道！
>
> ### 基准线（Baseline）与 乐观集（Optimistic）
>
> 为了解决 AOT 的问题，.NET 的 AOT 编译器（Crossgen2）引入了两个概念：
>
> - **Baseline (保底指令集)：** AOT 编译器假设所有运行这台程序的机器，**最差**也得支持这些指令（比如默认是 `x86-64-v2`，即必须有 SSE4.2）。
> - **Optimistic (乐观指令集)：** AOT 编译器“期盼”用户机器支持的高级指令。
>
> **AOT 的编译规则（极度严格）：**
>
> 1. 如果你用了 `Vector<T>`，而且 Baseline 没指定最低是 AVX2，**AOT 拒绝编译这个方法**（把它推迟到运行时交给 JIT 处理），因为它不知道长度。
> 2. 如果你用了不在 Optimistic 里的超前 CPU 特性，AOT 也会拒绝编译。
> 3. **最高原则：** AOT 编译出来的代码，在任何机器上运行的**结果（语义）绝对不能变**。不能因为换了 CPU，算出来的账就不一样了。
>
> ### BCL的特权与“紧箍咒”
>
> `.NET` 自己的核心库（`System.Private.CoreLib`）为了追求变态的启动速度和性能，必须大量使用硬件指令，且必须能被 AOT 编译。
>
> 为了防止微软自己的工程师写出导致 AOT 崩溃的代码，文档规定了极其严苛的代码审查规则，并配套了 **Roslyn 分析器**：
>
> - **强制包裹：** 用任何底层 API，必须用 `if (Avx2.IsSupported)` 包裹。
> - **语义等价：** 你的 `if (AVX2)` 分支和 `else (普通的for循环)` 分支，算出来的结果必须**100%完全一致**。
> - **神奇的 `[CompExactlyDependsOn]` 特性：** 这是一个打给 AOT 编译器的标记。意思是：“嘿，AOT 编译器，这个方法里我查了 AVX2，请你帮我编译出两份机器码（或带着守卫代码），在运行时动态选择，而且我保证这两份代码语义一样。”
>
> ### 唯一合法的“非确定性”（薛定谔的算力）
>
> 文档最后提到一个有趣的特例：有些 API 允许在不同的 CPU 上算出**不一样**的结果。
>
> - 比如 `ConvertToIntegerNative`（把极大的浮点数强转整数）或者 `ReciprocalSqrtEstimate`（快速平方根倒数估算）。
> - 因为底层不同 CPU 的估算指令（如 x86 的 `rsqrtps` 和 ARM 的相应指令）精度本身就不同。为了极致性能，.NET 允许这些 API 暴露出底层的“不一致性”。但为了防止 AOT 在编译期直接把结果算死（常量折叠），JIT 内部有专门的机制（`BlockNonDeterministicIntrinsics`）来阻止过度优化。
