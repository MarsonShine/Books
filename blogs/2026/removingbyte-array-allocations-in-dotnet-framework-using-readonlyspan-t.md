# 在 .NET Framework 中使用 ReadOnlySpan<T> 消除 byte[] 分配

这篇文章介绍一种很简单的办法，可以消除一部分 `byte[]` 分配，而且不管你面向哪个 .NET 版本都适用，包括 .NET Framework。对于经常在现代 .NET 中编写性能敏感代码的人来说，这个技巧大概并不陌生；不过我最近才意识到，它同样能应用在更老的运行时上，比如 .NET Framework。

本文会看看：为了减少分配，你需要怎样修改 C# 代码；编译器在背后是如何实现这一变化的；以及使用这套技巧时有哪些坑和边界条件需要注意。

## `Span<T>` 和 `ReadOnlySpan<T>` 已经成了 .NET 性能优化的基本功

`ReadOnlySpan<T>` 和 `Span<T>` 进入 .NET 已经很多年了，但它们对代码写法的影响非常大，尤其是在性能敏感的场景里。它们的核心价值在于：可以基于已有数据提供一个“窗口”或“视图”，而不用复制那份数据。

最经典的例子就是处理 `string`。与其调用 `Substring()`，为字符串片段创建额外副本，不如调用 `AsSpan()`，得到一个 `ReadOnlySpan<char>` 片段。这样你几乎可以把它当成独立的 `string` 来操作，但又不需要承担复制成本。

这大概是应用代码里最常见的 `Span<T>` 用法，但从本质上说，只要你想给某段内存提供一个视图，`Span<T>` 就有用武之地。由于 `Span<T>` 的底层承载几乎可以是任何东西，所以你可以保持同一套“公开 API”，而在底层自由替换实现。

另一个常见例子是解析代码。如果你需要一个临时缓冲区来保存中间结果，在 `Span<T>` 出现之前，几乎一定会在堆上分配一个普通数组；但有了 `Span<T>` 之后，你就能很方便地用 `stackalloc` 在栈上分配，从而减轻垃圾回收器的压力：

```csharp
Span<byte> buffer = requiredSize <= 256                  // 如果所需缓冲区比较小
                        ? stackalloc byte[requiredSize]  // 就直接分配在栈上。
                        : new byte[requiredSize];        // 否则退回普通堆分配
```

