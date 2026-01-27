将 .NET 移植到新的处理器架构的指南
========================

本文档分为 2 个主要部分。

1.  将 .NET Runtime 移植的各个移植阶段

2.  关于移植到新架构时受影响的主要组件的技术讨论

移植阶段与步骤
========================

将 .NET Runtime 移植到一个新架构通常会沿着如下路径推进。

随着工程工作沿着开发路径持续推进，最好尽早将逻辑合入 runtime 的主分支。这样会带来 2 个主要效果。

1.  单个提交更容易评审。

2.  并非所有用于修复问题的方法都一定会被认为可接受。
    很可能某个变更永远都不会被上游 git 仓库接受，及早发现此类问题可以避免大量沉没成本。

3.  当某些变更破坏了其他平台时，通常可以相对容易地定位到破坏点。
    如果一直拖到所有改动都完成、产品完全可用之后才合入，那么这项工作往往会困难得多。

Stage 1 初始 Bring Up
------------------------

将 .NET 移植到新平台，起步通常是将 CoreCLR 移植到新的体系结构（architecture）。

该过程遵循如下策略

-   在构建环境中增加一个新的目标体系结构，并让它能够构建通过。

-   决定是否有足够的动机来 bring up 解释器（interpreter），或者仅让 JIT 处理新架构是否更便宜。
    CLR 中的解释器目前只用于 bring up 场景，并不维护为“普遍可用”。
    对于熟悉 CoreCLR 代码库的工程师来说，预计需要 1-2 个月时间才能启用解释器。
    一个可用的解释器可以让移植团队将工程师分成两类：一类专注于 JIT，另一类专注于运行时的 VM 部分。

-   建立一套可运行 coreclr 测试的脚本集合。运行 coreclr 测试的常规方式是 XUnit，而它只有在框架大体可用之后才适用。
    这套脚本会在开发过程中不断演进，以支持不断增长的开发需求。预期这套脚本需要完成如下任务。

    -   运行测试的某个子集。测试按类别组织成目录结构，因此该子集机制只需要基于目录结构即可。

    -   需要按测试用例（test-by-test）排除某些测试。产品准备发布时，大多数被禁用的测试都需要重新启用，但也有一些测试会在产品 bring up 到质量水平的过程中被禁用数月/数年。

    -   生成 crash 或 core dump。该阶段很多测试的失败模式是崩溃。一个能够捕获 core dump 的测试运行工具会让这些问题更容易诊断。

    -   生成按桶（bucketized）的失败列表。通常的做法是按断言（assertion）分组；若发生崩溃，则按崩溃的调用栈（callstack）分组。

-   首先应专注于 JIT 类别的测试，以 bring up 运行 .NET 代码的一般能力。大多数这些测试非常简单，但让一些代码跑起来是处理更复杂场景的前提。
    在初始 bring up 时，将 GC 的 Gen0 budget 配置为一个很大的数，以便 GC 在大多数测试期间不尝试运行，会非常有用。（设置 `DOTNET_GCgen0size=99999999`）

-   一旦基本代码能够执行，重点就转向让 GC 可工作。
    在这一初始阶段，正确的选择是通过 `FEATURE_CONSERVATIVE_GC` 宏启用保守（conservative）的 GC 跟踪。
    该特性将使垃圾回收在很大程度上能够正确工作，但不适合用于 .NET 的生产场景，并且在某些情况下可能触发无界（unbounded）的内存使用。

-   一旦基本 GC 可用且具备基本的 JIT 功能，工作就可以扩展到运行时的各项特性上。
    对移植运行时的工程师而言，测试套件中尤其值得关注的是 EH、栈遍历（stackwalking）与互操作（interop）部分。

-   在该阶段，从 <https://github.com/dotnet/diagnostics> 移植 SOS 插件将非常有用。
    该工具提供的各种命令（如 dumpmt、dumpdomain 等）对尝试移植运行时的开发者来说经常很有帮助。

Stage 2 扩展场景覆盖
--------------------------------

