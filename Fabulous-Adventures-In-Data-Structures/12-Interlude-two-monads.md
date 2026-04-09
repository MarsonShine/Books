# 插曲二：单子

**本章涵盖**

- 什么是单子，它为什么也算“广义抽象胡说”？
- 为什么单子对面向对象程序员有用？
- 什么是 Map 和 Bind？
- 加法性单子与序列运算符之间有什么关系？

在每个编程语言爱好者的人生里，大概都会有那么一段时间：逮到谁愿意听，就想向谁解释单子（monad），而我也不例外！单子其实是一种出奇简单、也极其常见的设计模式，它让我们能够在现有类型之上增加新的能力。你很可能已经愉快地用过单子很多次，只是从未意识到自己正在这么做。理解看似彼此无关的类型背后竟然共享着更深层的数学结构，改变了我构造程序的方式；尤其是，单子很适合用来把程序组织成由一连串操作构成的工作流。

为了理解单子为什么如此强大，我们会看几个例子：把一个对整数做平凡操作——除以二——的函数，应用到那些被赋予某种特殊能力的整数上，例如可空性、惰性、异步性等等。然后，我们会总结出一套机制，把任何接受某种类型的函数转换成等价的单子操作。

## 12.1 这对面向对象程序员有什么价值？

我们在第 7 章已经知道，可以用范畴论的视角来看待泛型类型；它们是类型范畴中的自函子（endofunctor）。如果继续戴着范畴论的眼镜看世界，那么单子就是满足若干额外规则的自函子。但如果换上面向对象程序员的眼镜，我们可以把单子看作一种适用于强大、可组合泛型类型的设计模式。更具体地说，单子是一种泛型类型：它在增加新能力的同时，还允许我们保留既有语义。比如：

- `Nullable<T>` 为任何原本不可为 `null` 的值类型 `T` 增加了“可以为 null”的能力。
  - `int` 不能为 `null`，但 `Nullable<int>` 的取值既可以是任意 `int`，也可以是 `null`。
- `Func<T>` 为任意类型 `T` 增加了“按需计算”的能力。如果你有一个 `string` 变量，就必须事先知道要把什么引用放进去；如果你有一个 `Func<string>`，那么每次需要这个字符串时都可以调用函数再取到它。
- `Lazy<T>` 和 `Func<T>` 很像，但额外提供了结果缓存的能力。
- `Task<T>` 为 `T` 增加了异步计算的能力。
- `IEnumerable<T>` 为 `T` 增加了按需、逐个地产生任意多个值的能力。

你也许已经注意到，其中不少类型看起来像是“同一个主题的不同变奏”。如果眯着眼看，`Func<int>` 和一个表示单元素序列的 `IEnumerable<int>` 有点像：它们都能按需产生一个 `int`。这些类型还能组合：`Func<Nullable<int>>` 有点像一个包含零个或一个元素的序列。你也可以想想 `IObservable<T>` 这样的类型，它结合了序列和 `Task` 的特性。

显然，这类给任意底层类型增加新能力的泛型类型很有用。但到底满足什么设计模式，一个泛型类型才算单子？所有单子都有两个特征：它们是包装类型，而且能与现有函数很好地协作。

第一，对任意单子 `M<T>`，都存在一个传统上称为 `Unit` 的函数，它接受任意 `T` 值并返回 `M<T>`。例如：

```csharp
static Nullable<T> NullableUnit<T>(T t) where T : struct =>
    new Nullable<T>(t);
static Lazy<T> LazyUnit<T>(T t) => new Lazy<T>(() => t);
static Task<T> TaskUnit<T>(T t) => Task.FromResult(t);
static IEnumerable<T> SeqUnit<T>(T t) => new T[1] { t };
```

单子总能用来包装某个具体值。这可以通过一个接受 `T` 的构造函数，或者其他工厂方法来做到；重要的只是必须存在某种方式来创建一个包装后的值。

第二，由接受底层类型的函数调用构成的工作流，可以被转换成接受单子类型的函数调用工作流；这种转换既保留原工作流的含义，又增加单子提供的新能力。

第二点才是真正强大的地方。让我们看看，怎样把底层类型上的操作转换成单子类型上的操作。

## 12.2 把一个数除以二能有多难？

先来看一个直截了当的函数：接受一个整数，转成 `double`，再除以二：

```csharp
static double DivTwo(int x) => (double)x / 2.0;
```

那么，这个函数在可空、惰性、`Task` 和序列版本里分别是什么样呢？可空版本应该在输入为 `null` 时产出 `null`；否则就对底层值执行该操作。在接下来的一小段时间里，我会故意写得啰唆一点，以突出“拆包”发生在什么地方：

