# 附录 B：Disposables

Rx 使用现有的 `IDisposable` 接口来表示订阅。这种设计选择意味着我们可以使用知道如何使用该接口的现有语言功能。Rx 还提供了多个 `IDisposable` 的公共实现。这些实现可以在 `System.Reactive.Disposables` 命名空间中找到。本附录将简要介绍其中的每一个。

除了 [ScheduledDisposable](#ScheduledDisposable) 之外，这些实现与 Rx 没有特别的联系，但在任何需要使用 `IDisposable` 的代码中都很有用。(尽管这些代码都存在于 `System.Reactive` 中，因此尽管您可以完全在基于 Rx 的代码之外使用这些功能，但如果这样做，您仍将依赖于 Rx.NET）。

## Disposable.Empty

该静态属性公开了 `IDisposable` 的一个实现，当调用 `Dispose` 方法时，该实现不会执行任何操作。当您有义务提供一个 `IDisposable`（如果使用 `Observable.Create`，就会出现这种情况），但又不需要在取消时执行任何操作时，该属性就会非常有用。

## Disposable.Create(Action)

该静态方法公开了一个 `IDisposable` 的实现，当调用 `Dispose` 方法时，该实现会调用所提供的方法。由于该实现遵循幂等原则，因此只有在首次调用 `Dispose` 方法时才会调用该操作。

## BooleanDisposable

该类实现了 `IDisposable.Dispose` 方法，还定义了一个只读属性 `IsDisposed`。该类构建时 `IsDisposed` 为 `false`，调用 `Dispose` 方法时则设为 `true`。

## CancellationDisposable

`CancellationDisposable` 类是.NET 取消范例（`CancellationTokenSource`）和资源管理范例（`IDisposable`）之间的集成点。您可以通过向构造函数提供 `CancellationTokenSource` 或让无参数构造函数为您创建 `CancellationTokenSource` 来创建 `CancellationDisposable` 类的实例。调用 `Dispose` 会调用 `CancellationTokenSource` 上的 `Cancel` 方法。`CancellationDisposable` 公开了两个属性（`Token` 和 `IsDisposed`）；它们是 `CancellationTokenSource` 属性的包装器，分别是 `Token` 和 `IsCancellationRequested`。

## CompositeDisposable

通过 `CompositeDisposable` 类型，您可以将许多可释放资源视为一体。您可以通过传入可释放资源的 `params` 数组来创建 `CompositeDisposable` 实例。在 `CompositeDisposable` 上调用 `Dispose` 时，将按照提供的顺序在每个资源上调用 `Dispose`。此外，`CompositeDisposable` 类还实现了 `ICollection<IDisposable>`；这样就可以从集合中添加和删除资源。处理完 `CompositeDisposable` 后，任何添加到该集合的资源都将立即被处理掉。从集合中移除的任何项目也会被处理掉，无论集合本身是否已被处理。这包括删除和清除方法的使用。

## ContextDisposable

`ContextDisposable` 允许您强制在给定的 `SynchronizationContext` 上执行资源处置。构造函数需要一个 `SynchronizationContext` 和一个 `IDisposable` 资源。在 `ContextDisposable` 上调用 `Dispose` 方法时，所提供的资源将在指定的上下文中进行处置。

## MultipleAssignmentDisposable

`MultipleAssignmentDisposable` 公开了一个只读 `IsDisposed` 属性和一个读/写 `Disposable` 属性。调用 `MultipleAssignmentDisposable` 上的 `Dispose` 方法将处理 `Disposable` 属性所持有的当前值。然后将该值设置为空。只要 `MultipleAssignmentDisposable` 未被释放，您就可以按照预期将 `Disposable` 属性设置为 `IDisposable` 值。一旦 `MultipleAssignmentDisposable` 被释放，尝试设置 `Disposable` 属性将导致该值立即被释放；与此同时，`Disposable` 将保持为空。

## RefCountDisposable

`RefCountDisposable` 提供了在所有从属资源被释放之前防止释放底层资源的功能。您需要一个底层 `IDisposable` 值来构建 `RefCountDisposable`。然后，您可以调用 `RefCountDisposable` 实例上的 `GetDisposable` 方法来检索从属资源。每次调用 `GetDisposable` 时，内部计数器都会递增。每次处置来自 `GetDisposable` 的一个从属可释放资源时，计数器都会递减。只有当计数器达到零时，才会对底层进行处置。这样，您就可以在计数为零之前或之后对 `RefCountDisposable` 本身调用 `Dispose`。

## ScheduledDisposable

与 `ContextDisposable` 类似，`ScheduledDisposable` 类型允许您指定一个调度程序，底层资源将被处理到该调度程序上。您需要向构造函数传递 `IScheduler` 实例和 `IDisposable` 实例。当 `ScheduledDisposable` 实例被释放时，底层资源的处理将通过所提供的调度程序执行。

## SerialDisposable

`SerialDisposable` 与 `MultipleAssignmentDisposable` 非常相似，因为它们都暴露了一个可读/写的 `Disposable` 属性。它们之间的区别在于，只要在 `SerialDisposable` 上设置了 `Disposable` 属性，之前的值就会被废弃。与 `MultipleAssignmentDisposable` 一样，一旦 `SerialDisposable` 被释放，`Disposable` 属性将被设置为空，任何进一步设置该属性的尝试都将导致该值被释放。该值将保持为空。

## SingleAssignmentDisposable

`SingleAssignmentDisposable` 类还公开了 `IsDisposed` 和 `Disposable` 属性。与 `MultipleAssignmentDisposable` 和 `SerialDisposable` 类似，当 `SingleAssignmentDisposable` 被释放时，`Disposable` 值将被设置为空。实现上的不同之处在于，如果有人试图在 `Disposable` 属性值未为空且 `SingleAssignmentDisposable` 未被释放时设置该属性，`SingleAssignmentDisposable` 将抛出 `InvalidOperationException` 异常。