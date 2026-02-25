# 用于实现 Runtime Async 特性的代码生成器职责

本文档描述了代码生成器必须遵循的行为，以便正确地使 runtime async 特性正常工作。

本文档并不旨在描述 runtime-async 特性本身。runtime-async 更适合参考 runtime-async 规范。参见 (https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md)。

runtime-async 代码生成器的一般职责

1. 将返回 Task 和 ValueTask 的函数体包裹在 try/finally 块中，在其中设置/重置 `ExecutionContext` 与 `SynchronizationContext`。

2. 允许 async thunk 逻辑正常工作。

3. 生成 Async 调试信息（本文档尚未描述）f

# 识别可由 runtime-async 以优化方式处理的对 Runtime-Async 方法的调用

在编译对某个可能以优化方式调用的方法的调用点时，需要识别如下序列。

```
call[virt] <Method>
[ OPTIONAL ]
{
[ OPTIONAL - Used for ValueTask based ConfigureAwait ]
{
stloc X;
ldloca X
}
ldc.i4.0 / ldc.i4.1
call[virt] <ConfigureAwait> (The virt instruction is used for ConfigureAwait on a Task based runtime async function) NI_System_Threading_Tasks_Task_ConfigureAwait
}
call       <Await> One of the functions which matches NI_System_Runtime_CompilerServices_AsyncHelpers_Await
```

如果已知 Method 是 async，则会搜索该序列。

对 async 函数的派发会在挂起（suspension）时通过 `AsyncHelpers.CaptureExecutionContext` 保存 `ExecutionContext`，并在恢复（resumption）时通过 `AsyncHelpers.RestoreExecutionContext` 恢复它。

如果设置了 PREFIX_TASK_AWAIT_CONTINUE_ON_CAPTURED_CONTEXT，则 continuation 模式应为 ContinuationContextHandling::ContinueOnCapturedContext；否则为 ContinuationContextHandling::ContinueOnThreadPool。

# 非优化模式（Non-optimized pattern）

代码也允许直接简单地使用 `AsyncHelpers.Await`、`AsyncHelpers.AwaitAwaiter`、`AsyncHelpers.UnsafeAwaitAwaiter` 或 `AsyncHelpers.TransparentAwait`。为支持这种用法，这些函数即便不返回 Task/ValueTask，也会被标记为 async。

与其他 async 调用一样，对这些函数的派发会在挂起/恢复时保存与恢复执行上下文。

对这些函数的派发会将 continuation 模式设置为 ContinuationContextHandling::None。

# 对 async 函数的 calli

对这些函数的派发仅会在 async 派发时保存与恢复执行上下文。

# System.Runtime.CompilerServices.AsyncHelpers::AsyncSuspend 内在函数

遇到该内在函数时，会触发函数立即挂起，并返回传入的 Continuation。

# 上下文的保存与恢复

在挂起之前捕获执行上下文；当函数恢复时，调用 `AsyncHelpers.RestoreExecutionContext`。该上下文应存入 Continuation。可通过调用 `AsyncHelpers.CaptureExecutionContext` 来捕获该上下文。

# async 函数处理的 ABI

会额外增加一个参数：Continuation。正常调用函数时，该参数始终为 0；恢复执行时，该参数为 Continuation 对象。同时还会有一个额外的返回参数：要么为 0，要么为 Continuation。如果它是 continuation，则调用方需要挂起（如果调用方是 async 函数），或生成一个 Task/ValueTask（如果调用方是 async 函数包装器）。

## 挂起路径（Suspension path）

这是在 async 函数中调用 async 函数时使用的路径。

```
bool didSuspend = false; // Needed for the context restore

(result, continuation) = call func(NULL /* Continuation argument */, args)
if (continuation != NULL)
{
    // Allocate new continuation
    // Capture Locals
    // Copy resumption details into continuation (Do things like call AsyncHelpers.CaptureContinuationContext or AsyncHelpers.CaptureExecutionContext as needed)
    // Chain to continuation returned from called function
    // IF in a function which saves the exec and sync contexts, and we haven't yet suspended, restore the old values.
    // return.

    // Resumption point

    // Copy values out of continuation (including captured sync context and execution context locals)
    // If the continuation may have an exception, check to see if its there, and if it is, throw it. Do this if CORINFO_CONTINUATION_HAS_EXCEPTION is set.
    // If the continuation has a return value, copy it out of the continuation. (CORINFO_CONTINUATION_HAS_RESULT is set)
}
```

## Thunks 路径（Thunks path）

这是在非 async 函数中调用 async 函数时使用的路径。一般用于 AsyncResumptionStub 以及返回 Task 的 thunk。
```
(result, continuation) = call func(NULL /* Continuation argument */, args)
place result onto IL evaluation stack
Place continuation into a local for access using the StubHelpers.AsyncCallContinuation() helper function.
```

为 StubHelpers.AsyncCallContinuation() 实现一个内在函数，它会加载最近一次存入 continuation local 的值。

# ContinuationContextHandling 的行为