-   一旦 coreclr 测试大体通过，下一步就是启用 XUnit。
    此时 CLR 大概率已经基本具备运行 XUnit 测试的能力，而为 libraries 测试增加测试将需要 XUnit 良好工作。

-   一旦 XUnit 可用，就 bring up libraries 测试集合。
    CoreCLR 代码库中有相当多内容主要是通过 libraries 测试套件来覆盖的。

-   工程师也应在此时开始尝试真实场景测试，例如 ASP.NET Core 应用。
    如果 libraries 测试套件可用，那么 ASP.NET Core 通常也应该可用。

Stage 3 聚焦性能
----------------------------

-   此时吞吐性能（throughput performance）可能并不理想。
    在这个阶段有三大机会来提升性能。

    -   用精确（precise）GC 替换保守（conservative）GC。

    -   调优汇编 stub，使其在该平台上具备高性能；并实现一些可选的汇编 stub：在这些场景中，手写汇编会比等价的 C++ 代码更快。

    -   改进 JIT 生成的代码。

-   在此之前，工程师很可能一直对所有代码使用 JIT，而没有在该平台上启用 Ready To Run 编译器（crossgen/crossgen2）。
    在此时实现 AOT 编译器会开始变得有价值，因为它可以改善启动性能。

Stage 4 聚焦压力测试
-----------------------

-   压力测试对于建立“系统确实能工作”的信心是必要的。

-   参见 CI 中进行的各种测试 pass，但最关键的是需要进行 GCStress 测试。
    参见关于使用 DOTNET_GCStress 环境变量的文档。

Stage 5 产品化
----------------------

-   产品化的目标是让运行时能够在某个平台上以可交付的方式稳定运行。

-   本文档不尝试列出此处需要做的工作，因为它很大程度上取决于所使用的平台以及众多利益相关方的观点。

设计问题
=============

这些与特定体系结构相关的大型设计问题会对 JIT 与 VM 产生重大影响。

1.  调用约定（calling convention）规则——Caller pop 与 Callee pop、HFA 参数、结构体参数传递规则等。
    CoreCLR 的设计旨在尽量采用与 OS API 大体相似的 ABI。
    托管到托管（managed-to-managed）调用通常会对 ABI 做一小部分为了 VM 效率的调整或扩展，但总体意图是托管代码的 ABI 与本机代码的 ABI 非常相似。
    （这不是硬性要求；在 Windows x86 上，运行时既支持托管到托管 ABI，也支持用于互操作的 3 种不同的本机 ABI，但通常不建议这种方案。）
    参见 [CLR-ABI](clr-abi.md) 文档了解现有体系结构是如何工作的。
    请确保在 CLR-ABI 文档中补齐新平台所需的所有细节与特殊情况。
    在为 CoreCLR 定义新的处理器架构 ABI 行为时，我们必须保持：

    1.  无论其他参数如何，`this` 指针始终在同一个寄存器中传递。

    2.  多种 stub 类型将需要一个额外的“secret”参数。具体放置位置通常由性能细节驱动。

    3.  执行托管代码时，必须能够劫持（hijack）返回地址。
        现有实现要求返回地址始终位于栈上才能做到这一点，尽管对于 arm64 之类的 RISC 平台而言，这是一个已知的性能缺陷。

2.  体系结构特定的重定位信息（用于表示为 load、store、jmp 与 call 指令生成重定位信息）。
    参见 <https://learn.microsoft.com/windows/win32/debug/pe-format#coff-relocations-object-only> 了解需要定义的那类细节。

3.  在进程内部访问处理器单步（single step）特性的行为与可访问性。
    在 Unix 上，CLR 调试器使用进程内线程来单步执行函数。

4.  展开（unwind）信息。
    CoreCLR 内部即使在 Unix 平台上也使用 Windows 风格的展开数据，因此必须定义一种 Windows 风格的展开结构。
    此外，还可以选择启用 DWARF 数据生成，以便通过 GDB JIT 接口暴露：
    <https://sourceware.org/gdb/onlinedocs/gdb/JIT-Interface.html>。
    该支持由一个 
    \#ifdef 条件控制，但过去曾用于支持新平台的 bring up。

