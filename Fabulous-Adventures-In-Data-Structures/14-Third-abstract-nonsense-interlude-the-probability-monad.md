# 第三个抽象胡说八道插曲：概率单子

**本章内容包括**

- 离散分布类型是一个加性单子
- 对目前工作的批评

在第一个抽象胡说八道插曲（第7章）中，我们看到了如何通过范畴论的视角来审视泛型类型系统。泛型类型可以被看作是“自函子”：接受并返回类型的函数。在第二个插曲（第11章）中，我们研究了同样受范畴论启发的单子设计模式，并看到诸如可空类型、序列类型、惰性类型和任务类型等不同的泛型类型，都有一种方法可以将“常规”类型的工作流转换为相同泛型类型的工作流。在这最后一个简短的插曲中，我们将再看一个例子：概率分布类型也是一个单子。任何操作普通值的工作流都可以被改造为也能操作随机值。

## 成为单子的要求

在第11章中，我们确定了单子模式的三个关键部分：它是一个增加新能力的泛型类型；它有一个“单位”操作，可以包装任何值；它可以将一个新函数绑定到现有工作流上。概率分布类型满足其中两个要求：

- 我们的新泛型类型 `IDiscreteDistribution<T>` 增加了一个新能力：它可以根据需求生成任意多个随机的 `T` 值，且这些值服从某个特定的概率质量函数。
- 函数 `Singleton<T>.Distribution(T)` 创建了一个包装类的实例，该实例记住单个值。这就是单子的“单位”操作。

我们没有实现一个“绑定”方法，该方法接受一个函数并将其添加到概率工作流中。每个单子类型 `M<T>` 都有一个如下形式的函数：
```csharp
M<R> Bind<T,R>(M<T> X, Func<T, M<R>> func)
```
幸运的是，我们可以用已有的部分来编写这个函数，方法是结合联合分布与边缘化投影：
```csharp
static class Extensions
{
    // ...
    public static IDiscreteDistribution<R> Bind<T, R>(this IDiscreteDistribution<T> prior, Func<T, IDiscreteDistribution<R>> likelihood) =>
        prior.ToJoint(likelihood).Project(joint => joint.Item2);
}
```
概率分布单子上的 `Bind` 操作是将给定的单子值视为先验，将给定的函数视为似然函数。然后我们可以计算联合分布并对其进行边缘化。回忆第13章中关于打喷嚏的似然函数的例子：
```csharp
static IDiscreteDistribution<Sneezed> SneezedGivenCold(Cold c)
{
    List<Sneezed> sneezed = [Sneezed.No, Sneezed.Yes];
    return c == Cold.No ?
        sneezed.ToCategorical(97, 3) :
        sneezed.ToCategorical(14, 86);
}
```
如果我们将该似然函数绑定到一个10%的先验上，并打印出得到的打喷嚏分布……
```csharp
List<Cold> cold = [Cold.No, Cold.Yes];
var prior = cold.ToCategorical(90, 10);
var marginal = prior.Bind(SneezedGivenCold);
Console.WriteLine(marginal.ShowWeights());
```
……那么得到的打喷嚏分布来源于表14.1的右边缘；这就是我们称之为边缘分布的原因：
```
No:887
Yes:113
```

表14.1 根据我们的模型和10%患感冒先验，打喷嚏的分布比例为113:887。

|            | 感冒：是 | 感冒：否 | 合计  |
| ---------- | -------- | -------- | ----- |
| 打喷嚏：是 | 860      | 270      | 1130  |
| 打喷嚏：否 | 140      | 8730     | 8870  |
| 合计       | 1000     | 9000     | 10000 |