```csharp
static Nullable<double> DivTwo(Nullable<int> x)
{
    if (x.HasValue)
    {
        int unwrapped = x.Value;
        double result = DivTwo(unwrapped);
        return new Nullable<double>(result); #A
    }
    else
        return null;
}
```

\#A 这就是 Unit 操作

也许我们可以把同样的模式应用到惰性整数上。仔细看看这段代码；它真的实现了 `Lazy<T>` 的目的吗？

```csharp
static Lazy<double> DivTwo(Lazy<int> x)
{
    int unwrapped = x.Value;
    double result = DivTwo(unwrapped);
    return new Lazy<double>(() => result); #A
}
```

\#A 这里又出现了 Unit

看出这段代码为什么不对了吗？它还不够惰性！拥有一个惰性计算值的目的，就是尽可能推迟计算 `x` 的底层值所付出的代价。下面的实现更好，因为它把可能很昂贵的 `x.Value`，以及调用 `DivTwo(int)` 的那一点小开销，都推迟到了以后：

```csharp
static Lazy<double> DivTwo(Lazy<int> x)
{
    return new Lazy<double>(() =>
    {
        int unwrapped = x.Value;
        double result = DivTwo(unwrapped);
        return result;
    });
}
```

那么 `Task<int>` 呢？这一种特别容易写错。错误的做法，是先同步拆出值，同步完成除法，再同步把结果包回 `Task`。错误示范如下：

```csharp
static Task<double> DivTwo(Task<int> x)
{
    int unwrapped = x.Result;
    double result = DivTwo(unwrapped);
    return Task.FromResult(result); #A
}
```

\#A Unit 又一次出现了

它之所以错，是因为我们没有保留 `Task` 单子所增加的能力；上面的工作流根本没有异步性，而 `Task<T>` 的全部意义恰恰在于它表示异步工作流中的一个值！我们希望保留这种异步性。正确实现会异步等待——`await`——这个值，然后依赖编译器为你生成把结果包装起来的代码：

```csharp
static async Task<double> DivTwo(Task<int> x)
{
    int unwrapped = await x;
    double result = DivTwo(unwrapped);
    return result; #A
}
```

\#A 编译器会为你生成把结果包装成 `Task` 的代码

最后，怎样把一个函数应用到序列中的每个元素上？写一个生成新序列的循环就可以了：

```csharp
static IEnumerable<double> DivTwo(IEnumerable<int> x)
{
    foreach (int unwrapped in x)
    {
        double result = DivTwo(unwrapped);
        yield return result;
    }
}
```

把 `DivTwo` “提升”到不同单子上的方式各不相同，因为究竟何时、怎样拆包，以及返回的单子如何构造，这些精确细节共同决定了单子的语义。

如果为了加入单子赋予的能力，我们必须为每个函数都重写一个新版本，那就太痛苦了。幸运的是，对每个单子我们都可以写一个高阶辅助函数，替我们完成这项工作。

## 12.3 用 Map 和 Bind 推广到任意函数

`DivTwo` 这个函数本身并没有任何特殊之处；同样的逻辑当然也应该能用于任意函数，对吧？如果我们有一个 `Lazy<T>`，以及任意一个从 `T` 到 `R` 的函数，那么我们就能得到一个 `Lazy<R>`。让我们写一个传统上称为 `Map` 的辅助方法，把惰性这种能力应用到任意一个从 `T` 到 `R` 的函数上。（我又会开始用更简洁的写法了。）

```csharp
static Lazy<R> Map<T, R>(Lazy<T> x, Func<T, R> func) =>
    new Lazy<R>(() => func(x.Value));
```

现在，如果我们有任意一个 `Lazy<T>` 实例 `x`，以及一个现成的从 `T` 到 `R` 的函数 `func`，就可以用 `Map` 生成一个 `Lazy<R>`。这个 `Lazy<R>` 既保留了 `func` 原本的行为，又拥有惰性单子带来的新能力。

我们也可以通过上一节同样的方式，为 `Nullable<T>`、`Task<T>` 和 `IEnumerable<T>` 写出对应的 `Map` 函数；我就不再赘述了。不过这里还有一个小问题。假设我们有这样一个函数：

```csharp
static Lazy<double> Weird(int x) { … whatever … }
```

也就是说，我们有一个奇怪的函数：它接受一个整数，返回一个惰性的 `double`，而不是直接返回 `double`。这种函数签名完全合法，因此我们理应也能把它传给 `Map`，并搭配一个 `Lazy<int>` 使用：

```csharp
Lazy<int> x = new Lazy<int>(() => SomethingExpensive());
Lazy<Lazy<double>> result = Map(x, Weird);
```

我们的辅助函数返回了一个 `Lazy<Lazy<double>>`，这似乎懒得过头了。我们真的需要这么多层惰性吗？如果我们也为可空、`Task` 和序列单子写出类似的 `Map`，那么就会出现对应的情况：得到一个 `Nullable<Nullable<R>>`、一个 `Task<Task<R>>`，以及一个 `IEnumerable<IEnumerable<R>>`。

