# 聚合（Aggregation）

数据的原始形式并不总是可处理的。有时，我们需要对收到的堆积如山的数据进行合并、整理、组合或压缩。这可能只是将数据量减少到可管理的水平。例如，考虑来自仪器仪表、金融、信号处理和运营智能等领域的快速移动数据。对于单个数据源来说，这类数据的变化率可能超过每秒十个数值，如果我们观察的是多个数据源，变化率会更高。人真的能消费这些数据吗？对于人类消费而言，平均值、最小值和最大值等汇总值可能更有用。

我们通常可以做到更多。我们可以通过组合和关联的方式来揭示模式，提供从任何单个信息或简单的单一统计测量中无法获得的洞察力。Rx 的可组合性使我们能够对数据流进行复杂而微妙的计算，这让我们不仅能够减少用户必须处理的信息量，还能提高人类收到的每条信息的价值。

我们将从最简单的聚合函数开始，这些函数以某种特定的方式将可观测序列简化为具有单一值的序列。然后，我们将讨论更通用的操作符，使您能够定义自己的聚合机制。

## 简单数值聚合

Rx 支持各种标准 LINQ 操作符，可将序列中的所有值缩减为一个数字结果。

### Count

`Count` 可以告诉您一个序列包含多少个元素。虽然这是一个标准的 LINQ 操作符，但 Rx 的版本与 `IEnumerable<T>` 版本有所不同，因为 Rx 将返回一个可观察的序列，而不是一个标量值。与往常一样，这是因为 Rx 具有与推相关的性质。Rx 的 `Count` 无法要求源程序立即提供所有元素，因此它只能等待，直到源说它已经完成。`Count` 返回的序列将始终是 `IObservable<int>` 类型，与源的元素类型无关。在源完成之前，它不会做任何事情，此时它会发出一个值，报告源产生了多少个元素，然后它会立即完成。本示例将 `Count` 与 `Range` 结合使用，因为 `Range` 会尽快生成所有值，然后完成，这意味着我们会立即从 `Count` 得到一个结果：

```c#
IObservable<int> numbers = Observable.Range(0,3);
numbers.Count().Dump("count");
```

输出：

```
count-->3
count Completed
```

如果您希望序列中的值超过 32 位有符号整数的计数范围，您可以使用 `LongCount` 操作符。除了返回 `IObservable<long>` 之外，该操作与 `Count` 相同。

### Sum

`Sum` 运算符将源值中的所有值相加，产生的唯一输出是总数。与 `Count` 运算符一样，Rx 的 `Sum` 运算符与大多数其他 LINQ 提供者的不同之处在于，它的输出不是标量。它产生的是一个可观察序列，在源完成之前什么也不做。当源完成时，`Sum` 返回的可观察序列产生一个单一值，然后立即完成。本示例展示了它的使用：

```c#
IObservable<int> numbers = Observable.Range(1,5);
numbers.Sum().Dump("sum");
```

输出：

```
sum-->15
sum completed
```

