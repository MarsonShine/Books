# ConfigureAwait in .NET8

## ConfigureAwait（true） 和 ConfigureAwait（false）

首先，让我们回顾一下原版 `ConfigureAwait` 的语义和历史，它采用了一个名为 `continueOnCapturedContext` 的布尔参数。

当对任务（`Task` 、`Task<T>`、`ValueTask` 或 `ValueTask<T>`）执行 `await` 操作时，其默认行为是捕获“上下文”的；稍后，当任务完成时，该 `async` 方法将在该上下文中继续执行。“上下文”是 `SynchronizationContext.Current` 或 `TaskScheduler.Current`（如果未提供上下文，则回退到线程池上下文）。通过使用 `ConfigureAwait(continueOnCapturedContext: true)` 可以明确这种在捕获上下文中继续的默认行为。

如果不想在该上下文上恢复，`ConfigureAwait(continueOnCapturedContext: false)` 就很有用。使用 `ConfigureAwait(false)` 时，异步方法会在任何可用的线程池线程上恢复。

`ConfigureAwait(false)` 的历史很有趣（至少对我来说是这样）。最初，社区建议在所有可能的地方使用 `ConfigureAwait(false)`，除非需要上下文。这也是我在 [Async 最佳实践](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming?WT.mc_id=DT-MVP-5000058#configure-context)一文中推荐的立场。在那段时间里，我们就默认为 `true` 的原因进行了多次讨论，尤其是那些不得不经常使用 `ConfigureAwait(false)` 的库开发人员。

不过，多年来，”尽可能使用 `ConfigureAwait(false)`“的建议已被修改。第一次（尽管是微小的）变化是，不再是”尽可能使用 `ConfigureAwait(false)`“，而是出现了更简单的指导原则：在库代码中使用 `ConfigureAwait(false)`，而不要在应用代码中使用。这条准则更容易理解和遵循。尽管如此，关于必须使用 `ConfigureAwait(false)` 的抱怨仍在继续，并不时有人要求在整个项目范围内更改默认值。出于语言一致性的考虑，C# 团队总是拒绝这些请求。

最近（具体来说，自从 ASP.NET 在 ASP.NET Core 中放弃了 `SynchronizationContext` 并修复了所有需要 sync-over-async（即同步套异步代码） 的地方之后），C# 团队开始放弃使用 `ConfigureAwait(false)`。作为一名库作者，我完全理解让 `ConfigureAwait(false)` 在代码库中随处可见是多么令人讨厌！有些库作者决定不再使用 `ConfigureAwait(false)`。就我自己而言，我仍然在我的库中使用 `ConfigureAwait(false)`，但我理解这种挫败感。

既然谈到了 `ConfigureAwait(false)`，我想指出几个常见的误解：

1. `ConfigureAwait(false)` 并不是避免死锁的好方法。这不是它的目的，充其量只是一个值得商榷的解决方案。为了在直接阻塞时避免死锁，你必须确保所有异步代码都使用 `ConfigureAwait(false)`，包括库和运行时中的代码。这并不是一个非常容易维护的解决方案。还有[更好的解决方案](https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development?WT.mc_id=DT-MVP-5000058)。
2. `ConfigureAwait` 配置的是 `await`，而不是任务。例如，`SomethingAsync().ConfigureAwait(false).GetAwaiter().GetResult()` 中的 `ConfigureAwait(false)` 完全没有任何作用。同样，`var task = SomethingAsync(); task.ConfigureAwait(false); await task;` 中的 `await` 仍在捕获的上下文中继续，完全忽略了 `ConfigureAwait(false)`。多年来，我见过这两种错误。
3. `ConfigureAwait(false)` 并不意味着”在线程池线程上运行此方法的后续部分“或”在不同的线程上运行此方法的后续部分“。它只在 `await` 暂停执行并稍后恢复异步方法时生效。具体来说，如果 `await` 的任务已经完成，它将不会暂停执行；在这种情况下，`ConfigureAwait` 将不会起作用，因为`await` 会同步继续执行。

好了，既然我们已经重新理解了 `ConfigureAwait(false)`，下面就让我们看看 `ConfigureAwait` 在 .NET8 中是如何得到增强的。`ConfigureAwait(true)` 和 `ConfigureAwait(false)` 仍具有相同的行为。但是，有一种新的 `ConfigureAwait` 即将出现！

## ConfigureAwait(ConfigureAwaitOptions)

`ConfigureAwait` 有几个新选项。[ConfigureAwaitOptions](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.configureawaitoptions?view=net-8.0) 是一种新类型，它提供了配置 awaitables 的所有不同方法：

```csharp
namespace System.Threading.Tasks;
[Flags]
public enum ConfigureAwaitOptions
{
    None = 0x0,
    ContinueOnCapturedContext = 0x1,
    SuppressThrowing = 0x2,
    ForceYielding = 0x4,
}
```

首先，请注意：这是一个 Flags 枚举；这些选项的任何组合都可以一起使用。

接下来我要指出的是，至少在 .NET8 中，`ConfigureAwait(ConfigureAwaitOptions)` 仅适用于 `Task` 和 `Task<T>`。它还没有添加到 `ValueTask/ValueTask<T>`。未来的 .NET 版本有可能为 `ValueTask` 添加 `ConfigureAwait(ConfigureAwaitOptions)`，但目前它仅适用于引用任务，因此如果您想在 `ValueTask` 中使用这些新选项，则需要调用 `AsTask`。

现在，让我们依次讲解这些选项。

## ConfigureAwaitOptions.None 和 ConfigureAwaitOptions.ContinueOnCapturedContext

这两个选项都很熟悉，但有一点不同。

`ConfigureAwaitOptions.ContinueOnCapturedContext`--从名字就能猜到与 `ConfigureAwait(continueOnCapturedContext: true)` 相同。换句话说，await 将捕获上下文，并在该上下文上继续执行异步方法。

```csharp
Task task = ...;

// 下面做的事情相同
await task;
await task.ConfigureAwait(continueOnCapturedContext: true);
await task.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
```

`ConfigureAwaitOptions.None` 与 `ConfigureAwait(continueOnCapturedContext: false)` 相同。换句话说，除了不捕获上下文外，await 的行为完全正常；假设 await 确实产生了结果（即任务尚未完成），那么异步方法将在任何可用的线程池线程上继续执行。

```csharp
Task task = ...;

// 下面两行代码效果一样
await task.ConfigureAwait(continueOnCapturedContext: false);
await task.ConfigureAwait(ConfigureAwaitOptions.None);
```

这里有一个转折点：使用新选项后，默认情况下不会捕获上下文！除非你在标记中明确包含 `ContinueOnCapturedContext`，否则上下文将不会被捕获。当然，await 本身的默认行为不会改变：在没有任何 `ConfigureAwait` 的情况下，await 的行为将与使用了 `ConfigureAwait(true)` 或 `ConfigureAwaitOptions.ContinueOnCapturedContext)` 时一样。

```csharp
Task task = ...;

// 默认的行为还是会继续捕捉上下文
await task;

// 默认选项 (ConfigureAwaitOptions.None): 不会捕捉上下文
await task.ConfigureAwait(ConfigureAwaitOptions.None);
```

因此，在开始使用这个新的 `ConfigureAwaitOptions` 枚举时，请记住这一点。

## ConfigureAwaitOptions.SuppressThrowing

`SuppressThrowing` 标志可抑制等待任务时可能出现的异常。在正常情况下，`await` 会通过在 `await` 时重新引发异常来观察任务异常。通常情况下，这正是你想要的行为，但在某些情况下，你只想等待任务完成，而不在乎任务是成功完成还是出现异常。那么 `SuppressThrowing` 选项允许您等待任务完成，而不观察其结果。

```csharp
Task task = ...;

// 下面两行代码等价
await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
try { await task.ConfigureAwait(false); } catch { }
```

我预计这将与取消任务一起发挥最大作用。在某些情况下，有些代码需要先取消任务，然后等待现有任务完成后再启动替代任务。在这种情况下，`SuppressThrowing` 将非常有用：代码可以使用 `SuppressThrowing` 等待，当任务完成时，无论任务是成功、取消还是出现异常，方法都将继续。

```csharp
// 取消旧任务并等待完成，忽略异常情况
_cts.Cancel();
await _task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

// 开启新任务
_cts = new CancellationTokenSource();
_task = SomethingAsync(_cts.Token);
```

如果使用 `SuppressThrowing` 标志等待，异常就会被视为”已观察到“，因此不会引发 `TaskScheduler.UnobservedTaskException` 异常。我们的假设是，你在等待任务时故意丢弃了异常，所以它不会被认为是未观察到的。

```csharp
TaskScheduler.UnobservedTaskException += (_, __) => { Console.WriteLine("never printed"); };

Task task = Task.FromException(new InvalidOperationException());
await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
task = null;

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

Console.ReadKey();
```

这个标记还有另一个考虑因素。当与 `Task` 一起使用时，其语义很清楚：如果任务失败了，异常将被忽略。但是，同样的语义对 `Task<T>` 并不完全适用，因为在这种情况下，`await` 表达式需要返回一个值（`T` 类型）。目前还不清楚在忽略异常的情况下返回 `T` 的哪个值合适，因此当前的行为是在运行时抛出 `ArgumentOutOfRangeException`。为了帮助在编译时捕捉到这种情况，[最近添加](https://github.com/dotnet/roslyn-analyzers/pull/6669)了一个新的警告：`CA2261` `ConfigureAwaitOptions.SuppressThrowing 仅支持非泛型任务`。该规则默认为警告，但我建议将其设为错误，因为它在运行时总是会失败。

```csharp
Task<int> task = Task.FromResult(13);

// 在构建时导致 CA2261 警告，在运行时导致 ArgumentOutOfRangeException。
await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
```

最后要说明的是，除了 `await` 之外，该标记还影响同步阻塞。具体来说，您可以调用 `.GetAwaiter().GetResult()` 来阻塞从 `ConfigureAwait` 返回的 awaiter。无论使用 `await` 还是 `GetAwaiter().GetResult()`，`SuppressThrowing` 标记都会导致异常被忽略。以前，当 `ConfigureAwait` 只接受一个布尔参数时，你可以说”ConfigureAwait 配置了 await“；但现在你必须说得更具体：”ConfigureAwait 返回了一个已配置的 await“。现在，除了 await 的行为外，配置的 awaitable 还有可能修改阻塞代码的行为。除了修改 await 的行为之外。现在的 `ConfigureAwait` 可能有点误导性，但它仍然主要用于配置 `await`。当然，不推荐在异步代码中进行阻塞操作。

```csharp
Task task = Task.Run(() => throw new InvalidOperationException());

// 同步阻塞任务（不推荐）。不会抛出异常。
task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
```

## ConfigureAwaitOptions.ForceYielding

最后一个标志是 `ForceYielding` 标志。我估计这个标志很少会用到，但当你需要它时，你就需要它！

`ForceYielding` 类似于 `Task.Yield`。`Yield` 返回一个特殊的 awaitable，它总是声称尚未完成，但会立即安排其继续。这意味着 await 始终以异步方式执行，让出给调用者，然后异步方法尽快继续执行。[`await` 的正常行为](https://blog.stephencleary.com/2023/11/%%20post_url%202012-02-02-async-and-await%20%)是检查可等待对象是否完成，如果完成，则继续同步执行；`ForceYielding` 阻止了这种同步行为，强制 `await` 以异步方式执行。

就我个人而言，我发现强制异步行为在单元测试中最有用。在某些情况下，它还可以用来避免堆栈潜入。在实现异步协调基元（如我的 AsyncEx 库中的原语）时，它也可能很有用。基本上，在任何需要强制 `await` 以异步方式运行的地方，都可以使用 `ForceYielding` 来实现。

我觉得有趣的一点是，使用 `ForceYielding` 的 `await` 会让 `await` 的行为与 JavaScript 中的一样。在 JavaScript 中，await 总是会产生结果，即使你传递给它一个已解析的 Promise 也是如此。在 C# 中，您现在可以使用 `ForceYielding` 来等待一个已完成的任务，`await` 的行为就好像它尚未完成一样，就像 JavaScript 的 await 一样。

```csharp
static async Task Main()
{
  Console.WriteLine(Environment.CurrentManagedThreadId); // main thread
  await Task.CompletedTask;
  Console.WriteLine(Environment.CurrentManagedThreadId); // main thread
  await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
  Console.WriteLine(Environment.CurrentManagedThreadId); // thread pool thread
}
```

请注意，`ForceYielding` 本身也意味着不在捕获的上下文中继续执行，因此等同于说”将该方法的剩余部分调度到线程池“或者”切换到线程池线程“。

```csharp
// ForceYielding 强制 await 以异步方式执行。
// 缺少 ContinueOnCapturedContext 意味着该方法将在线程池线程上继续执行。
// 因此，该语句之后的代码将始终在线程池线程上运行。
await task.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
```

`Task.Yield` 将在捕获的上下文中恢复执行，因此它与仅使用 `ForceYielding` 不完全相同。实际上，它类似于带有 `ContinueOnCapturedContext` 的`ForceYielding`。

```csharp
// 下面两行代码效果相同
await Task.Yield();
await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding | ConfigureAwaitOptions.ContinueOnCapturedContext);
```

当然，`ForceYielding` 的真正价值在于它可以应用于任何任务。以前，在需要进行让步的情况下，您必须要么添加单独的 `await Task.Yield()` 语句，要么创建自定义的可等待对象。现在有了可以应用于任何任务的 `ForceYielding`，这些操作就不再必要了。

## 拓展阅读

很高兴看到 .NET 团队在多年后仍然在改进 async/await 的功能！

如果您对 `ConfigureAwaitOptions` 背后的历史和设计讨论更感兴趣，可以查看相关的 [Pull Request](https://github.com/dotnet/runtime/pull/87067)。在发布之前，曾经有一个名为`ForceAsynchronousContinuation` 的选项，但后来被删除了。它具有更加复杂的用例，基本上可以覆盖 `await` 的默认行为，[将异步方法的继续操作调度为 `ExecuteSynchronously`](https://blog.stephencleary.com/2012/12/dont-block-in-asynchronous-code.html)。也许未来的更新会重新添加这个选项，或者也许将来的更新会为 `ValueTask` 添加 `ConfigureAwaitOptions` 的支持。我们只能拭目以待！

## 原文链接

[ConfigureAwait in .NET 8 (stephencleary.com)](https://blog.stephencleary.com/2023/11/configureawait-in-net-8.html)

