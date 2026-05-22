# 马尔可夫过程与Metropolis 算法

**本章内容**

- 马尔可夫过程会生成一个随机的状态序列
- 随机马尔可夫文本
- 使用 Metropolis 算法对后验分布进行采样

在这段关于如何在程序中以原则性技术使用随机性的长篇引论中，最后一步是能够从任意连续分布中采样。特别是，我们希望从一个对世界建模的先验分布开始，用真实世界的观测对该分布进行条件化，从而推导出一个能够正确更新模型的后验分布。面对不确定世界做决策的程序应该使用有原则、数学上可靠的算法；这些工具能帮助我们做到这一点。

令人惊讶的是，语言模型用来生成随机文本的马尔可夫过程，也是用于从连续后验分布中采样的 Metropolis 算法的基础。让我们从总体上了解马尔可夫过程，开启这最后一次精彩的冒险，然后再看看如何使用它们对连续分布进行采样。

## 什么是马尔可夫过程？

我们先看几个例子：

- 你在一座陌生城市的十字路口散步。你随机选择一个方向前进，一直走到另一个十字路口。然后你随机选择停下来，或者选择一个方向继续走。
- 你是赌场里的一名赌徒，口袋里有随机数量的一美元筹码。只要你还有筹码，你就选择要么在轮盘赌桌上随机下注其中一部分，要么退出并回家。
- 你是一位亿万富翁，试图说服投资者相信你发明了人工智能。你让工程师编写一个自动补全算法，生成一个随机的单词序列，并希望结果看起来合理。

这些都是**马尔可夫过程(Markov processes)**，它们以俄罗斯数学家 Andrey Markov 的名字命名，他在现代概率论中做出了开创性工作。上述每个例子都有以下共同点：

- 我们从一个随机选择的“初始状态”开始。状态可以是任何东西——空间中的一个位置、一笔钱、一个单词。状态可以是离散的，比如整数数量的筹码，也可以是连续的，比如一个实数。
- 我们要么终止状态序列，要么通过基于当前状态随机选择一个新状态来进行转移。可选的新状态只取决于当前状态。注意，并不要求每个序列最终都会停止；有限和无限的马尔可夫过程都很常见。
- 一个马尔可夫过程的样本是一个状态序列。

让我们为一个通用的马尔可夫分布编写代码。我们的策略是：

- 由于“状态”可以是任何东西，我们可以把它做成一个泛型参数 T。

- 马尔可夫过程是 T 的序列的分布。

- 初始状态从某个 T 的初始分布中采样得到。

- 转移函数接受当前状态，并返回一个可能的新状态的分布。在第 13 章中，我们把这些称为似然函数(likelihood functions)。

  如果它返回第 14 章中的空分布，则该序列终止。

清单 16.1 一个通用的马尔可夫过程是序列的分布

```csharp
sealed class Markov<T> : IDistribution<IEnumerable<T>>
{
    private readonly IDistribution<T> initial;
    private readonly Func<T, IDistribution<T>> transition;
 
    public static Markov<T> Distribution(
        IDistribution<T> initial,
        Func<T, IDistribution<T>> transition) =>
        new Markov<T>(initial, transition);
 
    private Markov(
      IDistribution<T> initial,
      Func<T, IDistribution<T>> transition)
    {
        this.initial = initial;
        this.transition = transition;
    }
    public IEnumerable<T> Sample()
    {
        var distribution = initial;
        while (distribution is not Empty<T>)
        {
            var state = distribution.Sample();
            yield return state;
            distribution = transition(state);
        }
    }
}
```

我们可以用一个名为“赌徒破产”的模拟来测试它——之所以这样称呼，是因为很多时候，赌徒会身无分文地回家。这里的“状态”是你剩余的筹码数量。在这个版本的场景中，如果你有 0 个或 10 个筹码，你就回家。否则，你掷一枚硬币，新的状态要么是少一个筹码（如果是反面），要么是多一个筹码（如果是正面）。转移函数如下：

```csharp
IDistribution<int> GamblersTransition(int pocket) =>
    (pocket == 0 || pocket == 10) ?
        Empty<int>.Distribution :
        DiscreteUniform.Distribution(2)
            .Project(x => x == 0 ? pocket - 1 : pocket + 1);
```

假设你一开始口袋里有 4 到 7 个之间的随机数量的筹码；这就是我们的初始分布。然后我们可以对筹码数量序列进行采样：