这仅适用于 ContinuationContextHandling 不是 ContinuationContextHandling::None 的调用。

如果设置为 ContinuationContextHandling::ContinueOnCapturedContext

- Continuation 应为捕获的上下文分配一个数据成员，并在 continuation 上设置 CORINFO_CONTINUATION_HAS_CONTINUATION_CONTEXT 标志。

- Continuation 将存储捕获到的同步上下文。填充 `Continuation` 时，通过调用 `AsyncHelpers.CaptureContinuationContext(ref newContinuation.ContinuationContext, ref newContinuation.Flags)` 完成。

如果设置为 ContinuationContextHandling::ContinueOnThreadPool
- Continuation 应设置 CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL 标志

# 异常处理行为

如果在 try 块中调用 async 函数（在 jit 中 hasTryIndex 返回 true），则在 Continuation 上设置 CORINFO_CONTINUATION_HAS_EXCEPTION 位，并使其足够大。

# 本地变量（locals）处理

不得捕获 ByRef locals。事实上，我们应当将所有 ByRef 或 ByRef-like 的 locals 置为 NULL。当前我们不会在同步执行时这样做，但从逻辑上讲，可能确实应该这样做。

# 同步上下文与执行上下文的保存与恢复

当以 null continuation context 直接调用时，代码生成器必须在所有 Task/ValueTask 方法的函数体周围保存/恢复同步上下文与执行上下文。EE 会通过 `getMethodInfo` 返回的 `CORINFO_ASYNC_SAVE_CONTEXTS` 标志来告知何时需要这样做。

