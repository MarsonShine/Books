方法描述符（Method Descriptor）
=============================

作者：Jan Kotas ([@jkotas](https://github.com/jkotas)) - 2006

简介
====

MethodDesc（方法描述符）是托管方法在运行时内部的表示形式。它有几个用途：

- 提供一个可在整个运行时中使用的唯一方法句柄。对于普通方法，MethodDesc 是一个 <模块, 元数据标记, 实例化> 三元组的唯一句柄。
- 缓存一些从元数据计算成本较高、但经常使用的信息（例如：该方法是否为 static）。
- 记录方法的运行时状态（例如：是否已经为该方法生成过本机代码）。
- 持有该方法的入口点（entry point）。

设计目标与非目标
----------------

### 目标

**性能：** MethodDesc 的设计针对“尺寸”进行了大量优化，因为每个方法都对应一个 MethodDesc。例如，在当前设计中，普通的非泛型方法的 MethodDesc 只有 8 字节。

### 非目标

**丰富性：** MethodDesc 并不会缓存关于方法的全部信息。对于较少使用的信息（例如方法签名），预期仍需要访问底层元数据。

MethodDesc 的设计
================

MethodDesc 的种类
----------------

存在多种类型的 MethodDesc：

**IL**

用于常规 IL 方法。

**Instantiated**

用于较少见的 IL 方法：这类方法要么带有泛型实例化，要么在 MethodTable 中没有预分配的槽位。

**FCall**

由非托管代码实现的内部方法。这些包括：[用 MethodImplAttribute(MethodImplOptions.InternalCall) 标记的方法](corelib_cn.md)、委托构造函数以及 tlbimp 生成的构造函数。

**PInvoke**

P/Invoke 方法，即使用 DllImport 特性标记的方法。

**EEImpl**

由运行时提供实现的委托方法（Invoke、BeginInvoke、EndInvoke）。参见 [ECMA 335 Partition II - Delegates](../../../project/dotnet-standards_cn.md)。

**Array**

由运行时提供实现的数组方法（Get、Set、Address）。参见 [ECMA Partition II – Arrays](../../../project/dotnet-standards_cn.md)。

**ComInterop**

COM 接口方法。由于非泛型接口默认可用于 COM 互操作，这一类型通常用于所有接口方法。

**Dynamic**

没有底层元数据、动态创建的方法。由 Stub-as-IL 和 LKG（light-weight code generation，轻量级代码生成）产生。

替代实现
--------

在 C++ 中，实现各种不同类型的 MethodDesc，最自然的方式是使用虚方法和继承。虚方法会为每个 MethodDesc 增加一个 vtable 指针，从而浪费大量宝贵的空间。在 x86 上，vtable 指针占用 4 字节。相反，这里的虚拟化是通过根据 MethodDesc 的种类进行切换来实现的，而该种类只需要 3 个比特即可表示。例如：

```c++
DWORD MethodDesc::GetAttrs()
{
    if (IsArray())
        return ((ArrayMethodDesc*)this)->GetAttrs();

    if (IsDynamic())
        return ((DynamicMethodDesc*)this)->GetAttrs();

    return GetMDImport()->GetMethodDefProps(GetMemberDef());
}
```

方法槽位（Method Slots）
------------------------

每个 MethodDesc 都有一个槽位（slot），其中包含该方法当前的入口点。所有方法都必须有这个槽位，即使是永远不会执行的抽象方法也是如此。运行时中有多个位置依赖“入口点 ↔ MethodDesc”的映射关系。

从逻辑上讲，每个 MethodDesc 都有一个入口点，但我们不会在创建 MethodDesc 时就急切地分配入口点。这里的不变性（invariant）是：一旦某个方法被识别为“将要运行的方法”，或被用于虚方法重写（virtual overriding），我们就会为它分配入口点。

槽位要么位于 MethodTable 中，要么位于 MethodDesc 自身中；其位置由 MethodDesc 上的 `mdcHasNonVtableSlot` 位决定。

对于需要通过槽位索引进行高效查找的方法（例如虚方法，或泛型类型上的方法），槽位存放在 MethodTable 中。此时 MethodDesc 会包含该槽位索引，从而支持快速定位入口点。

否则，槽位就是 MethodDesc 自身的一部分。这种安排提升了数据局部性并降低工作集（working set）。此外，对于动态创建的 MethodDesc（例如 Edit & Continue 添加的方法、泛型方法的实例化、或 [DynamicMethod](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/DynamicMethod.cs)），也并不总是能在 MethodTable 中预先分配槽位。

MethodDesc 块（Chunks）
----------------------

为节省空间，MethodDesc 以“块（chunk）”为单位分配。多个 MethodDesc 往往共享相同的 MethodTable，以及元数据 token 的高位。MethodDescChunk 通过把公共信息提升（hoist）到多个 MethodDesc 数组之前来实现共享；单个 MethodDesc 只保存它在数组中的索引。

![Figure 1](./asserts/methoddesc-fig1.png)

图 1 MethodDescChunk 与 MethodTable

调试
----

下面这些 SOS 命令对调试 MethodDesc 很有帮助：

- **DumpMD** – 转储 MethodDesc 内容：

```text
!DumpMD 00912fd8
Method Name: My.Main()
Class: 009111ec
MethodTable: 00912fe8md
Token: 06000001
Module: 00912c14
IsJitted: yes
CodeAddr: 00ca0070
```

- **IP2MD** – 根据给定代码地址查找 MethodDesc：

```text
!ip2md 00ca007c
MethodDesc: 00912fd8
Method Name: My.Main()
Class: 009111ec
MethodTable: 00912fe8md
Token: 06000001
Module: 00912c14
IsJitted: yes
CodeAddr: 00ca0070
```

- **Name2EE** – 根据给定方法名查找 MethodDesc：

```text
!name2ee hello.exe My.Main
Module: 00912c14 (hello.exe)
Token: 0x06000001
MethodDesc: 00912fd8
Name: My.Main()
JITTED Code Address: 00ca0070
```

- **Token2EE** – 根据给定 token 查找 MethodDesc（对于名字很奇怪的方法很有用）：

```text
!token2ee hello.exe 0x06000001
Module: 00912c14 (hello.exe)
Token: 0x06000001
MethodDesc: 00912fd
8Name: My.Main()
JITTED Code Address: 00ca0070
```

- **DumpMT -MD** – 转储给定 MethodTable 中的所有 MethodDesc：

```text
!DumpMT -MD 0x00912fe8
...
MethodDesc Table
   Entry MethodDesc      JIT Name
79354bec   7913bd48   PreJIT System.Object.ToString()
793539c0   7913bd50   PreJIT System.Object.Equals(System.Object)
793539b0   7913bd68   PreJIT System.Object.GetHashCode()
7934a4c0   7913bd70   PreJIT System.Object.Finalize()
00ca0070   00912fd8      JIT My.Main()
0091303c   00912fe0     NONE My..ctor()
```

在调试版本（debug build）中，MethodDesc 会包含方法名与签名字段。当运行时状态严重损坏、SOS 扩展无法工作时，这对调试很有帮助。

预代码（Precode）
================

预代码是一小段代码，用来实现临时入口点，以及对存根（stub）的高效包装器。预代码是一个针对这两类场景的“小众”代码生成器，其目标是生成尽可能高效的代码。在理想情况下，运行时动态生成的所有本机代码都应由 JIT 生成；但在这两种场景中，由于具体需求限制，这并不可行。x86 上的基础预代码可能如下：

```asm
mov eax,pMethodDesc // 将 MethodDesc 加载到暂存寄存器
jmp target          // 跳转到目标
```

**高效的存根包装器：** 某些方法（例如 P/Invoke、委托调用、多维数组的 setter/getter）的实现由运行时提供，通常是手写的汇编存根。预代码在存根之上提供一种空间效率更高的包装方式，使得同一个存根可以复用给多个调用方。

存根的工作代码由一段预代码包装；该预代码片段能够映射到 MethodDesc，并跳转到存根的工作代码。这样，存根的工作代码可以在多个方法之间共享。这是用于实现 P/Invoke 编组存根（marshalling stubs）的重要优化。它也在 MethodDesc 与入口点之间建立了 1:1 映射，从而形成一个简单高效的底层机制。

**临时入口点：** 方法在被 JIT 编译之前也必须提供入口点，以便已 JIT 的代码能拿到可调用的地址。这些临时入口点由预代码提供；它们本质上是存根包装器的一种特例。

这种技术是一种“延迟 JIT”的方式，可同时带来空间与时间上的优化。否则，一个方法的传递闭包（transitive closure）需要在执行前全部被 JIT 编译；这通常是浪费的，因为只有被实际走到的代码分支（例如 if 的某个分支）所依赖的内容才需要被 JIT。

每个临时入口点都比典型的方法体小得多。它们必须很小，因为它们数量巨大，即便以牺牲一些性能为代价也值得。临时入口点在生成方法的真实代码之前只会执行一次。

临时入口点的目标是 PreStub（一种触发方法 JIT 编译的特殊存根）。它会以原子方式把临时入口点替换为稳定入口点。稳定入口点必须在方法生命周期内保持不变；这个不变性对保证线程安全是必要的，因为方法槽位总是在不加锁的情况下被访问。

**稳定入口点**要么是本机代码（native code），要么是预代码。**本机代码**要么是 JIT 编译得到的代码，要么是保存在 NGen 镜像中的代码。在语境中，即便我们实际指的是本机代码，也经常直接说“JIT 编译的代码”。

![Figure 2](./asserts/methoddesc-fig2.png)

图 2 入口点状态图

如果在执行实际方法体之前需要做额外工作，那么一个方法可能同时拥有本机代码与预代码。这种情况通常出现在 NGen 镜像修复（fixup）时。本机代码在这种情况下是一个可选的 MethodDesc 槽位，用于以一种廉价且统一的方式查找方法的本机代码。

![Figure 3](./asserts/methoddesc-fig3.png)

图 3 预代码、存根与本机代码最复杂的组合情况

单次可调用 vs. 多次可调用入口点
------------------------------

调用方法需要入口点。MethodDesc 提供了一组方法来封装逻辑，以便在不同场景下获得最有效的入口点。关键区别在于：入口点是只用于调用一次，还是会用于多次调用。

例如，使用临时入口点来多次调用方法可能不是好主意，因为每次都会经过 PreStub；但如果只调用一次，使用临时入口点通常没问题。

从 MethodDesc 获取可调用入口点的方法包括：

- `MethodDesc::GetSingleCallableAddrOfCode`
- `MethodDesc::GetMultiCallableAddrOfCode`
- `MethodDesc::TryGetMultiCallableAddrOfCode`
- `MethodDesc::GetSingleCallableAddrOfVirtualizedCode`
- `MethodDesc::GetMultiCallableAddrOfVirtualizedCode`

预代码的类型
------------

预代码存在多种专门类型。

预代码类型必须能从指令序列中以很低成本计算出来。在 x86 与 x64 上，预代码类型通过读取固定偏移处的某个字节来判定；这当然会对实现不同预代码类型的指令序列施加限制。

**StubPrecode**

StubPrecode 是基础的预代码类型。它把 MethodDesc 加载到一个暂存寄存器<sup>2</sup>中，然后跳转。为了让预代码机制工作，它必须实现。当没有其他专门预代码类型可用时，它也会作为回退方案使用。

其他所有预代码类型都是可选优化，由平台相关文件通过 HAS\_XXX\_PRECODE 宏开关启用。

StubPrecode 在 x86 上大致如下：

```asm
mov eax,pMethodDesc
mov ebp,ebp // 用于标记预代码类型的“占位”指令
jmp target
```

最初，“target”指向 prestub；之后会被打补丁改为指向最终目标。最终目标（存根或本机代码）可能会使用也可能不会使用 eax 中的 MethodDesc：存根经常会用，而本机代码不会用。

**FixupPrecode**

当最终目标不需要在暂存寄存器<sup>2</sup>中携带 MethodDesc 时，可以使用 FixupPrecode。FixupPrecode 通过避免把 MethodDesc 装载到暂存寄存器来节省几个周期。

大多数使用到的存根都属于更高效的形式；在不需要专门 Precode 形式时，目前除互操作（interop）方法外，基本都可以使用这种形式。

x86 上 FixupPrecode 的初始状态：

```asm
call PrecodeFixupThunk // 该调用不会返回：它会弹出返回地址
                       // 并利用返回地址取到下面的 pMethodDesc，
                       // 从而知道需要 JIT 的方法是哪一个
pop esi // 用于标记预代码类型的“占位”指令
dword pMethodDesc
```

当它被打补丁改为指向最终目标后：

```asm
jmp target
pop edi
dword pMethodDesc
```

<sup>2</sup> 在暂存寄存器中传递 MethodDesc 有时被称为 **MethodDesc 调用约定（MethodDesc Calling Convention）**。

**ThisPtrRetBufPrecode**

ThisPtrRetBufPrecode 用于：对于返回值为值类型（valuetype）的开放实例委托（open instance delegate），交换返回缓冲区（return buffer）与 this 指针。它用于把 `MyValueType Bar(Foo x)` 的调用约定转换为 `MyValueType Foo::Bar()` 的调用约定。

该预代码总是按需分配，作为实际方法入口点的包装器，并存储在一个表中（FuncPtrStubs）。

ThisPtrRetBufPrecode 形如：

```asm
mov eax,ecx
mov ecx,edx
mov edx,eax
nop
jmp entrypoint
dw pMethodDesc
```

**PInvokeImportPrecode**

PInvokeImportPrecode 用于对非托管 P/Invoke 目标进行延迟绑定（lazy binding）。该预代码的目的在于方便实现，并减少平台相关的“管道代码（plumbing）”数量。

每个 PInvokeMethodDesc 除了常规预代码外，还包含一个 PInvokeImportPrecode。

PInvokeImportPrecode 在 x86 上大致如下：

```asm
mov eax,pMethodDesc
mov eax,eax // 用于标记预代码类型的“占位”指令
jmp PInvokeImportThunk // 为 pMethodDesc 延迟加载 P/Invoke 目标
```

> ### 1. 核心定义：MethodDesc 是方法的“身份证”
>
> 在 .NET 中，你写的每一段 C# 代码最终都会编译成 IL。但在运行时，.NET 引擎（EE, Execution Engine）需要一个统一的、高性能的对象来代表这个方法。
>
> - **MethodDesc 就是这个代表。** 它是方法在内存中的“句柄”或“身份证”。
> - **它存什么？** 存这个方法是不是静态的、属于哪个类、它的 Token 是什么，以及最关键的：**它的机器码（Native Code）在哪里？**
>
> ### 2. 设计哲学：极致的内存压缩
>
> 文档中多次提到“尺寸（Size）”和“优化”。
>
> - **背景：** 一个中大型 .NET 程序可能有数万甚至数十万个方法。如果每个 `MethodDesc` 都很大，内存开销会非常恐怖。
> - 理解重点：
>   - **放弃虚函数（No Vtable）：** 按照 C++ 习惯，不同类型的方法（如 P/Invoke 方法、泛型方法）应该继承自基类。但为了省下 4 或 8 字节的虚表指针（vtable pointer），CLR 选择了手动判断类型（通过 3 个 bit 位）。
>   - **块（Chunk）分配：** 很多方法属于同一个类，它们共享一些元数据。CLR 把它们放在一个 `MethodDescChunk` 里，让它们共享头部信息，从而把单个 `MethodDesc` 的开销压缩到几字节。
>
> ### 3. 入口点管理：从“占位符”到“真实代码”
>
> 这是文档中最技术性的部分，解释了方法是如何被调用起来的。
>
> - **延迟加载（Lazy JIT）：** .NET 不会在启动时编译所有代码。
> - 预代码（Precode）的作用：
>   - 当你第一次调用一个还没被编译（JIT）的方法时，你实际跳进的是一段叫 **Precode** 的“小存根（Stub）”。
>   - **Precode 的任务：** “嘿，JIT，这个方法还没编译，快帮我编译一下”。
>   - **打补丁（Patching）：** 编译完成后，Precode 会被“原地覆盖”或者修改跳转地址，下次再调用时，就会直接跳到编译好的机器码，不再经过 JIT。
>
> ### 4. 各种类型的 MethodDesc（分类意识）
>
> 理解这个问题时，不需要背下所有类型，但要意识到运行时会根据方法的来源采取不同的处理策略：
>
> - **IL：** 普通的 C# 代码。
> - **PInvoke：** 调用 DLL 里的非托管代码。
> - **Instantiated：** 泛型方法（每个不同的 T 可能需要不同的处理）。
> - **Dynamic：** 反射或表达式树动态生成的方法。
>
> ### 5. 调试视角：如何“看见”它
>
> 文档最后列出的 SOS 命令（如 `!DumpMD`）是非常实用的工具。理解这部分的最好方法是： 如果你在调试生产环境的 Dump 文件，发现某个地址报错，你可以通过 `!ip2md`（指令指针转方法描述符）瞬间知道这个报错地址属于哪个 C# 方法。
>
> ### 总结：你应该记住的三个核心概念
>
> 1. **唯一性**：在整个运行时中，一个具体的方法对应一个唯一的 `MethodDesc` 内存地址。
> 2. **槽位（Slot）**：它是通往真实机器码的门牌号。
> 3. **极致性能**：为了省内存，不惜放弃标准的 C++ 对象设计，采用位运算和内存块共享。
>
> **一句话理解：** 这一章描述了 .NET 运行时如何以**最省钱（内存）**的方式，给每个方法发一张**高效的身份证**，并管理这些方法**从 IL 变成机器码并被执行**的全过程