几乎所有新的 .NET 运行时 API 都会提供对 `Span<T>` 或 `ReadOnlySpan<T>` 的支持。即使在 `.NET Framework` 这类旧运行时里，你也可以通过 [System.Memory NuGet 包](https://www.nuget.org/packages/System.Memory) 使用它们（当然，性能收益不会和 .NET Core 上完全一样）。

能够安全、方便地处理任意来源的内存块，而无需直接退回 `unsafe` 和指针，正是 `Span<T>` 在性能代码里如此重要的原因。而这种“为任意内存提供视图”的能力，也给了编译器额外的优化空间——下一节就会看到。

## 用 `ReadOnlySpan<byte>` 消除 `byte[]` 分配

编译器之所以能做出本文后面要讲的优化，关键就在于它可以为任意内存创建视图。

假设你手里有一个 `byte[]`，某段逻辑需要用到它。你事先就知道里面要放哪些数据，因此把它放进 `static readonly` 字段里，确保数据只初始化一次：

```csharp
public static class MyStaticData
{
    private static readonly byte[] ByteField = new byte[] { 1, 2, 3, 4 };
}
```

这当然能正常工作，但它意味着：第一次访问这份数据时，运行时仍然需要创建数组实例、填充内容，然后把它存进字段里。后续访问很便宜，但首次初始化还是会带来一点启动成本。

不过，从 C# 8.0 开始，只要你需要的只是这份数据的只读视图，就可以换一种写法：暴露一个 `ReadOnlySpan<byte>` **属性**，而不是字段：

```csharp
public static class MyStaticData
{
    // 之前
    private static readonly byte[] ByteField = new byte[] { 1, 2, 3, 4 };

    // 之后
    private static ReadOnlySpan<byte> ReadOnlySpanProp => new byte[] { 1, 2, 3, 4 };
}
```

一般来说，这种代码本来应该立刻让你警觉起来：它**看上去**像是每次访问属性都会新建一个 `byte[]`，但事实并不是这样。

> 稍后我们会详细看一下生成的 IL，这里先从高层说明。

当编译器看到上面的模式时，会做两件事：

*   把 `byte[]` 数据嵌入到最终程序集的元数据中；
*   当调用 `ReadOnlySpanProp` 时，不去创建 `byte[]`，而是直接构造一个指向程序集内嵌数据的 `ReadOnlySpan<byte>`。

也就是说，返回的 `ReadOnlySpan<byte>` 指向的既不是堆上的数据，也不是栈上的数据，而是**直接嵌在程序集里**的数据。这意味着根本没有分配，不仅消除了启动时的初始化开销，也完全不会给垃圾回收器带来压力。

还要注意，这是一项**编译器特性**。也就是说，只要环境里存在 `System.ReadOnlySpan<T>` 类型，就能使用它。所以，如果你的 .NET Framework 应用引用了 System.Memory NuGet 包，也可以享受到这种零分配技巧。

而且，这不只适用于把 `static readonly byte[]` 字段改成 `static ReadOnlySpan<byte>` 属性；它同样适用于**局部变量**。也就是说，下面这类代码虽然**看起来**像是在分配数组，实际上并不会：

```csharp
public static void TestData()
{
    // 看起来像分配了数组，其实没有
    ReadOnlySpan<byte> arr = new byte[] { 0, 1, 2 };
}
```

顺便一提，这也适用于 [UTF-8 字符串字面量](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/utf8-string-literals)。从类型系统的角度看，它本质上也是以 `byte[]` 的逻辑形式表示，所以这里同样是零分配：

```csharp
public static class MyStaticData
{
    private static ReadOnlySpan<byte> ReadOnlySpanUtf8 => "Hello world"u8;
}
```

这些当然都很好，但我第一次看到这种 `byte[]` 写法时，心里还是有点打鼓。毕竟它**看上去**就是在分配，而且似乎会非常低效，所以我想确认一下。最直接的办法，就是去看编译器实际生成了什么 IL。

## 底层到底发生了什么？

查看编译器生成代码的办法有很多。如果你只是想验证一小段代码，[sharplab.io](https://sharplab.io/#v2:EYLgtghglgdgNAExAagD4AEBMBGAsAKAPQGYACdbANnM1IFkBPAZQBcIWoBjAEXYgIDeBUiNIB6MaQDyAaWGiADgCcoAN3YBTclVJKNEBAHsYAGwalgDFhoDaAXVIAhKxoBiUDSYSkAvKRgaAO4WLvakAqSYcKTE0QAs0QCspAC+ANzyIspqmtrUltZhAKosAGYAHAAKSoYKGkos5n4ARAASniaGpIGGSl7NAK7lAHQAKoYAgkpKEAwAFACUGYT4oqTZ6tZ5pABK+ghSpswKEDAAPAUaAHy7+4dmTCcw1bW+NwHBl2ER2NFRMfFUssCCkgA=) 是最快最方便的；另外还有 [ILSpy](https://github.com/icsharpcode/ilspy)、JetBrains 的 [dotPeek](https://www.jetbrains.com/decompiler/) 和 [Rider](https://www.jetbrains.com/rider/)，我想 Visual Studio 应该也有类似插件。

为了验证自己的判断，我先用 `dotnet new classlib` 创建了一个新项目，然后把它调整成 .NET Framework。这里要说明一下：前面展示的技巧适用于所有目标框架；但我就是想专门拿 .NET Framework 做实验，证明这不是只有“新框架”才能用。项目文件如下：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- 👇 把目标框架改成 .NET Framework -->
    <TargetFramework>net48</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- 👇 使用最新 C# 版本（搭配 .NET 10 SDK 时就是 C# 14） -->
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  
  <!-- 👇 引用 System.Memory，这样才能使用 ReadOnlySpan<T> -->
  <ItemGroup>
    <PackageReference Include="System.Memory" Version="4.6.3" />
  </ItemGroup>
</Project>
```

然后我写了下面这个非常简单的类，编译之后用 Rider 查看生成的 IL：

```csharp
public static class MyStaticData
{
    private static ReadOnlySpan<byte> ReadOnlySpanProp => new byte[] { 1, 2, 3, 4 };
    private static ReadOnlySpan<byte> ReadOnlySpanUtf8 => "Hello world"u8;
}
```

下面这段 IL 我加了一些注释说明它在做什么，但**关键点**是：我们看不到 `newarr`、`InitializeArray()`、`ToArray()` 之类的调用。取而代之的是：IL 加载一个地址，该地址指向嵌在 PE 映像（也就是程序集）中的数据；再加载数据长度（4 个字节）；然后把指针和长度传给 `new ReadOnlySpan<T>()` 构造函数并返回。没有拷贝，没有新数组，只是对已经加载到内存中的字节做了一层包装。

```
.class public abstract sealed auto ansi beforefieldinit
  MyStaticData
    extends [mscorlib]System.Object
{

  .field private static initonly unsigned int8 One

  .method private hidebysig static specialname valuetype [System.Memory]System.ReadOnlySpan`1<unsigned int8>
    get_ReadOnlySpanProp() cil managed
  {
    .maxstack 8

    // 👇 把包含数组数据 blob 的静态字段地址压栈
    IL_0000: ldsflda      int32 '<PrivateImplementationDetails>'::'9F64A747E1B97F131FABB6B447296C9B6F0201E79FB3C5356E6C77E89B6A806A'
    // 👇 把数值 4 压栈
    IL_0005: ldc.i4.4
    // 👇 创建一个新的 ReadOnlySpan<byte>
    IL_0006: newobj       instance void valuetype [System.Memory]System.ReadOnlySpan`1<unsigned int8>::.ctor(void*, int32)
    IL_000b: ret // 返回该 span

  } // end of method MyStaticData::get_ReadOnlySpanProp

  .method private hidebysig static specialname valuetype [System.Memory]System.ReadOnlySpan`1<unsigned int8>
    get_ReadOnlySpanUtf8() cil managed
  {
    .maxstack 8

    // 👇 把包含 UTF-8 数据 blob 的静态字段地址压栈
    IL_0000: ldsflda      valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '<PrivateImplementationDetails>'::'27518BA9683011F6B396072C05F6656D04F5FBC3787CF92490EC606E5092E326'
    // 👇 把数值 11 压栈（也就是 "Hello world" 的 UTF-8 长度）
    IL_0005: ldc.i4.s     11 // 0x0b
    // 👇 创建一个新的 ReadOnlySpan<byte>
    IL_0007: newobj       instance void valuetype [System.Memory]System.ReadOnlySpan`1<unsigned int8>::.ctor(void*, int32)
    IL_000c: ret // 返回该 span

  } // end of method MyStaticData::get_ReadOnlySpanUtf8
}
```

很好，这就清楚地证明它确实如我们预期那样工作，而且这里还是 .NET Framework。也就是说，这确实只是编译器特性，没有额外的运行时要求，因此到处都能用。

但事情也不能掉以轻心……前面我证明了它对 `byte[]` 有效，可它并不是对**所有类型**都有效。

## 小心：这里真的有坑

如果你一路看到这里，可能会想：“很好，那我把所有静态数组数据都改成这种写法就行了。”先别急。这里确实有坑。上面的模式只有在以下条件同时成立时才安全：

*   你的数组类型是 `byte[]`、`sbyte[]` 或 `bool[]`；
*   数组里的**所有值**都是常量；
*   数组是不可变的（也就是返回 `ReadOnlySpan<T>`，而不是 `Span<T>`）。

只要违反其中任何一条，对性能都可能是灾难。下面逐条来看。

### 只有 `byte`、`sbyte` 和 `bool` 才行

前面展示的编译器优化，只能用于**单字节原始类型**，也就是 `byte`、`sbyte` 和 `bool`。原因在于：常量数据会以小端序（little endian）存储，而运行时可能运行在大端序（big endian）硬件上，此时就需要额外的字节序修正。

这意味着，如果你像下面这样把 `byte` 换成 `int`，代码依然能正常编译，但它**不会**生成你期待的“零分配”代码：

```csharp
public static class MyStaticData
{
    // ⚠️ 这里用的是 int 而不是 byte，
    // 在 .NET Framework 和 .NET 7 之前，这会导致分配数组
    private static ReadOnlySpan<int> ReadOnlySpanPropInt => new int[] { 1, 2, 3, 4 };
}
```

如果查看 .NET Framework 下为这段代码生成的 IL，就会看到问题所在：`newarr` 和 `InitializeArray` 都出现了。编译器其实已经帮你避开了“每次访问都创建新数组”这个最糟糕的情况——它会创建一次数组，把它缓存到一个静态字段里，后续再重复使用——但它仍然有启动成本，做的事情也比优化过的 `byte[]` 方案更多：

```
.method private hidebysig static specialname valuetype [System.Memory]System.ReadOnlySpan`1<int32>
  get_ReadOnlySpanPropInt() cil managed
{
  .maxstack 8

  // 👇 尝试从静态缓存字段加载 int[] 数据
  IL_0000: ldsfld       int32[] '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72_A6
  IL_0005: dup                                  // 复制一份引用
  IL_0006: brtrue.s     IL_0020                 // 如果不为 null，说明已缓存，直接跳转到结尾
  IL_0008: pop                                  // 否则移除刚才复制的 null
  IL_0009: ldc.i4.4                             // 加载数据长度（4）
  IL_000a: newarr       [mscorlib]System.Int32  // 在堆上分配新数组
  IL_000f: dup                                  // 保留一份数组引用
  // 👇 加载程序集里嵌入的 int[] 数据地址
  IL_0010: ldtoken      field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16' '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72
  // 👇 用嵌入的数据初始化新数组
  IL_0015: call         void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
  IL_001a: dup                                  // 再复制一份引用
  // 👇 把填充好的数组存进静态缓存字段
  IL_001b: stsfld       int32[] '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72_A6
  // 👇 用该数组构造 ReadOnlySpan<int>
  IL_0020: newobj       instance void valuetype [System.Memory]System.ReadOnlySpan`1<int32>::.ctor(!0/*int32*/[])
  IL_0025: ret

} // end of method MyStaticData::get_ReadOnlySpanPropInt
```

所以，“好消息”是：它和直接使用 `static readonly int[]` 相比，并没有差到离谱；但坏消息是，它显然也**不是** `byte[]` 那种零分配版本。

另外，如果你运行在 .NET 7+ 上，[运行时新增了一个 API](https://github.com/dotnet/runtime/issues/60948)，确实支持这种模式。所以如果我们把目标框架换成 .NET 10 再重新编译，IL 就会恢复成零分配版本，因为它调用了 [`RuntimeHelpers::CreateSpan`](https://github.com/dotnet/runtime/blob/b70c35ed8a2e7ae0d91de76f4f5d26c2e7d2c6cd/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/RuntimeHelpers.cs#L154)，由它来处理字节序修正问题：

```
.method private hidebysig static specialname valuetype [System.Runtime]System.ReadOnlySpan`1<int32>
    get_ReadOnlySpanPropInt() cil managed
  {
    .maxstack 8

    // 👇 加载数据地址
    IL_0000: ldtoken      field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16_Align=4' '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B724
    // 👇 调用 RuntimeHelpers::CreateSpan 并返回
    IL_0005: call         valuetype [System.Runtime]System.ReadOnlySpan`1<!!0/*int32*/> [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::CreateSpan<int32>(valuetype [System.Runtime]System.RuntimeFieldHandle)
    IL_000a: ret

  } // end of method MyStaticData::get_ReadOnlySpanPropInt