第一种在 C# 里甚至都不合法；类型系统不允许你这样嵌套可空类型，因为一个东西要么是 `null`，要么不是，没有必要再给一个已经可空的东西“增加可空能力”。同理，也没有必要给一个已经异步的东西增加更多异步性，或者给一个已经惰性的东西增加更多惰性。最后一种至少还有点意义——序列的序列本来就很有用——但总感觉哪里不太对。我们可以做得更好。

幸运的是，我们可以通过创建一个修改版的 `Map` 辅助函数来修复这个缺陷。它传统上称为 `Bind`，因为可以把它理解为把一个新函数“绑定”到现有工作流上。下面是四种单子的 `Bind` 方法：

```csharp
static Nullable<R> Bind<T, R>(Nullable<T> x, Func<T, Nullable<R>> func)
    where T : struct
    where R : struct =>
    x.HasValue ? func(x.Value) : null;
static Lazy<R> Bind<T, R>(Lazy<T> x, Func<T, Lazy<R>> func) =>
    new Lazy<R>(() => func(x.Value).Value);
static async Task<R> Bind<T, R>(Task<T> x, Func<T, Task<R>> func) =>
    await func(await x);
static IEnumerable<R> Bind<T, R>(
    IEnumerable<T> x,
    Func<T, IEnumerable<R>> func)
{
    foreach (T t in x)
        foreach (R r in func(t))
            yield return r;
}
```

单子之所以能带来巨大好处，靠的就是 `Bind` 和 `Map`。如果你有一个工作流，其中的函数接受“普通”值，并返回“普通”值或“单子”值，那么你就可以用 `Bind` 或 `Map` 把它转换成一个接受单子值的工作流。新的工作流拥有单子赋予的新能力，同时保留每个函数原本的行为。一个处理“普通”值的工作流，可以被转换成一个行为等价、但处理可空值、惰性值、异步值、序列值等等的工作流。

总结一下：

- 单子是一种泛型类型 `M<T>`，它在底层类型 `T` 之上叠加一种新能力，例如可空性、异步性、惰性、序列等等。
- 单子总能充当单个值的包装壳。必须存在一个 `Unit` 函数：某个工厂方法或构造函数，接受任意一个 `T` 实例，并得到包装实例 `M<T>`。
- 存在一个 `Bind` 函数，它接受一个 `M<T>` 和一个 `Func<T, M<R>>`，并返回一个 `M<R>`，同时保留单子和函数二者的语义。

`Bind` 和 `Unit` 就够了。只要有它们，我们就可以按下面的模式实现 `Map`：

```csharp
static M<R> Map<T, R>(M<T> x, Func<T, R> func) =>
    Bind(x, t => Unit(func(t)))
```

此外，如果我们真的得到了一个不想要的 `M<M<T>>`，那么只要实现一个传统上称为 `Join` 的函数，就能把它变成等价的 `M<T>`。我们只需把一个恒等函数绑定到那个多余的 `M<M<T>>` 上，就会得到一个 `M<T>`：

```csharp
static M<T> Join<T>(M<M<T>> mmt) => Bind(mmt, x => x)
```

我们再次看到了为什么范畴论会被叫作“广义抽象胡说”；我们已经说明了单子必须具备哪些特征，但在纯抽象层面上，很难一眼看出这些特征为什么有用。让我们再具体一点，更仔细地看看一种常见单子。

## 12.4 序列单子是一种加法性单子

单子之所以显得神秘，其中一个原因是：你可能已经写过大量使用序列的代码，却从来没有把它们当作单子来思考。线索就藏在我们上面推导出来的辅助函数名字 `Map` 里——你可能在 Python、Java 或许多其他语言里见过它。在 C# 里，这个操作叫作 `Select`，这样对于熟悉 SQL 的程序员来说，它的用途就一目了然了。下面是一个实现；注意，它只是我们先前推导出的 `Map`，只是换了个名字：

```csharp
static class Extensions
{
    public static IEnumerable<R> Select<T, R>(
        this IEnumerable<T> items, Func<T, R> projection)
    {
        foreach (T t in items)
            yield return projection(t);
    }
```

那么 `Bind` 呢？在 Python 和 Java 里，`Bind` 叫作 `flatMap`；在 C# 里，它叫作 `SelectMany`，而且有多个版本。其中一个和我们上面推导出的 `Bind` 一样，另一个则额外在结尾加上一个投影，作为便利版本：