> 传统上，**`async/await` 只是 C# 编译器（Roslyn）的“语法糖”**。Roslyn 会在编译期把你的 async 方法拆解成一个复杂的、实现了 `IAsyncStateMachine` 的状态机结构体（`struct`）。
>
> 但这篇文档揭示了一个**底层架构的巨大变革**：**Runtime Async（运行时原生异步）**。
>
> 这篇文档的核心意思是：**.NET 打算（或正在尝试）把异步状态机的生成工作，从 C# 编译器（编译期）转移到 JIT 编译器/运行时（运行期），让 CLR 原生理解和支持“异步挂起与恢复”。**
>
> 以下是这篇底层设计文档的核心技术解构：
>
> ### 1. 核心变革：ABI（应用二进制接口）的改变
>
> 这是整篇文档最硬核的部分。如果 CLR 原生支持 async，那么在机器码层面，方法的签名必须改变。 文档规定，所有的 async 方法在底层会被隐式修改：
>
> - **输入：** 增加一个隐藏的参数 `Continuation`（延续点）。正常调用时传 `NULL`，从 `await` 恢复时传入上次保存的状态。
> - 输出：返回值不再只是结果，而是变成了一个元组 `(result, continuation)`。
>   - 如果方法同步跑完了，`continuation` 返回 `NULL`。
>   - 如果方法遇到了 `await` 需要挂起，它就返回一个 `continuation` 对象。
>
> ### 2. JIT 的模式匹配（Pattern Matching）
>
> JIT 现在成了“聪明的拦截者”。当 JIT 编译 IL（中间语言）代码时，它会像正则表达式一样去匹配特定的指令序列。
>
> - **场景：** 当它看到 `call 方法` -> `ConfigureAwait(false/true)` -> `Await` 这样的连续指令集时。
> - **动作：** JIT 不会去老老实实执行这些方法，而是**直接将其替换为底层高度优化的“原生挂起/恢复代码”**。它可以直接把 `ConfigureAwait` 的参数翻译成底层的 `ContinuationContextHandling` 标志位（决定是回到原线程，还是扔给线程池）。
>
> ### 3. 上下文的“幽灵管家”：ExecutionContext 与 SynchronizationContext
>
> 在异步编程中，最难搞的就是上下文流动（比如 `AsyncLocal<T>` 和 UI 线程的 `Dispatcher`）。
>
> - 传统 C# 状态机需要在状态机流转时小心翼翼地捕获和恢复这些上下文。
> - **文档要求：** JIT 必须在底层为你悄悄生成一段 `try/finally` 代码，包裹住整个 async 方法体。当方法挂起时（Suspension），JIT 负责调用 `Capture` 捕获上下文；当方法恢复时（Resumption），JIT 负责调用 `Restore` 恢复上下文。这就彻底把上下文管理的开销下沉到了 C++ 运行时层。
>
> ### 4. 挂起与恢复（Suspension & Resumption）的底层机制
>
> 文档清晰地描述了当一个原生 async 函数执行到 `await` 时，CPU 在干什么：
>
> - 挂起（Suspension）：
>   1. 申请一个新的 `Continuation` 内存块。
>   2. **把当前 CPU 寄存器里的局部变量（Locals）全部拷贝进这个 `Continuation` 里。**
>   3. 捕获当前的执行上下文。
>   4. **直接 `return` 退出当前函数，把栈帧（Stack Frame）交还给调用者。**
> - 恢复（Resumption）：
>   1. 下次被唤醒时，把 `Continuation` 作为隐藏参数传进来。
>   2. **把 `Continuation` 里的局部变量重新拷贝回寄存器/栈上。**
>   3. 检查有没有异常（如果别的线程抛了异常，存进了 Continuation 里，这里就要 `throw` 出来）。
>   4. 提取返回值，继续往下执行。
>
> ### 5. 一些底层的物理限制（Locals 处理）
>
> 文档特别提到：**绝对不能捕获 `ByRef`（`ref` 变量）**。 为什么？因为 `ref` 本质上是指向当前线程调用栈（Call Stack）某个地址的指针。当遇到 `await` 时，当前函数会 `return`，栈帧会被销毁。如果把 `ref` 保存到堆上的 `Continuation` 里，等唤醒时，那个指针指向的内存早就变成垃圾或者别人的数据了。所以文档规定：JIT 必须把所有 `ref` 局部变量置为 `NULL`（清空）。
>
> ### 6. Thunks（垫片/跳板）
>
> 当你从一个同步方法（比如普通的 `Main`）调用一个异步方法时，因为同步方法不懂这个新的 ABI（不懂怎么处理返回的 `continuation`），JIT 会生成一段叫 Thunk 的跳板代码。这层代码负责拦截底层的 `(result, continuation)`，并把它包装回传统的 `Task`，让上层代码依然可以用 `.Wait()` 或者 `.Result` 来操作。
>
> 微软试图剥离 Roslyn 生成的那套臃肿的、基于类的 C# 状态机，转而让 CLR 的 JIT 编译器在生成机器码时，直接利用寄存器和极简的内存结构来实现协程（Coroutines）/ 绿色线程（Green Threads）。
>
> **核心目的只有一个：极大地减少 `async/await` 带来的内存分配（Allocation）和上下文切换开销，把异步操作的性能压榨到 C++ 乃至汇编的极限级别。**
>
> 
>
> **特别注意的是：**“代码生成器（Code Generator）”不只是 JIT
>
> 你潜意识里可能认为：离开 Roslyn 后，剩下的就是运行时 JIT（Just-In-Time），所以会拖慢启动。 但实际上，**.NET 的 AOT 编译器（Crossgen2 / NativeAOT）和 JIT 编译器，共用的是同一个底层代码生成引擎（RyuJIT）。**
>
> - **现在的情况（Roslyn 状态机）：** C# 编译器在编译期，把 `async/await` 翻译成了一大堆冗长复杂的 IL（中间语言）代码（生成包含 `MoveNext` 的结构体、异常捕获、装箱操作）。
> - **Runtime Async 的提案：** C# 编译器不再生成那些恶心的状态机 IL，而是直接生成非常简洁的“原生 Async IL”。然后，**由 RyuJIT 负责把这些原生 IL 翻译成机器码**。
>
> **关键点：** 这个“翻译成机器码”的动作，**同样可以在 AOT 编译阶段（打包发布时）完成！** 因此，它并不会强制增加用户侧的程序启动时间。
>
> ### 冗长的 IL 反而会拖慢 JIT/AOT 的编译速度
>
> 退一步讲，假设程序就是以 JIT 模式运行的。把 Async 移交给 JIT，编译时间一定会变长吗？**恰恰相反，可能会变短！**
>
> - **Roslyn 制造的“垃圾”：** Roslyn 生成的 async 状态机 IL 非常庞大。对于一个只有 10 行代码的 async 方法，Roslyn 会生成上百行的状态机 IL。
> - **编译器的负担：** 当 JIT/AOT 引擎读取这些状态机 IL 时，它必须花大量时间去分析这些控制流、分配寄存器、做内联优化。但因为状态机的逻辑太绕了（大 `switch/case` 和指针跳转），JIT 往往很难优化，而且编译得很慢。
> - **Runtime Async 的降维打击：** 如果 JIT 直接认识 `async` 和 `await` 指令，它就不需要去死磕那上百行的烂 IL。它直接按内置的 ABI 规则（就像文档里说的，加个 Continuation 参数，压栈出栈），几条汇编指令就生成完了。**逻辑越原生，机器码生成越快。**
>
> ### 性能的权衡：极端的“空间换时间”与“吞吐量为王”
>
> 在 ASP.NET Core 这种每秒处理百万请求的服务器中，`async/await` 最大的痛点是**堆内存分配（Heap Allocation）**和**闭包开销**：
>
> 1. 每次 `await`，Roslyn 状态机都可能要把局部变量从栈（Stack）“搬家”到堆（Heap）上的状态机盒子里（Hoisted locals）。
> 2. 这会产生海量的对象碎片，导致 GC（垃圾回收器）疯狂工作，最终拖垮性能。
>
> **Runtime Async 的终极目的：** 通过在机器码级别原生支持挂起（Suspension），JIT 可以精准控制寄存器，**把局部变量塞进极致紧凑的 Continuation 内存块，甚至完全消灭闭包和 Task 对象的分配。**