```

总结一下：这里的收益高度依赖运行时版本。除非你运行在 .NET 7+，否则基本得不到什么好处。如果你还需要兼容旧框架，那么相比这套写法，继续使用传统的 `static readonly int[]` 字段往往更稳妥。

### 所有值都必须是常量

第二个问题是：本文介绍的整套技巧只有在集合里的所有值都是**常量**时才成立。比如下面这个例子，在数组里引用了一个 `static readonly` 值，代码同样能正常编译：

```csharp
public static class MyStaticData
{
    private static readonly byte One = 1;
    private static ReadOnlySpan<byte> ReadOnlySpanPropNonConstant => new byte[] { One, 2, 3, 4 };
}
```

但即使是在 .NET 7+ 上，它也**不会**走你以为的零分配路径，反而会变成非常糟糕的“每次访问都新建一个数组”。

```
.method private hidebysig static specialname valuetype [System.Runtime]System.ReadOnlySpan`1<unsigned int8>
  get_ReadOnlySpanPropNonConstant() cil managed
{
  .maxstack 8

  IL_0000: ldc.i4.4                                  // 加载数组长度
  IL_0001: newarr       [System.Runtime]System.Byte  // 创建新数组
  IL_0006: dup                                       // 复制一份引用
  // 👇 获取数据引用并初始化数组
  IL_0007: ldtoken      field int32 '<PrivateImplementationDetails>'::'1E6175315920374CAA0A86B45D862DEE3DDAA28257652189FC1DFBE07479436A'
  IL_000c: call         void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [System.Runtime]System.Array, valuetype [System.Runtime]System.RuntimeFieldHandle)
  IL_0011: dup
  IL_0012: ldc.i4.0                                      // 加载要修改的索引 0
  IL_0013: ldsfld       unsigned int8 MyStaticData::One  // 加载静态字段 One
  IL_0018: stelem.i1                                     // 设置 array[0] = One
  // 👇 返回包裹这个新数组的 ReadOnlySpan<byte>
  IL_0019: call         valuetype [System.Runtime]System.ReadOnlySpan`1<!0/*unsigned int8*/> valuetype [System.Runtime]System.ReadOnlySpan`1<unsigned int8>::op_Implicit(!0/*unsigned int8*/[])
  IL_001e: ret
} // end of method MyStaticData::get_ReadOnlySpanPropNonConstant
```

