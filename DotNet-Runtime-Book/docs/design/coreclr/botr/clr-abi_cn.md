# CLR ABI

本文档描述 .NET 公共语言运行时（CLR）的软件约定（或 ABI，“应用二进制接口”）。它重点介绍 x64（又称 AMD64）、ARM（又称 ARM32 或 Thumb-2）以及 ARM64 处理器架构的 ABI。关于 x86 ABI 的文档相对缺乏，但本文档底部包含了调用约定基础知识的相关信息。

它描述了即时（JIT）编译器对虚拟机（VM）施加的要求，以及 VM 反过来对 JIT 施加的要求。

关于 JIT 代码库的说明：JIT32 指最初生成 x86 代码、后来移植以生成 ARM 代码的原始 JIT 代码库。JIT64 指支持 AMD64 的旧版 .NET Framework 代码库。RyuJIT 编译器由 JIT32 演进而来，如今支持所有平台和架构。有关 RyuJIT 的更多历史，请参阅[这篇文章](https://devblogs.microsoft.com/dotnet/the-ryujit-transition-is-complete)。

NativeAOT 指一种针对预先编译（AOT）优化的运行时。为简化并在跨平台上保持一致性，NativeAOT ABI 在少数细节上有所不同。

# 入门

阅读已记录的 Windows 与非 Windows ABI 文档中的全部内容。CLR 遵循这些基本约定。本文档仅描述 CLR 特有的内容，或相对于那些文档的例外情况。

## Windows ABI 文档

AMD64：[参见 x64 软件约定](https://learn.microsoft.com/cpp/build/x64-software-conventions)。

ARM：[参见 ARM32 ABI 约定概览](https://learn.microsoft.com/cpp/build/overview-of-arm-abi-conventions)。

ARM64：[参见 ARM64 ABI 约定概览。](https://learn.microsoft.com/cpp/build/arm64-windows-abi-conventions)

## 非 Windows ABI 文档

Arm 公司（用于 ARM32 与 ARM64）的 ABI 文档在[这里](https://developer.arm.com/architectures/system-architectures/software-standards/abi)和[这里](https://github.com/ARM-software/abi-aa)。Apple 的 ARM64 调用约定差异可在[这里](https://developer.apple.com/documentation/xcode/writing-arm64-code-for-apple-platforms)找到。

Linux System V x86_64 ABI 记录在《System V 应用二进制接口 / AMD64 架构处理器补充》一文中，文档源材料在[这里](https://github.com/hjl-tools/x86-psABI/wiki/x86-64-psABI-1.0.pdf)。

LoongArch64 ABI 文档在[这里](https://github.com/loongson/LoongArch-Documentation/blob/main/docs/LoongArch-ELF-ABI-EN.adoc)。

RISC-V ABI 规范：[最新发布版](https://github.com/riscv-non-isa/riscv-elf-psabi-doc/releases/latest)、[最新草案](https://github.com/riscv-non-isa/riscv-elf-psabi-doc/releases)、[文档源代码仓库](https://github.com/riscv-non-isa/riscv-elf-psabi-doc)。

# 通用展开信息/栈帧布局

对于所有非 x86 平台，所有方法都必须具备展开信息，以便垃圾回收器（GC）能够对其进行展开（不同于原生代码，其中叶子方法可能被省略）。

ARM 与 ARM64：托管方法必须始终将 LR 压入栈，并创建一个最小栈帧，以便可以使用返回地址劫持（return address hijacking）对方法进行正确的劫持。

## 帧指针链

当帧指针寄存器指向栈上的某个位置，而该位置存放着已保存的前一个帧指针值的地址（来自当前函数调用者）时，就存在帧指针链。在某些场景下需要这种链，例如：

- Linux 上 gdb 调试器的栈回溯（stack walking）。
- ETW 事件跟踪的栈回溯。

有两个考量点：

- 为栈回溯保留帧指针寄存器，不将其用于其他用途（如通用代码生成），以及
- 创建帧链。

注意：即使某个函数没有被加入帧链，只要该函数不修改帧指针，现有的帧链仍然可用，不过在遍历链时该函数不会出现。即使不创建帧链，JIT 也可能有理由创建并使用帧指针寄存器，例如为了在异常处理 funclet 中访问主函数的局部变量。

各架构的帧指针寄存器分别为：ARM：r11，ARM64：x29，x86：EBP，x64：RBP。

除 Windows x64 外，JIT 在大多数情况下会为所有平台创建帧链。非常简单的函数可能不会被加入帧链，目的是通过减少栈帧建立成本来提升性能（该选择的启发式逻辑在 `Compiler::rpMustCreateEBPFrame()` 中）。对于 Windows x64，展开将始终使用生成的 unwind code，而不是通过简单的帧链遍历来完成。

一些额外链接：

- 关于该架构的栈帧设计文档，参见 [ARM64 JIT 栈帧布局](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/arm64-jit-frame-layout.md)。
- CoreCLR 在 Unix x64 上改为始终创建 RBP 链的变更在[这里](https://github.com/dotnet/coreclr/pull/4019)（相关讨论的问题在这里）。

> 可以把“帧指针链”想象成在调用堆栈（Stack）上建立的一个**“单向链表”**。
>
> - **帧指针寄存器（Frame Pointer, FP）**：这是一个特殊的 CPU 寄存器（在 x64 上是 `RBP`，在 ARM64 上是 `x29`）。它始终指向当前函数在栈上的“基准地址”。
> - **链条的形成**：当函数 A 调用函数 B 时，函数 B 在执行自己的逻辑之前，会先把函数 A 的帧指针（旧的 FP）压入栈中保存，然后让 FP 寄存器指向当前这个位置。
> - **结果**：这样，当前的 FP 指向的位置存储着上一个函数的 FP 地址，上一个 FP 指向的位置又存储着再上一个 FP 的地址…… 这样就形成了一条完整的“链”。

# 特殊/额外参数

## this 指针

托管的 this 指针被当作一种原生 ABI 未覆盖的新参数类型处理，因此我们选择始终将其作为第一个参数传递：在（AMD64）`RCX` 中，或在（ARM、ARM64）`R0` 中。

仅限 AMD64：在 .NET Framework 4.5 之前，托管 this 指针与原生 this 指针完全一致（这意味着当调用使用返回缓冲区时，它是第二个参数，并且在 `RDX` 中传递而不是 `RCX`）。从 .NET Framework 4.5 开始，它始终是第一个参数。

## 可变参数（Varargs）

Varargs 指在一次调用中传递或接收数量可变的参数。

C# 的 varargs（使用 `params` 关键字）在 IL 层面只是具有固定参数数量的普通调用。

托管 varargs（使用 C# 伪文档化的 “...”、`__arglist` 等）几乎与 C++ varargs 完全一样实现。最大的区别是：JIT 会在可选的返回缓冲区与可选的 this 指针之后、任何其他用户参数之前，添加一个 “vararg cookie”。被调用方必须将该 cookie 以及其后所有参数溢出（spill）到它们的 home 位置，因为它们可能会通过以 cookie 为基址的指针算术进行寻址。该 cookie 恰好是一个指向签名的指针，运行时可以解析它来：（1）报告可变参数部分中的任何 GC 指针，或（2）对通过 ArgIterator 提取的任何参数进行类型检查（并正确跨越遍历）。这由 `IMAGE_CEE_CS_CALLCONV_VARARG` 标记，不应与 `IMAGE_CEE_CS_CALLCONV_NATIVEVARARG` 混淆：后者才是真正的原生 varargs（没有 cookie），并且只应出现在能正确处理 pinning 与其他 GC 机制的 PInvoke IL stub 中。

在 AMD64 上，与原生一致，任何通过浮点寄存器传递的浮点参数（包括固定参数）都会在整数寄存器中进行 shadow（即复制一份）。

在 ARM 与 ARM64 上，与原生一致，不会向浮点寄存器中放入任何东西。

不过不同于原生 varargs，所有浮点参数不会被提升为 double（`R8`），而是保留其原始类型（`R4` 或 `R8`）（当然，这并不妨碍像托管 C++ 这样的 IL 生成器在调用点显式注入一次 upcast，并相应调整调用点签名）。这会导致将原生 C++ 移植到 C#，甚至仅仅通过不同风味的托管 C++ 来托管化时出现一些出乎意料的行为。

托管 varargs 仅在 Windows 上受支持。

托管/原生 varargs 仅在 Windows 上受支持。关于在非 Windows 平台支持托管/原生 varargs 的工作由此 [issue](https://github.com/dotnet/runtime/issues/82081) 跟踪。

## 泛型（Generics）

共享泛型。在代码地址无法唯一标识某个方法的泛型实例化时，就需要一个“泛型实例化参数”。很多时候 `this` 指针可以兼作实例化参数。当 `this` 指针不是泛型参数时，泛型参数将作为额外参数传递。在 ARM 与 AMD64 上，它位于可选的返回缓冲区与可选的 this 指针之后、任何用户参数之前。在 ARM64 与 RISC-V 上，泛型参数位于可选的 this 指针之后、任何用户参数之前。在 x86 上，如果函数的所有参数（包括 `this` 指针）都能放入参数寄存器（`ECX` 与 `EDX`），并且仍有可用的参数寄存器，我们就将隐藏参数放入下一个可用的参数寄存器；否则它作为最后一个栈参数传递。对于泛型方法（即方法本身带有类型参数，而不是类型带有类型参数），当前泛型参数是一个 MethodDesc 指针（我认为是 InstantiatedMethodDesc）。对于静态方法（没有 this 指针），泛型参数是一个 MethodTable 指针/TypeHandle。

有时 VM 会要求 JIT 报告并保持泛型参数存活（keep alive）。在这种情况下，它必须被保存在栈上的某处，并通过常规 GC 报告在整个方法期间保持存活（如果它是 `this` 指针，而不是 MethodDesc 或 MethodTable），但序言（prolog）与尾声（epilog）除外。还要注意：将其 home 的代码必须位于 GC 信息中被报告为 prolog 的代码范围内（这可能与 unwind 信息中报告为 prolog 的代码范围不同）。

泛型参数与 varargs cookie 之间没有定义/强制/声明的顺序，因为运行时不支持这种组合。VM 与各 JIT 中有一些代码看起来似乎支持它，但其他地方会断言并禁止它，因此没有任何测试；我会假设其中存在 bug 与差异（例如某个 JIT 使用的顺序与另一个 JIT 或 VM 不同）。

### 示例

```text
call([this 指针] [返回缓冲区指针] [泛型上下文] [用户参数]...)
```

> **当多套不同的泛型实例化（例如 `List<string>` 和 `List<object>`）共享同一份机器码时，运行时如何识别当前的类型上下文。**
>
> 为了理解这一部分，我们需要拆解以下几个核心概念：
>
> ### 1. 共享泛型 (Shared Generics)
>
> 在 .NET 中，为了节省内存和编译时间，JIT 编译器不会为每一种引用类型实例化都生成一份新的机器码。
>
> - **规则**：所有引用类型（Runtime handles them as pointers）的泛型实例化通常共享同一份机器码。例如，`MyMethod<string>` 和 `MyMethod<object>` 运行的是完全相同的汇编指令。
> - **问题**：既然代码是一样的，如果代码里有一句 `typeof(T)` 或者 `new T()`，机器码怎么知道当前的 `T` 到底是 `string` 还是 `object`？
>
> ### 2. 泛型实例化参数 (Generic Instantiation Parameter)
>
> 为了解决上述问题，JIT 在调用这些共享代码时，会悄悄地多传一个**隐藏参数**。这个参数就像是一个“字典”或“上下文索引”，告诉代码当前运行的具体类型信息。
>
> - 这个参数通常是一个指向  MethodTable（类型元数据）或 Generic Dictionary（泛型字典）的指针。
> - **传递位置**：文档中给出的调用顺序是： `call([this 指针] [返回缓冲区指针] [泛型上下文] [用户参数]...)` 可以看到，它排在非常靠前的位置，通常紧跟在 `this` 指针之后。
>
> ### 3. `this` 指针的优化
>
> 文档提到：“通常 `this` 指针可以充当泛型上下文”。
>
> - **逻辑**：如果你调用的是一个实例方法（比如 `list.Add(item)`），`this` 指针本身就指向了对象，而对象头上就有 MethodTable 指针。通过 `this` 指针，代码就能顺藤摸瓜找到 `T` 是什么。
> - **例外**：如果是静态方法（没有 `this` 指针）或者某些特殊的接口调用，就必须额外传递那个隐藏的泛型上下文参数。
>
> ### 4. GC 报备 (GC Reporting)
>
> 这一部分涉及到内存管理。
>
> - 那个隐藏的“泛型上下文参数”本质上是一个指向运行时内部结构的指针。
> - **要求**：VM 会要求 JIT 将这个参数保存在栈上的某个位置，并告诉垃圾回收器（GC）：“嘿，这个位置存的是泛型信息，别把它当成垃圾给回收了，也别在移动内存时把它搞丢了”。

## 异步（Async）

当支持时，Async 调用约定是在其他调用约定之上的增量扩展。适用场景被限制为常规的静态/虚调用，例如不支持 PInvoke 或 varargs。至少支持普通静态调用、带 `this` 参数的调用或带泛型隐藏参数的调用。

Async 调用约定增加了一个额外的 `Continuation` 参数以及一个额外的返回值；当该返回不为 null 时，它在语义上具有优先级。返回时 `Continuation` 非 null 表示计算尚未完成，形式结果尚未就绪。传入参数 `Continuation` 非 null 表示函数正在恢复执行，应从 `Continuation` 中提取状态并继续执行（同时忽略所有其他参数）。

`Continuation` 是一个托管对象，需要据此进行跟踪。GC 信息会把 Async 调用点处的 continuation 结果标记为存活。

### 返回 Continuation

为了返回 `Continuation`，我们使用一个易失/被调用方可破坏（callee-trash）的寄存器，该寄存器不能用于返回实际结果。

| arch | `REG_ASYNC_CONTINUATION_RET` |
| ------------- | ------------- |
| x86  | ecx  |
| x64  | rcx  |
| arm | r2  |
| arm64  | x2  |
| risc-v  | a2  |

### 传递 Continuation 参数

`Continuation` 参数与泛型实例化参数处于相同的位置；若两者同时存在，则紧随其后。对 x86 而言，参数顺序相反。

```text
call(["this" pointer] [return buffer pointer] [generics context] [continuation] [userargs])   // not x86

call(["this" pointer] [return buffer pointer] [userargs] [continuation] [generics context])   // x86
```

> ### 1. 什么是 Return Continuation（返回延续任务）？
>
> 在标准的 C# 异步编程中，如果一个方法不能立即完成，它会返回一个 `Task` 对象。这个 `Task` 存储在堆上，会产生内存分配开销。
>
> 在新的 **Async ABI** 下，为了压榨性能，CLR 改变了规则：
>
> - 核心逻辑：一个异步方法被调用后，它会有两个“返回值”：
>   1. **实际结果**（如 `int`、`bool` 等）：放在标准的返回值寄存器里（如 `RAX`）。
>   2. **Continuation 信号**：放在一个**额外的、专门的寄存器**里。
> - 如何理解这个信号？
>   - 如果这个额外的寄存器返回 `null`：表示方法**同步完成**了。调用者可以直接去标准返回值寄存器拿结果，不需要任何异步等待。
>   - 如果这个寄存器返回 **非空（一个对象地址）**：表示方法**挂起了（未完成）**。这个返回的对象就是 **Continuation**。它代表了“当异步操作真正完成后，下一步该做什么”。
>
> **通俗地说：** 传统的做法是：“我给你一个盒子（Task），你自己盯着它看什么时候有东西。” 新的 ABI 做法是：“我直接尝试给你结果。如果我能立刻做完，结果就在左手；如果我做不完，我右手会递给你一个‘回执单’（Continuation），等我做完了我会凭这张单子通知你。”
>
> ### 2. 为什么需要专门的寄存器？
>
> 为了效率。文档定义了 `REG_ASYNC_CONTINUATION_RET`，不同架构下使用的寄存器不同：
>
> | 架构      | 寄存器  |
> | :-------- | :------ |
> | **x64**   | **rcx** |
> | **ARM64** | **x2**  |
> | **x86**   | **ecx** |
>
> **注意：** 标准的返回值是在 `rax` (x64) 或 `x0` (ARM64)。这里特意选用了另一个寄存器来返回 Continuation，这样调用者可以同时检查两个寄存器，实现极速转换。
>
> ### 3. 参数传递规则（Passing Continuation）
>
> 除了“返回”回执，当你调用一个异步方法时，可能也需要“传入”一个回执。
>
> 文档规定了它的位置：
>
> - 它紧跟在**泛型实例化参数**（Generics Context）之后。
> - **非 x86 架构**：顺序是 `[this] [返回缓冲区] [泛型上下文] [Continuation] [用户参数...]`。
> - **x86 架构**：由于历史原因，顺序是反着的，`Continuation` 被放在了最后面。
>
> ### 为什么要这么做？
>
> 这套 ABI 设计的主要目的是**减少内存分配（Allocation Free）**：
>
> 1. **消除 Task 分配**：如果一个异步方法频繁地同步完成（例如从缓存读取数据），这种 ABI 允许它像普通方法一样通过寄存器返回，完全不需要创建 `Task` 对象。
> 2. **GC 友好**：`Continuation` 是一个托管对象，由 GC 跟踪。通过 ABI 明确它的位置，可以让 GC 在扫描栈时准确地知道哪里存了异步状态。
> 3. **高性能调度**：JIT 编译器可以根据这些寄存器约定生成非常精简的代码，省去了复杂的异步状态机包装。

> **ValueTask 设计之初也是为了减少 Task 带来的异步开销，那么它们之间的区别是什么？**
>
> 简单来说：`ValueTask` 是在 C# 语言和类型层面做的优化，而 ABI 文档里写的 Async 约定是在 **CPU 寄存器和 JIT 编译层面**做的“终极榨汁”。
>
> ### ValueTask 解决了什么？
>
> 在 `ValueTask` 出现之前，即使一个 `async` 方法立即返回（同步完成），它也必须在堆上分配一个 `Task` 对象。
>
> - **ValueTask 的做法**：通过引入一个 `struct`，如果结果已经准备好了，就直接把值塞在结构体里返回，**避免了同步完成时的堆分配**。
> - 遗留问题：虽然避免了 `Task` 分配，但由于它仍然遵循标准的 C# 异步状态机模式，它还是会有一些开销：
>   - 需要把 `ValueTask` 这个结构体（包含多个字段）在栈上拷来拷去。
>   - 调用方需要通过逻辑判断（`if (task.IsCompleted) ...`）来决定是拿值还是挂起。
>
> ### ABI 文档里的 Async 约定在做什么？
>
> 文档里提到的 `Continuation` 参数和 `REG_ASYNC_CONTINUATION_RET` 寄存器，是 .NET 10 引入的 **“零开销异步（Zero-overhead Async）”** 计划的一部分。
>
> 它的核心思想是：**与其返回一个包装了结果的结构体（ValueTask），不如直接让 CPU 寄存器同时返回“结果”和“挂起状态”。**
>
> #### 如何理解 Return Continuation？
>
> 在传统的调用约定中，一个函数通常只在 `RAX` (x64) 寄存器里返回一个值。而在这种新的 Async ABI 下，函数会**同时利用两个寄存器**返回信息：
>
> 1. **结果寄存器**（如 `RAX`）：直接存放方法的返回值（比如 `int` 或 `long`）。
> 2. **异步延续寄存器（REG_ASYNC_CONTINUATION_RET，如 `RCX`）**：存放 `Continuation`（延续任务）。
>
> **逻辑如下：**
>
> - **如果方法同步完成**：`RCX` (Continuation) 返回 **`null`**。调用方一看 `RCX` 是空的，直接从 `RAX` 拿走结果。**整个过程就像调用普通非异步函数一样快，没有任何对象分配。**
> - **如果方法需要异步等待**：`RCX` 返回一个 **非空的 Continuation 对象**。调用方一看 `RCX` 有东西，就知道：“哦，还没完呢，我得把这个对象挂起来等它回调。”
>
> ### 为什么要这么设计？（比 ValueTask 强在哪？）
>
> 这种设计是**极致的性能优化**：
>
> - **真正的零分配**：`ValueTask` 虽然减少了 `Task` 分配，但如果异步方法内部逻辑复杂，JIT 还是可能生成状态机。而这种 ABI 配合 JIT 的优化，可以让很多简单的异步方法完全不生成状态机类。
> - **寄存器级通信**：不再需要从内存或栈上的结构体（ValueTask）里读取字段。CPU 直接看一眼第二个寄存器是否为零，就能判断异步状态。
> - **更少的指令**：这种约定允许 JIT 生成更精简的代码来处理“快速路径”（即同步完成的情况）。

## 仅 AMD64：按值传递的值类型

与原生一致，AMD64 存在隐式 byref。任何大小不是 1、2、4 或 8 字节（即 3、5、6、7 或者 >= 9 字节）的结构体（在 IL 术语中为值类型），如果声明为按值传递，实际上会改为按引用传递。对于 JIT 生成的代码，它遵循原生 ABI：传入的引用是一个指向编译器在栈上生成的临时局部变量的指针。然而，在远程处理（remoting）或反射（reflection）的某些情况下，显然 `stackalloc` 太难了，因此它们会传入指向 GC 堆内的指针；因此 JIT 编译后的代码必须把这些隐式 byref 参数按“内部指针”（在 JIT 术语中为 BYREF）进行报告，以防被调用方走的是这些反射路径之一。同样，所有写入都必须使用带检查的写屏障（checked write barriers）。

AMD64 的原生调用约定（Windows 64 与 System V）要求被调用方在 RAX 中返回返回缓冲区地址。JIT 也遵循该规则。

## 仅 RISC-V：结构体按硬件浮点调用约定传递/返回

目前，像原生那样按照硬件浮点调用约定来传递/返回结构体的能力，[只支持到 16 字节](https://github.com/dotnet/runtime/issues/107386)；更大的结构体会与标准 ABI 不同，改为按整数调用约定（通过隐式引用）传递/返回。

## 返回缓冲区（Return buffers）

自 .NET 10 起，返回缓冲区必须始终由调用方在栈上分配。调用完成后，调用方负责在必要时使用写屏障将返回缓冲区复制到最终目的地。JIT 可以假定返回缓冲区总是在栈上，并据此进行优化，例如在向返回缓冲区写入 GC 指针时省略写屏障。此外，该缓冲区允许在方法内部用作临时存储，因为其内容不得被别名化，也不得跨线程可见。

仅 ARM64：当方法返回一个大于 16 字节的结构体时，调用方会预留一个具有足够大小与对齐的返回缓冲区来容纳结果。该缓冲区的地址作为参数通过 `R8`（在 JIT 中定义为 `REG_ARG_RET_BUFF`）传递给方法。被调用方不要求保留 `R8` 中存放的值。

> 在编译器和运行时层面，当一个方法需要返回数据时，通常有两种方式：
>
> - **寄存器返回**：如果返回值很小（如 `int`、`long`、`float` 或对象引用 `string`），它会被直接塞进 CPU 寄存器（如 x64 上的 `RAX`）。这是最快的方式。
> - **返回缓冲区**：如果返回值是一个**大的值类型（Struct）**，寄存器放不下（例如一个包含 4 个 `string` 字段的结构体）。此时，**调用方（Caller）** 会在自己的栈帧上预留一块内存，并将这块内存的地址作为一个“隐藏参数”传给**被调用方（Callee）**。这个预留的内存区域就叫“返回缓冲区”。
>
> 当你写  `MyStruct result = GetLargeStruct();` 时，底层实际发生的逻辑更像是：
>
> ```c#
> // 伪代码表示底层行为
> MyStruct buffer; // 调用方在栈上分配
> GetLargeStruct(&buffer); // 将缓冲区地址传给 Callee
> ```
>
> 被调用方法 `GetLargeStruct` 内部不会在最后执行 `ret` 传回值，而是通过这个指针直接将数据写入调用方的栈空间。

## 隐藏参数（Hidden parameters）

*Stub dispatch* —— 当一次虚调用使用 VSD stub 时，为了避免回填（back-patching）调用代码（或反汇编它），JIT 必须将用于加载调用目标的 stub 的地址，即“stub 间接单元”（stub indirection cell），放入 (x86) `EAX` / (AMD64) `R11` / (ARM) `R4` / (ARM NativeAOT ABI) `R12` / (ARM64) `R11`。在 JIT 中，这由 `VirtualStubParamInfo` 类封装。

*Calli Pinvoke* —— VM 需要将 PInvoke 的地址放在 (AMD64) `R10` / (ARM) `R12` / (ARM64) `R14`（在 JIT 中：`REG_PINVOKE_TARGET_PARAM`），并将签名（pInvoke cookie）放在 (AMD64) `R11` / (ARM) `R4` / (ARM64) `R15`（在 JIT 中：`REG_PINVOKE_COOKIE_PARAM`）。

*Normal PInvoke* —— VM 会基于签名共享 IL stub，但它希望调用栈与异常里能显示正确的方法，因此会将精确 PInvoke 的 MethodDesc 通过 (x86) `EAX` / (AMD64) `R10` / (ARM, ARM64) `R12`（在 JIT 中：`REG_SECRET_STUB_PARAM`）传入。然后在 IL stub 中，当 JIT 获得 `CORJIT_FLG_PUBLISH_SECRET_PARAM` 时，必须把该寄存器移动到一个编译器临时变量中。该值会作为 intrinsic `NI_System_StubHelpers_GetStubContext` 的返回值返回。

## 小的基本类型返回值（Small primitive returns）

小于 32 位的基本值类型会被扩展到 32 位：有符号小类型进行符号扩展，无符号小类型进行零扩展。这可能不同于标准调用约定：标准约定可能会让返回寄存器中未使用位的状态保持未定义。

## 小的基本类型参数（Small primitive arguments）

小的基本类型参数其高位是未定义的。这可能不同于标准调用约定：标准约定可能要求归一化（例如在 ARM32 与 Apple ARM64 上）。

在 RISC-V 上，小的基本类型参数会按照标准调用约定进行扩展。

> ### 1. 隐藏参数 (Hidden Parameters) —— “额外的暗号”
>
> 在 C# 代码里，你可能只写了 `obj.Method(a, b)`，但在底层 CPU 执行时，仅仅靠 `a` 和 `b` 是不够的。运行时需要一些“额外信息”来保证程序正确运行。
>
> - **Stub Dispatch（虚验证存根分派）：**
>   - **背景**：当你调用一个接口方法或虚方法时，程序在运行那一刻才知道到底该执行哪个具体的类方法。
>   - **问题**：为了性能，.NET 使用了一种叫 VSD (Virtual Stub Dispatch) 的技术。它不想每次调用都去查表，也不想在代码运行中频繁修改（back-patching）指令。
>   - **方案**：JIT 会找一个“备用寄存器”（比如 AMD64 上的 `R11`），把存根（Stub）的地址偷偷塞进去。这样方法跳转时，就能通过这个寄存器直接找到目标，而不需要修改调用方的原始指令。
> - **PInvoke（调用非托管 C/C++ 代码）：**
>   - **Calli PInvoke**：当你通过函数指针调用外部函数时，VM 需要知道目标的精确地址和签名。它把地址放在 `R10`，把签名（Cookie）放在 `R11`。这就像给非托管代码递了一张“入场券”。
>   - **Normal PInvoke**：即使是普通的 `[DllImport]`，为了让你在调试时能看到漂亮的堆栈信息，或者在报错时知道是哪个 C# 方法调用的，JIT 会把该方法的元数据（MethodDesc）藏在一个“秘密寄存器”（`REG_SECRET_STUB_PARAM`）里传给底层的辅助代码。
>
> **核心逻辑：** 这些参数对程序员透明，但对 JIT 和运行时维持高性能、支持调试和异常处理至关重要。
>
> ### 2. 小基本类型返回值 —— “把坑填满”
>
> - **场景**：如果你返回一个 `byte`（8位）或 `short`（16位），而 CPU 寄存器通常是 32 位或 64 位的。
> - 规则：.NET 规定必须进行扩展（Extension）。
>   - 如果是 `short`（有符号），就进行**符号扩展**：高位补 0 或 1，取决于正负，填满 32 位。
>   - 如果是 `byte`（无符号），就进行**零扩展**：高位全部补 0，填满 32 位。
> - **原因**：这比标准 C++ 的调用约定更严格。标准约定可能不管高位（高位可能是脏数据）。.NET 这样做是为了让接下来的操作更安全、更快，不需要在每次读取返回值后手动清理高位的“垃圾数据”。
>
> ### 3. 小基本类型参数 —— “谁负责清理？”
>
> 这部分说的是把参数**传给**方法时的情况：
>
> - **默认规则**：在大多数架构上，JIT 比较“懒”。如果你传一个 `byte`，寄存器的高位通常是**未定义**的（脏数据）。接收方方法如果需要用到完整的 32/64 位，必须自己负责去清理或截断这些高位。
> - 平台差异：
>   - **Apple ARM64 / ARM32**：这些平台有特殊的规则，可能要求在传参时就清理干净（归一化）。
>   - **RISC-V**：这是目前最守规矩的，严格遵守标准调用约定进行扩展。

# PInvokes

约定是：任何带有 InlinedCallFrame 的方法（无论是 IL stub，还是包含内联 PInvoke 的普通方法）都会在其 prolog/epilog 中分别保存/恢复所有非易失整数寄存器。这样做是为了让 InlinedCallFrame 只需包含返回地址、栈指针和帧指针。随后仅使用这三者就能通过常规的 RtlVirtualUnwind 启动一次完整的栈回溯。

遇到 PInvoke 时，JIT 会向 VM 查询是否应抑制 GC transition。是否抑制通过在 PInvoke 定义上添加一个特性来指示。如果 VM 指示要抑制 GC transition，那么在 IL stub 或内联场景中都会省略 PInvoke frame，并在非托管调用点附近插入一次 GC Poll。如果某个外层函数包含多个内联 PInvoke，但并非全部都请求抑制 GC transition，那么仍会为其他内联 PInvoke 构造 PInvoke frame。

对于 AMD64，带有 InlinedCallFrame 的方法必须使用 RBP 作为帧寄存器。

对于 ARM 与 ARM64，我们也将始终使用帧指针（R11）。这部分是由于帧链要求所致。不过，VM 对带有 InlinedCallFrames 的 PInvokes 也要求如此。

对于 ARM，VM 还依赖 `REG_SAVED_LOCALLOC_SP`。

这些依赖都会体现在 `InlinedCallFrame::UpdateRegDisplay` 的实现中。

当当前方法存在 PInvokes/InlinedCallFrame 时，JIT32 只生成一个 epilog（并让所有返回分支到该 epilog）。

## 按栈帧的 PInvoke 初始化（Per-frame PInvoke initialization）

InlinedCallFrame 在 IL stub 的入口处初始化一次，并在每一条执行内联 PInvoke 的路径上初始化一次。

在 JIT64 中，这发生在实际包含调用的基本块里，但会把初始化推出带有落地垫（landing pad）的循环之外，然后寻找支配块（dominator blocks）。对于 IL stub 与带有 EH 的方法，我们会放弃上述优化，把初始化放在第一个基本块中。

在 RyuJIT/JIT32（ARM）中，所有方法都被当作 JIT64 的 IL stub 来处理（即：按栈帧初始化在 prolog 之后立刻执行一次）。

JIT 会生成对 `CORINFO_HELP_INIT_PINVOKE_FRAME` 的调用，传入 InlinedCallFrame 的地址，以及 IL stub 场景下的 NULL 或 secret parameter。`JIT_InitPInvokeFrame` 会初始化 InlinedCallFrame，并将其设置为指向当前 Frame 链的顶部。然后它返回当前线程的原生 Thread 对象。

在 AMD64 上，JIT 会生成代码将 RSP 和 RBP 保存到 InlinedCallFrame 中。

仅对 IL stub，按栈帧初始化还包括将 `Thread->m_pFrame` 设置为 InlinedCallFrame（相当于把 Frame “压栈”）。

## 按调用点的 PInvoke 工作（Per-call-site PInvoke work）

当 GC transition 未被抑制时，会执行以下步骤。

1. 对直接调用，JIT 编译后的代码将 `InlinedCallFrame->m_pDatum` 设为调用目标的 MethodDesc。
   - 对 JIT64，IL stub 内的间接调用会将其设为 secret parameter（这看起来是冗余的，但可能自按栈帧初始化后发生了变化？）。
   - 对 JIT32（ARM）的间接调用，根据注释会把该成员设为压栈参数的大小；但实现实际上总是传 0。
2. 仅对 JIT64/AMD64：随后，对非 IL stub，会通过把 `Thread->m_pFrame` 设为指向 InlinedCallFrame 来将 InlinedCallFrame “压栈”（回忆一下：按栈帧初始化已将 `InlinedCallFrame->m_pNext` 设为指向先前的顶部）。对 IL stub，该步骤在按栈帧初始化中完成。
3. 通过设置 `InlinedCallFrame->m_pCallerReturnAddress` 使 Frame 生效。
4. 然后通过设置 `Thread->m_fPreemptiveGCDisabled = 0` 来切换 GC 模式。
5. 从现在开始，寄存器中不允许有任何 GC 指针处于存活状态。RyuJit 的 LSRA 通过在非托管调用与特殊 helper 之前添加特殊的 refPosition `RefTypeKillGCRefs` 来满足该要求。
6. 接下来是实际的 call/PInvoke。
7. 通过设置 `Thread->m_fPreemptiveGCDisabled = 1` 将 GC 模式切回。
8. 然后检查 `g_TrapReturningThreads` 是否被置位（非零）。如果是，则调用 `CORINFO_HELP_STOP_FOR_GC`。
   - 对 ARM，该 helper 调用会保留返回寄存器：`R0`、`R1`、`S0` 与 `D0`。
   - 对 AMD64，生成的代码必须手动保留 PInvoke 的返回值，例如将其移动到非易失寄存器或栈位置。
9. 从现在开始，寄存器中可以再次有 GC 指针处于存活状态。
10. 将 `InlinedCallFrame->m_pCallerReturnAddress` 清回 0。
11. 仅对 JIT64/AMD64：对非 IL stub，通过把 `Thread->m_pFrame` 重置回 `InlinedCallFrame.m_pNext` 来“弹栈” Frame 链。

保存/恢复所有非易失寄存器还有助于防止当前栈帧中未使用的寄存器意外携带来自父栈帧的存活 GC 指针值。参数寄存器与返回寄存器是“安全”的，因为它们不能是 GC 引用。任何引用都应已在别处被固定（pinned），并改以原生指针形式传递。

对于 IL stub，Frame 链不会在调用点弹出，因此必须在 epilog 之前以及任何 jmp 调用之前弹出。看起来我们不支持来自 PInvoke IL stub 的尾调用（tail calls）？

> **托管代码（C#）如何安全地跳入非托管代码（C++/Win32 API），以及这个过程中垃圾回收（GC）和堆栈追踪如何保持正常。**
>
> 在 C# 里，GC 随时可能发生。当 GC 发生时，它需要“暂停”所有线程，并扫描堆栈找到所有的对象引用。
>
> - **问题**：如果你正在执行一个 C++ 函数（PInvoke），GC 无法控制 C++ 代码。
> - **风险**：如果 C++ 代码跑了很久，GC 就会一直等，导致整个程序卡死；或者 GC 移动了对象，但 C++ 里的指针没更新，导致崩溃。
>
> 所以，PInvoke 不仅仅是一个函数调用，它是一次**“边境穿越”**。
>
> ### 什么是 `InlinedCallFrame`？（“边境护照”）
>
> `InlinedCallFrame` 就像是你离开托管区时在栈上留下的一个**“书签”**或**“护照”**。
>
> - **它的作用**：它记录了你离开托管代码瞬间的 CPU 状态（返回地址、栈指针、帧指针）。
> - **GC 如何利用它**：如果 GC 在你跑 C++ 代码时启动了，它会看到这个“书签”，从而知道：“哦，这个线程去外星旅游了，它的托管领地到这里为止，我可以安全地扫描它之前的栈”。
> - **寄存器保存**：文中提到要保存“非易失寄存器”，是为了确保从 C++ 回来后，C# 环境能原封不动地恢复。
>
> ### GC 模式切换：协作式 vs 抢占式
>
> 这是理解 PInvoke 的关键：
>
> - **托管模式（协作式 GC）**：线程必须定期检查 GC 是否要运行。
> - **非托管模式（抢占式 GC）**：线程告诉 GC：“我现在去跑 C++ 了，我保证不碰任何托管对象。你想什么时候运行 GC 请便，不用等我。”

# 异常处理（Exception handling）

本节描述 JIT 在生成用于实现托管异常处理（EH）的代码时需要遵循的约定。JIT 与 VM 必须就这些约定达成一致，才能得到正确的实现。

## Funclets

在所有平台上，托管 EH 处理程序（finally、fault、filter、filter-handler 以及 catch）都会被提取为各自独立的 “funclet”。对操作系统而言，它们就像一等函数一样对待（独立的 PDATA 与 XDATA（`RUNTIME_FUNCTION` 条目）等）。CLR 目前在很多方面仍把它们当作父函数的一部分。主函数与所有 funclet 必须在一次代码分配中完成分配（参见冷热拆分 hot cold splitting）。它们“共享” GC 信息。只有主函数的 prolog 可以被热补丁（hot patched）。

进入处理程序 funclet 的唯一方式是通过一次调用。在异常情况下，该调用来自 VM 的 EH 子系统，作为异常分派/展开（dispatch/unwind）的一部分。在非异常情况下，这称为本地展开（local unwind）或非本地退出（non-local exit）。在 C# 中，这可以通过从 try 体简单地贯穿/退出（fall-through/out）或显式 goto 来实现。在 IL 中，这总是通过在 try 体内使用 LEAVE 指令来完成，其目标是 try 体外的某个 IL 偏移。在这些情况下，该调用来自父函数的 JIT 代码。

## 克隆的 finally（Cloned finallys）

RyuJIT 试图通过沿“正常”控制流（即以非异常方式离开 try 体，例如 C# 的 fall-through）把被调用的 finally “内联化”，来加速正常控制流。该优化在所有架构上都受支持。

## 调用 Finally/非本地退出（Invoking Finallys/Non-local exits）

为了保证正确的前进性（forward progress）与 `Thread.Abort` 语义，对于 call-to-finally 的位置以及调用点的形态存在限制。返回地址不能位于对应的 try 体内（否则 VM 会认为 finally 在保护它自身）。返回地址必须位于任何外层受保护区域之内（以便 finally 体产生的异常能被正确处理）。

RyuJIT 会创建类似跳转岛（jump island）的结构：在 try 体之外放置一段代码，先调用 finally，然后分支到 leave/非本地退出的最终目标。随后，这个跳转岛会在 EH 表中被标记得仿佛它是一个克隆的 finally。克隆的 finally 子句可防止 Thread.Abort 在进入处理程序之前触发。通过让返回地址位于 try 体之外，我们满足了另一项约束。

## ThreadAbortException 相关考量（ThreadAbortException considerations）

线程中止（thread abort）有三种：（1）粗暴线程中止（rude thread abort），无法阻止，并且不会运行（所有？）处理程序；（2）调用 `Thread.Abort()` API；（3）由另一个线程注入的异步线程中止（asynchronous thread abort）。

注意：ThreadAbortException 在桌面框架（desktop framework）中完全可用，并在例如 ASP.NET 中被大量使用。然而，它在 .NET Core、CoreCLR 或 Windows 8 “modern app profile” 中不受支持。尽管如此，JIT 在所有平台上都会生成兼容 ThreadAbort 的代码。

对于非粗暴线程中止，VM 会遍历栈，运行任何能捕获 ThreadAbortException（或其父类，如 System.Exception 或 System.Object）的 catch 处理程序，并运行 finally。ThreadAbortException 有一个非常特殊的特性：如果某个 catch 处理程序捕获了 ThreadAbortException，并且处理结束后返回时没有调用 Thread.ResetAbort()，那么 VM 会 *自动重新抛出 ThreadAbortException*。为此，它使用 catch 处理程序返回的恢复地址（resume address）作为重新抛出被认为发生的位置的有效地址。该地址是 catch 处理程序中由 LEAVE 指令指定的标签地址。在某些情况下，JIT 必须插入合成的 “step blocks”，使该标签处于合适的外层 “try” 区域内，以确保该重新抛出能够被外层 catch 处理程序捕获。

例如：

```cs
try { // try 1
    try { // try 2
        System.Threading.Thread.CurrentThread.Abort();
    } catch (System.Threading.ThreadAbortException) { // catch 2
        ...
        LEAVE L;
    }
} catch (System.Exception) { // catch 1
     ...
}
L:
```

在这种情况下，如果 catch 2 返回的、对应标签 L 的地址位于 try 1 之外，那么 VM 重新抛出的 ThreadAbortException 将不会被 catch 1 捕获，而这并非预期。JIT 需要插入一个块，使得有效代码生成如下：

```cs
try { // try 1
    try { // try 2
        System.Threading.Thread.CurrentThread.Abort();
    } catch (System.Threading.ThreadAbortException) { // catch 2
        ...
        LEAVE L';
    }
    L': LEAVE L;
} catch (System.Exception) { // catch 1
     ...
}
L:
```

同样，ThreadAbortException 的自动重新抛出地址不能位于 finally 处理程序内部，否则 VM 会中止重新抛出并吞掉异常。这可能由于如上所述、被标记为 “cloned finally” 的 call-to-finally thunk 而发生。例如（这是伪汇编代码，不是 C#）：

```cs
try { // try 1
    try { // try 2
        System.Threading.Thread.CurrentThread.Abort();
    } catch (System.Threading.ThreadAbortException) { // catch 2
        ...
        LEAVE L;
    }
} finally { // finally 1
     ...
}
L:
```

这会生成类似如下内容：

```asm
	// beginning of 'try 1'
	// beginning of 'try 2'
	System.Threading.Thread.CurrentThread.Abort();
	// end of 'try 2'
	// beginning of call-to-finally 'cloned finally' region
L1:	call finally1
	nop
	// end of call-to-finally 'cloned finally' region
	// end of 'try 1'
	// function epilog
	ret

Catch2:
	// do something
	lea rax, &L1; // load up resume address
	ret

Finally1:
	// do something
	ret
```

注意：JIT 必须已经插入一个 “step” 块以便 finally 会被调用。然而，这还不足以支持 ThreadAbortException 的处理，因为 “L1” 被标记为 “cloned finally”。在这种情况下，JIT 必须再插入另一个 step 块：它位于 “try 1” 内部但在 cloned finally 块之外，从而保证正确的重新抛出语义。例如：

```asm
	// beginning of 'try 1'
	// beginning of 'try 2'
	System.Threading.Thread.CurrentThread.Abort();
	// end of 'try 2'
L1':	nop
	// beginning of call-to-finally 'cloned finally' region
L1:	call finally1
	nop
	// end of call-to-finally 'cloned finally' region
	// end of 'try 1'
	// function epilog
	ret

Catch2:
	// do something
	lea rax, &L1'; // load up resume address
	ret

Finally1:
	// do something
	ret
```

注意：JIT64 并未正确实现这一点。C# 编译器过去总会插入所有必要的 “step” 块。Roslyn C# 编译器一度没有这么做，但后来又改回再次插入它们。

## Funclet 参数（Funclet parameters）

Catch、Filter 以及 Filter-handler 会接收一个 Exception 对象（GC 引用）作为参数（`REG_EXCEPTION_OBJECT`）。在 AMD64 上，它通过 RCX（Windows ABI）或 RSI（Unix ABI）传递。在 ARM 与 ARM64 上，它是第一个参数并通过 R0 传递。

## Funclet 返回值（Funclet Return Values）

Filter funclet 在常规返回寄存器中返回一个简单的布尔值（x86：`EAX`，AMD64：`RAX`，ARM/ARM64：`R0`）。非零表示向 VM/EH 子系统表明对应的 filter-handler 将处理该异常（即开始第二趟）。零表示向 VM/EH 子系统表明该异常未被处理，应继续寻找另一个 filter 或 catch。

Catch 与 filter-handler funclet 会在常规返回寄存器中返回一个代码地址，用于指示 VM 在展开栈并完成异常清理后应从何处恢复执行。该地址应位于父 funclet 中（或者如果 catch 或 filter-handler 未嵌套于其他 funclet 中，则位于主函数中）。由于 IL 的 leave 指令可以退出任意层级的 funclet 与 try 体嵌套，JIT 往往需要注入 step blocks。它们是中间分支目标，然后再分支到下一个更外层的目标，直到真正目标能够在原生 ABI 约束下被直接到达。这些 step blocks 也可以调用 finally（参见 *调用 Finally/非本地退出*）。

Finally 与 fault funclet 没有返回值。

## 寄存器值与异常处理（Register values and exception handling）

异常处理对带有异常处理的函数中的寄存器使用施加了某些限制。

CoreCLR 与 “desktop” CLR 行为相同。CLR 的 Windows 与非 Windows 实现都遵循这些规则。

一些定义：

*非易失*（也称 *callee-saved* 或 *preserved*）寄存器是 ABI 定义的、函数调用必须保留的寄存器。非易失寄存器包括帧指针与栈指针等。

*易失*（也称 *caller-saved* 或 *trashed*）寄存器是 ABI 定义的、函数调用不必保留的寄存器，因此函数返回后它们可能具有不同的值。

### 进入 funclet 时的寄存器（Registers on entry to a funclet）

当异常发生时，VM 会被调用去做一些处理。如果异常位于某个 “try” 区域内，它最终会调用对应的处理程序（也包括调用 filter）。函数内的异常位置可能是某条 “throw” 指令执行处、如空指针解引用或除零之类的处理器异常发生点，或某次调用点（被调用方抛出异常但未捕获）。

VM 会将帧寄存器设为与父函数相同。这使得 funclet 能够使用基于帧的相对地址访问局部变量。

对于 filter funclet，在对应 “try” 区域的异常点处存在的所有其他寄存器值，在进入 funclet 时都会被破坏（trashed）。也就是说，唯一具有已知值的寄存器是 funclet 参数寄存器与帧寄存器。

对于其他 funclet，所有非易失寄存器都会恢复为异常点时的值。JIT 的代码生成器[目前并未利用这一点](https://github.com/dotnet/runtime/pull/114630#issuecomment-2810210759)。

### 从 funclet 返回时的寄存器（Registers on return from a funclet）

当 funclet 执行结束，VM 将执行返回到函数（或如果存在 EH 子句嵌套，则返回到外层 funclet）时，非易失寄存器会被恢复为异常点时的值。注意：易失寄存器已经被破坏。

funclet 中对寄存器值所做的任何更改都会丢失。如果 funclet 想让某个变量的更改对主函数（或包含该 “try” 区域的 funclet）可见，则该变量更改需要写入共享的主函数栈帧。这不是根本性限制。如有必要，运行时可以更新为保留 funclet 中对非易失寄存器所做的更改。

funclet 不要求保留非易失寄存器。

> ### 1. Funclets（异常处理小函数）
>
> 在 C# 源代码中，`catch` 或 `finally` 看起来是函数内部的一个块。但在底层，JIT 会将它们提取成**物理上独立的函数段**，称为 **Funclet**。
>
> - **为什么要这么做？** 为了兼容操作系统的栈回溯（Stack Unwind）机制。当异常发生时，OS 需要通过 `PDATA/XDATA` 查找当前指令所属的函数。将 EH 块变成独立的 Funclet，使得它们拥有独立的元数据，像普通函数一样被 OS 识别。
> - **共享状态**：虽然物理上分离，但它们共享父函数的栈帧（Stack Frame）。Funclet 通过父函数的帧指针（Frame Pointer）来访问局部变量。
>
> ### 2. 克隆 Finally 与非本地退出（Non-local exits）
>
> “非本地退出”是指通过 `leave`、`break` 或 `return` 离开 `try` 块。
>
> - **克隆 Finally**：为了性能优化，当代码正常离开 `try` 块（不是因为报错）时，JIT 往往会直接在原地插入一段 `finally` 的逻辑（内联），而不是通过复杂的异常分发机制去调用 `finally` Funclet。
> - **跳转岛（Jump Islands）**：当你从 `try` 块 `goto` 到外部的一个标签时，不能直接跳过去。JIT 会生成一个中转站（跳转岛），先在这里调用 `finally`，然后再跳往最终目标。
>
> ### 3. Funclet 的参数与返回值（ABI 层面）
>
> 这部分定义了寄存器级别的通信协议：
>
> - **输入**：当 VM 调用一个 `catch` 块时，它会将异常对象（Exception object）放在指定的寄存器中（如 x64 的 `RCX`）。这解释了为什么你在 C# 里写 `catch (Exception ex)` 就能拿到对象——它是作为参数传进来的。
> - **Filter 返回值**：`filter` 块返回一个布尔值给 VM，告诉 VM：“这个异常归我管（1）”还是“继续往后找（0）”。
> - **Catch 返回值**：这是一个关键点。`catch` 执行完后，它返回给 VM 的不是数据，而是一个**代码地址**。这个地址告诉 VM：当栈清理完毕后，程序应该从哪个地方**恢复执行**。
>
> ### 4. 寄存器状态的持久性（Register Volatility）
>
> 这是最硬核的部分，涉及编译器在异常发生时如何看待寄存器里的值。
>
> - 进入 Funclet 时：
>   - **Filter**：几乎所有寄存器都是“脏”的。它包含了帧指针（找变量用）和异常对象参数，你不应该指望寄存器里还存着什么有意义的值。
>   - **Catch/Finally**：非易失性寄存器（Callee-saved）会被恢复到异常发生瞬间的值。这意味着如果你在 `try` 块里改了一个保存在寄存器里的变量，进 `catch` 时它通常是正确的。
> - 返回到主函数时：
>   - **现状**：目前，如果在 Funclet（如 `catch`）里修改了一个本该存在寄存器里的局部变量，当回到主函数时，这个修改**可能会丢失**（除非 JIT 强制将该变量放在栈上）。
>   - **逻辑**：因为 Funclet 结束后，VM 会根据异常发生时的原始上下文恢复非易失性寄存器，从而覆盖掉 Funclet 里的修改。

# EH 信息、GC 信息以及热/冷拆分（Hot & Cold Splitting）

所有 GC 信息偏移与 EH 信息偏移都会把主函数与各个 funclet 当作一个巨大的方法体来处理。因此所有偏移都相对于主方法的起始位置。默认认为 funclet 总是在主函数全部代码的末尾（之后）。因此，如果主函数包含任何冷代码，那么所有 funclet 都必须是冷的。反过来，如果存在任何热的 funclet 代码，那么主方法的全部代码都必须是热的。

## EH 子句排序（EH clause ordering）

EH 子句必须按从内到外、从先到后排序，依据是 try 起始/try 结束这一对 IL 偏移。唯一的例外是克隆的 finally（cloned finallys），它们总是出现在最后。

## EH 如何影响 GC 信息/报告（How EH affects GC info/reporting）

因为当某个主函数的 funclet 在栈上时，该主函数体总是也在栈上，所以 GC 信息必须谨慎避免重复报告。JIT64 通过让所有具名局部变量都出现在父方法栈帧中来实现这一点；函数与 funclet 之间共享的任何内容都会被 home 到栈上；并且只有父函数报告栈上的局部变量（funclet 可能报告局部寄存器）。JIT32 与 RyuJIT（用于 AMD64、ARM、ARM64）采取了相反的方向：最叶子层（leaf-most）的 funclet 负责报告所有可能在离开 funclet 后仍然存活的内容（对于 filter，可能会恢复到原始方法体）。这通过 GC 头标志 WantsReportOnlyLeaf 来实现（JIT32 与 RyuJIT 会设置它，JIT64 不会），并由 VM 跟踪它是否已经为某个栈帧见过一个 funclet。一旦 JIT64 完全退役，我们就可以把这个标志从 GC 信息中移除。

在 VM 对 WantsReportOnlyLeaf 模型的实现中存在一个“边角情况”，它会影响 JIT 被允许生成的代码。考虑这个带有嵌套异常处理的函数：

```cs
public void runtest() {
    try {
        try {
            throw new UserException3(ThreadId);	// 1
        }
        catch (UserException3 e){
            Console.WriteLine("Exception3 was caught");
            throw new UserException4(ThreadId);
        }
    }
    catch (UserException4 e) { // 2
        Console.WriteLine("Exception4 was caught");
    }
}
```

当执行内部的 `throw new UserException4` 时，异常处理第一趟会找到外层 catch 处理程序将处理该异常。异常处理第二趟会将栈帧展开回 “runtest” 栈帧，然后执行该 catch 处理程序。在这期间存在一段时间：原来的 catch 处理程序（`catch (UserException3 e)`）已经不在栈上，但新的 catch 处理程序尚未开始执行。在这段时间内，可能发生一次 GC。在这种情况下，VM 需要确保为 “runtest” 函数正确报告 GC roots。内部 catch 已经被展开，所以我们不能报告它。我们也不想在仍在栈上的 “// 1” 位置进行报告，因为那等同于在执行上“倒退”，并不能正确反映哪些对象引用是存活的。我们需要在下一处将要执行的位置报告存活对象引用，也就是 “// 2” 位置。然而，我们不能报告 catch funclet 的第一个位置，因为那会是不可中断（non-interruptible）的。VM 会向前查找该处理程序中第一个可中断点，并在该位置报告 JIT 所报告的存活引用。这个位置将是处理程序 prolog 之后的第一个位置。VM 的这一实现对 JIT 有若干含义，它要求：

1. 具有 EH 子句的方法是完全可中断的（fully interruptible）。
2. 所有 catch funclet 在 prolog 之后立刻拥有一个可中断点。
3. catch funclet 的第一个可中断点需要在栈上报告以下存活对象：
   - 仅报告与父方法共享的对象，即不报告只在 catch funclet 中存活、而在父方法中不存活的额外栈对象。
   - 报告所有在 catch funclet 以及后续控制流中被引用的共享对象为存活。

## Filter 的 GC 语义（Filter GC semantics）

Filter 在 EH 处理的第一趟中被调用，因此执行可能恢复到故障地址（faulting address），也可能恢复到 filter-handler，或其他位置。由于 VM 必须允许在 filter 调用期间及之后发生 GC，但此时 EH 子系统尚不知道将从何处恢复执行，我们需要让故障地址处以及 filter 内部的所有内容都保持存活。这通过三种手段实现：（1）VM 的栈回溯器与 GCInfoDecoder 会把 filter 栈帧及其对应的父栈帧都报告为存活；（2）JIT 会把 filter 内部存活的所有栈槽编码为 pinned；（3）JIT 会把任何从 filter 中“活出”（live-out）的内容报告为存活（并可能进行零初始化）。由于（1），一个在 filter 与 try 体内都存活的栈变量很可能会被重复报告。GC 的标记阶段中，重复报告不是问题。问题只在对象被搬移（relocated）时出现：如果同一位置被报告两次，GC 会尝试对该位置存放的地址执行两次搬移。因此我们通过 pin 住它来防止对象被搬移，这也解释了为何必须做（2）。（3）的目的在于：filter 返回之后，我们仍然可以在执行 filter-handler 或同一栈帧中的任何外层处理程序之前安全地触发一次 GC。出于同样原因，控制流必须通过 filter 区域的最终块退出（换言之，一个 filter 区域必须以离开该 filter 区域的那条指令结束，程序不得通过其他路径退出 filter 区域）。

## 覆盖同一 try 区域的子句（Clauses covering the same try region）

若干个连续的子句可能覆盖同一个 `try` 块。一个与前一个子句覆盖相同区域的子句，会通过 `COR_ILEXCEPTION_CLAUSE_SAMETRY` 标志来标记。当异常 ex1 在处理另一个异常 ex2 的 handler 期间被抛出，并且异常 ex2 逃逸出了 ex1 的 handler 栈帧时，这使得运行时能够跳过那些覆盖与处理 ex1 的子句相同 `try` 块的子句。
该标志被 NativeAOT 使用，也被 CoreCLR 的一种新异常处理机制使用。NativeAOT 不会把该标志存入编码后的子句数据中，而是在拥有相同 `try` 块的子句之间注入一个虚拟子句。CoreCLR 则把该标志作为运行时子句数据表示的一部分保留。当前 CoreCLR 的异常处理并不使用它，但正在开发的一种[新异常处理机制](https://github.com/dotnet/runtime/issues/77568)正在利用它。

## GC 可中断性与 EH（GC Interruptibility and EH）

VM 假定：任何时候线程被停止时，它必须处于一个 GC 安全点，或者当前栈帧是不可恢复的（即该 throw 永远不会在同一栈帧中被捕获）。因此，实际上所有带 EH 的方法都必须完全可中断（或至少所有 try 体）。目前 GC 信息看起来支持在同一方法中混合部分可中断与完全可中断区域，但没有任何 JIT 使用这一点，因此风险自负。

调试器总是希望停在 GC 安全点，因此可调试代码应当完全可中断，以最大化调试器能够安全停止的位置。如果 JIT 在完全可中断代码中创建了不可中断区域，代码应确保每个序列点（sequence point）都从一条可中断指令开始。

仅 AMD64/JIT64：如有需要，JIT 会插入一个可中断的 NOP。

## 安全对象（Security Object）

安全对象是一个 GC 指针，必须按 GC 指针进行报告，并在方法的整个持续时间内保持存活。

## GS Cookie

GS Cookie 不是 GC 对象，但仍需要被报告。由于它在 GC 信息中的编码/报告方式，它只能拥有一个生命周期。由于一旦弹栈 GS Cookie 就不再有效，epilog 不能成为存活区间的一部分。由于只能有一个存活区间，这意味着在带有 GS cookie 的方法中，epilog 之后不能再有任何代码（funclet 除外）。

## NOP 与其他填充（NOPs and other Padding）

### AMD64 填充信息（AMD64 padding info）

unwind 回调不知道当前栈帧是叶子帧还是返回地址。因此，JIT 必须确保一次调用的返回地址与该调用处于同一个区域中。具体而言，如果某次调用之后将直接紧邻以下位置的起始处：try 体的开始、try 体的结束、或方法的结束，那么 JIT 必须在该调用之后插入一个 NOP（或其他指令）。

操作系统的 unwinder 有一个优化：如果一次 unwind 的结果使 PC 落在（或正位于）某个 epilog 中，它会认为该栈帧不重要并再次 unwind。由于 CLR 认为每个栈帧都很重要，它不希望发生这种二次 unwind 行为，因此要求 JIT 在任何调用与任何 epilog 之间放置一个 NOP（或其他指令）。

### ARM 与 ARM64 填充信息（ARM and ARM64 padding info）

操作系统 unwinder 使用 `RUNTIME_FUNCTION` 的范围（extents）来确定要从哪个函数或 funclet 展开出去。其结果是：对 `IL_Throw` 的一次调用（bl 指令）不能是最后一条指令。因此类似 AMD64，当 `bl IL_Throw` 原本会成为某个函数或 funclet 的最后一条指令、热区末尾之前的最后一条指令，或（这可能是 x86 习惯泄漏到 ARM）某个“特殊 throw 块”之前的最后一条指令时，JIT 必须注入一条额外指令（在这种情况下是一个断点）。

> ### 1. 热/冷拆分 (Hot/Cold Splitting)
>
> **核心逻辑**：为了提高 CPU 缓存命中率，JIT 会把不常用的代码（冷代码，如 `throw` 块或 `catch` 块）放到内存的另一个区域。
>
> - **约束**：在汇编层面，所有的偏移量（Offset）都是相对于方法起始地址（Main Method Start）计算的。
> - **规则**：Funclets（异常处理小函数）默认是冷的。如果主函数有冷代码，Funclets 必须跟着变冷，否则相对于起始点的偏移量计算会乱套。
>
> ### 2. GC 报告：谁来负责扫描栈上的引用？
>
> 当一个方法抛出异常并进入 `catch` 块（Funclet）时，栈上其实存在两个逻辑相关的帧：**父方法帧**和**当前的 Funclet 帧**。
>
> #### WantsReportOnlyLeaf 模式
>
> - **问题**：如果两个帧都向 GC 报告同一个局部变量（存放在主方法栈帧里），GC 可能会对同一个内存地址执行两次“搬移”（Relocation），导致指针损坏。
> - **解决方案**：RyuJIT 使用“仅报告叶子节点”模式。即当前执行到哪（哪个 Funclet），就由谁负责报告所有相关的 GC 引用。
>
> #### 异常转换期间的 GC 漏洞（The Gap）
>
> 考虑下面的 Demo：
>
> ```c#
> void Run() {
>     try {
>         try { throw new Exception3(); } // 位置 1
>         catch (Exception3) { 
>             // 此时 Exception3 帧已销毁
>             throw new Exception4(); 
>         } 
>     }
>     catch (Exception4) { // 位置 2
>         /* 执行到这里之前，如果发生 GC 怎么办？ */
>     }
> }
> ```
>
> - **技术细节**：在 `Exception3` 的 `catch` 块弹出，但 `Exception4` 的 `catch` 块尚未进入的间隙，如果发生 GC，VM 必须能找到引用。
> - **JIT 要求**：`catch` 块的 Prolog 之后的第一条指令必须是一个 **GC 安全点（Interruptible Point）**，并且在这个点上，必须报告所有父方法中存活的引用。
>
> ### 3. 完全可中断性 (Fully Interruptible)
>
> **核心逻辑**：通常代码只有在 `call` 这种特定指令（安全点）才能停下来做 GC。但带 EH 的方法必须支持**在任何一条指令处**停下来。
>
> - **理由**：异常可能在任何指令发生（如空指针）。如果方法不是完全可中断的，GC 就无法在异常发生的那一刻精确地扫描栈。
>
> ###  4.指令填充 (NOP Padding)：保护边界
>
> 这是一个非常经典的底层 Bug 预防机制。
>
> #### 案例：调用在 `try` 块的最后一行
>
> ```asm
> ; 伪代码
> try_start:
>     call DoSomething  ; 如果这是 try 块最后一条指令
> try_end:
>     ...
> ```
>
> - **问题**：`call` 指令的“返回地址”是它的下一条指令地址。如果 `call` 恰好是 `try` 的最后一条，那么它的返回地址就会落在 `try_end` 之外。
>
> - **后果**：如果 `DoSomething` 抛出异常，OS 的 Unwinder 会查看返回地址。因为它落在 `try_end` 之外，系统会判定异常发生在 `try` 块外部，导致 `catch` 失效！
>
> - JIT 修正：
>
>   ```asm
>   try_start:
>       call DoSomething
>       nop             ; 插入 NOP，确保返回地址仍在 try 范围内
>   try_end:
>   ```
>
> #### ARM 上的特殊处理
>
> 在 ARM64 上，如果 `bl IL_Throw`（抛异常的调用）是函数最后一条指令，Unwinder 可能会误判当前的函数范围。JIT 会强制在末尾插入一个断点（Breakpoint）或指令，防止 `throw` 变成方法的“物理终点”。

# Profiler Hook

如果 JIT 接收到 `CORJIT_FLG_PROF_ENTERLEAVE`，那么它可能需要插入原生的进入/退出/尾调用探针（probes）。为了确定是否确实需要，JIT 必须调用 GetProfilingHandle。该 API 通过 out 参数返回：一个真实的动态布尔值，用于指示 JIT 是否应当实际插入探针；以及一个要传给回调的参数（类型为 void*），并带有可选的一层间接（用于 NGEN）。该参数始终是所有回调外呼（call-out）的第一个参数（因此按常规第一个参数寄存器传递：`RCX`（AMD64）或 `R0`（ARM、ARM64））。

在 prolog 之外（位于一个 GC 可中断的位置），JIT 会注入一次对 `CORINFO_HELP_PROF_FCN_ENTER` 的调用。对 AMD64 而言，在 Windows 上所有参数寄存器都会被 home 到由调用方分配的栈位置（类似 varargs）；在 Unix 上，所有参数寄存器会被存入内部结构体。对 ARM 与 ARM64，所有参数都会被预先溢出（prespilled，同样类似 varargs）。

在计算出返回值并将其放入正确寄存器之后，但在任何 epilog 代码之前（包括可能的 GS cookie 检查之前），JIT 会注入一次对 `CORINFO_HELP_PROF_FCN_LEAVE` 的调用。对 AMD64，这次调用必须保留返回寄存器：在 Windows 上为 `RAX` 或 `XMM0`，在 Unix 上为 `RAX` 与 `RDX` 或 `XMM0` 与 `XMM1`。对 ARM，返回值会从 `R0` 移到 `R2`（如果它原本在 `R0` 中）；被调用方必须保留 `R1`、`R2` 与 `S0/D0`（long 会用 `R2`、`R1`——注意这种不寻常的寄存器顺序；float 在 `S0`，double 在 `D0`，更小的整数在 `R2`）。

TODO：描述 ARM64 的 profile leave 约定。

对任何尾调用或跳转调用，在进行参数设置之前（但在任何参数副作用之后），JIT 会注入一次对 `CORINFO_HELP_PROF_FCN_TAILCALL` 的调用。注意：对于被改写成循环的自递归尾调用，不会调用它。

对 ARM 的尾调用，JIT 实际上会先加载外发参数，然后在 profiler 外呼之前，把 `R0` 中的参数溢出到另一个非易失寄存器，进行调用（在 `R0` 中传入回调参数），然后再恢复 `R0`。

对 AMD64，所有探针都会接收第二个参数（按默认参数规则在 `RDX` 中传递），它是参数 home 位置起始地址（等价于调用方栈指针的值）。

TODO：描述 ARM64 的尾调用约定。

在 Linux/x86 上，profiling hook 使用 `__cdecl` 特性声明。在 cdecl（C declaration）中，子程序参数通过栈传递。整数值与内存地址在 EAX 寄存器中返回，浮点值在 ST0 x87 寄存器中返回。寄存器 EAX、ECX 与 EDX 由调用方保存，其余由被调用方保存。调用新函数时，x87 浮点寄存器 ST0 到 ST7 必须为空（已弹出或释放）；函数退出时 ST1 到 ST7 必须为空。ST0 在不用于返回值时也必须为空。托管代码的返回值在 leave/tailcall profiling hook 之前就已形成，因此这些 hook 应当保存它们并在从 hook 返回时恢复。用于实现 profiling hook 的汇编指令 `ret` 不应带参数。

当存在 profiler hook 时，JIT32 只生成一个 epilog（并使所有返回都跳转到它）。

# 同步方法（Synchronized Methods）

当方法被同步（synchronized）时，JIT32/RyuJIT 也只生成一个 epilog（并使所有返回都跳转到它）。参见 `Compiler::fgAddSyncMethodEnterExit()`。用户代码会被包裹在 try/finally 中。在 try 体之外/之前，代码会把一个布尔值初始化为 false。随后调用 `CORINFO_HELP_MON_ENTER` 或 `CORINFO_HELP_MON_ENTER_STATIC`，传入锁对象（实例方法传 `this` 指针，静态方法传 Type 对象）以及该布尔值的地址。如果获取到锁，该布尔值被设置为 true（以“原子”操作的意义，即 Thread.Abort/EH/GC 等不能在布尔值与锁的已获取状态不一致时中断线程）。在 finally 中放置 `CORINFO_HELP_MON_EXIT` / `CORINFO_HELP_MON_EXIT_STATIC` 的调用时，JIT32/RyuJIT 遵循完全相同的逻辑与参数。

# Rejit

为支持 AMD64 上的 profiler attach 场景，JIT 可能被要求确保每个生成的方法都可热补丁（参见 `CORJIT_FLG_PROF_REJIT_NOPS`）。我们的做法是确保代码的前 5 个字节是不可中断的，并且这 5 个字节内没有分支目标（包括调用/返回）。这样 VM 就能停止所有线程（例如为 GC）并安全地用一条跳转指令替换这 5 个字节，从而跳到该方法的新版本（推测由 profiler 插桩）。JIT 通过添加 NOP 或增大在 GC 信息中报告的 prolog 大小来满足这两项要求。

在带有异常处理的函数中，只有主函数受影响；funclet 的 prolog 不会被做成可热补丁。

# 编辑并继续（Edit and Continue）

Edit and Continue（EnC）是一种特殊风格的未优化代码。调试器必须能够可靠地把方法状态（指令指针与局部变量）从原始方法代码重映射到编辑后的方法代码。这对 JIT 所做的方法栈布局施加了约束。关键约束是：编辑后既有局部变量的地址必须保持不变。之所以需要这一约束，是因为局部变量的地址可能已被存入方法状态。

在当前设计中，JIT 无法访问该方法的先前版本，因此它必须按最坏情况处理。EnC 以简单为目标，而不是生成代码的性能。

EnC 目前只在 x86、x64 与 ARM64 上启用，但如果未来在其他平台启用，同样的原则也适用。

以下各节描述必须遵循的 Edit and Continue 代码约定。

## GCInfo 中的 EnC 标志（EnC flag in GCInfo）

JIT 会在 GC 信息中记录它已遵循 EnC 代码约定。在 x64/ARM64 上，该标志通过记录在 EnC 编辑之间需要保留的栈帧区域大小来隐含表示（`GcInfoEncoder::SetSizeOfEditAndContinuePreservedArea`）。对 x64，该区域大小会增加以包含 `RSI` 与 `RDI`，这样就可以使用 `rep stos` 进行块初始化与块移动。ARM64 在启用 EnC 时只保存 FP 与 LR 寄存器，并且不使用其他被调用方保存寄存器。

为了成功执行 EnC 迁移，运行时需要知道它正在迁移的两个栈帧的大小。对 x64 代码，这个大小可以从 unwind code 中提取。对 arm64 代码则不行，因为栈帧的建立方式使得 unwind code 无法取回该值。因此，在 ARM64 上，GC 信息还包含用于 EnC 目的的固定栈帧大小。

## 以反向方式分配局部变量（Allocating local variables backward）

这要求在 EnC 编辑追加新局部变量时，既有局部变量的地址能够保持不变。换言之，第一个局部变量必须分配在最高的栈地址。需要特别注意处理对齐。编辑后方法栈帧总大小既可能增长（增加了更多局部变量），也可能缩小（需要的临时变量更少）。VM 会把新增的局部变量清零。

## 固定的一组被调用方保存寄存器（Fixed set of callee-saved registers）

这消除了在 VM 中处理不同寄存器集合的需要，并让保持局部变量地址更容易。易失寄存器数量很多，因此缺少非易失寄存器不会严重影响未优化代码的质量。
x64 当前保存 RBP、RSI 与 RDI，而 ARM64 只保存 FP 与 LR。

## EnC 支持带 EH 的方法

不过，EnC 重映射不支持在 funclet 内部进行。funclet 的栈布局对 EnC 不重要。

## Localloc

EnC 代码允许 localloc，但在方法执行过 localloc 指令之后，禁止重映射。VM 使用上述不变量（x64 上 `RSP == RBP`，ARM64 上 `FP + 16 == SP + stack size`）来检测该方法是否执行过 localloc。

## 安全对象（Security object）

在 x64/arm64 上，这不需要 JIT 做任何特殊处理。（不同于 x86。）如有需要，VM 会在重映射期间拷贝安全对象。安全对象的位置通过 GC 信息找到。

## 同步方法（Synchronized methods）

JIT 为同步方法创建的额外状态（“已获取锁”标志）必须在重映射期间保留。JIT 将该状态存入保留区域，并相应增大在 GC 信息中报告的保留区域大小。

## 泛型（Generics）

EnC 支持添加与编辑泛型方法、泛型类型上的方法，以及非泛型类型上的泛型方法。

## 异步方法（Async methods）

JIT 会在运行时异步方法中保存当前的 `ExecutionContext` 与 `SynchronizationContext`，这些必须在重映射期间保留。新的 GC 编码器会把这部分状态计入 EnC 栈帧头大小；而对 JIT32，当 `getMethodInfo` 向 JIT 报告了 `CORINFO_ASYNC_SAVE_CONTEXTS` 时，EE 期望这部分状态存在。

# 可移植入口点（Portable entrypoints）

在允许动态代码生成的平台上，运行时通过分配 [`Precode`](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/method-descriptor.md#precode)s 来抽象动态加载方法的执行策略。`Precode` 是一小段代码片段，用作临时的方法入口点，直到获得实际的方法代码。`Precode` 也用于那些没有常规 JIT 或 AOT 编译代码的方法的执行过程，例如 stub 或解释执行方法。`Precode` 使原生代码能够在不考虑目标方法所采用的执行策略的情况下，始终使用相同的原生调用约定。

在不允许动态代码生成的平台（Wasm）上，运行时通过为动态加载的方法分配可移植入口点来抽象执行策略。`PortableEntryPoint` 是一种数据结构，允许高效地切换到目标方法所需的执行策略。当运行时被配置为使用可移植入口点时，托管调用约定会按如下方式修改：

- 要调用的原生代码通过对入口点解引用获得
- 入口点地址作为一个额外的最后隐藏参数传入。该额外隐藏参数必须出现在所有方法的签名中。它在 JIT 或 AOT 编译方法的代码中不会被使用。

带可移植入口点的调用伪代码：

> ```
> (*(void**)pfn)(arg0, arg1, ..., argN, pfn)
> ```

目前，可移植入口点只用于仅解释器模式的 Wasm。注意：对带原生 AOT 的 Wasm 来说不需要可移植入口点，因为原生 AOT 不支持动态加载。

# System V x86_64 支持

本节主要涉及 System V 系统（如 Ubuntu Linux 与 Mac OS X）上的调用约定。总体遵循 System V x86_64 ABI 文档中概述的通用规则，但有少量例外，如下所述：

1. 按值传递结构体的隐藏参数总是在 `this` 参数之后（如果存在）。这与 System V ABI 不同，并且只影响内部 JIT 调用约定。对 PInvoke 调用，隐藏参数总是第一个参数，因为此时没有 `this` 参数（除 `CallConvMemberFunction` 情况外）。
2. 没有任何字段的托管结构体总是按值在栈上传递。
3. 为了帮助原生 OS 工具进行栈展开等，JIT 会主动生成帧寄存器栈帧（以 `RBP` 作为帧寄存器）。
4. 关于 PInvoke、EH 与泛型支持的其他内部 VM 契约仍然适用。更多细节请参阅上面的相关章节。但请注意，由于调用约定不同，在 System V 上使用的寄存器也不同。例如，整数参数寄存器依次为 RDI、RSI、RDX、RCX、R8、R9。因此，在 Windows AMD64 上通常放在 RCX 中的第一个参数（通常是 `this` 指针），在 System V 上会放在 RDI 中，依此类推。
5. 显式布局（explicit layout）的结构体总是按值在栈上传递。
6. 下表描述了 System V x86_64 ABI 下的寄存器用法：

```text
| Register      | Usage                                   | Preserved across  |
|               |                                         | function calls    |
|---------------|-----------------------------------------|-------------------|
| %rax          | temporary register; with variable argu- | No                |
|               | ments passes information about the      |                   |
|               | number of SSE registers used;           |                   |
|               | 1st return argument                     |                   |
| %rbx          | callee-saved register; optionally used  | Yes               |
|               | as base pointer                         |                   |
| %rcx          | used to pass 4st integer argument to    | No                |
|               | to functions                            |                   |
| %rdx          | used to pass 3rd argument to functions  | No                |
|               | 2nd return register                     |                   |
| %rsp          | stack pointer                           | Yes               |
| %rbp          | callee-saved register; optionally used  | Yes               |
|               | as frame pointer                        |                   |
| %rsi          | used to pass 2nd argument to functions  | No                |
| %rdi          | used to pass 1st argument to functions  | No                |
| %r8           | used to pass 5th argument to functions  | No                |
| %r9           | used to pass 6th argument to functions  | No                |
| %r10          | temporary register, used for passing a  | No                |
|               | function's static chain pointer         |                   |
| %r11          | temporary register                      | No                |
| %r12-%r15     | callee-saved registers                  | Yes               |
| %xmm0-%xmm1   | used to pass and return floating point  | No                |
|               | arguments                               |                   |
| %xmm2-%xmm7   | used to pass floating point arguments   | No                |
| %xmm8-%xmm31  | temporary registers                     | No                |
```

# x86 的调用约定细节（Calling convention specifics for x86）

不同于 RyuJIT 支持的其他架构，托管 x86 调用约定与默认的原生调用约定不同。这对 Windows 与 Unix x86 都成立。

标准托管调用约定是 Windows x86 fastcall 约定的一种变体。它主要区别在于参数入栈的顺序。

只有以下值可以通过寄存器传递：托管与非托管指针、对象引用、以及内建整数类型 int8、unsigned int8、int16、unsigned int16、int32、unsigned it32、native int、native unsigned int，以及仅包含一个 4 字节整数基本类型字段的枚举与值类型。枚举按其底层类型传递。所有浮点值与 8 字节整数值都通过栈传递。当返回类型是无法通过寄存器传递的值类型时，调用方应当创建一个缓冲区来保存结果，并将该缓冲区地址作为隐藏参数传入。

参数按从左到右顺序传递，从 `this` 指针开始（用于实例与虚方法），随后是必要时的返回缓冲区指针，然后是用户指定的参数值。在这些参数中，第一个能够放入寄存器的会放入 ECX，下一个放入 EDX，其余都通过栈传递。这与 x86 原生调用约定形成对比：原生约定会按从右到左顺序把参数压栈。

返回值处理如下：

1. 浮点值在硬件 FP 栈顶返回。
2. 不超过 32 位的整数在 EAX 中返回。
3. 64 位整数通过 EAX 保存最低有效 32 位、EDX 保存最高有效 32 位来返回。
4. 其他所有情况都需要使用返回缓冲区，通过该缓冲区返回值。参见 [返回缓冲区](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/clr-abi.md#return-buffers)。

# Windows 上的控制流保护（CFG）支持（Control Flow Guard (CFG) support on Windows）

控制流保护（CFG）是 Windows 提供的一种安全缓解机制。
当启用 CFG 时，操作系统会维护一些数据结构，用于验证某个地址是否应被视为有效的间接调用目标。
该机制通过两个具有不同特性的 helper 函数暴露出来。

第一种机制是验证器（validator）：它以目标地址作为参数；若目标地址不是预期的间接调用目标，则快速失败（fails fast）；否则什么都不做并返回。
第二种机制是分派器（dispatcher）：它在一个非标准寄存器中接收目标地址；验证成功后，它会直接跳转到目标函数。
Windows 只在 ARM64 与 x64 上提供分派器，而验证器在所有平台上可用。
不过，JIT 只在 ARM64 与 x64 上支持 CFG，并且在这些平台上默认禁用 CFG。
CFG 功能的预期用途是：在需要 CFG 的受限环境中运行的 NativeAOT 场景。

这些 helper 以标准 JIT helper 的形式暴露给 JIT：`CORINFO_HELP_VALIDATE_INDIRECT_CALL` 与 `CORINFO_HELP_DISPATCH_INDIRECT_CALL`。

要使用验证器，JIT 会把间接调用展开为：先调用验证器，然后再调用已验证的地址。
对分派器，JIT 会改写调用以传递目标地址，但其他调用设置保持正常。

注意：这里的“间接调用”指任何不是指向（指令流中）立即数地址的调用。
例如，即使是直接调用，JIT 代码生成也可能因为分层编译（tiering）或目标尚未编译等原因发出间接调用指令；这些也同样会用 CFG 机制来展开。

接下来的小节描述 JIT 对这些 helper 所期望的调用约定。

## ARM64 的 CFG 细节（CFG details for ARM64）

在 ARM64 上，`CORINFO_HELP_VALIDATE_INDIRECT_CALL` 在 `x15` 中接收调用地址。
除常规寄存器外，它还会保留所有浮点寄存器、`x0`-`x8` 以及 `x15`。

`CORINFO_HELP_DISPATCH_INDIRECT_CALL` 在 `x9` 中接收调用地址。
JIT 默认不使用该分派 helper，因为它的分支预测器性能更差。
因此它会通过验证 helper 加上一次手动调用来展开所有间接调用。

## x64 的 CFG 细节（CFG details for x64）

在 x64 上，`CORINFO_HELP_VALIDATE_INDIRECT_CALL` 在 `rcx` 中接收调用地址。
除常规寄存器外，它还保留所有浮点寄存器、`rcx` 与 `r10`；此外，不要求分配 shadow stack 空间。

`CORINFO_HELP_DISPATCH_INDIRECT_CALL` 在 `rax` 中接收调用地址，并保留使用并破坏 `r10` 与 `r11` 的权利。
只要可能，JIT 就会在 x64 上使用分派 helper，因为预期代码体积收益将超过分支预测不够准确的代价。
不过需要注意：分派器会使用 `r11`，这使其与 VSD 调用不兼容；在这种情况下 JIT 必须回退到验证器加手动调用。

# 关于 Memset/Memcpy 的说明（Notes on Memset/Memcpy）

一般而言，`memset` 与 `memcpy` 不提供任何原子性保证。这意味着，只有当 `memset`/`memcpy` 修改的内存对任何其他线程（包括 GC）都不可观察，或者按照我们的[内存模型](https://github.com/dotnet/runtime/blob/main/docs/design/specs/Memory-model.md)不存在原子性要求时，才应当使用它们。尤其重要的是：当我们修改包含托管指针的堆内存时——这些必须以原子方式更新，例如使用指针大小的 `mov` 指令（托管指针总是对齐的）——参见[原子内存访问](https://github.com/dotnet/runtime/blob/main/docs/design/specs/Memory-model.md#Atomic-memory-accesses)。还值得注意的是，这里的“更新”隐含的是“设为零”；否则，我们需要写屏障（write barrier）。

示例：

```cs
struct MyStruct
{
	long a;
	string b;
}

void Test1(ref MyStruct m)
{
	// 我们不允许在这里使用 memset
	m = default;
}

MyStruct Test2()
{
	// 我们可以在这里使用 memset
	return default;
}
```

# 解释器 ABI 细节（Interpreter ABI details）

解释器数据栈与普通的“线程”栈分开分配，并且向上增长。解释器执行控制栈分配在“线程”栈上，由一系列 `InterpMethodContextFrame` 值组成，这些值通过单向链表链接到一个 `InterpreterFrame` 上，而该 `InterpreterFrame` 会被放入线程的 Frame 链中。`InterpMethodContextFrame` 结构总是按内存地址递减方向分配，因此被调用方法关联的 `InterpMethodContextFrame` 在内存中总是位于其调用方或包含它的 `InterpreterFrame` 之下。

方法内部的基准栈指针从不改变，但当解释器中发生函数调用时，它会拥有一个与传入参数集合相关联的栈指针。实际上，参数传递是通过把调用方函数临时参数空间的一部分交给被调用方函数来完成的。

所有会寻址栈指针的指令与 GC 都相对于当前栈指针，而该栈指针不会移动。这要求 localloc 指令的实现必须在堆上实际分配内存，并且 localloc 得到的内存并不以任何方式与数据栈绑定。

所有解释器函数中的栈指针始终按 `INTERP_STACK_ALIGNMENT` 边界对齐。目前这是一项 16 字节对齐要求。

栈元素总是至少按 `INTERP_STACK_SLOT_SIZE` 对齐，且不会超过 `INTERP_STACK_ALIGNMENT`。鉴于当前实现把 `INTERP_STACK_SLOT_SIZE` 设为 8，把 `INTERP_STACK_ALIGNMENT` 设为 16，这意味着栈上的所有数据要么是 8 字节对齐，要么是 16 字节对齐。

小于 4 字节的基本类型在栈上时总是被零扩展或符号扩展到 4 字节。

当一个函数是 async 时，它会有一个 continuation 返回。该返回不通过数据栈完成，而是通过设置 `InterpreterFrame` 中的 Continuation 字段来完成。Thunk 负责在进入/离开由 JIT 编译的代码时设置/重置该值。