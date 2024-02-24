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

如果前文所述的内置聚合不能满足您的需求，您可以创建自己的聚合。Rx 提供了两种不同的方法。

### Aggregate

`Aggregate` 方法非常灵活：我们可以用它构建本章迄今为止所展示的任何运算符。你为它提供一个函数，它就会为每个元素调用一次该函数。但它并不只是将元素传递给函数：它还为函数提供了一种汇总信息的方法。除了当前元素，它还会传递一个累加器。累加器可以是任何你喜欢的类型--这取决于你想要累加哪类信息。无论函数返回的是什么值，都将成为新的累加器值，并将该值与源的下一个元素一起传入函数。这种方法有几种变体，但最简单的重载是这样的：

```c#
IObservable<TSource> Aggregate<TSource>(
    this IObservable<TSource> source, 
    Func<TSource, TSource, TSource> accumulator)
```

如果您想为 `int` 值生成自己版本的 `Count`，只需提供一个函数，将源代码生成的每个值加 1 即可：

```c#
IObservable<int> sum = source.Aggregate((acc, element) => acc + 1);
```

为了理解这到底是在做什么，让我们来看看 `Aggregate` 将如何调用这个 lambda。为了更容易理解，假设我们将 lambda 放入自己的变量中：

```c#
Func<int, int, int> c = (acc, element) => acc + 1;
```

现在，假设源产生了一个值为 100 的项目。`Aggregate` 将调用我们的函数：

```
c(0, 100) // returns 1
```

第一个参数是当前累加器的值。`Aggregate` 使用 `default(int)` 作为初始累加器值，即 0。因此，如果数据源产生了第二个值，比如 200，`Aggregate` 将传递新的累加器和数据源的第二个值：

```
c(1, 200) // returns 2
```

这个函数完全忽略了它的第二个参数（源元素）。它只是每次在累加器中加 1。因此，累加器只不过是函数被调用次数的记录。

现在我们来看看如何使用 `Aggregate` 实现 `Sum`：

```c#
Func<int, int, int> s = (acc, element) => acc + element
IObservable<int> sum = source.Aggregate(s);
```

对于第一个元素，`Aggregate` 将再次传递我们所选累加器类型的默认值，即 int：0，并传递第一个元素的值。因此，如果第一个元素是 100，它将这样做

```
s(0, 100) // returns 100
```

然后，如果第二个元素是 200，`Aggregate` 就会进行调用：

```
s(100, 200) // returns 300
```

请注意，这次的第一个参数是 100，因为这是上一次调用 s 的返回值。因此在这种情况下，在看到 100 和 200 元素后，累加器的值是 300，即所有元素的总和。

如果我们希望累加器的初始值不是 `default(TAccumulator)` 呢？有一个重载可以解决这个问题。例如，我们可以在下面使用 `Aggregate` 实现类似 All 的功能：

```c#
IObservable<bool> all = source.Aggregate(true, (acc, element) => acc && element);
```

顺便说一下，这并不完全等同于真正的 `All`：它处理错误的方式不同。如果 `All` 看到一个元素是假的，它就会立即取消订阅源，因为它知道源产生的其他任何东西都不可能改变结果。这就意味着，如果信息源即将产生错误，它将不再有机会产生错误，因为 `All` 取消了订阅。但是，`Aggregate` 无法知道累加器已经进入了一个永远无法离开的状态，因此它将继续订阅源，直到源完成（或者直到订阅了 `Aggregate` 返回的 `IObservable<T>` 的代码取消订阅）。这意味着，如果源返回 `true`，然后又返回 `false`，与 `All` 不同，`Aggregate` 将继续订阅源，因此如果源继续调用 `OnError`，`Aggregate` 将接收该错误，并将其传递给订阅者。

以下是一些人认为对 `Aggregate` 有帮助的思考方式。如果源产生 1 到 5 的值，如果我们传递给 `Aggregate` 的函数称为 `f`，那么源完成后，`Aggregate` 产生的值将是这样的：

```
T result = f(f(f(f(f(default(T), 1), 2), 3), 4), 5);
```

因此，在我们重现 `Count` 的例子中，累加器的类型是 `int`，所以就变成了...：