```csharp
var initial = DiscreteUniform.Distribution(4).Project(x => x + 4);
var gamblersRuin = Markov<int>.Distribution(initial, GamblersTransition);
Console.WriteLine(gamblersRuin.Sample().Comma());
```

我们会得到一个合理的筹码数量序列：

```text
5,4,3,4,5,6,5,4,3,2,1,2,1,2,1,0
```

在实践中，所有赌徒破产样本都是有限序列；你最终总会回家，但在途中可能会掷很多次硬币。让我们看看 100000 个随机序列长度的直方图：

```csharp
Console.WriteLine (gamblersRuin.Samples().Take (100000).Select(x => x.Count()).Histogram());
```

前几行显示，通常我们会在大约八九次投掷之后回家：

```
4|  3233|********************************
  5|  3141|********************************
  6|  3885|***************************************
  7|  3867|***************************************
  8|  3903|***************************************
  9|  3921|****************************************
 10|  3658|*************************************
 11|  3758|**************************************
 12|  3324|*********************************
 13|  3331|*********************************
 14|  3048|*******************************
 15|  3051|*******************************
 16|  2733|***************************
 17|  2732|***************************
 18|  2609|**************************
 19|  2482|*************************
 20|  2248|**********************
 ...
```

我把它截断了，因为尾部非常长；有一个样本包含 287 次投掷！

马尔可夫过程在商业和科学中都有大量应用。

- 悬浮在液体中的粒子的随机“布朗”运动是一个马尔可夫过程：粒子的下一个位置是相对于当前位置的一个随机偏移。
- DNA 突变是一个马尔可夫过程：受到辐射的 DNA 分子中的新碱基序列，是对其当前碱基序列的一次随机改变。
- 任何工作队列都可以建模为一个马尔可夫过程：状态是队列中的任务数量、正在执行任务的工作者数量，以及发出请求的客户端数量。转移函数会根据工作者数量随机移除任务，并根据客户端数量随机添加任务。你还可以对客户端到达或放弃、工作者随机离开进行建模——可能是由于硬件故障，也可能是有人请病假。根据一个对真实工作队列建模的马尔可夫过程生成许多序列，是一种快速发现关键信息的好方法：队列可能变得多长，平均客户端要等待多久才能完成任务，等等。

我相信你能想到很多其他应用：明天的天气是从今天的天气随机变化而来的，一秒后的股票价格是从当前价格随机变化而来的，等等。马尔可夫过程无处不在！有一个用例在我年轻时了解它时只是一个迷人的奇闻，但如今已经变得极其重要：马尔可夫文本生成。我们将稍微绕个弯看看文本生成，然后再看看如何使用马尔可夫过程从后验分布中采样。

## 马尔可夫文本

马尔可夫过程中的“状态”可以是任何东西；如果状态是一个单词，那么该过程就会生成单词序列。一个简单的“随机莎士比亚”生成器就是一个很好的例子。该算法在高层次上是：

- 获取莎士比亚全集的文本文件。把它拆分成一个句子序列，其中每个句子都是一个单词序列。
- 该过程的初始分布是所有句子中首词的分布。
- 转移函数接受一个单词，并生成紧随其后的单词的分布。当给定的是句子中的最后一个单词时，转移函数返回一个空分布。
- 对得到的马尔可夫过程进行采样会生成随机的新句子。

有许多复杂的方法可以把文本解析成单词和句子；为了我们的目的，我们会使用一种简单的方法。我创建了一个扩展方法，用空格拆分每一行，然后从每个单词中修剪掉不需要的标点符号——但句号、感叹号和问号除外，我们会保留它们，用作句子终止符：

清单 16.2 一个简单的单词和句子解析器

```c#
public static class Extensions
{
    static readonly char[] punct = "*()[]#:;_\"".ToCharArray();
    public static IEnumerable<List<string>> Sentences(
        this IEnumerable<string> lines)
    {
        List<string> current = [];
        foreach (var line in lines)
        {
            foreach (var word in line.Split())
            {
                var trimmed = word.Trim(punct);
                if (trimmed.Length > 0)
                {
                    current.Add(trimmed);
                    char c = trimmed[trimmed.Length - 1];
                    if (c == '.' || c == '!' || c == '?')
                    { 
                        yield return current;
                        current = [];
                    }
                }
            }
        }
    }
    public static string Space<T>(this IEnumerable<T> ts) => 
        string.Join(" ", ts);
    [...]
}
```

