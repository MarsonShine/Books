
本文是针对“runtime async”特性的 ECMA-335 变更草案。当该特性正式支持时，可并入最终的 ECMA-335 增补文档。

# Runtime-async

Async 目前是各 .NET 语言通过编译器重写实现的特性，使方法能在特定的“挂起”点将控制权“让出”给调用方。虽然有效，但认为直接在 .NET 运行时中实现可以带来改进，尤其是性能方面。

## 规范修改

以下是对 ECMA-335 规范关于 runtime-async 的拟议修改。

### I.8.4.5 同步与异步方法

方法可以是 'sync' 或 'async'。异步方法定义是带有 `[MethodImpl(MethodImplOptions.Async)]` 特性的方法。

`MethodImplOptions.Async` 的适用性：
* `[MethodImpl(MethodImplOptions.Async)]` 仅在应用于返回 Task 或 ValueTask 的泛型或非泛型变体的方法定义时生效。
* `[MethodImpl(MethodImplOptions.Async)]` 仅在应用于具有 CIL 实现的方法定义时生效。
* 异步方法定义仅在支持 async 的程序集内有效。支持 async 的程序集指的是引用了包含 `abstract sealed class RuntimeFeature` 且具有名为 `Async` 的 `public const string` 字段成员的 corlib。
* 将 `MethodImplOptions.Async` 与 `MethodImplOptions.Synchronized` 组合是无效的。
* 将 `MethodImplOptions.Async` 应用于返回 `byref` 或 `ref-like` 的方法是无效的。
* 将 `MethodImplOptions.Async` 应用于可变参数（vararg）方法是无效的。

_[注：这些规则在泛型替换之前生效，这意味着只有在替换之后才满足要求的方法不被视为有效。]_

同步方法是所有其他方法。

与同步方法不同，异步方法支持挂起。挂起允许异步方法在某些明确的挂起点将控制流让回给调用方，并在稍后的时间或位置恢复执行剩余方法，可能在另一线程上。挂起点是可以发生挂起的位置，但如果所有类 Task 对象都已完成，则不要求挂起。

异步方法在返回类型约定上也不同于同步方法。对于同步方法，在 `ret` 指令之前，栈上应包含可转换为声明的返回类型的值。对于异步方法，当返回 `Task` 或 `ValueTask` 时栈应为空；当返回 `Task<T>` 或 `ValueTask<T>` 时栈上应为其类型参数。

异步方法通过以下方法之一支持挂起：

  ```C#
  namespace System.Runtime.CompilerServices
  {
      public static class AsyncHelpers
      {
          [MethodImpl(MethodImplOptions.Async)]
          public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion;
          [MethodImpl(MethodImplOptions.Async)]
          public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion;

          [MethodImpl(MethodImplOptions.Async)]
          public static void Await(Task task);
          [MethodImpl(MethodImplOptions.Async)]
          public static void Await(ValueTask task);
          [MethodImpl(MethodImplOptions.Async)]
          public static T Await<T>(Task<T> task);
          [MethodImpl(MethodImplOptions.Async)]
          public static T Await<T>(ValueTask<T> task);

          [MethodImpl(MethodImplOptions.Async)]
          public static void Await(ConfiguredTaskAwaitable configuredAwaitable);
          [MethodImpl(MethodImplOptions.Async)]
          public static void Await(ConfiguredValueTaskAwaitable configuredAwaitable);
          [MethodImpl(MethodImplOptions.Async)]
          public static T Await<T>(ConfiguredTaskAwaitable<T> configuredAwaitable);
          [MethodImpl(MethodImplOptions.Async)]
          public static T Await<T>(ConfiguredValueTaskAwaitable<T> configuredAwaitable);
      }
  }
  ```

这些方法只能在异步方法内部调用。`...AwaitAwaiter` 方法的语义将类似于当前的 `AsyncTaskMethodBuilder.AwaitOnCompleted/AwaitUnsafeOnCompleted` 方法。调用任一方法后，可以认为任务或等待器已完成。`Await` 方法像 `...AwaitAwaiter` 方法一样执行挂起，但针对异步方法调用的返回值进行了优化。为获得最佳性能，应优先采用两个 `call` 指令的 IL 序列——先调用异步方法，紧接着调用 `Await` 方法。

跨越挂起点使用的局部变量被视为“提升（hoisted）”。也就是说，只有被“提升”的局部变量在从挂起返回后才会保留其状态。By-ref 变量不能跨越挂起点被提升，挂起点之后对 by-ref 变量的任何读取都会产生 null。Byref-like 结构也不会跨越挂起点被提升，且在挂起点之后将具有其默认值。
同样地，被固定（pinning）的局部变量不能跨越挂起点“提升”，并在挂起点之后将具有 `null` 值。

异步方法目前有一些临时限制，可能会在以后解除：
* 禁止使用 `tail` 前缀
* 禁止使用 `localloc` 指令

其他限制可能是永久的，包括
* By-ref 局部变量不能跨越挂起点被提升
* 挂起点不得出现在异常处理块中。
* “runtime-async” 方法的返回类型仅支持四种：`System.Threading.Task`、`System.Threading.ValueTask`、`System.Threading.Task<T>` 和 `System.Threading.ValueTask<T>`


### II.23.1.11 方法标志 [MethodImplAttributes]

| Flag  | Value | Description |
| ------------- | ------------- | ------------- |
| . . . | . . . | . . . |
|Async |0x2000 |方法是异步方法。|

该标志在 IL 中以 `async` 关键字表示。`ilasm` 和 `ildasm` 等工具会识别该标志。