```c#
int sum = s(s(s(s(s(0, 1), 2), 3), 4), 5);
// Note: Aggregate doesn't return this directly -
// it returns an IObservable<int> that produces this value.
```

Rx 的 `Aggregate` 并不会一次性执行所有这些调用：每次源产生一个元素时，它都会调用函数，因此计算会分散到不同的时间。如果你的回调函数是一个纯粹的函数--不受全局变量和其他环境因素的影响，并且对任何特定输入总是返回相同的结果--那么这并不重要。`Aggregate` 的结果将与前面例子中的大表达式一样。但是，如果你的回调行为受到全局变量或文件系统当前内容的影响，那么当源产生每个值时都会调用它这一事实可能会更加重要。

顺便说一下，聚合在某些编程系统中还有其他名称。有些系统称其为 `reduce`。它还经常被称为折叠（fold）。(特别是左折叠。右折叠则相反。通常，它的函数以相反的顺序接受参数，因此看起来像 `s(1、s(2、s(3、s(4、s(5、0)))))`。Rx 不提供内置的右折叠。这并不自然，因为它必须等到收到最后一个元素后才能开始，这意味着它需要保留整个序列中的每个元素，然后在序列完成时一次性评估整个折叠）。

你可能已经发现，在我重新实现某些内置聚合运算符的过程中，我直接从 `Sum` 转到了 `Any`。那么平均值呢？事实证明，我们无法使用我迄今为止向你展示的重载来实现它。这是因为 `Average` 需要累积两个信息--运行中的总数和计数--而且它还需要在最后执行一个步骤：用总数除以计数。通过目前展示的重载，我们只能实现部分功能：

```c#
IObservable<int> nums = Observable.Range(1, 5);

IObservable<(int Count, int Sum)> avgAcc = nums.Aggregate(
    (Count: 0, Sum: 0),
    (acc, element) => (Count: acc.Count + 1, Sum: acc.Sum + element));
```

它使用元组作为累加器，使其能够累加两个值：计数和总和。但累加器的最终值变成了结果，而这并不是我们想要的。我们缺少了用总和除以计数来计算平均值的最后一步。幸运的是，`Aggregate` 提供了第三个重载，使我们能够提供最后一步。我们传递第二个回调，当源完成时，它将被调用一次。`Aggregate` 会将最后的累加器值传入这个 lambda，然后无论它返回什么，都会成为 `Aggregate` 返回的可观察对象生成的单个项目。

```c#
IObservable<double> avg = nums.Aggregate(
    (Count: 0, Sum: 0),
    (acc, element) => (Count: acc.Count + 1, Sum: acc.Sum + element),
    acc => ((double) acc.Sum) / acc.Count);
```

我一直在展示 `Aggregate` 如何重新实现一些内置的聚合运算符，以说明它是一个功能强大且非常通用的运算符。然而，这并不是我们使用它的目的。`Aggregate` 之所以有用，正是因为它允许我们定义自定义聚合。

例如，假设我想建立一个列表，列出所有通过 AIS 广播过详细信息的船只名称。下面是一种方法：

```c#
IObservable<IReadOnlySet<string>> allNames = vesselNames
    .Take(10)
    .Aggregate(
        ImmutableHashSet<string>.Empty,
        (set, name) => set.Add(name.VesselName));
```

我在这里使用 `ImmutableHashSet<string>`，是因为它的使用模式恰好与 `Aggregate` 非常匹配。普通的 `HashSet<string>` 也可以使用，但不太方便，因为它的 `Add` 方法不返回集合，所以我们的函数需要额外的语句来返回累积的集合：

```c#
IObservable<IReadOnlySet<string>> allNames = vesselNames
    .Take(10)
    .Aggregate(
        new HashSet<string>(),
        (set, name) =>
        {
            set.Add(name.VesselName);
            return set;
        });
```

无论采用上述哪种实现， `vesselNames` 都将生成一个 `IReadOnlySet<string>` 值，其中包含在报告名称的前 10 条信息中看到的每个船名。

在最后两个示例中，我不得不对一个问题进行了修改。我让它们只在前 10 条合适的信息中工作。想想如果我没有 `Take(10)` 会发生什么。代码可以编译，但我们会遇到问题。我在各种示例中使用的 AIS 信息源是一个无穷无尽的信息源。在可预见的未来，船舶将继续在大洋上航行。Ais.NET 不包含任何代码来检测文明的终结，或者发明将船只使用变得过时的技术，因此它永远不会调用订阅者的 `OnCompleted` 方法。由 `Aggregate` 返回的可观察对象在其源完成或失败之前不会报告任何内容。因此，如果我们移除 `Take(10)`，行为将与 `Observable.Never<IReadOnlySet<string>>` 完全相同。我不得不强制输入到 `Aggregate` 的数据结束，以使其产生结果。但还有另一种方法。

### Scan

虽然聚合允许我们将完整的序列还原为一个单一的最终值，但有时这并不是我们所需要的。如果我们要处理的是一个无穷无尽的数据源，我们可能更需要一个类似于运行总计的东西，每次接收到一个值时都进行更新。`Scan` 操作符正是为满足这种需求而设计的。`Scan` 和 `Aggregate` 的签名是一样的，区别在于 `Scan` 不会等待输入结束。它在每个项目后都会产生一个聚合值。

我们可以用它来建立一组船只名称，如上一节所述，但使用 `Scan` 时，我们不必等到输入结束。每次收到包含名称的信息时，它都会报告当前列表：

```c#
IObservable<IReadOnlySet<string>> allNames = vesselNames
    .Scan(
        ImmutableHashSet<string>.Empty,
        (set, name) => set.Add(name.VesselName));
```

请注意，即使没有任何变化，这个 `allNames` 可观察对象也会产生元素。如果累积的名称集合已经包含了刚刚从 `vesselNames` 中出现的名称，调用 `set.Add` 将什么也做不了，因为该名称已经在集合中了。但是 `Scan` 扫描会为每个输入产生一个输出，而且不会在意累加器是否不变。至于这一点是否重要，取决于你打算如何使用 `allNames` 可观察对象，但如果需要，你可以使用[第 5 章中的 DistinctUntilChanged 操作符](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/05_Filtering.md#distinct-and-distinctuntilchanged)轻松解决这个问题。

你可以把 `Scan` 看作是显示其工作的 `Aggregate` 版本。如果我们想看看计算平均值的过程是如何聚合计数和总和的，我们可以这样写：

```c#
IObservable<int> nums = Observable.Range(1, 5);

IObservable<(int Count, int Sum)> avgAcc = nums.Scan(
    (Count: 0, Sum: 0),
    (acc, element) => (Count: acc.Count + 1, Sum: acc.Sum + element));

avgAcc.Dump("acc");
```

输出结果如下：

```
acc-->(1, 1)
acc-->(2, 3)
acc-->(3, 6)
acc-->(4, 10)
acc-->(5, 15)
acc completed
```

在这里你可以清楚地看到，每次源产生一个值时，`Scan` 都会发射当前的累加值。

与 `Aggregate` 不同的是，`Scan` 并不提供重载，而是使用第二个函数将累加器转换为结果。因此，我们在这里可以看到包含计数和求和的元组，但看不到我们想要的实际平均值。不过，我们可以使用[转换一章](transformation-sequences.md)中介绍的 `Select` 操作符来实现：

```c#
IObservable<double> avg = nums.Scan(
    (Count: 0, Sum: 0),
    (acc, element) => (Count: acc.Count + 1, Sum: acc.Sum + element))
    .Select(acc => ((double) acc.Sum) / acc.Count);

avg.Dump("avg");
```

输出：

```
avg-->1
avg-->1.5
avg-->2
avg-->2.5
avg-->3
avg completed
```

`Scan` 是一个比 `Aggregate` 更通用的操作符。您可以通过将 `Scan` 与 [过滤一章中描述的 `TakeLast()` 操作符](operation-filtering.md)相结合来实现 `Aggregate`。

```c#
source.Aggregate(0, (acc, current) => acc + current);
// is equivalent to 
source.Scan(0, (acc, current) => acc + current).TakeLast();
```

聚合对于减少数据量或将多个元素结合起来以产生平均值或其他包含多个元素信息的测量值非常有用。但要进行某些分析，我们还需要在计算汇总值之前对数据进行切分或其他重组。因此，在下一章中，我们将了解 Rx 提供的各种数据分区机制。