在第11章中，我们还指出，“绑定”和“单位”操作的存在意味着“映射”和“连接”操作的存在。每个单子都有一个签名为 `M<R> Map<T, R>(M<T> X, Func<T, R> func)` 的 `Map` 操作。我们已经实现了它；那就是我们的辅助函数 `Project`。每个单子还有一个“连接”操作，用于将“双重包装”的单子展平为单一包装的单子：
```csharp
M<T> Join<T>(M<M<T>> x)
```
如果我们有一个分布，当对它抽样时会生成另一个分布，我们可以通过绑定恒等函数将其展平：
```csharp
public static IDiscreteDistribution<T> Join<T>(this IDiscreteDistribution<IDiscreteDistribution<T>> deep) =>
    deep.Bind(x => x);
```
作为 `Join` 有用性的一个例子，假设我们有以下过程：抛一枚公平硬币，如果是反面，则掷一个公平的六面骰子；如果是正面，则掷一个公平的十面骰子。我们可以将其建模为一个硬币抛掷分布，该分布产生另一个分布，然后将其展平为一个等价的非嵌套整数分布：
```csharp
var d6 = DiscreteUniform.Distribution(6).Project(d => d + 1);
var d10 = DiscreteUniform.Distribution(10).Project(d => d + 1);
List<IDiscreteDistribution<int>> dice = [d6, d10];
IDiscreteDistribution<IDiscreteDistribution<int>> deep = dice.ToCategorical(1, 1);
IDiscreteDistribution<int> shallow = deep.Join();
Console.WriteLine(shallow.ShowWeights());
```
为了“掷一个骰子”，我们会对 `deep` 进行抽样以产生一个整数分布，然后对该分布进行抽样。通过使用 `Join` 将其展平，我们得到了 `shallow`，一个等价的整数分布；这个过程等价于掷一个不公平的十面骰子，其权重如下：
```
1:8  2:8
3:8  4:8
5:8  6:8
7:3  8:3
9:3  10:3
```
对于任何单子，拥有 `Join` 操作的好处是泛型类型永远不会嵌套得太深。
在第11章中，我们还描述了加性单子。拼接是序列单子的“加法”；是否存在某种类似的方法来将两个概率分布“加在一起”？

## 概率分布作为可加单子

在第11章中，我们了解到具有一些额外特性的单子被称为“可加”单子。对于一个可加单子 `M<T>`，我们需要满足：

- 存在一种将两个 `M<T>` 实例“相加”得到第三个实例的方法。
- 存在一个加法单位元值，传统上称为 `MZero`。
- 将 `MZero` 传递给单子的 `Bind` 方法总是产生 `MZero`。

此外，对于任何可加单子，我们可以实现一个函数，该函数接受一个谓词，用于从可加单子中过滤掉某些值：

```csharp
static M<T> Where<T>(M<T> x, Func<T, bool> predicate) =>
    Bind(x, t => predicate(t) ? Unit(t) : MZero);
```

我们看到序列单子是可加的，因为连接是加法的一种形式，空序列是加法单位元，并且在空序列上调用 `SelectMany`（序列单子版本的 `Bind`）总是产生空序列。上面 `Where` 的实现是一种低效的过滤序列的方法（因为它不必要地分配了大量内存），但它的确能正确过滤序列。概率分布单子也是可加的吗？是的！让我们逐一检查各项要求。

我们可以通过合并支持集和权重来“相加”两个离散概率分布（具有相同的底层类型）。如果我们有一个概率分布是从一个罐子中抽取红色和蓝色弹珠，另一个分布是从中抽取红色和黄色弹珠，那么我们可以通过将所有弹珠倒入一个更大的罐子中，然后从中抽取红色、蓝色和黄色弹珠来对这两个分布求和。

我们可以为概率分布单子创建一个 `MZero` 值：空分布是所有事情都不可能发生的分布：

```csharp
public sealed class Empty<T> : IDiscreteDistribution<T>
{
    public static readonly Empty<T> Distribution = new();
    private Empty() { }
    public T Sample() => throw new InvalidOperationException();
    public IEnumerable<T> Support() => Enumerable.Empty<T>();
    public int Weight(T t) => 0;
}
```

如果我们创建了这个分布，那么我们还必须修改分类分布的构造函数，使其在请求所有权重为零的分布时返回空分布：

```csharp
public sealed class Categorical : IDiscreteDistribution<int>
{ [...]
    public static IDiscreteDistribution<int> Distribution(
        params IEnumerable<int> weights) =>
        (weights.All(w => w == 0)) ?
            Empty<int>.Distribution :
            new Categorical(weights.ToList());
}
```

