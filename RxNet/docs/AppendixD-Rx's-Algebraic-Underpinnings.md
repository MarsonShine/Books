# 附录 D：Rx 的代数基础

Rx 操作符或多或少可以以你能想象到的任何方式组合在一起，而且它们的组合通常不会出现任何问题。这并不仅仅是一个意外。一般来说，软件组件之间的集成往往是软件开发过程中最大的痛苦之一，因此它能如此出色地工作是非常难能可贵的。这在很大程度上要归功于 Rx 依赖于一些基本理论。Rx 的设计使你不需要知道这些细节就能使用它，但好奇的开发者通常都想知道这些事情。

本书前面的章节已经谈到了 Rx 的一个形式方面：可观测源与可观测源之间的契约。对于 `IObserver<T>` 的可接受使用，有一个定义明确的语法。这超出了 .NET 类型系统所能执行的范围，因此我们只能依靠代码来做正确的事情。不过，`System.Reactive` 库确实始终遵守这一契约，而且它还设置了一些防护类型，可以在应用程序代码不完全遵守规则时进行检测，并防止其造成严重破坏。

`IObserver<T>` 语法非常重要。组件依靠它来确保正确的操作。以 `Where` 操作符为例。它提供了自己的 `IObserver<T>` 实现，用于订阅底层源。它从该源接收项，然后决定将哪些项转发给订阅了由 `Where` 呈现的 `IObservable<T>` 的观察者。您可以想象它的实现可能类似于以下示例：

```c#
public class OverSimplifiedWhereObserver<T> : IObserver<T>
{
    private IObserver<T> downstreamSubscriber;
    private readonly Func<T, bool> predicate;

    public OverSimplifiedWhereObserver(
        IObserver<T> downstreamSubscriber, Func<T, bool> predicate)
    {
        this.downstreamSubscriber = downstreamSubscriber;
        this.predicate = predicate;
    }

    public void OnNext(T value)
    {
        if (this.predicate(value))
        {
            this.downstreamSubscriber.OnNext(value);
        }
    }

    public void OnCompleted()
    {
        this.downstreamSubscriber.OnCompleted();
    }

    public void OnError(Exception x)
    {
        this.downstreamSubscriber.OnCompleted(x);
    }
}
```

它不会采取任何明确的步骤来遵循 `IObserver<T>` 语法。如果它订阅的源也遵守这些规则，它就不需要这样做。由于该操作符只在自己的 `OnNext` 中调用订阅者的 `OnNext`，`OnCompleted` 和 `OnError`，因此只要该操作符订阅的底层源遵守这三个方法的规则，该类也会自动遵守这些规则。

事实上，`System.Reactive` 并不完全信任这一点。它确实有一些代码用于检测语法规范的某些违规行为，但即使是这些措施也仅确保一旦执行进入Rx，就会遵守语法规范。在系统边界处进行了一些检查，但 Rx 的内部在很大程度上依赖于上游数据源遵守规则的事实。

然而，`IObservable<T>` 的语法并不是 Rx 依赖形式主义来确保正确运行的唯一地方。它还依赖于一组特定的数学概念：

- 单子（Monads）
- 卡范畴（Catamorphisms）
- 阿纳范畴（Anamorphisms）

标准的 LINQ 操作符可以完全用这三个概念来表达。