我们可以从中抓取几个句子：

```c#
var sentences = File.ReadLines("shakespeare.txt").Sentences();
foreach(var sentence in sentences.Skip(106).Take(6))
    Console.WriteLine(sentence.Space());
```

果然，我们得到了几行来自《皆大欢喜》的句子：

```
BERTRAM.
What is it, my good lord, the king languishes of?
LAFEW.
A fistula, my lord.
BERTRAM.
I heard not of it before.
```

现在我们有了一个句子集合，就可以用它来构建初始单词的分布和转移函数。我们将创建一个可变的分类分布构建器，它会统计某个项被添加到构建器中的次数，并把这些计数用作权重：

清单 16.3 一个用于分类分布的可变构建器

```c#
sealed class CategoricalBuilder<T> where T : notnull
{
    private Dictionary<T, int> weights = [];
    public void Add(T t)
    {
        weights[t] = weights.GetValueOrDefault(t) + 1;
    }
    public IDistribution<T> ToDistribution()
    {
        var keys = weights.Keys.ToList();
        return keys.ToCategorical(keys.Select(k => weights[k]));
    }
}
```

然后我们可以对转移函数做同样的事情：每个状态都有自己的分类分布构建器。这样，我们就可以逐步构建稍后将由似然函数返回的分布：

清单 16.4 一个用于马尔可夫过程的可变构建器

```c#
sealed class MarkovBuilder<T> where T : notnull
{
    private CategoricalBuilder<T> initial = new();
    private Dictionary<T, CategoricalBuilder<T>> transitions = new();
    public void AddInitial(T t)
    {
        initial.Add(t);
    }
    public void AddTransition(T t1, T t2)
    {
        if (!transitions.ContainsKey(t1))
            transitions[t1] = new();
        transitions[t1].Add(t2);
    }
    public Markov<T> ToDistribution()
    {
        var init = initial.ToDistribution();
        var trans = transitions.ToDictionary(
          kv => kv.Key,
          kv => kv.Value.ToDistribution()); #A
        return Markov<T>.Distribution(init,
          k => trans.GetValueOrDefault(k, Empty<T>.Distribution));
    }
}
```

\#A 将所有分布构建器转换为分布

现在我们可以把句子序列转换成一个马尔可夫过程，并从中生成一些句子：

```c#
var mb = new MarkovBuilder<string>();
foreach (var sentence in sentences)
{
    mb.AddInitial(sentence[0]);
    for (int i = 0; i < sentence.Count - 1; i += 1)
        mb.AddTransition(sentence[i], sentence[i + 1]);
}
var markov = mb.ToDistribution();
Console.WriteLine(markov.Samples().Take(8)
    .Select(x => x.Space()).Newlines());
```

我添加了几个换行，让它更容易阅读；结果明显具有莎士比亚风格，但当然没有任何意义：

```
PAGE.
Bear free itself, but not from his gleeks?
Pardon me, good man of courtesy, for some 
sailor's wife hear it in so treacherous feet 
were she was to break our present recompense.
Tongues spit all this noble Antony.
A public ear of what dignity, 
Like sweet society, True honest kindness.
 
LYSANDER.
Now I bid my good soldier, and then, our fortunes 
To let me from him, and moulded on by interchangement 
of the black mantle, till my offences done?
Well, now.
```

我们用几十行代码（外加几十万行莎士比亚文本）构建了一个非常小的语言模型。大型语言模型（LLM）可以以你选择的任何风格生成更可信的随机文本；它们是如何做得好得多的呢？下面是一些技术，大致按从最便宜到最昂贵的顺序排列：

