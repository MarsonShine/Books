# 序列转换

我们从序列中获取的值并不总是我们需要的格式。有时信息量比我们需要的要多，我们需要从中挑选出我们感兴趣的值。有时，每个值都需要扩展成一个更丰富的对象或更多的值。

到目前为止，我们已经了解了序列的创建、序列的转换以及通过筛选减少序列。本章我们将探讨序列的转换。

## Select

最直接的转换方法是 `Select`。它允许您提供一个函数，接收 `TSource` 的值并返回 `TResult` 的值。`Select` 的签名反映了它将序列元素从一种类型转换为另一种类型的能力，即从 `IObservable<TSource>` 转换为 `IObservable<TResult>`。

```c#
IObservable<TResult> Select<TSource, TResult>(
    this IObservable<TSource> source, 
    Func<TSource, TResult> selector)
```

您不必改变类型--`TSource` 和 `TResult` 可以是相同的。第一个示例将一个整数序列加上 3，得到另一个整数序列。

```c#
IObservable<int> source = Observable.Range(0, 5);
source.Select(i => i+3)
      .Dump("+3")
```

它使用了我们在[过滤](operation-filtering.md)一章开头定义的 `Dump` 扩展方法。输出结果如下

```
+3 --> 3
+3 --> 4
+3 --> 5
+3 --> 6
+3 --> 7
+3 completed
```

下一个示例将以改变数值类型的方式转换数值。它将整数值转换为字符。

```c#
Observable.Range(1, 5);
          .Select(i => (char)(i + 64))
          .Dump("char");
```

输出：

```
char --> A
char --> B
char --> C
char --> D
char --> E
char completed
```

本例将整数序列转换为匿名类型的序列：

```c#
Observable.Range(1, 5)
          .Select(i => new { Number = i, Character = (char)(i + 64) })
          .Dump("anon");
```

`Select` 是 C# 查询表达式语法支持的标准 LINQ 操作符之一，因此我们可以这样编写最后一个示例：

```c#
var query = from i in Observable.Range(1, 5)
            select new {Number = i, Character = (char) (i + 64)};

query.Dump("anon");
```

在 Rx 中，`Select` 还有另一种重载，其中的选择器函数接受两个值。附加参数是元素在序列中的索引。如果元素在序列中的索引对选择器函数很重要，请使用此方法。

## SelectMany

`Select` 可以为每个输入生成一个输出，而 `SelectMany` 则可以将每个输入元素转换成任意数量的输出。让我们先来看一个只使用 `Select` 的示例，看看它是如何工作的：

```c#
Observable
    .Range(1, 5)
    .Select(i => new string((char)(i+64), i))
    .Dump("strings");
```

输出：

```
strings-->A
strings-->BB
strings-->CCC
strings-->DDDD
strings-->EEEEE
strings completed
```

正如你所看到的，对于 `Range` 生成的每个数字，我们的输出都包含一个字符串，其长度就是这个字符串的长度。如果我们不将每个数字转换成字符串，而是将其转换成 `IObservable<char>` 会怎样呢？我们只需在构造字符串后添加 `.ToObservable()` 即可：

```c#
Observable
    .Range(1, 5)
    .Select(i => new string((char)(i+64), i).ToObservable())
    .Dump("sequences");
```