5.  EH Funclets。
    .NET 需要两阶段（2 pass）异常模型才能正确支持异常筛选器（exception filters）。
    这与大多数 Linux 架构上使用的典型 Itanium ABI 模型有很大不同。

6.  OS 的 Signals 行为。尤其是报告的 instruction pointer 精确位于哪里。

7.  小端（little endian）与大端（big endian）。
    虽然过去 .NET 运行时曾被移植到大端平台（著名例子包括 Mono 对某些游戏主机的支持，以及 POWER、以及 XNA 在 Xbox360 上的支持），但目前 CoreCLR 没有任何对大端平台的移植。

受移植到新架构影响的运行时组件
==================================================================

这是 .NET runtime 中值得关注的、与体系结构相关的组件列表。
该列表并不完整，但覆盖了大多数需要开展工作的领域。

显著组件

1.  JIT。
    JIT 维护了整个栈中最多的体系结构相关逻辑。
    这并不意外。参见 [Porting RyuJit](../jit/porting-ryujit.md) 获取指导。

2.  CLR PAL。
    当移植到非 Windows OS 时，PAL 会是第一个需要移植的组件。

3.  CLR VM。
    VM 同时包含完全与体系结构无关的逻辑，以及非常机器相关的路径。

4.  unwinder。
    unwinder 用于在非 Windows 平台上进行栈展开。
    它位于 https://github.com/dotnet/runtime/tree/main/src/coreclr/unwinder。

4.  System.Private.CoreLib/System.Reflection。
    这里几乎没有 bring up 所必需的体系结构相关工作。
    一些“锦上添花”的工作包括：为该体系结构添加对 System.Reflection.ImageFileMachine 枚举、ProcessorArchitecture 枚举的支持，以及操作这些枚举的相关逻辑。

5.  为 PE 文件格式添加新架构所需的更改。
    另外，C# 编译器可能也需要一个新开关来生成该新架构对应的机器码。

6.  Crossgen/Crossgen2。
    作为将通用 MSIL 转换为机器相关逻辑的 AOT 编译器，它们将用于改善启动性能。

7.  R2RDump。
    用于诊断预编译代码中的问题。

8.  coredistools。
    对 GCStress 是必要的（如果确定指令边界并不简单），同时也用于 JIT 开发中的 SuperPMI asm diff。

9.  debug 与诊断组件。
    托管调试器与 profiler 超出本文档范围。

CLR PAL
------
PAL 提供了一个类似 Win32 API 的接口，因为 CLR 代码库最初是为运行在 Windows 平台而设计的。
PAL 主要关注 OS 无关性，但其中也包含体系结构相关的组件。

1. pal.h - 包含用于处理展开场景的体系结构相关细节，例如 `CONTEXT` / `_KNONVOLATILE_CONTEXT_POINTERS`/ `_RUNTIME_FUNCTION`。

2. `seh-unwind.cpp` 中的展开支持

3. context.cpp - 负责操纵与捕获寄存器上下文

4. jitsupport.cpp - 取决于 CPU 特性如何暴露，可能需要调用 OS API 来收集 CPU 特性信息。

5. pal arch 目录 - https://github.com/dotnet/runtime/tree/main/src/coreclr/pal/src/arch
   该目录主要包含用于体系结构相关信号与异常处理的汇编 stub。

除了 PAL 源码之外，还有一套完整的 PAL 测试，位于 https://github.com/dotnet/runtime/tree/main/src/coreclr/pal/tests。

CLR VM
------

VM 中对体系结构相关逻辑的支持以多种方式编码。

1.  完全体系结构特定的组件。这些放在一个体系结构特定的文件夹中。

2.  只在某些体系结构上启用的特性。例如 `FEATURE_HFA`。

3.  针对特定体系结构的临时性（ad-hoc）\#if 块。
    需要时再添加。总体目标是尽量减少这类块，但难度主要取决于该处理器架构需要哪些特殊行为。