- 初始的单词和句子解析器并不复杂。标点字符可以被单独分词，而不是总是附着在单词上。我们没有考虑莎士比亚不寻常的大小写用法。还有许多其他方法可以改进解析器。
- 在莎士比亚文本中，跟在 “the” 后面的不同单词有 8254 个：“the King” 出现了 273 次，但有 5000 多个单词只在 “the” 后面出现一次，比如 “the puddle”。这个过程选择 “the” 后面 5000 个不常见单词之一的可能性，是选择 “King” 的 18 倍。我们可以人为增加 “the” 后面常见单词的权重，让它们更频繁地被选中。
- 马尔可夫过程的一个特点是它没有“记忆”；下一个状态是根据当前状态随机生成的，而不是根据任何先前状态生成的。这可能导致句子不可信，因为句子的结尾与开头之间的联系很弱。一个常见的修正方法是让更多单词成为状态的一部分。我们可以用两个或三个单词的组合来构建转移函数，而不是用单个单词。例如，我们的转移函数构建器记录了所有跟在 “of” 后面的单词的分布，但如果记录所有跟在 “of France”、“of the”、“of his” 等之后的单词的分布，我们就能做得更好。当最后生成的两个单词是 “of his” 时，我们就可以优先从跟在 “of his” 后面的单词分布中选择下一个单词，而不是从跟在 “his” 后面的更多样化的单词分布中选择。
- 许多 LLM 将前一种技术推向极致：在随机生成下一个单词时，它们把到目前为止整个会话文本都用作“状态”。随着文本变长，重新分析文本以预测下一个单词的开销也会变大。生成文档的成本可能与其长度呈二次方关系；这是一个相当昂贵的解决方案。
- 莎士比亚全集只有几十万行，而且属于公有领域；这并不是很多可用文本。不道德的 LLM 公司会未经所有者许可、也不给予补偿地使用数百万部受版权保护作品的盗版副本。这在计算上代价高昂；如果你被起诉，在财务上也代价高昂。
- LLM 对原始文本所做的分析，不只是记录单词的分布。例如，如果 “he”、“him” 和 “his” 这些词经常出现在包含 “doctor” 的句子中，而 “she”、“her” 和 “hers” 经常出现在 “nurse” 附近，那么语义模型就可以推断出源文本中存在的性别歧视偏见，并把这些偏见传递到新生成的文本中。当原始文本语料库很大时，这种自动化分析在计算上非常昂贵。
- 所有这些“无监督”分析通常仍然不足以生成可信的随机文本。LLM 公司雇用一小支人类大军来提供“监督式”输入，以修正模型。你可以通过雇用经济欠发达国家、劳动保护薄弱地区的人来降低成本。

即使有了所有这些相对于我的小型语言模型的“改进”，小型和大型语言模型中这个过程的基本马尔可夫性质仍然相同：基于先前生成的单词，随机生成下一个单词。马尔可夫过程并没有一个它想要表达的想法，然后计划如何表达它。它不会在句子写到一半时意识到前面选错了词，并认为最好回溯并修正它。它只是一个随机单词接着一个随机单词，直到转移函数表示输出完成为止。

不过，小型和大型语言模型最重要的共同点，是意图所在的位置。我的意图是让我的简单模型随机生成看起来像莎士比亚风格的句子。LLM 的开发者意图让他们的复杂模型随机生成对给定提示词的可信回应。想要让读者误以为某个过程的输出具有某种东西的愿望，属于模型的作者，而不属于随机文本生成算法。一些模型作者愿意在随机单词生成算法上花费数十亿美元，以欺骗读者相信文本是有意图的。

相比之下，当我创作一段由人类生成的文本时，我的意图是真正传达一个想法。你永远无法从马尔可夫过程中得到这一点。

让我们结束这段题外话，来看马尔可夫过程一个真正有用、强大且令人惊讶的应用：我们可以使用它们从任意连续分布中采样。这使我们能够为世界建立一个先验统计模型，输入对世界的观测，并获得一个更准确的后验模型。

## 计算连续分布的后验

在第 13 章中，我们看到贝叶斯定理将原因 “A” 和结果 “B” 的概率联系起来：

P(A|B) = P(A) P(B|A) / P(B)

如果 “A” 是“感冒了”，而 “B” 是“打喷嚏了”，那么：

- P(A) 是先验概率：某人感冒的概率。这是新证据到来之前我们所知道的。
- P(B|A) 是在某人感冒的条件下其打喷嚏的似然。这是我们的统计模型，用来把原因和结果联系起来：感冒的人比健康的人更可能打喷嚏。
- P(A|B) 是在观察到某人打喷嚏之后，其感冒的后验概率。我们在代码中把它表示为一个接受 B 并返回 A 的分布的函数。这是我们希望计算的内容。打喷嚏的人比不打喷嚏的人更可能感冒，但确切地说，可能性高多少？
- P(B) 是某人最近打喷嚏的概率。

我们的目标是：在给定先验和似然的情况下，自动创建一个后验——一个接受证据并生成与该证据一致的更好分布的函数。让我们通过尝试编写代码来推导它。在第 13 章中，我们写过一个扩展方法，它接受一个先验和一个似然函数，并返回后验的似然函数。它的签名是：