```csharp
public static IEnumerable<R> SelectMany<T, R>(
    this IEnumerable<T> items,
    Func<T, IEnumerable<R>> func)
{
    foreach (T t in items)
        foreach (R r in func(t))
            yield return r;
}
public static IEnumerable<R> SelectMany<T, U, R>(
    this IEnumerable<T> items,
    Func<T, IEnumerable<U>> func,
    Func<T, U, R> projection)
{
    foreach (T t in items)
        foreach (U u in func(t))
            yield return projection(t, u);
}
```

在其他语言里它之所以叫 `flatMap`，是因为我们从一个序列出发，为原序列中的每个元素得到一个新序列，然后并不产生“序列的序列”，而是通过把所有新序列拼接起来，将整个结果“压平”。C# 的设计者更偏好数据库术语，因此给这些方法取名为 `Select`、`Where` 和 `Join`；`Bind` 这个名字并不会让人自然联想到序列，而 `flatMap` 这个名字也不会让人自然联想到数据库表。

你大概还用过另一个基本的序列操作：在 C# 里叫 `Where`，在 Java 和 Python 里常叫 `filter`，等等。它的作用是对序列中的每个元素应用一个谓词——也就是返回布尔值的函数——然后丢弃那些结果为假的元素：

```csharp
public static IEnumerable<T> Where<T>(
    this IEnumerable<T> items, Func<T, bool> predicate)
{
    foreach (T t in items)
        if (predicate(t))
            yield return t;
}
}
```

这是实现这个序列操作最容易、也最合理的方法，但还有另一种方式，它只用到了我们知道每个单子都必须具备的两样东西：`Unit` 和 `Bind`。

我们已经看到，序列的 `Unit` 就是创建一个只含单个元素的数组。我们也知道，`Bind` 是序列上的“压平”操作：它会得到一批序列，并把它们全部拼接起来。根据一个谓词去过滤序列，在逻辑上等价于根据这个谓词把一堆零项或一项的序列拼接起来：

```csharp
public static IEnumerable<T> Where<T>(
    this IEnumerable<T> items, Func<T, bool> predicate) =>
    items.SelectMany(t => predicate(t) ? #A
        new T[] { t } : #B
        new T[0]); #C
```

\#A SelectMany 就是 Bind
\#B 这里有 Unit
\#C 空序列

这个实现低效得离谱，因为它分配了大量数组，但它确实能工作。序列单子是一种加法性单子（additive monad）；加法性单子满足以下规则：

- 两个加法性单子的实例可以通过某种方式“加”在一起。以序列单子为例，它的 `Bind` 会把多个序列拼接起来，我们可以把这看作一种“加法”。
- 这种单子存在一个传统上称为 `MZero` 的值，它是该加法运算的单位元。数字加零不变，因此零是数字加法的单位元。把空序列与任意其他序列拼接，都会得到原来的那个序列，所以空序列就是序列单子的加法单位元。
- 对 `MZero` 做任何 `Bind`（等价地说，做任何 `Select`、`SelectMany`、`Where` 等操作），结果都会得到 `MZero`。例如，对空序列做 `Select`，只能得到空序列。

我们可以沿着这个模式，为任意加法性单子构造出一个 `Where` 操作：

```csharp
static M<T> Where<T>(M<T> x, Func<T, bool> predicate) =>
    Bind(x, t => predicate(t) ? Unit(t) : MZero);
```

我们在第 6 章看到，`SelectMany` 可以用来实现序列上的笛卡儿积，这可以看成序列的一种“乘法”。不难想到，任何东西乘以零都得零：空序列与任意其他序列的笛卡儿积仍然是空序列。这给了我们一个洞见：可以把 `Bind` 看成有点像单子的乘法。我们会在本书最后一部分看到这个洞见有多有用，也会看到 `Where` 操作一个令人惊讶的应用。

## 12.5 总结

- “单子”这个名字听上去很吓人，但它代表的是一种简单而强大的设计模式：可以用泛型类型为现有类型增加新能力，同时仔细保留那些作用于底层类型的现有函数的语义。
- 单子的基本操作叫作 `Unit` 和 `Bind`。`Unit` 会围绕任意值构造一个单子包装对象；`Bind` 则把一个返回单子的函数“绑定”到一个单子值上。
- 一旦有了 `Unit` 和 `Bind`，我们就可以推导出 `Map` 和 `Join`：`Map` 把一个不返回单子的函数绑定到单子值上；`Join` 则把深度嵌套的单子类型“去掉一层嵌套”。
- `SelectMany` 是序列单子的 `Bind`；`Select` 是它的 `Map`。
- 加法性单子拥有一个 `MZero` 值。把任何东西绑定到 `MZero` 上，结果仍然是 `MZero`。对序列单子来说，空序列就是 `MZero`。
- 加法性单子可以拥有一个 `Where` 方法：它把 `Unit` 单子和 `MZero` 单子“加”在一起。我们将在下一部分看到一个例子。