这些概念来自[范畴论](https://en.wikipedia.org/wiki/Category_theory)，范畴论是数学中关于数学结构的一个非常抽象的分支。20 世纪 80 年代末，一些计算机科学家开始探索这一数学领域，希望将其用于模拟程序的行为。可以说是意大利计算机科学家 [Eugenio Moggi](https://en.wikipedia.org/wiki/Eugenio_Moggi)（当时在爱丁堡大学工作）首先认识到单子特别适合描述计算，正如他在1991年的论文[《计算的概念和单子》](https://person.dibris.unige.it/moggi-eugenio/ftp/ic91.pdf)中所解释的那样。这些理论思想被纳入了 Haskell 编程语言中，主要是由 Philip Wadler 和 Simon Peyton Jones 完成的。1992 年，菲利普-瓦德勒（Philip Wadler）和西蒙-佩顿-琼斯（Simon Peyton Jones）发表了关于 [IO 的单子处理](https://www.microsoft.com/en-us/research/wp-content/uploads/1993/01/imperative.pdf)的建议。到 1996 年，Haskell 在其 v1.3 版本中完全采用了这一方法，使程序在处理输入和输出（如处理用户输入或将数据写入文件）时，能够以强大的数学基础为支撑。这被广泛认为是 Haskell 早期尝试在纯函数式语言中模拟杂乱的 IO 现实的重大改进。

这一切的重要性在于什么？这些数学基础正是为什么 LINQ  操作符可以自由组合的原因。

范畴论这门数学学科对各种数学结构有着非常深刻的理解，其中对程序员最有用的一点是，范畴论提供了一些规则，如果遵循这些规则，可以确保软件元素在组合在一起时能够良好地运行。诚然，这只是一个比较肤浅的解释。如果你想详细了解范畴理论如何应用于编程，以及为什么这样做有用，我强烈推荐 Bartosz Milewski 的[《程序员范畴理论》](https://bartoszmilewski.com/2014/10/28/category-theory-for-programmers-the-preface/)。从这本书中获得的大量信息可以清楚地看出，我为什么不打算在本附录中做全面的解释。相反，我的目标只是概述基本概念，并解释它们如何与 Rx 的特性相对应。

## 单子

单子（Monads）是支撑 LINQ（因此也支撑Rx）设计的最重要的数学概念。要能够使用 Rx，并不需要对单子的概念有丝毫了解。最重要的事实是，单子的数学特性（尤其是它们支持组合的特性）使得 Rx 操作符能够自由地组合在一起。从实际的角度来看，真正重要的是它能够正常工作，但如果您已经阅读到这里，那可能不会满足您的好奇心。

通常很难准确描述数学对象的真正含义，因为它们本身就是抽象的。因此，在介绍 monad 的定义之前，了解一下 LINQ 如何使用这一概念可能会有所帮助。LINQ 将 monad 视为项目容器的通用表示法。作为开发人员，我们知道有很多种东西可以包含项。有数组和其他集合类型，如 `IList<T>`。此外还有数据库，虽然数据库表与数组有很多不同之处，但它们也有一些相似之处。支撑 LINQ 的基本观点是，有一种数学抽象可以捕捉到容器共通之处的本质。如果我们确定某个 .NET 类型代表一个单子，那么数学家多年来对单子的特性和行为的研究成果将适用于该 .NET 类型。

例如，`IEnumerable<T>` 是一个单子，`IQueryable<T>` 也是。对于 Rx 来说，最关键的是 `IObservable<T>` 也是一个单子。LINQ 的设计依赖于单子的属性，因此如果您确定某个 .NET 类型是一个单子，那么它就是一个 LINQ 实现的候选对象。（相反，如果您尝试为一个不是单子的类型创建 LINQ 提供程序，可能会遇到问题。）

那么，LINQ 依赖的这些特性是什么呢？第一个特性与包含直接相关：必须能够将某个值放入单子中。你会注意到，到目前为止我举的所有例子都是泛型类型，这绝非巧合：单子本质上是类型构造器，而类型参数指示您希望单子包含的内容的种类。因此，给定某个类型为 `T` 的值，必须能够将其封装在该类型的单子中。给定一个 `int`，我们可以得到一个 `IEnumerable<int>`，如果我们不能做到这一点，那么 `IEnumerable<T>` 就不是单子。第二个特性略微难以确定，但基本上可以归结为：如果我们有可以应用于单个包含项的函数，而且这些函数以有用的方式进行了组合，那么我们就可以创建新的函数，这些函数不仅作用于单个值而且还作用于容器的新函数，而且至关重要的是，这些函数也可以以同样的方式进行组合。

这样，我们就可以像处理单个值一样，自由地处理整个容器。

### 单子操作：return 和 bind

我们刚刚看到，单子不仅仅是一种类型。它们需要提供某些操作。第一种操作，即在单子中封装值的能力，有时在数学文本中被称为“单元（unit）”，但在计算环境中更常被称为“返回(return)”。这就是 `Observable.Return` 名称的由来。

实际上并不一定需要一个真正的函数。只要某种机制可用来将值放入单子中，单子定律就得到满足。例如，与 `Observable` 不同，`Enumerable` 类型没有定义 `Return` 方法，但这并不重要。你只需编写 `new[] { value }`，这就足够了。

单子需要提供另一个操作。数学文献称之为 bind，一些编程系统称之为 `flatMap`，而 LINQ 则称之为 `SelectMany`。这个操作往往会引起最多的困惑，因为虽然它有一个明确的形式定义，但比起 return 来，它的真正作用却更难说清楚。然而，我们通过它们表示容器的能力来看待单子，这为我们理解 bind/`SelectMany` 提供了一种相当直观的方法：它允许我们将每个项都是嵌套容器的容器（例如，数组的数组，或者 `IEnumerable<IEnumerable<T>>`）展开，并将其扁平化。例如，一个列表的列表将变成一个列表，包含来自每个列表的每个项。正如我们很快将看到的，这显然与 bind 的形式数学定义没有直接关系，后者更抽象，但它是与之兼容的，这就足够让我们享受数学家们的劳动成果。

从严格意义上说，要成为一个单子，刚才描述的两个操作（return 和 bind）必须符合某些规则，通常在文献中被称为定律。共有三条法则。这些定律都是关于 bind 操作的工作方式，其中两个定律涉及 return 和 bind 如何相互作用。这些定律是基于单子的操作的组合性的基础。这些定律有些抽象，所以并不明显它们为什么能够实现这一点，但它们是不可置否的。如果您的类型和操作不遵循这些定律，那么您就没有一个单子，因此不能依赖单子所保证的特性。

那么，bind 实际上是什么样的呢？下面是 `IEnumerable<T>` 的绑定过程：

```c#
public static IEnumerable<TResult> SelectMany<TSource, TResult> (
    this IEnumerable<TSource> source,
    Func<TSource,IEnumerable<TResult>> selector);
```

因此，这是一个接收两个输入的函数。第一个输入是 `IEnumerable<TSource>`。第二个输入是一个函数，当提供一个 `TSource` 时，会产生一个 `IEnumerable<TResult>`。使用这两个参数调用 `SelectMany`（即 bind）时，会得到一个 `IEnumerable<TResult>`。虽然 bind 的正式定义要求它具有这个形式，但它并不规定任何特定的行为——只要符合定律的任何行为都是可以接受的。但在 LINQ 的上下文中，我们期望特定的行为：对于源枚举（第一个参数）中的每个 `TSource`，它将调用函数（第二个参数）一次，并收集所有由该函数的所有调用返回的 `IEnumerable<TResult>` 集合产生的 `TResult` 值，将它们作为一个大的 `IEnumerable<TResult>` 返回。在 `IEnumerable<T>` 的特定情况下，我们可以将 `SelectMany` 描述为为每个输入值获取一个输出集合，然后将所有这些输出集合连接起来。

但我们现在的描述有点过于具体了。即使我们专门看 LINQ 使用单子来表示广义容器，`SelectMany` 并不一定意味着连接。它只是要求 `SelectMany` 返回的容器包含函数产生的所有项。连接是一种策略，但 Rx 的做法有所不同。由于可观察对象倾向于在需要时产生值，因此 `Observable.SelectMany` 返回的 `IObservable<TResult>` 只会在函数产生的每个 `TSource` `IObservable<TResult>` 中的任何一个产生值时产生一个值。(它执行了一些同步操作，以确保遵循 Rx 对 `IObserver<T>` 的调用规则，因此如果这些观测值中有一个在调用订阅者的 `OnNext` 时产生了值，它将等待该调用返回后再推送下一个值。除此之外，它会直接推送所有值）。因此，源值在这里基本上是交错的，而不是串联的。但更广泛的原则--结果是一个包含每个输入的回调产生的值的容器

数学上的单子绑定定义具有相同的基本形式，只是没有规定特定的行为。因此，任何单子都将具有一个绑定操作，它接受两个输入：为某个输入值类型（`TSource`）构造的单子类型的实例，以及接受 `TSource` 作为输入并产生为某个输出值类型（`TResult`）构造的单子类型的实例的函数。当你用这两个输入调用绑定时，结果是为输出值类型构造的单子类型的实例。我们无法在 C# 的类型系统中精确表示这个一般概念，但以下代码大致给出了广义的味道：

```c#
// An impressionistic sketch of the general form of a monadic bind
public static M<TResult> SelectMany<TSource, TResult> (
    this M<TSource> source,
    Func<TSource, M<TResult>> selector);
```

将你选择的单体类型（`IObservable<T>`、`IEnumerable<T>`、`IQueryable<T>` 或其他）替换为 `M<T>`，就能知道该特定类型的绑定方式。

但仅仅提供 return 和 bind 这两个函数是不够的。它们不仅必须具有正确的形式，还必须遵守单子定律。

### 单子定律

一个单子由一个类型构造器（例如，`IObservable<T>`）和两个函数 `Return` 和 `SelectMany` 组成（从现在开始，我将使用这些类似 LINQ 的名称）。但是，为了符合单子的条件，这些特征必须遵守三个“定律”（在这里以非常紧凑的形式给出，我将在以下部分进行解释）：

1. `Return` 对于 `SelectMany` 是“左单位元”
2. `Return` 对于 `SelectMany` 是“右单位元”
3. `SelectMany` 在效果上应该是可结合的

让我们对每个定律进行更详细的讨论：

#### 单子定律1：Return 对于 SelectMany 是“左标识”

这个定律意味着如果你将某个值 x 传递给 Return，然后将结果作为 `SelectMany` 的一个输入，另一个输入是一个函数`SomeFunc`，那么结果应该与直接将 x 传递给 `SomeFunc` 相同。例如：

```c#
// Given a function like this:
//  IObservable<bool> SomeFunc(int)
// then these two should be identical.
IObservable<bool> o1 = Observable.Return(42).SelectMany(SomeFunc);
IObservable<bool> o2 = SomeFunc(42);
```

下面是一种非正式的理解方式。`SelectMany` 将输入容器中的每个项都通过 `SomeFunc` 函数进行处理，每次调用将产生一个`IObservable<bool>` 类型的容器，并将所有这些容器收集到一个大的 `IObservable<bool>` 中，其中包含来自各个单独的`IObservable<bool>` 容器的项。但在本例中，我们提供给 `SelectMany` 的输入只包含一个项，这意味着不需要做任何收集工作。`SelectMany` 将使用唯一的输入调用我们的函数一次，这将只产生一个 `IObservable<bool>` 输出。`SelectMany` 必须返回一个 `IObservable<bool>`，该 `IObservable<bool>` 包含从对 `SomeFunc` 的单次调用返回的 `IObservable<bool>` 中的所有内容。在这种情况下，它没有实际的进一步处理工作要做。由于只调用了一次 `SomeFunc`，因此在这种情况下，它不需要合并多个容器中的项：单次调用 `SomeFunc` 生成的单个输出包含应该在 `SelectMany` 返回的容器中的所有内容。因此，我们可以直接使用单个输入项调用 `SomeFunc`。

如果 `SelectMany` 还做其他事情，那就太奇怪了。如果 `o1` 在某些方面与 `o2` 有所不同，这将意味着有以下三种情况之一：

- o1 包含的项目不在 o2 中（这意味着它以某种方式包含了 `SomeFunc` 未生成的项）
- o2 包含的项目不在 o1 中（这意味着 `SelectMany` 忽略了一些由 `SomeFunc` 生成的项）
- o1 和 o2 包含相同的条目，但在使用的单子类型中以某种可检测的方式不同（例如，项的顺序不同）

因此，这个定律本质上规定了 `SelectMany` 不应添加或删除项，也不应破坏单子通常应保留的特性，如顺序。请注意，在 .NET  LINQ 提供程序中，这通常不要求这些对象完全相同。它们通常不会相同。它只意味着它们必须表示完全相同的事物。例如，在本例中，o1 和 o2 都是 `IEnumerable<bool>`，因此它意味着它们应该产生完全相同的 `bool` 值序列。

单体法则 2：Return 是 SelectMany 的 "左标识
该定律的意思是，如果将 Return 作为函数输入传递给 SelectMany，然后将构造的单元类型的某个值作为另一个参数传递进去，那么输出应该是相同的值。例如

#### 单体法则 2：Return 是 SelectMany 的“左标识”

该定律的意思是，如果将 `Return` 作为函数输入传递给 `SelectMany`，然后将构造的单子类型的某个值作为另一个参数传递进去，那么输出应该是相同的值。例如：

```c#
// These two should be identical.
IObservable<int> o1 = GetAnySource();
IObservable<int> o2 = o1.SelectMany(Observable.Return);
```

通过使用 `Return` 作为 `SelectMany` 的函数，我们实质上是要求将输入容器中的每个项目都封装到自己的容器中（`Return` 封装了单个项目），然后再将所有这些容器平铺到一个容器中。我们正在添加一层包装，然后再将其移除，因此这不会产生任何影响。

#### 单体法则 3：SelectMany 是可结合的

假设我们有两个函数 `Tx1` 和 `Tx2`，每个函数都适合作为 `SelectMany` 的参数传递。我们有两种方法可以应用这些函数：

```c#
// These two should be identical.
IObservable<int> o1 = source.SelectMany(x => Tx1(x).SelectMany(Tx2));
IObservable<int> o2 = source.SelectMany(x => Tx1(x)).SelectMany(Tx2);
```

这里的区别只是括号位置的细微变化：变化的只是右侧对 `SelectMany` 的调用是在传递给另一个 `SelectMany` 的函数内部调用，还是在另一个 `SelectMany` 的结果上调用。下一个示例调整了布局，并将 `lambda x => Tx1(x) `替换为完全等价的 `Tx1`，这可能会让人更容易看出结构上的差异：

```c#
IObservable<int> o1 = source
    .SelectMany(x => Tx1(x).SelectMany(Tx2));
IObservable<int> o2 = source
    .SelectMany(Tx1)
    .SelectMany(Tx2);
```

根据第三定律，这两个调用中的任何一个都会产生相同的效果。第二个 `SelectMany` 调用（对于 `Tx2`）是发生在第一个 `SelectMany` 调用之后还是“内部”，并不重要。

一个简单的思考方式是，`SelectMany` 实际上应用了两个操作：转换和解封装。转换由你传递给 `SelectMany` 的函数定义，但由于该函数返回单子类型（在 LINQ 术语中，它返回一个可能包含任意数量项的容器），当它将项传递给函数时，`SelectMany` 会解封装返回的每个容器，以便将所有项收集到最终返回的单个容器中。当你嵌套这种操作时，解封装发生的顺序并不重要。例如，考虑以下函数：

```c#
IObservable<int> Tx1(int i) => Observable.Range(1, i);
IObservable<string> Tx2(int i) => Observable.Return(i.ToString());
```

第一种方法是将一个数字转换成相同长度的数字范围。`1` 变成 `[1]`，`3` 变成 `[1,2,3]`，以此类推。在我们学习 `SelectMany` 之前，请想象一下，如果我们在产生一系列数字的可观测源上使用 `Select`，会发生什么情况：

```c#
IObservable<int> input = Observable.Range(1, 3); // [1,2,3]
IObservable<IObservable<int>> expandTx1 = input.Select(Tx1);
```

`expand1` 实际上就是这样：

```
[
    [1],
    [1,2],
    [1,2,3],
]
```

如果我们使用 `SelectMany`：

```c#
IObservable<int> expandTx1Collect = input.SelectMany(Tx1);
```

会应用相同的转换，但会将结果重新平铺成一个列表：

```c#
[
    1,
    1,2,
    1,2,3,
]
```

我保留了换行符，以强调此输出与前面输出之间的联系，但我也可以直接写成 `[1,1,2,1,2,3]`。

如果我们想应用第二次变换，可以使用选择：

```c#
IObservable<IObservable<string>> expandTx1CollectExpandTx2 = expandTx1Collect
    .SelectMany(Tx1)
    .Select(Tx2);
```

这会将 `expandTx1Collect` 中的每个数字传递给 `Tx2`，后者会将其转换为包含单个字符串的序列：

```
[
    ["1"],
    ["1"],["2"],
    ["1"],["2"],["3"]
]
```

但是，如果我们在最后一个位置也使用 `SelectMany`：

```c#
IObservable<string> expandTx1CollectExpandTx2Collect = expandTx1Collect
    .SelectMany(Tx1)
    .SelectMany(Tx2);
```

它能将这些值展开：

```
[
    "1",
    "1","2",
    "1","2","3"
]
```

根据类似关联的要求，如果我们在传递给第一个 `SelectMany` 的函数中应用 `Tx1`，而不是在第一个 `SelectMany` 的结果中应用 `Tx1`，也没有关系。因此，我们不能以下面的方式开始

```c#
IObservable<IObservable<int>> expandTx1 = input.Select(Tx1);
```

我们可以这样写：

```c#
IObservable<IObservable<IObservable<string>>> expandTx1ExpandTx2 =
    input.Select(x => Tx1(x).Select(Tx2));
```

这样就会产生如下情况：

```
[
    [["1"]],
    [["1"],["2"]],
    [["1"],["2"],["3"]]
]
```

如果我们将嵌套调用改为使用 `SelectMany`：

```c#
IObservable<IObservable<string>> expandTx1ExpandTx2Collect =
    input.Select(x => Tx1(x).SelectMany(Tx2));
```

这将使内部项变得扁平（但我们仍在外部使用 `Select`，因此仍会得到一个列表），从而产生这样的结果：

```
[
    ["1"],
    ["1","2"],
    ["1","2","3"]
]
```

然后，如果我们将第一个 `Select` 改为 `SelectMany`：

```c#
IObservable<string> expandTx1ExpandTx2CollectCollect =
    input.SelectMany(x => Tx1(x).SelectMany(Tx2));
```

它将展开外层的列表：

```
[
    "1",
    "1","2",
    "1","2","3"
]
```

这和我们之前得到的最终结果是一样的，正如第三单体定律所要求的那样。

概括地说，这里的两个过程是

- 扩展并变换 Tx1，展开，扩展并变换 Tx2，展开
- 扩展并变换 Tx1，扩展并变换 Tx2，展开，展开

虽然中间步骤看起来不同，但我们最终得到的结果是一样的，因为不管是在每次变换后解包装，还是在解包前执行两次变换，都没有关系。

#### 为什么这些定律这么重要

这三个定律直接反映了数字上组合直接函数时成立的定律。如果我们有两个函数 $f$ 和 $g$，我们可以写出一个新函数 $h$，定义为 $g(f(x))$。这种组合函数的方法称为_组合_，通常写成 $g \circ f$。如果把同一函数称为 $id$，那么下面的说法都是对的：

* $id \circ f$  等价于 $f$
* $f \circ id$ 等价于 $f$
* $(f \circ g) \circ s$ 等价于 $f \circ (g \circ s)$

这些直接对应于三个单子定律。简单来说，这反映了单子绑定操作（`SelectMany`）与函数组合具有深层结构上的相似性。这就是为什么我们可以自由地组合 LINQ 操作符的原因。

### 使用 SelectMany 重现其他操作符

请注意，在 LINQ 的核心有三个数学概念：单子（monads）、展开（anamorphisms）和折叠（catamorphisms）。因此，尽管前面的讨论集中在了 `SelectMany` 上，但其重要性更加广泛，因为我们可以用这些基本概念来表达其他标准的 LINQ 操作符。例如，下面展示了如何只使用 `Return` 和 `SelectMany` 来实现 [Where](operation-filtering.md#Where) 操作：

```c#
public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
{
    return source.SelectMany(item =>
        predicate(item)
            ? Observable.Return(item)
            : Observable.Empty<T>());
}
```

`Select` 实现：

```c#
public static IObservable<TResult> Select<TSource, TResult>(
    this IObservable<TSource> source, Func<TSource, TResult> f)
{
    return source.SelectMany(item => Observable.Return(f(item)));
}
```

有些操作符需要展开或折叠，我们现在就来看看这些运算符。

## 折叠（Catamorphisms）

catamorphism 本质上是对任何一种将容器中的每个项都考虑在内的处理方式的泛化形式。在 LINQ 的实际应用中，这通常是指检查所有值并产生单个值作为结果的过程，如 [Observable.Sum](operation-aggregation.md#Sum)。更一般地说，任何形式的聚合都构成折叠。数学上对折叠的定义更加通用，例如它不一定要将事物归纳到单个值，但是为了理解 LINQ，以容器为导向的观点是最直接的思考方式。

折叠是 LINQ 的基本构建块之一，因为你不能用其他元素构建折叠。但是，可以使用 LINQ 的最基本的 [Aggregate](operation-aggregation.md#Aggregation) 操作符构建许多 LINQ 操作符。例如，以下是使用 `Aggregate` 来实现 `Count` 的一种方式：

```c#
public static IObservable<int> MyCount<T>(this IObservable<T> items)
    => items.Aggregate(0, (total, _) => total + 1);
```

我们可以这样实现 `Sum`：

```c#
public static IObservable<T> MySum<T>(this IObservable<T> items)
    where T : INumber<T>
    => items.Aggregate(T.Zero, (total, x) => x + total);
```

这个例子比[聚合章节](operation-aggregation.md)中展示的类似的求和例子更加灵活，因为它适用于任何类似数字的类型，这是由 C# 11.0 和 .NET 7.0 中添加的泛型数学功能实现的。但是，操作的基本原则是相同的。

如果你对理论感兴趣，仅仅知道各种聚合操作符都是 `Aggregate` 的特例可能不够。什么是折叠（catamorphism）？其中一个定义是“从一个初始代数到另一个代数的唯一同态映射”，但是通常情况下，这种解释只有在你已经理解它试图描述的概念时才最容易理解。如果你试图用学校数学中的代数形式来理解这一描述，即我们在写方程时用字母来表示一些值，那么你就很难理解这一定义。这是因为折叠采用了更一般的视角来定义“代数”，本质上意味着可以构建和评估某种表达式的系统。

更准确地说，折叠是与称为F-代数(F-algebra)的东西相关联的。它是以下三个元素的组合：

1. 一个函子（Functor）F，它在某个范畴 C 上定义了某种结构。
2. 范畴 C 中的某个对象 A。
3. 从 F A 到 A 的态射，有效地评估结构。

但是这引出了更多问题。所以让我们从一个明显的问题开始：什么是函子？从 LINQ 的角度来看，它本质上是任何实现了 `Select` 的东西（有些编程系统称之为 `fmap`）。从我们以容器为导向的观点来看，它有两个方面：1）一个类似容器的类型构造器（例如 `IEnumerable<T>` 或 `IObservable<T>`）；2）一种将函数应用于容器中的每个元素的方法。因此，如果你有一个将字符串转换为整数的函数，一个函子可以让你一次将该函数应用于它包含的所有元素。

`IEnumerable<T>` 及其 `Select` 扩展方法的组合就是一个函子。你可以使用 `Select` 将 `IEnumerable<string>` 转换为`IEnumerable<int>`。`IObservable<T>` 及其 `Select` 形成另一个函子，我们可以使用它们将 `IObservable<string>` 转换为 `IObservable<int>`。那么关于“在某个范畴 C 上”的部分呢？这暗示了数学上对函子的描述更加广泛。当开发人员使用范畴论时，我们通常使用表示类型（如编程语言中的 `int` 类型）和函数的范畴。严格来说，一个函子将一个范畴映射到另一个范畴，因此在最一般的情况下，一个函子将范畴 C 中的对象和态射映射到范畴 D 中的对象和态射。但是出于编程目的，我们总是使用表示类型的范畴，因此对于我们使用的函子，C 和 D 是相同的。严格来说，这意味着我们应该称它们为 Endofunctors，但似乎没有人在意。实际上，我们使用更一般形式的名称函子（Functor），并且可以默认为我们指的是在类型和函数的范畴上的Endofunctor。

这就是函子部分。我们继续讨论第 2 点，“范畴 C 中的某个对象 A”。C 是函子的范畴，我们刚刚确定该范畴中的对象是类型，所以这里的 A 可能是 `string` 类型。如果我们选择的函子 是 `IObservable<T>` 及其 `Select` 方法的组合，那么 F A 就是 `IObservable<string>`。

关于第 3 点中的“态射”，对于我们的目的来说，我们只是使用类型和函数上的 Endofunctor，因此在这个上下文中，态射只是函数。因此，我们可以将F-代数的定义重新表述为更熟悉的术语：

1. 一些类似容器的泛型类型，比如`IObservable<T>`。
2. 一个项类型A（例如字符串或整数）。
3. 一个接受 `IObservable<A>` 并返回类型为 A 的值的函数（例如 `Observable.Aggregate<A>`）。

这更加具体。范畴论通常关注捕捉数学结构的最一般性质，而这种重新表述丢弃了这种一般性。然而，从一个希望依赖数学理论的程序员的角度来看，这是可以接受的。只要我们所做的符合F-代数的模式，数学家已经推导出的所有通用结果都适用于我们对理论的更专业化应用。

尽管如此，为了让你了解F-代数的一般概念可以实现的各种可能性，函子可以是表示编程语言中表达式的类型，并且可以创建一个评估这些表达式的F-代数。这类似于 LINQ 的 `Aggregate`，它遍历了由函子表示的整个结构（如果是 `IEnumerable<T>`，则是列表中的每个元素；如果是表示一个表达式，则是每个子表达式），并将整个结构减少为单个值，但与我们的函子表示一系列内容不同，它具有更复杂的结构：某种编程语言中的表达式。

所以这就是一个F-代数。从理论的角度来看，第三部分并不一定要进行减少。从理论上讲，类型可以是递归的，项目类型 A 就是 F A。（这对于表达式等固有递归结构很重要）而且通常存在一个最大通用的 F 代数，在这个代数中，3 中的函数（或变形）只处理结构，实际上不执行任何还原。（例如，给定一些表达式语法，你可以想象代码包含了走完表达式的每一个子表达式所需的全部知识，但它对应用什么处理方法并没有特别的看法）折叠的概念是对于同一个函字而言，可用的其他 F-代数的通用性较低。

例如，对于 `IObservable<T>`，通用概念是某个源产生的每个项都可以通过重复应用某个具有两个参数的函数进行处理，其中一个参数是来自容器的类型为 `T` 的值，另一个参数是一种表示到目前为止聚合的所有信息的累加器。这个函数将返回更新后的累加器，准备好与下一个 `T` 一起再次传递到函数中。然后还有更具体的形式，其中应用了特定的累积逻辑（例如求和或确定最大值）。从技术上讲，这里的折叠是从一般形式到更专门形式的连接。但实际上，通常将特定的专门形式（例如 [Sum](operation-aggregation.md#Sum) 或 [Average](operation-aggregation.md#Average)）称为折叠。

## 展开（Anamorphisms）

简单来说，展开（Anamorphisms）是对折叠（Catamorphisms）的反操作。而折叠基本上将某种结构折叠为更简单的形式，z展开则将某个输入扩展为更复杂的结构。例如，对于给定的某个数字（例如 5），我们可以想象一种机制将其转换为具有指定数量元素的序列（例如 [0,1,2,3,4]）。

事实上，我们不必想象这样的事情：这就是 [Observable.Range](creating-observable-sequences.md#Observable.Range) 所做的工作。

我们可以将单子的 `Return` 操作视为非常简单的展开。给定类型为 `T` 的某个值，`Observable.Return` 将其扩展为`IObservable<T>`。展开本质上是这种思想的泛化。

展开的数学定义是“将余代数(coalgebra)分配给其到 Endofunctor 的最终余代数的唯一态射”。这是折叠的“对偶(dual)”定义，从范畴论的角度来看，实际上是指将所有态射的方向反转。在我们不完全通用的范畴论应用中，这里所涉及的态射是在折叠中将项减少为某个输出，因此在展开中，这变成了将某个值扩展到容器类型的某个实例（例如，从 `int` 到 `IObservable<int>`）。

我不会像讲解折叠那样详细地解释展开。相反，我将指出其中的关键部分：对于函字来说，最通用的F-代数体现了对函字基本结构的某种理解，而折叠利用这一点定义了各种减少操作。同样地，对于函字来说，最通用的余代数也体现了对函子基本结构的某种理解，而展开利用这一点定义了各种扩展。

[Observable.Generate](creating-observable-sequences.md#Observable.Generate) 表示这种最通用的能力：它有能力产生一个 `IObservable<T>`，但需要提供一些特定的扩展函数来生成任何特定的 `Observable`。

## 理论到此为止

既然我们已经回顾了 LINQ 背后的理论概念，现在让我们退后一步，看看如何使用它们。我们有三种类型的操作：

- **展开（Anamorphisms）**将值输入序列：`T1 --> IObservable<T2>`
- **绑定（Bind）**修改序列：`IObservable<T1> --> IObservable<T2>`
- **折叠（Catamorphisms）**离开序列。逻辑上是 `IObservable<T1> --> T2`，但实际上通常是 `IObservable<T1> --> IObservable<T2>`，其中输出的 `Observable` 只产生一个值

顺便提一下，绑定和折叠这两个术语在谷歌的 [MapReduce](http://en.wikipedia.org/wiki/MapReduce) 框架中变得很有名。在这里，谷歌将绑定和折叠称为在某些函数式语言中更常用的 Map 和 Reduce。

大多数 Rx 操作符实际上是高阶函数概念的特殊化。举几个例子：

- 展开（Anamorphisms）：
  - [Generate](creating-observable-sequences.md#Observable.Generate)
  - [Range](creating-observable-sequences.md#Observable.Range)
  - [Return](creating-observable-sequences.md#Observable.Return)
- 绑定（Bind）：
  - [SelectMany](transformation-sequences.md#SelectMany)
  - [Select](transformation-sequences.md#Select)
  - [Where](operation-filtering.md#Where)
- 折叠（Catamorphism）：
  - [Aggregate](operation-aggregation.md#Aggregate)
  - [Sum](operation-aggregation.md#Sum)
  - [Min 和 Max](operation-aggregation.md#Min 和 Max)

## Amb

当我开始使用 Rx 时，Amb 方法对我来说是一个新概念。[约翰-麦卡锡（John McCarthy）](https://en.wikipedia.org/wiki/John_McCarthy_(computer_scientist))于 1961 年在《西部联合计算机会议论文集》（Proceedings of the Western Joint Computer Conference）上发表的论文[《计算数学理论的基础》（A Basis for a Mathematical Theory of Computation）](https://www.cambridge.org/core/journals/journal-of-symbolic-logic/article/abs/john-mccarthy-a-basis-for-a-mathematical-theory-of-computation-preliminary-report-proceedings-of-the-western-joint-computer-conference-papers-presented-at-the-joint-ireaieeacm-computer-conference-los-angeles-calif-may-911-1961-western-joint-computer-conference-1961-pp-225238-john-mccarthy-a-basis-for-a-mathematical-theory-of-computation-computer-programming-and-formal-systems-edited-by-p-braffort-and-d-hirschberg-studies-in-logic-and-the-foundations-of-mathematics-northholland-publishing-company-amsterdam1963-pp-3370/D1AD4E0CDB7FBE099B04BB4DAF24AFFA)中首次介绍了这一函数。(这篇论文的电子版很难找到，但后来的版本于 1963 年发表在《计算机编程和格式系统》上）。它是 Ambiguous 一词的缩写。Rx 使用这个缩写略微偏离了正常的 .NET 类库命名规则，部分原因是 Amb 是这个操作符的既定名称，同时也是为了向 McCarthy 致敬，他的作品是 Rx 设计的灵感来源。

Amb（Ambiguous）函数是什么作用呢？一个[模糊（ambiguous）函数](http://www-formal.stanford.edu/jmc/basis1/node7.html)的基本思想是，我们可以定义多种产生结果的方式，其中一些或全部可能在实践中无法产生结果。假设我们定义了一个名为 `equivocate` 的模糊函数，也许对于某个特定的输入值，`equivocate` 的所有组成部分——我们给它计算结果的不同方式——都无法处理该值。（也许每个组成部分都将一个数除以输入值。如果我们提供输入值为 0，那么所有组成部分都无法为该输入产生结果，因为它们都会试图除以 0。）在这些情况下，当`equivocate` 的任何组成部分都无法产生结果时，`equivocate` 本身也无法产生结果。但是，假设我们提供了某个输入，其中恰好有一个组成部分能够产生结果。在这种情况下，该结果成为 `equivocate` 在该输入上的结果。

因此，从本质上讲，我们提供了一系列不同的方法来处理输入，如果其中有一种方法能产生结果，我们就选择这种结果。如果没有一种处理输入的方法能产生任何结果，那么我们的模糊函数也不会产生任何结果。

当模糊函数的多个组成部分都能产生结果时，情况就变得有点奇怪了（这也是 Rx 偏离 Amb 原始定义的地方）。在麦卡锡的理论表述中，模糊函数实际上产生了所有可能的输出结果。(这在技术上被称为非确定性计算，不过这个名称可能会产生误导：它让人觉得结果是不可预测的。但我们在谈论计算时所说的非确定性并不是这个意思。就好像计算机在评估模糊函数时会克隆自己，为每一个可能的结果生成一个副本，并继续执行每一个副本。你可以想象这样一个系统的多线程实现，每当一个模糊函数产生多个可能结果时，我们就会创建这么多新的线程，以便能够评估所有可能的结果。这是非确定性计算的合理心智模型，但 Rx 的 Amb 操作符实际上并非如此）。在引入模糊函数的理论工作中，模糊性最终往往会消失。计算可能有无数种进行方式，但最终都可能产生相同的结果。然而，这种理论上的担忧让我们偏离了 Rx's Amb 的作用，以及我们在实践中如何使用它。

[Rx's Amb](operation-combination.md#Amb) 所描述的情况是，要么没有任何输入产生任何结果，要么正好有一个输入产生任何结果。然而，它并没有尝试支持非确定性计算，所以它对多个成分都能产生价值的情况的处理过于简单，但麦卡锡的《Amb》首先是一个分析性的构造，所以它的任何实际应用都会有不足之处。

## 单子内部

在使用 Rx 时，我们很容易在不同的编程风格之间切换。对于很容易理解 Rx 如何应用的部分，我们自然会使用 Rx。但当事情变得棘手时，改变策略似乎是最简单的做法。最简单的做法可能是等待一个可观察对象，然后继续执行普通的顺序代码。或者，让传给 `Select` 或 `Where` 等操作符的回调除了执行其主要工作外还执行其他操作--让副作用做一些有用的事情，这可能是最简单的做法。

虽然这种方法有时可行，但在不同范式之间切换时应谨慎，因为这是导致并发问题（如死锁和可伸缩性问题）的常见根源。这样做的根本原因是，只要你仍然按照 Rx 的方式做事，你就能从数学基础的基本合理性中获益。但是，要实现这一点，您需要使用函数式风格。函数应该处理其输入，并根据这些输入确定性地产生输出，**它们既不应该依赖于外部状态，也不应该改变外部状态**。这可能是一个很高的要求，而且并不总是可行，但如果打破了这些规则，很多理论就会崩溃。组合不会像它所能做到的那样可靠。因此，使用函数式风格，并将代码保持在 Rx 的习性范围内，往往会提高可靠性。

## 副作用带来的问题

如果程序要做任何有用的事情，总会产生一些副作用--如果程序运行后世界没有任何变化，那还不如不运行它--因此，探讨副作用的问题可能很有用，这样我们就能知道在必要时如何最好地处理副作用。因此，我们现在将讨论在使用可观测序列时引入副作用的后果。如果一个函数除了返回值之外，还具有其他一些可观察到的效果，那么这个函数就被认为具有副作用。一般来说，“可观测效应”是对状态的修改。这种可观察到的影响可能是

- 修改范围大于函数的变量（即全局变量、静态变量或参数）
- I/O，如读取或修改文件、发送或接收网络信息或更新显示屏
- 引起物理活动，如自动售货机分发物品或将硬币投进投币箱

一般来说，函数式编程会尽量避免产生任何副作用。具有副作用的函数，尤其是那些修改状态的函数，要求程序员了解的不仅仅是函数的输入和输出。要完全理解函数的操作，可能需要了解被修改状态的全部历史和上下文。这会大大增加函数的复杂性，使其更难被正确理解和维护。

副作用并不总是有意产生的。减少意外副作用的一个简单方法就是减少变化的表面积。代码编写者可以采取以下两个简单的措施：减少状态的可见性或范围，并使其不可变。可以通过将变量的作用域扩展到方法（而不是字段或属性）等代码块来降低变量的可见性。你可以通过将类成员设置为私有或受保护来降低它们的可见性。根据定义，不可变数据不能被修改，因此不会产生副作用。这些都是合理的封装规则，将极大地提高 Rx 代码的可维护性。

举一个简单的例子来说明具有副作用的查询，我们将尝试通过更新变量（闭包）来输出订阅接收到的元素的索引和值。

```c#
IObservable<char> letters = Observable
    .Range(0, 3)
    .Select(i => (char)(i + 65));

int index = -1;
IObservable<char> result = letters.Select(
    c =>
    {
        index++;
        return c;
    });

result.Subscribe(
    c => Console.WriteLine("Received {0} at index {1}", c, index),
    () => Console.WriteLine("completed"));
```

输出：

```
Received A at index 0
Received B at index 1
Received C at index 2
completed
```

虽然这看起来无伤大雅，但试想一下，如果其他人看到这段代码，并明白这是团队正在使用的模式。反过来，他们自己也会采用这种风格。为了举例说明，我们将在之前的示例中添加一个重复的订阅。

```c#
var letters = Observable.Range(0, 3)
                        .Select(i => (char)(i + 65));

var index = -1;
var result = letters.Select(
    c =>
    {
        index++;
        return c;
    });

result.Subscribe(
    c => Console.WriteLine("Received {0} at index {1}", c, index),
    () => Console.WriteLine("completed"));

result.Subscribe(
    c => Console.WriteLine("Also received {0} at index {1}", c, index),
    () => Console.WriteLine("2nd completed"));
```

输出：

```
Received A at index 0
Received B at index 1
Received C at index 2
completed
Also received A at index 3
Also received B at index 4
Also received C at index 5
2nd completed
```

现在，第二个人的输出显然是无稽之谈。他们期望索引值是 0、1 和 2，但得到的却是 3、4 和 5。我在代码库中见过更邪恶的副作用。令人讨厌的副作用通常会修改布尔值状态，如 `hasValues`、`isStreaming` 等。

除了在现有软件中产生潜在的不可预测的结果外，表现出副作用的程序在测试和维护上也要困难得多。未来对具有副作用的程序进行重构、增强或其他维护时，也更容易出现问题。这一点在异步或并发软件中尤为明显。

## 在管道中组合数据

捕捉状态的首选方式是将其作为构成订阅的 Rx 操作流程的信息流的一部分。理想情况下，我们希望管道的每个部分都是独立和确定的。也就是说，组成管道的每个功能都应该只有输入和输出状态。为了纠正我们的示例，我们可以丰富管道中的数据，使其不存在共享状态。这将是一个很好的例子，我们可以使用公开索引的 `Select` 重载。

```c#
IObservable<int> source = Observable.Range(0, 3);
IObservable<(int Index, char Letter)> result = source.Select(
    (idx, value) => (Index: idx, Letter: (char) (value + 65)));

result.Subscribe(
    x => Console.WriteLine($"Received {x.Letter} at index {x.Index}"),
    () => Console.WriteLine("completed"));

result.Subscribe(
    x => Console.WriteLine($"Also received {x.Letter} at index {x.Index}"),
    () => Console.WriteLine("2nd completed"));
```

输出：

```
Received A at index 0
Received B at index 1
Received C at index 2
completed
Also received A at index 0
Also received B at index 1
Also received C at index 2
2nd completed
```

换位思考一下，我们也可以使用 `Scan` 等其他功能来实现类似的效果。下面就是一个例子。

```c#
var result = source.Scan(
                new
                {
                    Index = -1,
                    Letter = new char()
                },
                (acc, value) => new
                {
                    Index = acc.Index + 1,
                    Letter = (char)(value + 65)
                });
```

这里的关键是隔离状态，减少或消除任何副作用，如状态突变。