```c#
public static Func<B, IDiscreteDistribution<A>> Posterior<A, B>(
        this IDiscreteDistribution<A> prior,
        Func<A, IDiscreteDistribution<B>> likelihood) [ … ]
```

我们想做同样的事情，但使用连续先验。我们要用作证据来更新信念的观测值不一定是 double；它可以是任何类型，所以让我们把它做成一个从 double 到任意类型 B 的加权分布的函数。它应该返回一个函数，该函数接受类型为 B 的证据，并生成一个后验连续分布。我们可以勾勒出这个方法必须是什么样子：

清单 16.5 勾勒连续后验函数的实现

```c#

static class Extensions
{
    public static Func<B, IContinuousDistribution> Posterior<B>( #A
        this IContinuousDistribution prior,						 #B
        Func<double, IWeightedDistribution<B>> likelihood)		 #C
    {
        var posteriorFunc = (B evidence) =>
        {
            Func<double, double> posteriorPDF =
            a => something;
            return Miracle.Distribution(posteriorPDF);			 #D
        };
        return posteriorFunc;
    }
}
```

\#A 返回一个接受证据（B）并返回后验分布的函数
\#B 接受一个先验分布
\#C 接受一个对原因和结果建模的似然
\#D 这两行还需要处理

我们仍然需要两样东西：一个计算后验 PDF 的函数，以及某种可以神奇地接受任意 PDF 并生成一个能从该分布中采样的连续分布对象的东西。贝叶斯定理告诉我们如何计算我们需要的第一样东西，因为贝叶斯定理作用于 PDF 的方式与作用于概率的方式相同。在观察到某个证据时，后验的 PDF 是：

$P(A|evidence) = P(A) P(evidence|A) / P(evidence)$

- P(A) 是 prior.Weight(a)
- P(B|A) 是 likelihood(a).Weight(evidence)
- 但我们要除以的 P(evidence) 是什么？

我们没有 P(B) 的 PDF 来计算 P(evidence)。幸运的是，我们并不需要它，只要那个能够从任意 PDF 中采样的神奇算法可以处理非归一化 PDF。对于我们正在观察的任何特定证据，除以 P(evidence) 就是除以一个常数因子，而非归一化 PDF 可以除以或乘以任何常数因子；这只会改变总面积，而不会改变支撑集中任何成员的相对权重。我们已经解决了第一个问题；我们想要的后验 PDF 代码是：

```c#
Func<double, double> posteriorPDF =
            a => prior.Weight(a) * likelihood(a).Weight(evidence);
```

> 注意：我们统计模型的似然函数可以返回一个非归一化分布，但多次调用必须返回权重一致的分布。如果 likelihood(0.0) 返回一个连续分布，其非归一化 PDF 的总面积为 10.0，但 likelihood(1.0) 返回的总面积为 5.0，那么乘以 Weight(observation) 会让后验的 PDF 在 0.0 处过大。

我们仍然需要发生的奇迹，就是本章的探索目标：当我们拥有的只有某个概率分布的非归一化 PDF 时，我们希望创建一个可以从中采样的概率分布。如果我们能做到这一点，那么就能从后验中采样。

第 15 章中的拒绝采样算法要求我们知道后验 PDF 形状的许多细节，而这正是我们一开始想要确定的东西，所以它用处不大。幸运的是，Metropolis 算法解决了这个问题，并且只要求我们知道关于后验形状的少量事实。我们将实现 Metropolis 算法，并使用它从连续后验中采样。

## Metropolis 算法

Metropolis 算法用于在给定一个可能非归一化的 PDF 时，从分布中生成样本；它以 Nicolas Metropolis 的名字命名，他在 20 世纪 50 年代的计算机械化方面做出了开创性工作。它特别适合对多维分布进行采样，但为了简单起见，我会展示一维版本。生成一个样本的算法相当短：

- 如果这是第一个样本，就从一个初始分布中生成一个样本。它成为当前样本。返回它。
- 否则，创建一个以当前样本为中心生成随机数的提议分布。对它进行采样以获得提议样本。
- 将提议样本的权重除以当前样本的权重。
- 掷一枚硬币来决定提议样本是否可接受。也就是说，生成一个标准均匀样本，如果它小于或等于我们刚刚计算出的商，那么当前样本就被提议样本替换。如果不是，则拒绝提议样本并保留当前样本。
- 返回当前样本。

