# 分区

Rx 可以将一个序列分割成多个序列。这对于将项目分配给多个用户非常有用。在进行分析时，对分区进行聚合可能很有用。您可能已经熟悉了标准 LINQ 操作符 `GroupBy`。Rx 支持这种操作，而且还定义了一些自己的操作。

## GroupBy

正如 `IEnumerable<T>` 的 `GroupBy` 运算符一样，`GroupBy` 运算符允许您分割序列。开源的 [Ais.Net 项目](https://github.com/ais-dotnet)再次提供了一个有用的例子。它的 [ReceiverHost](https://github.com/ais-dotnet/Ais.Net.Receiver/blob/15de7b2908c3bd67cf421545578cfca59b24ed2c/Solutions/Ais.Net.Receiver/Ais/Net/Receiver/Receiver/ReceiverHost.cs) 类通过 Rx 提供 AIS 消息，定义了 `IObservable<IAisMessage>` 类型的 `Messages` 属性。这是一个非常繁忙的源，因为它会报告它能够访问的每一条消息。例如，如果将接收器连接到挪威政府慷慨提供的 AIS 信息源，那么每当挪威海岸的任何船只广播 AIS 信息时，接收器都会发出通知。挪威有很多船只在航行，所以这有点像消防水龙头。

如果我们确切地知道哪些船只是我们感兴趣的，就可以在[过滤一章](operation-filtering.md)中了解如何过滤这些信息流。但如果我们不知道，但仍然希望能够执行与单个船只相关的处理，那该怎么办呢？例如，也许我们想在任何时候发现任何船只改变了导航状态（`NavigationStatus`）（该状态会报告 `AtAnchor` 或 `Moored` 等值）。[过滤章节中的 Distinct 和 DistinctUntilChanged](operation-filtering.md) 部分展示了如何做到这一点，但它首先是将信息流过滤为来自单艘船只的信息。如果我们试图在所有船只的信息流中直接使用 `DistinctUntilChanged`，就不会产生有意义的信息。如果船只 A 停泊在岸边，船只 B 停泊在锚地，如果我们从船只 A 和船只 B 收到了不同的状态信息，那么 `DistinctUntilChanged` 会将每条信息都报告为状态变化，尽管两艘船只的状态都没有变化。

我们可以将“所有船只”序列拆分成许多小序列来解决这个问题：

```c#
IObservable<IGroupedObservable<uint, IAisMessage>> perShipObservables = 
   receiverHost.Messages.GroupBy(message => message.Mmsi);
```

此 `perShipObservables` 是可观测序列的可观测序列。更具体地说，它是一个分组可观测序列的可观测序列，但从 `IGroupedObservable<TKey, T>` 的定义中可以看出，分组可观测序列只是一种特殊的可观测序列：

```c#
public interface IGroupedObservable<out TKey, out TElement> : IObservable<TElement>
{
    TKey Key { get; }
}
```

每当 `receiverHost.Message` 报告一条 AIS 消息时，`GroupBy` 运算符就会调用回调，以找出此项目属于哪个组。我们将回调返回的值称为键，`GroupBy` 会记住它已经看到的每个键。如果这是一个新的键，`GroupBy` 将创建一个新的 `IGroupedObservable`，其键属性将是回调刚刚返回的值。它会从外部观察符（我们放在 `perShipObservables` 中的观察对象）发射这个 `IGroupedObservable`，然后立即使这个新的 `IGroupedObservable` 发射产生该键的元素（在本例中是 `IAisMessage`）。但是，如果回调生成的键是 `GroupBy` 以前看到过的，它就会找到已经为该键生成的 `IGroupedObservable`，并使其发出值。

因此，在这个例子中，其效果是，只要接收主机报告了来自我们之前未曾收到过的船只的信息，`perShipObservables` 就会发出一个新的可观测值，专门报告该船只的信息。我们可以用它来报告每次我们了解到的新船信息：

```c#
perShipObservables.Subscribe(m => Console.WriteLine($"New ship! {m.Key}"));
```

但这并没有做任何我们无法用 `Distinct` 实现的事情。`GroupBy` 的强大之处在于，我们可以在这里得到每艘飞船的可观测序列，因此我们可以继续设置一些针对每艘飞船的处理方法

```c#
IObservable<IObservable<IAisMessageType1to3>> shipStatusChangeObservables =
    perShipObservables.Select(shipMessages => shipMessages
        .OfType<IAisMessageType1to3>()
        .DistinctUntilChanged(m => m.NavigationStatus)
        .Skip(1));
```

这将使用 `Select`（在[转换](transformation-sequences.md)一章中介绍）对 `perShipObservables` 输出的每个组进行处理。请记住，每个组都代表一艘不同的船只，因此我们在此传递给 `Select` 的回调将对每艘船只精确调用一次。这意味着我们现在可以使用 `DistinctUntilChanged`。本示例为 `DistinctUntilChanged` 提供的输入是一个序列，它只代表一艘飞船的信息，因此它会告诉我们该飞船何时改变了状态。由于每艘飞船都有自己的 `DistinctUntilChanged` 实例，因此现在可以实现我们想要的功能。`DistinctUntilChanged` 总是转发它接收到的第一个事件--只有当项目与前一个项目相同时才会丢弃项目，而在本例中没有前一个项目。但这不可能是正确的行为。假设我们看到的第一条信息来自一艘名为 A 的船只，报告的状态是 `Moored`。有可能在我们开始运行之前，它处于某种不同的状态，而我们收到的第一条报告恰好代表了状态的改变。但更有可能的是，在我们开始运行之前，它已经停泊了一段时间。我们无法确定，但大多数状态报告并不代表变化，因此 `DistinctUntilChanged` 总是转发第一个事件的行为在这里很可能是错误的。因此，我们使用 `Skip(1)` 删除每艘飞船的第一条信息。

至此，我们有了一个可观察序列的可观察序列。外层序列会为它看到的每艘不同的飞船生成一个嵌套序列，嵌套序列会报告该特定飞船的导航状态变化。

我要做一个小小的调整：

```c#
IObservable<IAisMessageType1to3> shipStatusChanges =
    perShipObservables.SelectMany(shipMessages => shipMessages
        .OfType<IAisMessageType1to3>()
        .DistinctUntilChanged(m => m.NavigationStatus)
        .Skip(1));
```

我用 `SelectMany` 代替了 `Select`，这在[转换](transformation-sequences.md)一章中也有介绍。你可能还记得，`SelectMany` 将嵌套的可观察对象平铺成一个单一的平铺序列。你可以从返回类型中看到这一点：现在我们得到的只是一个 `IObservable<IAisMessageType1to3>` 而不是一个序列。

等一下！我不是刚刚推翻了 `GroupBy` 所做的工作吗？我让它按船只 id 对事件进行分区，为什么现在又要重新组合成一个单一的平面流？我一开始不就是这样做的吗？

流类型确实与我最初的输入具有相同的形状：这将是一个单一的可观测 AIS 消息序列。(它更专业一些--元素类型是 `IAisMessageType1to3`，因为我可以从那里获取 `NavigationStatus`，但这些仍然都实现了 `IAisMessage`）。所有不同的船只都将混合在一个数据流中。但实际上，我并没有否定 `GroupBy` 所做的工作。这个大理石图说明了发生了什么：

![](./asserts/Ch08-Partitioning-Marbles-Status-Changes.svg)

`perShipObservables` 部分展示了 `GroupBy` 如何为每艘不同的船只创建单独的观测值。(如果使用真正的源，从 `GroupBy` 生成的观测值会更多，但原理是一样的）。在对这些分组流进行扁平化处理之前，我们还需要做一些工作。如前所述，我们使用 `DistinctUntilChanged` 和 `Skip(1)` 来确保只有在确定船只状态发生变化时才会产生事件。(因为我们只看到过 `A` 报告 `Moored` 状态，所以据我们所知它的状态从未改变过，这就是为什么它的数据流是完全空的）。只有这样，我们才能将其平铺成一个单一的可观测序列。

大理石图需要简单明了才能在页面上显示，所以现在让我们快速看看一些真实的输出结果。这证实了它与原始的 `receiverHost.Messages` 非常不同。首先，我需要附加一个订阅者：

```c#
shipStatusChanges.Subscribe(m => Console.WriteLine(
   $"Vessel {((IAisMessage)m).Mmsi} changed status to {m.NavigationStatus} at {DateTimeOffset.UtcNow}"));
```

如果我让接收器运行大约十分钟，就会看到这样的输出：

```
Vessel 257076860 changed status to UnderwayUsingEngine at 23/06/2023 06:42:48 +00:00
Vessel 257006640 changed status to UnderwayUsingEngine at 23/06/2023 06:43:08 +00:00
Vessel 259005960 changed status to UnderwayUsingEngine at 23/06/2023 06:44:23 +00:00
Vessel 259112000 changed status to UnderwayUsingEngine at 23/06/2023 06:44:33 +00:00
Vessel 259004130 changed status to Moored at 23/06/2023 06:44:43 +00:00
Vessel 257076860 changed status to NotDefined at 23/06/2023 06:44:53 +00:00
Vessel 258024800 changed status to Moored at 23/06/2023 06:45:24 +00:00
Vessel 258006830 changed status to UnderwayUsingEngine at 23/06/2023 06:46:39 +00:00
Vessel 257428000 changed status to Moored at 23/06/2023 06:46:49 +00:00
Vessel 257812800 changed status to Moored at 23/06/2023 06:46:49 +00:00
Vessel 257805000 changed status to Moored at 23/06/2023 06:47:54 +00:00
Vessel 259366000 changed status to UnderwayUsingEngine at 23/06/2023 06:47:59 +00:00
Vessel 257076860 changed status to UnderwayUsingEngine at 23/06/2023 06:48:59 +00:00
Vessel 257020500 changed status to UnderwayUsingEngine at 23/06/2023 06:50:24 +00:00
Vessel 257737000 changed status to UnderwayUsingEngine at 23/06/2023 06:50:39 +00:00
Vessel 257076860 changed status to NotDefined at 23/06/2023 06:51:04 +00:00
Vessel 259366000 changed status to Moored at 23/06/2023 06:51:54 +00:00
Vessel 232026676 changed status to Moored at 23/06/2023 06:51:54 +00:00
Vessel 259638000 changed status to UnderwayUsingEngine at 23/06/2023 06:52:34 +00:00
```

这里最重要的一点是，在十分钟的时间里，`receiverHost.Messages` 产生了数千条信息。(这个速度因时间而异，但通常每分钟超过一千条。当我运行该代码时，它应该已经处理了大约一万条信息，才会产生这样的输出）。但如你所见，`shipStatusChanges` 只产生了 19 条信息。

这说明了 Rx 是如何驯服大容量事件源的，它比单纯的聚合强大得多。我们并没有将数据简化为只能提供概览的统计量。平均值或方差等统计量通常非常有用，但它们并不总能提供我们想要的特定领域洞察力。例如，它们无法告诉我们任何关于特定船只的信息。但在这里，每条信息都能告诉我们关于某艘特定船只的信息。尽管我们正在查看每一艘舰船，但我们仍然能够保留这种详细程度。我们可以指示 Rx 在任何一艘飞船改变状态时告诉我们。

我可能会觉得这太小题大做了，但实现这一结果所花费的精力实在太少，以至于我们很容易忽略 Rx 在这里为我们做了多少工作。这段代码完成了以下所有工作：

- 监控在挪威水域航行的每一艘船只
- 提供每艘船只的信息
- 以人类可以合理应对的速度报告事件

它可以处理成千上万条信息，并进行必要的处理，找出与我们真正相关的少数信息。

这是我在[转换一章中的‘SelectMany 的意义’中描述的“扇出，然后再扇入”技术](transformation-sequences.md#SelectMany 的意义)的一个示例。这段代码使用 `GroupBy` 将单个观测值扇形扩展到多个观测值。这一步的关键是创建嵌套的观察项，为我们要做的处理提供合适的细节级别。在本例中，详细程度是 “一艘特定的船”，但也不一定是这样。您可以想象按地区对信息进行分组--也许我们对比较不同的港口很感兴趣，因此我们希望根据船只最靠近的港口来划分信息源，或者根据其目的港来划分信息源。(AIS 为船只提供了一种广播其预定目的地的方式。）按照我们所需的任何标准对数据进行分区后，我们就可以定义要对每组数据进行的处理。在这种情况下，我们只需注意导航状态（`NavigationStatus`）的变化。这一步通常会减少数据量。例如，大多数船只每天最多只会更改几次导航状态。在将通知流减少到我们真正关心的事件之后，我们就可以将其合并为一个流，提供我们想要的高价值通知。

当然，这种能力是有代价的。让 Rx 为我们做这些工作并不需要太多的代码，但我们需要它付出相当大的努力：它需要记住迄今为止看到的每一艘飞船，并为每一艘飞船维护一个可观测源。如果我们的数据源覆盖面足够广，可以接收来自数以万计船只的信息，那么 Rx 就需要为每艘船只维护数以万计的可观测源。所示示例中没有任何类似于非活动超时的功能，只要程序运行，哪怕只有一条船只广播的信息也会被记住。(如果恶意行为者编造 AIS 信息，每条信息都使用不同的编造标识符，最终会导致代码因内存耗尽而崩溃）。根据数据源的不同，你可能需要采取一些措施来避免内存使用量的无限制增长，因此实际示例可能会比这更复杂，但基本方法还是很强大的。

既然我们已经看到了一个示例，下面让我们更详细地了解一下 `GroupBy`。它有几种不同的形式。我们刚才使用的重载是：

```c#
public static IObservable<IGroupedObservable<TKey, TSource>> GroupBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    IEqualityComparer<TKey> comparer)
```

还有两个重载使用元素选择器参数扩展了前两个重载：

```c#
public static IObservable<IGroupedObservable<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector)
{...}

public static IObservable<IGroupedObservable<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector, 
    IEqualityComparer<TKey> comparer)
{...}
```

这在功能上等同于在 `GroupBy` 后使用 `Select` 操作符。

顺便提一下，在使用 `GroupBy` 时，您可能会直接订阅嵌套的观测值：

```c#
// Don't do it this way. Use the earlier example.
perShipObservables.Subscribe(shipMessages =>
  shipMessages
    .OfType<IAisMessageType1to3>()
    .DistinctUntilChanged(m => m.NavigationStatus)
    .Skip(1)
    .Subscribe(m => Console.WriteLine(
    $"Ship {((IAisMessage)m).Mmsi} changed status to {m.NavigationStatus} at {DateTimeOffset.UtcNow}")));
```

这似乎有同样的效果：这里的 `perShipObservables` 是由 `GroupBy` 返回的序列，因此它会为每艘不同的船只生成一个可观察流。本示例订阅了该序列，然后在每个嵌套序列上使用了与之前相同的操作符，但并没有使用 `SelectMany` 将结果收集到一个单一的输出可观察数据流中，而是为每个嵌套数据流明确调用了 Subscribe。

如果你对 Rx 不熟悉，这似乎是一种更自然的工作方式。不过，虽然这看起来会产生相同的行为，但却带来了一个问题：Rx 无法理解这些嵌套订阅与外部订阅相关联。在这个简单的示例中，这不一定会造成问题，但如果我们开始使用额外的操作符，就会出现问题。请考虑以下修改：

```c#
IDisposable sub = perShipObservables.Subscribe(shipMessages =>
  shipMessages
    .OfType<IAisMessageType1to3>()
    .DistinctUntilChanged(m => m.NavigationStatus)
    .Skip(1)
    .Finally(() => Console.WriteLine($"Nested sub for {shipMessages.Key} ending"))
    .Subscribe(m => Console.WriteLine(
    $"Ship {((IAisMessage)m).Mmsi} changed status to {m.NavigationStatus} at {DateTimeOffset.UtcNow}")));
```

我为嵌套序列添加了一个 `Finally` 操作符。这使我们能够在序列因任何原因结束时调用回调。但是，即使我们取消订阅外部序列（调用 `sub.Dispose();`），这个 `Finally` 也永远不会执行任何操作。这是因为 Rx 无法知道这些内部订阅是外部订阅的一部分。

如果我们对先前的版本进行同样的修改，即通过 `SelectMany` 将这些嵌套序列收集到一个输出序列中，Rx 就会明白，到对内部序列的订阅只存在于对 `SelectMany` 返回的序列的订阅中。（事实上，正是 `SelectMany` 订阅了这些内部序列）因此，如果我们取消订阅该示例中的输出序列，它就会正确地运行对任何内部序列的 `Finally` 回调。

一般来说，如果有大量序列作为单个处理链的一部分出现，通常最好让 Rx 从头到尾管理整个流程。

## Buffer