`Sum` 只能处理 `int、long、float、double decimal` 或这些类型的可空版本。这就意味着，有些您可能希望使用 `Sum` 的类型却无法使用。例如，`System.Numerics` 名称空间中的 `BigInteger` 类型表示整数值，其大小仅受可用内存和执行计算所需等待时间的限制。(您可以使用 `+` 将其相加，因为该类型定义了该操作符的重载。但 `Sum` 一直以来都无法找到重载。C# 11.0 中引入了泛型数学，这意味着在技术上可以引入一个 `Sum` 版本，该版本可用于任何实现了 `IAdditionOperators<T, T, T>` 的 `T` 类型。不过，这意味着需要依赖 .NET 7.0 或更高版本（因为旧版本中没有泛型数学），而在编写本报告时，Rx 通过其 net6.0 目标支持 .NET 7.0。Rx 可以引入单独的 net7.0 或 net8.0 目标来实现这一功能，但目前尚未这样做。(公平地说，LINQ to Objects 中的 `Sum` 也还不支持这一点）。

如果您向 `Sum` 提供这些类型的可归零版本（例如，您的源是 `IObservable<int?>`），那么 `Sum` 也将返回一个具有可归零项类型的序列，如果输入值中有一个为空，那么它将产生空值。

虽然 `Sum` 只能处理一小部分固定的数字类型，但您的数据源并不一定要产生这些类型的值。`Sum` 提供了接受 lambda 的重载，可以从每个输入元素中提取合适的数值。例如，假设您想回答下面这个不太可能的问题：如果把碰巧通过 AIS 广播描述自己的下 10 艘船并排放在一起，它们是否都能容纳在某个特定宽度的通道中？要回答这个问题，我们可以从 AIS 信息中筛选出能提供船只大小信息的信息，用 `Take` 方法收集下 10 条此类信息，然后用 `Sum` 方法求和。Ais.NET 库的 `IVesselDimensions` 接口并没有实现加法运算（即使实现了加法运算，我们也已经看到 Rx 无法利用这一点），不过没关系：我们只需提供一个 lambda，它可以接收 `IVesselDimensions` 并返回 `Sum` 可以处理的某种数值类型的值：

```c#
IObservable<IVesselDimensions> vesselDimensions = receiverHost.Messages
    .OfType<IVesselDimensions>();

IObservable<int> totalVesselWidths = vesselDimensions
    .Take(10)
    .Sum(dimensions => 
            checked((int)(dimensions.DimensionToPort + dimensions.DimensionToStarboard)));
```

(如果您想知道这里的转换和 `checked` 关键字是怎么回事，AIS 将这些值定义为无符号整数，因此 Ais.NET 库将其报告为 `uint`，而 Rx's `Sum` 不支持 `uint` 类型。实际上，船只的宽度不大可能溢出 32 位有符号整数，因此我们只需将其转换为 `int`，如果遇到宽度超过 21 亿米的船只，`checked` 关键字将抛出异常（见第 4.3 节）。

### Average

标准 LINQ 运算符 `Average` 可以有效地计算出 `Sum` 计算出的值，然后将其除以 `Count` 计算出的值。同样，大多数 LINQ 实现会返回一个标量，而 Rx 的 `Average` 会产生一个可观察值。

虽然 `Average` 可以处理与 `Sum` 相同数值类型的值，但在某些情况下输出类型会有所不同。如果源值是 `IObservable<int>`，或者如果您使用的重载之一是从源值中提取值的 lambda，并且该 lambda 返回 `int`，那么结果将是 `double`。这是因为一组整数的平均值并不一定是整数。同样，取 `long` 值的平均值也会产生一个 `double`。然而，`decimal` 类型的输入会产生 `decimal` 类型的输出，同样，`float` 输入会产生 `float` 输出。

与 `Sum` 一样，如果 `Average` 的输入为空值，输出也将为空值。

### Min 和 Max

Rx 实现了标准的 LINQ `Min` 和 `Max` 操作符，用于查找具有最高或最低值的元素。与本节中的所有其他操作符一样，这些操作符不返回标量，而是返回产生单个值的 `IObservable<T>`。

Rx 为 `Sum` 和 `Average` 所支持的相同数值类型定义了专门的实现。不过，与这些操作符不同的是，它还定义了一个重载，可以接受任何类型的源项。在 Rx 没有定义专门实现的源类型上使用 `Min` 或 `Max` 时，它会使用 [Comparer<T>.Default](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.comparer-1.default) 来比较项。还有一个重载功能可以让您传递一个比较器。

与 `Sum` 和 `Average` 一样，也有接受回调的重载。如果使用这些重载，`Min` 和 `Max` 将为每个源项调用回调，并查找回调返回的最低或最高值。请注意，它们最终产生的单一输出将是回调返回的值，而不是产生该值的原始源项。要了解这意味着什么，请看下面的示例：

```c#
IObservable<int> widthOfWidestVessel = vesselDimensions
    .Take(10)
    .Max(dimensions => 
            checked((int)(dimensions.DimensionToPort + dimensions.DimensionToStarboard)));
```

`Max` 在这里返回一个 `IObservable<int>`，它将是报告船只尺寸的下 10 条信息中最宽船只的宽度。但如果您不想只看到宽度呢？如果您想要看到整条信息呢？

### MinBy 和 MaxBy

Rx 提供了 `Min` 和 `Max` 的两种微妙变化：`MinBy` 和 `MaxBy`。它们与我们刚才看到的基于回调的 `Min` 和 `Max` 类似，使我们能够处理不是数值但可能具有数值属性的元素序列。不同之处在于，`MinBy` 和 `MaxBy` 不是返回最小值或最大值，而是告诉你是哪个源元素产生了该值。例如，假设我们不只是想知道最宽的船的宽度，而是想知道它到底是哪艘船：

```c#
IObservable<IVesselDimensions> widthOfWidestVessel = vesselDimensions
    .Take(10)
    .MaxBy(dimensions => 
              checked((int)(dimensions.DimensionToPort + dimensions.DimensionToStarboard)));
```

这与上一节中的示例非常相似。我们正在处理一个元素类型为 `IVesselDimensions` 的序列，因此我们提供了一个回调，用于提取我们要用于比较的值。(就像 `Max` 一样，`MaxBy` 试图找出哪个元素在传递给回调时产生的值最高。在源完成之前，它无法知道哪个是最高值。如果数据源尚未完成，它只能知道最高值，但这个值可能会被未来的值超过。因此，就像我们在本章中学习的所有其他运算符一样，在源运算完成之前，这个运算符不会产生任何结果，这就是为什么我在这里加了一个 `Take(10)`。

不过，我们得到的序列类型有所不同。`Max` 返回的是一个 `IObservable<int>`，因为它会调用源中每个项的回调，然后产生我们的回调返回的最高值。但使用 `MaxBy` 时，我们返回的是 `IObservable<IVesselDimensions>`，因为 `MaxBy` 会告诉我们是哪个源元素产生了该值。

当然，宽度最大的元素可能不止一个，例如可能有三艘同样大的船。对于 `Max` 来说，这并不重要，因为它只试图返回实际值：有多少个源项具有最大值并不重要，因为所有情况下的值都是一样的。但使用 `MaxBy` 时，我们会返回产生最大值的原始条目，如果有三个条目都产生了最大值，我们就不希望 Rx 任意选择其中一个。

因此，与我们目前看到的其他聚合运算符不同，`MinBy` 或 `MaxBy` 返回的可观测值并不一定只产生一个值。它可能会产生多个值。你可能会问，这是否真的是一个聚合运算符，因为它并没有将输入序列还原为一个输出。但它确实将输入序列还原成了一个单一的值：回调返回的最小值（或最大值）。只是它呈现结果的方式略有不同。它根据聚合过程的结果生成一个序列。我们可以把它看作是聚合和过滤的结合：它执行聚合以确定最小值或最大值，然后将源序列过滤到回调产生该值的元素。

注：LINQ to Objects 也定义了 [MinBy](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.minby) 和 [MaxBy](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.maxby) 方法，但它们略有不同。这些 LINQ to Objects 版本实际上是任意选择一个源元素--如果有多个源值都产生最小（或最大）结果，LINQ to Objects 只给出其中一个，而 Rx 则给出所有源值。Rx 在 .NET 6.0 添加 LINQ to Objects 之前几年就定义了这些运算符的版本，因此如果您想知道为什么 Rx 的做法不同，真正的问题是为什么 LINQ to Objects 没有遵循 Rx 的先例。

## 简单布尔值聚合

LINQ 定义了几个标准运算符，可将整个序列还原为单个布尔值。

### Any

Any 运算符有两种形式。无参数重载有效地询问“此序列中是否有任何元素？“它返回一个可观察的序列，如果源完成后没有产生任何值，则该序列将产生一个 `false`。如果源确实产生了值，那么当第一个值产生时，结果序列将立即产生 `true`，然后完成。如果它收到的第一个通知是错误，那么它就会将错误传递下去。

```c#
var subject = new Subject<int>();
subject.Subscribe(Console.WriteLine, () => Console.WriteLine("Subject completed"));
var any = subject.Any();

any.Subscribe(b => Console.WriteLine("The subject has any values? {0}", b));

subject.OnNext(1);
subject.OnCompleted();
```

输出：

```
1
The subject has any values? True
subject completed
```

如果我们现在删除 `OnNext(1)`，输出结果将变为以下内容

```
subject completed
The subject has any values? False
```

如果信息源确实产生了一个值，`Any` 会立即取消订阅。因此，如果信息源想报告一个错误，`Any` 只能在它产生的第一个通知中看到这个错误。

```c#
var subject = new Subject<int>();
subject.Subscribe(Console.WriteLine,
    ex => Console.WriteLine("subject OnError : {0}", ex),
    () => Console.WriteLine("Subject completed"));

IObservable<bool> any = subject.Any();

any.Subscribe(b => Console.WriteLine("The subject has any values? {0}", b),
    ex => Console.WriteLine(".Any() OnError : {0}", ex),
    () => Console.WriteLine(".Any() completed"));

subject.OnError(new Exception());
```

输出：

```
subject OnError : System.Exception: Exception of type 'System.Exception' was thrown.
.Any() OnError : System.Exception: Exception of type 'System.Exception' was thrown.
```

但是，如果数据源在出现异常之前生成一个值，例如：

```c#
subject.OnNext(42);
subject.OnError(new Exception());
```

我们将看到以下输出：

```
42
The subject has any values? True
.Any() completed
subject OnError : System.Exception: Exception of type 'System.Exception' was thrown.
```

虽然直接订阅源主题的处理程序仍然看到了错误，但我们的 `any` 观察对象报告了一个 `True` 值，然后就完成了，这意味着它没有报告后面的错误。

`Any` 方法也有一个接受谓词的重载。这实际上提出了一个略有不同的问题：“这个序列中是否有符合这些条件的元素？“其效果类似于在使用 `Where` 之后使用 `Any` 的无参数形式。

```c#
IObservable<bool> any = subject.Any(i => i > 2);
// Functionally equivalent to 
IObservable<bool> longWindedAny = subject.Where(i => i > 2).Any();
```

### All

`All` 运算符类似于使用谓词的 `Any` 方法，不同之处在于所有值都必须满足谓词。一旦谓词拒绝了一个值，`All` 返回的可观测变量就会产生一个 `false`，然后结束。如果源在结束时没有产生任何不满足谓词的元素，那么 All 将推送 `true` 作为其值。(这样做的一个结果是，如果在一个空序列上使用 `All`，结果将是一个产生 `true` 的序列。这与 `All` 在其他 LINQ 提供程序中的工作方式是一致的，但对于不熟悉[形式逻辑惯例（即虚真）](https://en.wikipedia.org/wiki/Vacuous_truth)的人来说，这可能会令人惊讶）。

一旦 `All` 决定产生一个 `false`，它就会立即取消订阅源（就像 `Any` 在确定可以产生 true 值时所做的那样）。如果在此之前源产生了错误，该错误将被传递给 `All` 方法的订阅者。

```c#
var subject = new Subject<int>();
subject.Subscribe(Console.WriteLine, () => Console.WriteLine("Subject completed"));
IEnumerable<bool> all = subject.All(i => i < 5);
all.Subscribe(b => Console.WriteLine($"All values less than 5? {b}"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(6);
subject.OnNext(2);
subject.OnNext(1);
subject.OnCompleted();
```

输出：

```
1
2
6
All values less than 5? False
all completed
2
1
subject completed
```

### IsEmpty

LINQ `IsEmpty` 运算符在逻辑上与无参数 `Any` 方法相反。当且仅当源完成时没有产生任何元素，它才会返回 `true`。如果源产生了一个项目，`IsEmpty` 会产生 `false` 并立即取消订阅。如果源产生了错误，则会转发该错误。

### Contains

`Contains` 运算符用于确定序列中是否存在特定元素。您可以使用 `Any` 来实现它，只需提供一个回调函数，将每个项目与您要查找的值进行比较即可。不过，这样做通常会更简洁，也更能直接表达编写 `Contains` 的意图。

```c#
var subject = new Subject<int>();
subject.Subscribe(
    Console.WriteLine, 
    () => Console.WriteLine("Subject completed"));

IEnumerable<bool> contains = subject.Contains(2);

contains.Subscribe(
    b => Console.WriteLine("Contains the value 2? {0}", b),
    () => Console.WriteLine("contains completed"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
    
subject.OnCompleted();
```

输出：

```
1
2
Contains the value 2? True
contains completed
3
Subject completed
```

`Contains` 还有一个重载，允许您指定 `IEqualityComparer<T>` 的实现，而不是该类型的默认实现。如果您有一系列自定义类型，而这些类型根据使用情况可能有一些特殊的平等规则，那么这将非常有用。

## 构建自己的聚合器