如果提议样本的权重大于当前样本的权重，那么商会大于 1.0。那是一个更好的样本，因此我们总是接受它。如果提议样本权重除以当前样本权重，比如说是 0.1，那么我们只有 10% 的时间接受提议样本；其余 90% 的时间会再次产生当前样本。这避免了拒绝采样算法中的循环问题，后者可能在生成一个样本之前循环任意多次尝试。Metropolis 算法要么产生提议样本，要么退回到再次产生当前样本，而不会循环；它为每个生成的样本执行恒定量的工作。

> 注意：该算法只需要提议权重与当前权重的商；这就是为什么权重函数是否非归一化并不重要。

当我第一次学到这个算法时，我很惊讶这样一个简短、清晰的算法竟然能生成与任意目标分布相匹配的样本。当然，魔鬼藏在细节里；我还没有说明如何确定初始分布或提议分布。我们先实现代码，然后再思考初始分布和提议分布必须具备哪些特征才能让算法工作。

马尔可夫过程通过选择一个初始状态来生成一个随机序列，然后进行一次“随机游走”，每个后续状态都是从前一个状态随机移动得到的。这正是 Metropolis 算法所做的事情：每个后续样本要么与前一个样本相距一个随机距离，要么在提议样本被拒绝时，等于前一个样本。Metropolis 算法生成的样本序列，就是对马尔可夫过程进行采样所生成的状态序列。

我们可以重用前面的 Markov 代码来实现 Metropolis 算法。对于转移函数，我们将生成一个提议，并返回一个“掷硬币”分布，在提议样本和当前样本之间进行选择。下面是一个掷硬币分布：

清单 16.6 一个掷硬币分布

```c#
public sealed record class Flip<T>(T Heads, T Tails, double PHeads):
    IDistribution<T>
{
    public T Sample() => 
        ContinuousUniform.Standard.Sample() <= PHeads ? Heads : Tails;
}
```

为了实现 Metropolis 算法，我们从马尔可夫过程中生成一个序列，并在每次需要样本时从中枚举一个元素：

清单 16.7 Metropolis 算法

```c#
public sealed class Metropolis: IContinuousDistribution
{
    private readonly IEnumerator<double> enumerator;
    private readonly Func<double, double> target;
    public static Metropolis Distribution(
      Func<double, double> target,
      IDistribution<double> initial,
      Func<double, IDistribution<double>> proposal)
    {
        var markov = Markov<double>.Distribution(initial, transition);
        return new Metropolis(target, markov.Sample().GetEnumerator());
 
        IDistribution<double> transition(double current)
        {
            double proposed = proposal(current).Sample();
            return new Flip<double>(proposed, current, 
                target(proposed) / target(current));
        }
    }
    private Metropolis(
        Func<double, double> target, IEnumerator<double> enumerator)
    {
        this.enumerator = enumerator;
        this.target = target;
    }
    public double Sample()
    {
        enumerator.MoveNext();
        return enumerator.Current;
    }
    public double Weight(double d) => target(d);
}
```

让我们重新看一下第 15 章中的例子：

```c#
var target = (double x) =>
    Exp(-x * x) + Exp((1.0 - x) * (x - 1.0) * 10.0);
```

要用 Metropolis 算法对它进行采样，我们还需要两样东西：一个初始分布，以及一个根据当前状态生成提议分布的函数。

初始分布的唯一要求是，来自初始分布的任何样本都必须位于目标分布的支撑集中，但如果初始样本靠近目标 PDF 的“高点”，效果会更好。在这个例子中，目标分布下面的大部分面积位于 -2.0 和 2.0 之间，因此标准正态分布作为初始分布完全可以接受。

提议分布必须以当前样本为中心且无偏。也就是说，它提出一个小于当前样本的值和提出一个大于当前样本的值应当同样可能。我们希望它提出的值与当前样本相距一个“合理”的距离。如果许多提议都“太近”，那么样本会“聚成团”。如果许多提议都“太远”，那么提议往往会落在低概率的尾部，并且大多被丢弃，这会导致当前样本被重复使用。对于这个例子，一个均值为当前样本、标准差为 1.0 的正态分布是一个合适的折中点：它是无偏的，并且会经常产生既不太接近也不太远离当前样本的提议。让我们试试看：