(或者，我们也可以用 `i => Observable.Repeat((char)(i+64),i)` 替换选择表达式。两者效果完全相同）。输出结果并不十分有用：

```
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings completed
```

我们有了一个可观察序列的可观察序列。但如果我们将 `Select` 替换为 `SelectMany`，会发生什么呢？

```c#
Observable
    .Range(1, 5)
    .SelectMany(i => new string((char)(i+64), i).ToObservable())
    .Dump("chars");
```

这样我们就得到了一个 `IObservable<char>`，其输出是这样的：

```
chars-->A
chars-->B
chars-->B
chars-->C
chars-->C
chars-->D
chars-->C
chars-->D
chars-->E
chars-->D
chars-->E
chars-->D
chars-->E
chars-->E
chars-->E
chars completed
```

顺序变得有点乱，但如果你仔细观察，就会发现每个字母出现的次数与我们发射字符串时的相同。例如，只有一个 `A`，但 `C` 出现了三次，`E` 出现了五次。

`SelectMany` 希望转换函数为每个输入返回一个 `IObservable<T>`，然后将这些结果合并为一个结果。与 LINQ to Objects 相对应的功能就没那么混乱了。如果运行以下代码

```c#
Enumerable
    .Range(1, 5)
    .SelectMany(i => new string((char)(i+64), i))
    .ToList()
```

就会产生一个包含这些元素的列表：

```
[ A, B, B, C, C, C, D, D, D, D, E, E, E, E, E ]
```

顺序就不那么奇怪了。这其中的原因值得深入探讨。

### `IEnumerable<T>` vs. `IObservable<T>` `SelectMany`

`IEnumerable<T>` 是基于拉取的--序列只有在被要求时才会产生元素。`Enumerable.SelectMany` 以一种非常特殊的顺序从其源中提取项目。首先，它会询问其源 `IEnumerable<int>`（即上一示例中 `Range` 返回的 `IEnumerable<int>`）的第一个值。然后，`SelectMany` 会调用我们的回调，传递第一个项目，然后枚举我们的回调返回的 `IEnumerable<char>` 中的所有项目。只有在枚举完毕后，它才会向来源（`Range`）询问第二个项目。同样，它会将第二个项目传递给我们的回调，然后完全枚举 `IEnumerable<char>`，我们返回，以此类推。因此，我们会先获取第一个嵌套序列中的所有内容，然后获取第二个嵌套序列中的所有内容，等等。

`Enumerable.SelectMany` 之所以能以这种方式进行，有两个原因。首先，`IEnumerable<T>` 基于拉的特性使其能够决定处理事情的顺序。其次，对于 `IEnumerable<T>`，操作阻塞是很正常的，也就是说，在它们为我们带来新内容之前不会返回。在前面的示例中，当调用 `ToList` 时，直到将所有结果完全填充到 `List<T>` 中才会返回。

Rx 并非如此。首先，消费者不能告诉源何时产生每个项目--源会在准备好时发射项目。其次，Rx 通常是对正在进行的流程进行建模，因此我们不希望方法调用在完成之前阻塞。在某些情况下，Rx 序列会自然而然地快速生成所有项目并尽快完成，但我们倾向于使用 Rx 建模的信息源通常不会这样做。因此，Rx 中的大多数操作都不会阻塞--它们会立即返回一些内容（如 `IObservable<T>` 或代表订阅的 `IDisposable`），然后再产生值。

我们正在研究的 Rx 版本示例实际上就是这种不寻常的情况之一，其中每个序列都会尽快发送项目。从逻辑上讲，所有嵌套的 `IObservable<char>` 序列都在同时进行。结果是一团糟，因为这里的每个可观察源都试图以源消耗元素的速度产生每个元素。它们最终交错进行的事实与这类可观察源使用 Rx 调度器系统的方式有关，我们将在[调度和线程](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/11_SchedulingAndThreading.md)一章中介绍该系统。调度器确保即使我们在模拟逻辑上并发的进程时，Rx 的规则也能得到遵守，`SelectMany` 输出的观察者一次只能得到一个项目。下面的大理石图显示了导致我们看到的混乱输出的事件：

![](./asserts/Ch06-Transformation-Marbles-Select-Many-Marbles.svg)

我们可以稍作调整，防止所有子序列同时运行。(这也使用了 `Observable.Repeat`，而不是使用构造字符串然后调用 `ToObservable` 的间接方法。我在前面的示例中这样做是为了强调与 LINQ to Objects 示例的相似性，但在 Rx 中并不会这样做）。

```c#
Observable
    .Range(1, 5)
    .SelectMany(i => 
        Observable.Repeat((char)(i+64), i)
                  .Delay(TimeSpan.FromMilliseconds(i * 100)))
    .Dump("chars");
```

现在，我们得到了与 `IEnumerable<T>` 版本一致的输出结果：

```c#
chars-->A
chars-->B
chars-->B
chars-->C
chars-->C
chars-->C
chars-->D
chars-->D
chars-->D
chars-->D
chars-->E
chars-->E
chars-->E
chars-->E
chars-->E
chars completed
```

这就说明，`SelectMany` 可以让您为源产生的每个项目生成一个序列，并将所有这些新序列中的所有项目平铺到一个包含所有内容的序列中。虽然这可能会使理解更容易，但在现实中，你不会希望仅为了理解而引入这种延迟。这些延迟意味着所有元素需要大约一秒半的时间才能出现。这个示例中的代码通过使每个子可观察对象产生一小组项目来产生一个看起来合理的顺序，我们只是为了让它们之间有一些间隔而引入了空闲时间。

![](./asserts/Ch06-Transformation-Marbles-Select-Many-Marbles-Delay.svg)

我引入这些间隙纯粹是为了提供一个不那么令人困惑的示例，但如果你真的想要这种严格按顺序处理的方法，在实际中你就不会以这种方式使用 `SelectMany`。首先，它不能保证完全有效。(如果您尝试这个示例，但将其修改为使用越来越短的时间间隔，最终您会发现项目又开始变得杂乱无章。而且，由于 .NET 不是实时编程系统，因此实际上没有一个安全的时间跨度可以保证排序）。如果您绝对需要在看到第二个子序列中的所有项目之前，先看到第一个子序列中的所有项目，那么实际上有一种强大的方法可以满足您的要求：

```c#
Observable
    .Range(1, 5)
    .Select(i => Observable.Repeat((char)(i+64), i))
    .Concat())
    .Dump("chars");
```

不过，这并不是展示 `SelectMany` 功能的好方法，因为本例不再使用它（本例使用 `Concat`，这将在[组合序列](https://github.com/dotnet/reactive/blob/main/Rx.NET/Documentation/IntroToRx/09_CombiningSequences.md)一章中讨论）。我们使用 `SelectMany` 的情况要么是我们知道我们正在解包一个单值序列，要么是我们没有特定的排序要求，而想在元素从子观测变量中出现时获取它们。

### SelectMany 的意义