我的建议是：查看 Arm64 在 VM 中的实现，以作为实现 CPU 架构时最“最新”的参考模型。

### 体系结构特定组件

有一系列体系结构特定组件是所有体系结构都必须实现的。

1.  汇编 Stubs

2.  `cgencpu.h`（CPU 特定头文件，定义 stubs 及其他各类 CPU 相关细节。）

3.  VSD 调用 stub 生成（virtualcallstubcpu.hpp 及其关联逻辑）

4.  Precode/Prestub/Jumpstub 生成

5.  `callingconventions.h`/`argdestination.h`
    为 VM 组件使用的 ABI 提供实现。其实现通过一长串 C 预处理宏变为体系结构特定。

6. `gcinfodecoder.h`
   GC info 格式是体系结构特定的，因为它包含“哪些特定寄存器持有 GC 数据”的信息。
   实现通常被简化为用寄存器编号来定义，但如果该体系结构有比现有体系结构更多的可用寄存器，则该格式需要扩展。

#### 汇编 Stubs

运行时出于多种原因需要各类汇编 stub。
下面是 Unix 上 Arm64 实现的 stubs 的一份带注释列表。

1.  纯性能。
    有些 stub 有等价的 C++ 实现，如果没有汇编 stub 就会使用 C++ 版本。
    随着编译器不断改进，直接使用 C++ 版本变得更合理。
    但最大的性能成本/收益往往来自手写的 fast path：它们不需要建立栈帧。
    许多 casting helpers 属于这一类。

    1.  `JIT_Stelem_Ref` – `JIT_Stelem_Ref_Portable` 的一个略快版本。

2.  通用正确性。
    有些 helper 会以有趣的方式调整其调用目标的 ABI、操纵/解析“secret”参数，或做一些并不容易用标准化 C++ 表达的事。

    1.  `CallDescrWorkerInternal` – 支持 VM 调用托管函数。对所有应用都是必需的，因为主方法（main method）就是这样被调用的。

    2.  `PInvokeImportThunk` – 支持保存一次 p/invoke 调用的参数集合，以便运行时能够找到真实目标。同时也使用一个 secret 参数（所有 p/invoke 方法都会用到）。

    3.  `PrecodeFixupThunk` – 支持将 secret 参数从 FixupPrecode\* 转换为 MethodDesc\*。
        该函数存在的目的是减少 FixupPrecode 的代码尺寸，因为它们有很多（许多托管方法会用到）。

    4.  `ThePreStub` - 支持把一组参数保存到栈上，以便运行时找到或 JIT 到正确的目标方法。
        （任何 JIT 方法要执行都需要它；所有托管方法都会用到。）

    5.  `ThePreStubPatch` – 提供一个可靠的位置，供托管调试器设置断点。

    6.  GC 写屏障（Write Barriers）– 用于向 GC 提供“哪些内存被更新了”的信息。
        现有实现都很复杂，并且运行时可以通过多种控制来调整屏障行为。
        有些调整涉及修改代码以注入常量，甚至完全替换多个片段。
        若要实现高性能，这些特性都必须可用；但在产品早期 bring up、只需要支持一个简单 GC 时，可以先聚焦于单堆（single heap）的 workstation GC 情况。
        此外，FEATURE_MANUALLY_MANAGED_CARD_BUNDLES 与 FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP 可在性能需求驱动下再实现。

    7.  `ComCallPreStub`/ `COMToCLRDispatchHelper` /`GenericComCallStub` - 目前对非 Windows 平台并不需要

    8.  `TheUMEntryPrestub`/ `UMThunkStub` - 用于通过 Marshal.GetFunctionPointerForDelegate API 生成的入口点从非托管代码进入运行时。

    9. `OnHijackTripThread` - 线程挂起所需，以支持 GC 及其他需要挂起的事件。
        通常在非常早期的 bring up 阶段不需要，但对任何体量较大的应用都需要。

    10. `CallEHFunclet` – 用于调用 catch、finally 与 fault funclets。行为取决于 funclets 的具体实现方式。

    11. `CallEHFilterFunclet` – 用于调用 filter funclets。行为取决于 funclets 的具体实现方式。

    12. `ResolveWorkerChainLookupAsmStub`/ `ResolveWorkerAsmStub`
        用于 virtual stub dispatch（支持接口与某些 virtual 方法的虚调用）。
        它们与 virtualcallstubcpu.h 中的逻辑配合，用于实现 [Virtual Stub Dispatch](virtual-stub-dispatch.md) 文档中描述的逻辑。

    13. `ProfileEnter`/ `ProfileLeave`/ `ProfileTailcall` – 用于调用通过 ICorProfiler 接口获取的函数入口/退出 profile 函数。
        只在极少数情况下使用。
        合理的做法是在产品化的最后阶段再实现这些。
        大多数 profiler 并不使用该功能。

    14. `JIT_PInvokeBegin`/`JIT_PInvokeEnd` – 离开/进入托管运行时状态。
        对 ReadyToRun 预编译的 pinvoke 调用是必需的，以免造成 GC 饥饿（GC starvation）。

    15. `VarargPInvokeStub`/ `GenericPInvokeCalliHelper` 用于支持 calli pinvoke。
        预计 C\# 8.0 会增加该特性的使用。
        当前在 Unix 上使用该特性需要手写 IL；在 Windows 上该特性常被 C++/CLI 使用。