```c#
var target = (double x) =>
    Exp(-x * x) + Exp((1.0 - x) * (x - 1.0) * 10.0);
var initial = Normal.Standard;
var proposal = (double current) => Normal.Distribution(current, 1.0);
var mixture = Metropolis.Distribution(target, initial, proposal);
Console.WriteLine(mixture.Histogram(-2.0, 3.0));
```

在只有目标分布的非归一化 PDF，以及对良好初始分布和提议分布的猜测的情况下，我们可以成功地从目标分布中采样：

```
**
                       **
                      ***
                      ***
               **     ***
               ***   ****
             ****** ******
            **************
            **************
           ***************
          ****************
         ******************
         ******************
        *******************
       ********************
       *********************
      **********************
     ************************
   **************************
**************************************
```

现在我们拥有了前面缺少的、用于从后验分布中采样的工具：有了 Metropolis 算法，只要我们能提出合适的初始分布和提议分布，即使我们只有一个能够计算非归一化 PDF 的函数，也可以从任何分布中采样。

## 使用 Metropolis 对后验进行采样

我们之前尝试编写一个函数，它接受一个先验和一个似然，并返回一个函数，该函数在给定观测证据时生成一个后验分布。我们弄清楚了非归一化权重函数，但在尝试把它转换成一个分布时卡住了：

```c#
return Miracle.Distribution(posteriorPDF);
```

Metropolis 算法让我们摆脱了困境。我们仍然需要两样东西：一个初始分布和一个提议分布。初始分布必须产生一个位于目标分布支撑集中的样本，而后验几乎肯定与先验具有相同的支撑集。我们可以使用先验作为初始分布。在真实世界场景中，后验的形状很可能在某种程度上类似于先验的形状。

为了提出一个提议分布，同样地，我们通常知道一些关于后验分布样本预期范围的信息。我们做这项工作，是为了在观察到证据后量化某个过程中的不确定性。如果我们正在为工厂生产的小部件尺寸建模，我们知道典型的尺寸变化是什么样的，以及这些变化是以微米还是厘米来衡量的。我们可以使用一个以当前样本为中心的正态分布，其中标准差取适合该过程的值。

清单 16.8 完成连续后验采样的实现

```c#
static class Extensions
{
    public static Func<B, IContinuousDistribution> Posterior<B>(
        this IContinuousDistribution prior,
        Func<double, IWeightedDistribution<B>> likelihood,
        double stddev = 1.0)
    {
        return (B evidence) =>
        {
            Func<double, double> posteriorPDF = a =>
                prior.Weight(a) * likelihood(a).Weight(evidence);
            Func<double, IContinuousDistribution> proposal = 
                current => Normal.Distribution(current, stddev);
            return Metropolis.Distribution(posteriorPDF, prior, proposal);
        };
    }
}
```

让我们把所有内容整合起来，用第 13 章末尾那个例子的连续版本来说明。原来的例子是从一个袋子中抽出一枚硬币，袋子里有 999 枚公平硬币和一枚双面都是正面的硬币。抽到双正面硬币的先验概率是 0.1%，但如果我们接着观察到这枚硬币连续十次都是正面，那么后验概率会接近 50%，说明我们拿到的是双正面硬币，这是一次巨大的更新。

与其从袋子里抽硬币，不如假设我们有一家工厂，它生产的硬币具有某种公平性分布；有些接近真正公平，有些有 75% 的概率出现正面，等等。我们可以使用本章前面提到的截断正态分布，来建模从这家工厂中选择一枚具有特定公平性的硬币：

```c#
var prior = Normal.Distribution(0.5, 0.2).Clamp(0.0, 1.0);
```

这是一个关于我们在观察到硬币行为的证据之前，对硬币公平性的信念强度的模型。现在假设我们把这家工厂生产的一枚硬币掷十次，并观察十次中有多少次出现正面。这可以由二项分布来建模：我们把一枚不公平硬币掷若干次“试验”，并统计其中有多少次“成功”。权重函数通过把每次成功或失败的概率相乘，然后再乘以这些结果可能排列方式的数量来计算；幸运的是，我们在第 6 章中已经实现了一个用于计算排列数的函数：

清单 16.9 二项分布