这就……很糟，而且是**每次访问属性都会这样**。这一点在所有目标框架上都要特别小心。

### 只能用 `ReadOnlySpan<T>`，不要用 `Span<T>`

如果你把 `ReadOnlySpan<T>` 换成 `Span<T>`，也会遇到类似的“危险场景”：

```csharp
public static class MyStaticData
{
    private static Span<byte> SpanProp => new byte[] { 1, 2, 3, 4 };
}
```

因为你返回的是**可变数据**（`Span<T>` 而不是 `ReadOnlySpan<T>`），编译器就没法用前面那些花哨的优化了。它唯一能做的，就是创建一个新数组，用正确的初始值把它填好，然后再用一个可变 `Span<T>` 包起来返回：

```
.method private hidebysig static specialname valuetype [System.Runtime]System.Span`1<unsigned int8>
  get_SpanProp() cil managed
{
  .maxstack 8

  IL_0000: ldc.i4.4
  IL_0001: newarr       [System.Runtime]System.Byte
  IL_0006: dup
  IL_0007: ldtoken      field int32 '<PrivateImplementationDetails>'::'9F64A747E1B97F131FABB6B447296C9B6F0201E79FB3C5356E6C77E89B6A806A'
  IL_000c: call         void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [System.Runtime]System.Array, valuetype [System.Runtime]System.RuntimeFieldHandle)
  IL_0011: call         valuetype [System.Runtime]System.Span`1<!0/*unsigned int8*/> valuetype [System.Runtime]System.Span`1<unsigned int8>::op_Implicit(!0/*unsigned int8*/[])
  IL_0016: ret

} // end of method MyStaticData::get_SpanProp
```

这种失败路径其实很好理解：既然数据必须可变，那就根本不存在“安全的零分配方案”。真正的问题在于：代码表面上并**不明显**能看出它已经变成一个“超级爱分配”的属性，而不是零分配实现。如果你一不小心把 `ReadOnlySpan<T>` 写成了 `Span<T>`，或者某个 AI 帮你写错了，光靠代码审查其实很难一眼看出来……

唯一的好消息是：如果你使用了现代语法特性，也就是**集合表达式（collection expressions）**，那至少有机会在编译期抓到这类问题。

### 用集合表达式降低出错风险

那集合表达式怎么帮上忙呢？因为前面那两类问题——数组里含有非常量值，或者变量类型是 `Span<T>` 而不是 `ReadOnlySpan<T>`——在使用集合表达式的 `static` 属性模式下，根本**过不了编译**：

```csharp
public static class MyStaticData
{
    // 无法编译（这是好事！）
    private static ReadOnlySpan<byte> ReadOnlySpanPruopNonConstantCollectionExpression => [One, 2, 3, 4];