#### cgencpu.h

该头文件被 VM 目录中的多处代码包含。
它提供了大量体系结构特定的功能，包括但不限于

1.  体系结构特定的定义，用于指定 VM 需要创建的各类数据结构的尺寸等

2.  定义：指定哪些 JIT helper 应当用 asm 函数替代 portable 的 C++ 实现

3.  CalleeSavedRegisters、ArgumentRegisters 与 FloatArgumentRegisters，用于描述该平台调用约定

4.  ClrFlushInstructionCache 函数。
    如果体系结构不需要手动 flush icache，则该函数为空。

5.  用于解码与操纵跳转指令的各种函数。
    这些函数用于一些 stub 例程来预测代码将跳转到哪里，并生成简单的跳转 stub。

6.  体系结构对应的 StubLinkerCpu 类。
    每个体系结构都定义自己的 StubLinkerCpu API surface，并用它来生成 VM 生成的代码。
    有一小部分 API 会从通用 VM 代码被调用（如 EmitComputedInstantiatingMethodStub、EmitShuffleThunkshared），跨多个体系结构；然后还有一系列体系结构特定的汇编指令发射函数。
    StubLinker 用于生成复杂 stub，不同 stub 需要发射的指令序列也不同。

7.  各种 stub 数据结构。
    许多非常简单的 stub 并不是通过“发射一串字节流”生成，而是高度规整：对于不同 stub 基本是同一组指令，只是数据成员略有不同。
    在这种情况下，VM 不使用 StubLinker 机制，而是使用一些结构体来表示整个 stub 及其关联数据，并通过普通构造函数填入“魔数”。
    除了可执行之外，这些 stub 也经常会被解析，以确定某个函数是什么、在做什么、控制流会去哪里等。

#### virtualcallstubcpu.h

该头文件用于为 virtual stub dispatch 提供各种 stub 的实现。
这些 stub 是 lookup、resolver 与 dispatch stubs，正如 [Virtual Stub Dispatch](virtual-stub-dispatch.md) 中所描述。
出于历史原因与体积原因（这里逻辑很多），它与 cgencpu.h 的其余部分分离维护。

System.Private.CoreLib
----------------------

### 初始 Bring up

在 System.Private.CoreLib 中，初始 bring up 不需要做任何工作。

### 完整支持

完整支持意味着需要更改产品对外可见的 API surface。
这是一项通过 GitHub 上的公开 issue 以及与 API review board 讨论来处理的流程。

-   为该体系结构增加对 System.Reflection.ImageFileMachine 枚举，以及 System.Reflection.ProcessorArchitecture 枚举与相关逻辑的支持

-   为体系结构相关内在函数（如 SIMD 指令）或其他非标准 API surface 增加支持