```c#
public sealed class Binomial : IWeightedDistribution<int>
{
    private int trials;
    private double prob;
    private IDistribution<bool> flip;
 
    public static Binomial Distribution(int trials, double prob) =>
        new(trials, prob);
 
    private Binomial(int trials, double prob)
    {
        this.trials = trials;
        this.prob = prob;
        this.flip = new Flip<bool>(true, false, prob);						#A
    }
 
    public double Weight(int successes) =>
        Pow(prob, successes) * 
        Pow(1.0 - prob, trials - successes) *
        (double)trials.Choose(successes);									#B
 
    public int Sample() => flip.Samples().Take(trials).Count(x => x);		#C
}

#A true 表示一次“成功试验”——我们掷出了正面
#B 这个扩展方法来自第 6 章
#C 反复采样掷硬币结果并统计正面次数
```

> 注意：通过生成 n 次掷硬币结果并统计成功次数来从二项分布中采样是 O(n)；随着 n 变大，有渐近意义上更高效的二项分布采样算法，但对于我们的目的，在试验次数较少的情况下，线性算法就足够了。

十次试验中正面次数的分布由二项分布建模：

```csharp
var likelihood = (double fairness) => Binomial.Distribution(10, fairness);
```

现在生成一个接受证据并返回后验分布的函数：

```csharp
var posterior = prior.Posterior(likelihood);
```

最后，我们可以比较在观察到硬币行为之前，对硬币公平性的先验信念，以及在观察到比如十次投掷中有九次出现正面之后的后验信念：

```csharp
Console.WriteLine(prior.Histogram(0.0, 1.0));
Console.WriteLine(posterior(9).Histogram(o.0, 1.0));
```

我们的先验信念是在观察证据之前，硬币的公平性很可能接近 50%，但也可能显著更高或更低：

```
******
                *******
                *********
              ***********
              *************
             **************
             ***************
            ****************
           ******************
           ******************
          ********************
         **********************
        ************************
        *************************
       **************************
      ****************************
    ********************************
   **********************************
****************************************
****************************************
```

后验分布则完全不同；在观察到十次中有九次为正面之后，这枚硬币有 50% 公平性（或更低）的可能性非常小：

```
*****
                            *****
                            ******
                            ******
                           ********
                           ********
                          **********
                          **********
                         ***********
                         ************
                         ************
                        *************
                       ***************
                       ***************
                      ****************
                      ****************
                     ******************
                    *******************
                  **********************
           *****************************
```

当然，我们并不真的有一家生产有偏硬币的工厂。相反，我们可能对由恶意僵尸网络控制的新用户账户分布有一个先验信念，并有一个似然函数，将不同类型的恶意行为（垃圾信息、诈骗、虚假信息、选举干扰）与某些行为的概率联系起来——比如发送好友请求、在群组中发帖等等。通过观察许多真实行为，我们可以大大提高后验不确定性的精度，从而判断一个账户是真人还是恶意机器人。

贝叶斯推理在现代业务线编程中有极其多的应用；任何时候，只要我们对世界存在不确定性，并且这种不确定性可以通过观察证据变得更加精确，贝叶斯定理就适用。世界充满不确定性，而软件工程师越来越需要大规模地自动化决策。我们有道德义务使用有原则、可靠的技术来做到这一点。

## 总结

- 对马尔可夫过程进行采样会生成一个随机的值序列。生成的序列可以是有限的，也可以是无限的；这些值可以是离散的，也可以是连续的。
- 马尔可夫过程生成一个随机的“状态”序列，其中“状态”可以是任何东西。第一个状态是通过从某个初始分布中采样随机选择的。每个后续状态都是通过将最近生成的状态传入转移函数，并从得到的分布中采样来选择的。
- 如果状态是单词，那么马尔可夫过程就会生成文本。创建初始分布和转移函数的一种简单方法，是分析一段现有文本，并记录句子中初始单词的分布，以及这些句子中单词对的分布。
- 这种基本技术会生成不可信的文本；我们可以通过多种技术生成更可信的文本——也就是更可能欺骗读者的文本——其中一些技术代价高昂或不道德。生成可信随机文本的意图存在于模型作者那里，而不是模型本身那里。
- 如果我们拥有目标分布的 PDF，并且知道关于高权重区域以及支撑集中高概率部分大小的基本事实，那么我们可以使用 Metropolis 算法创建一个马尔可夫过程，其中状态序列就是来自目标分布的样本序列。
- Metropolis 算法对于生成连续后验分布尤其有用。给定一个表示我们在观察之前信念的分布，以及一个对世界如何运行进行建模的似然函数，我们可以获得一个后验分布，它会在新观测证据的语境中精确更新我们的不确定性。