    // 无法编译（这也是好事！）
    private static Span<byte> SpanPropCollectionExpression => [1, 2, 3, 4];
}
```

编译时会得到 `CS9203` 错误：

```
Error CS9203 : A collection expression of type 'ReadOnlySpan<byte>' cannot be used in this context because it may be exposed outside of the current scope.
Error CS9203 : A collection expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the current scope.
```

![Rider 中的上述错误](https://andrewlock.net/content/images/2026/readonlyspan_collectionexpressions.png)

这就相当于给你加了一层安全网。只要你在这种场景下始终使用集合表达式，就能避免犯下最严重的那几类错误。至于 `int` 这种情况，集合表达式依然允许，但前面已经说过，它没那么糟，因为 .NET 7+ 确实支持，而且在 .NET 7 之前你至少也只是“创建一次并缓存”，而不是每次都分配。

不幸的是，集合表达式**只在 `static` 属性**这个场景里帮得上忙。如果你是在创建局部变量，那么在 .NET Framework（以及所有 .NET 8 之前的版本）里，集合表达式并不能救你：

```csharp
public static class MyStaticData
{
    private static readonly byte One = 1;

    public static void TestData()
    {
        // 哦不，这些在 .NET Framework 上都会分配！
        ReadOnlySpan<int> intArray = [1, 2, 3, 4]; // .NET 7+ 里这一项不分配

        ReadOnlySpan<byte> nonConstantArray = [One, 2, 3, 4]; // 但后面这两项要到 .NET 8+
        Span<byte> spanArray = [1, 2, 3, 4];                  // 才能避免分配！
    }
}
```

如果我们查看这个方法在 .NET Framework 下生成的 IL，就会发现：`int[]` 走的是“创建静态数组并缓存”的路线；而非常量数组和 `Span<T>` 这两种情况，则是每次都新建一个数组，和 `static` 属性的情况完全一样：

```
    .method public hidebysig static void
    TestData() cil managed
  {
    .maxstack 5
    .locals init (
      [0] valuetype [System.Memory]System.ReadOnlySpan`1<int32> intArray,
      [1] valuetype [System.Memory]System.ReadOnlySpan`1<unsigned int8> nonConstantArray,
      [2] valuetype [System.Memory]System.Span`1<unsigned int8> spanArray
    )

    // [10 5 - 10 6]
    IL_0000: nop

    // 加载或初始化静态 int[] 字段数据
    IL_0001: ldsfld       int32[] '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72_A6
    IL_0006: dup
    IL_0007: brtrue.s     IL_0021
    IL_0009: pop
    IL_000a: ldc.i4.4
    IL_000b: newarr       [mscorlib]System.Int32
    IL_0010: dup
    IL_0011: ldtoken      field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16' '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72
    IL_0016: call         void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_001b: dup
    IL_001c: stsfld       int32[] '<PrivateImplementationDetails>'::CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72_A6
    IL_0021: newobj       instance void valuetype [System.Memory]System.ReadOnlySpan`1<int32>::.ctor(!0/*int32*/[])
    IL_0026: stloc.0      // intArray

    // 对于非常量数组，每次都会创建新数组
    IL_0027: ldloca.s     nonConstantArray
    IL_0029: ldc.i4.4
    IL_002a: newarr       [mscorlib]System.Byte
    IL_002f: dup
    IL_0030: ldtoken      field int32 '<PrivateImplementationDetails>'::'1E6175315920374CAA0A86B45D862DEE3DDAA28257652189FC1DFBE07479436A'
    IL_0035: call         void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_003a: dup
    IL_003b: ldc.i4.0
    IL_003c: ldsfld       unsigned int8 MyStaticData::One
    IL_0041: stelem.i1
    IL_0042: call         instance void valuetype [System.Memory]System.ReadOnlySpan`1<unsigned int8>::.ctor(!0/*unsigned int8*/[])

    // 对于 Span<byte> 数组，每次同样都会创建新数组
    IL_0047: ldloca.s     spanArray
    IL_0049: ldc.i4.4
    IL_004a: newarr       [mscorlib]System.Byte
    IL_004f: dup
    IL_0050: ldtoken      field int32 '<PrivateImplementationDetails>'::'9F64A747E1B97F131FABB6B447296C9B6F0201E79FB3C5356E6C77E89B6A806A'
    IL_0055: call         void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_005a: call         instance void valuetype [System.Memory]System.Span`1<unsigned int8>::.ctor(!0/*unsigned int8*/[])

    // [15 5 - 15 6]
    IL_005f: ret

  } // end of method MyStaticData::TestData
```

所以很遗憾，这里集合表达式也帮不上忙。当然，对于这么小的数组，你通常完全可以——而且也**应该**——直接用 `stackalloc`，所以这未必是个大问题。但前提是：你得先知道这里到底发生了什么。

那我们到底应该怎么理解这一切呢？

## 总结

好消息是：如果你使用得当，把保存只读数据的 `static readonly byte[]` 字段替换成 `static ReadOnlySpan<byte>` 属性，确实可以带来零分配、几乎零启动成本的收益，而且即使在 .NET Framework 上也成立。

不过，如果你要“改造”的并不是 `byte[]`、`bool[]` 或 `sbyte[]`，那就最好谨慎一点。像 `int[]` 以及其他类型，在 .NET 7+ 上确实也支持类似优化，但这需要运行时配合。如果你的目标平台还包括 .NET Framework、.NET Standard 或 .NET 6 及更早版本，那我会认真权衡一下，这种修改到底值不值得。

> 在 .NET 7+ 上，你[大概率还是能看到性能收益的](https://github.com/dotnet/runtime/issues/60948)。但据我目前的理解，这大概说的是数组首次创建时有约 15% 的提速。可如果你每次访问都要调用 `RuntimeHelpers.CreateSpan()`，而不是直接加载一个字段，那它在稳态下真的会更快吗？我也不知道，我还没测过，只是单纯有点好奇。

真正需要格外小心的，是一定要**只**在数组里使用常量值（拜托别塞 `static readonly` 值进去），并且**只**使用 `ReadOnlySpan<T>`，不要用 `Span<T>`。好在如果你在 `static` 属性里使用集合表达式，这些问题会自动暴露出来，因为代码压根过不了编译。这也再次说明：能用集合表达式的时候，就尽量用吧。

把 `static byte[]` 字段替换成 `static ReadOnlySpan<byte>` 大概是最常见的场景，但你**也可以**把这招用到局部变量上。不过我怀疑这种场景没那么常见，因为那种写法本来就**肉眼可见地很像会分配**，这通常意味着你“并不关心性能”，那自然也就没必要特地改成 `ReadOnlySpan<byte>` 了。

> 另外还有一个不太建议碰局部变量定义的原因：前面说的集合表达式“保护机制”，对局部变量并不会触发编译错误，所以你就没有同样简单的防护栏了。

如果你和我一样，会觉得这里有这么多一踩就掉下去的性能悬崖，多少有点出乎意料。通常 .NET 团队会很努力地避免这类悬崖，或者至少加一些分析器（analyzer）来引导你走上正确路径。但在这里，似乎并没有太多机制阻止你写出“看起来对、实际上很糟”的代码。

翻了一圈相关 issue 和讨论后会发现，这个问题已经被提过很多次了，但普遍的难点似乎在于：“那些有问题的代码模式，有时候其实又是合法的。”此外，还会有人提出“反正你本来就应该用 `stackalloc`”以及“集合表达式已经部分帮你兜底了”这两个观点：

*   [\[Analyzer\]: Unexpected allocation when converting arrays to spans](https://github.com/dotnet/runtime/issues/69577)
*   [\[Proposal\]: ReadOnlySpan initialization from static data](https://github.com/dotnet/csharplang/issues/5295)
*   [compile-time readonly arrays, const T[]](https://github.com/dotnet/csharplang/discussions/955)
*   ["ReadOnlySpan initialization from static data" language design notes](https://github.com/dotnet/csharplang/blob/main/meetings/2023/LDM-2023-10-09.md#readonlyspan-initialization-from-static-data)
*   [\[API Proposal\]: ReadOnlySpan CreateSpan(RuntimeFieldHandle)](https://github.com/dotnet/runtime/issues/60948)

总的来说，这种技巧更像是“自行承担风险地使用”。我仍然觉得，如果能至少提供一些**可选**分析器来保护开发者，会是件挺不错的事（也许已经有人做了）。不过话说回来，如果你手头正好有一堆静态数据，这种把初始化成本降到 0 的能力依然很有价值；只要确保自己用在**正确且安全**的场景里就行。

## 作者信息
Andrew Lock

## 原文链接
https://andrewlock.net/removingbyte-array-allocations-in-dotnet-framework-using-readonlyspan-t/
