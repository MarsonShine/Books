# 用于实现 Runtime Async 特性的代码生成器职责

本文档描述了代码生成器必须遵循的行为，以便正确地使 runtime async 特性正常工作。

本文档**并不**旨在描述 runtime-async 特性本身。runtime-async 更适合参考 runtime-async 规范。参见 (https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md)。



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

如果设置了 PREFIX_TASK_AWAIT_CONTINUE_ON_CAPTURED_CONTEXT，则 continuation mode 应为 ContinuationContextHandling::ContinueOnCapturedContext；否则为 ContinuationContextHandling::ContinueOnThreadPool。



# 非优化模式（Non-optimized pattern）

代码也允许直接简单地使用 `AsyncHelpers.Await`、`AsyncHelpers.AwaitAwaiter`、`AsyncHelpers.UnsafeAwaitAwaiter` 或 `AsyncHelpers.TransparentAwait`。为支持这种用法，这些函数即便不返回 Task/ValueTask，也会被标记为 async。

与其他 async 调用一样，对这些函数的派发会在挂起/恢复时保存与恢复执行上下文。

对这些函数的派发会将 continuation mode 设置为 ContinuationContextHandling::None。

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