我们还需要修改 `ToJoint` 以考虑总权重为零的分布的存在；总权重的最小公倍数不能包含空分布：

```csharp
public static class Extensions
{ [...]
    public static IDiscreteDistribution<(A, B)> ToJoint<A, B>(
        this IDiscreteDistribution<A> prior,
        Func<A, IDiscreteDistribution<B>> likelihood)
    {
        int lcm = prior.Support()
            .Select(a => likelihood(a).TotalWeight())
            .Where(w => w != 0)
            .LCM();
```

做了这些更改后，将空分布传递给 `Bind` 总是产生空分布：`Bind` 首先创建一个联合分布，而空分布与任何似然函数的联合分布都是空的。

最后，我们可以按照上述标准模式实现 `Where`：

```csharp
public static class Extensions
{
    [...]
    public static IDiscreteDistribution<T> Where<T>(
        this IDiscreteDistribution<T> distribution,
        Func<T, bool> predicate) =>
        distribution.Bind(t => predicate(t) ?
            (IDiscreteDistribution<T>)Singleton<T>.Distribution(t) :
            Empty<T>.Distribution);
}
```

这个 `Where` 的实现只是我们在第12章中开发的 `Filter` 函数的一个效率更低、可读性更差的版本。接口 `IDiscreteDistribution<T>` 满足可加单子的所有要求。

## 一个批判

在过去的几章中，我们取得了实质性的进步，超越了简单地调用 `Random` 类的方法来生成随机数：

- 我们拥有了表示概率分布的对象，并能够使用统计学的工具和术语来操作它们：我们将条件概率表示为似然函数，从中创建联合分布，并对它们进行过滤和投影以计算后验似然。
- 这些工具帮助程序员以强大且原则性的方式推理贝叶斯推断——这一领域常常违反直觉。
- 任何支持集大小为 n 的有限离散分布都可以在 O(n) 时间内构造，并在 O(1) 时间内采样，这要归功于别名算法。
- `IDiscreteDistribution<T>` 是一个单子，因此涉及“普通”量的任何工作流都可以转换为随机量上的相同工作流。

这些都很好！不过也有一些坏消息：

- 选择整数权重而非分数权重虽然简化了别名算法的实现，但也使得联合分布的计算更加困难。在第16章中，我们将针对连续分布使用非归一化的分数权重。
- 构造联合分布时，如果先验支持集大小为 n，似然函数返回的分布大小为 m，则需要 O(mn) 的时间和空间。联合分布可能会变得非常庞大！
- 我们选择通过可能花费大量计算来构造联合分布，以优化采样的渐进成本。
- 我们并未尝试构建适用于无限离散分布的工具。许多实际中的离散分布是无限的。例如，泊松分布用于建模在给定时间间隔内发生一定整数次独立事件的概率。一年中有多少人给客服发邮件、一分钟内有多少铀原子衰变等，都没有明显的上限。

实际上，我在前几章中构建的系统对于支持集较小的分类分布效果良好，这算是一个成就。但公平地说，这仅仅是整个统计问题空间中非常微小的一角。

在本书的最后两章中，我们将探讨两个更难的问题：从连续分布中采样，以及从随机序列分布中采样。我们会发现这两个问题以一种令人惊讶的方式紧密相关：如果我们能从随机序列中采样，我们也能自动化地应用贝叶斯定理来估计连续后验分布。

## 总结

- 离散分布类型满足单子的要求：我们可以围绕单个值创建 `Unit` 包装器，并且可以实现 `Bind`。
- 我们的 `Bind` 实现将传入的单子解释为先验，将传入的函数解释为似然。
- 如果我们愿意，可以通过创建一个空分布（其中所有事件都不可能发生）并对现有代码做一些小修改，使离散分布类型成为可加单子。
- 我们开发的工具相比于使用 `Random` 的方法是一个巨大的进步；我们可以构建能够以原则性、准确的贝叶斯推理来操作有限分类分布的软件。但这只是统计编程世界中的极小一部分。