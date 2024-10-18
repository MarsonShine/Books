接下来是一系列练习的比较分析，这些练习分别采用了传统的面向对象（OO）风格和“函数式”风格编写。前两个练习你可能已经很熟悉；OO 部分来自我在《Clean Craftsmanship》[^1]一书中发表的示例。

每个例子的两个版本都遵循测试驱动开发（TDD）的原则进行创建。测试与代码一同逐步展示。你会看到如何通过第一个测试，然后是第二个、第三个，依此类推。

本书这一部分的目的是探讨和审视面向对象实现与函数式实现之间的差异。

这些练习的复杂性逐步递增。**质因数分解**相对简单。**保龄球游戏**稍微复杂一些，而**消息的八卦传递**则更为复杂。最后一个练习，**工资系统**，是例子中最复杂的。我在《Agile Software Development: Principles, Patterns, and Practices》[^2]一书的第三部分中详细探讨了它。为了节省篇幅，这里只展示函数式版本。

随着复杂性的增加，不同编程方法之间的差异将变得更加明显。你会发现这很有教育意义。但也要做好准备迎接一些意外结论；结果可能不会如你所预期的那样。

# 质因数分解

函数式编程是否优于使用可变变量的编程？让我们对一些熟悉的练习进行比较分析。例如，以下是传统的 Java 版本，通过测试驱动开发（TDD）进行的质因数分解练习（Prime Factors kata）的推导，基本上与《Clean Craftsmanship》第2章[^3]中展示的内容一致。相关视频“Prime Factors”也可以观看。你可以通过访问 [https://informit.com/functionaldesign](https://informit.com/functionaldesign) 并注册来获取该视频。

## Java 版本

我们从一个简单的测试开始：

```java
public class PrimeFactorsTest {
	@Test
	public void factors() throws Exception {
		assertThat(factorsOf(1), is(empty()));
	}
}
```

我们以简单的方式传递给它：

```java
private List<Integer> factorsOf(int n) {
	return new ArrayList<>();
}
```

当然，这个测试通过了。所以下一个最简单的测试是 2：

```java
assertThat(factorsOf(2), contains(2));
```

我们通过一些简单且显而易见的代码让它通过：

```java
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	if (n>1)
		factors.add(2);
	return factors;
}
```

接下来是 3：

```java
assertThat(factorsOf(3), contains(3));
```

然后通过一个小技巧，将 `2` 替换为 `n` 使其通过：

```java
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	if (n>1)
		factors.add(n);
	return factors;
}
```

接下来是 4，这是我们第一次在列表中有不止一个质因数的情况：

```java
assertThat(factorsOf(4), contains(2, 2));
```

我们通过一个看起来相当糟糕的 hack 使其通过：

```java
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	if (n>1) {
		if (n % 2 == 0) {
			factors.add(2);
			n /= 2;
		}
	}
	if (n>1)
		factors.add(n);
	return factors;
}
```

不出所料，下面三个测试都通过：

```java
assertThat(factorsOf(5), contains(5));
assertThat(factorsOf(6), contains(2,3));
assertThat(factorsOf(7), contains(7));
```

8 这个测试案例是我们第一次在质因数列表中看到超过两个元素的情况：

```
assertThat(factorsOf(8), contains(2, 2, 2));
```

我们通过将其中一个 `if` 语句优雅地转换为 `while` 来使其通过：

```java
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	if (n>1) {
		while (n % 2 == 0) {
			factors.add(2);
			n /= 2;
		}
	}
	if (n>1)
		factors.add(n);
	return factors;
}
```

接下来的测试是 9，由于我们的解决方案目前没有处理 3 的因数，因此它也必须失败：

```
assertThat(factorsOf(9), contains(3, 3));
```

为了解决这个问题，我们需要提取 3 的因数。我们可以这样做：

```
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	if (n>1) {
		while (n % 2 == 0) {
			factors.add(2);
			n /= 2;
		}
		while (n % 3 == 0) {
            factors.add(3);
            n /= 3;
        }
	}
	if (n>1)
		factors.add(n);
	return factors;
}
```

但这太糟糕了，因为这意味着无穷无尽的重复。我们可以通过将另一个 `if` 改为 `while` 来解决这个问题：

```java
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	int divisor = 2;
    while (n>1) {
        while (n % divisor == 0) {
            factors.add(divisor);
            n /= divisor;
        }
        divisor++;
    }
	if (n>1)
		factors.add(n);
	return factors;
}
```

稍微重构一下，我们得到了这个：

```java
private List<Integer> factorsOf(int n) {
	ArrayList<Integer> factors = new ArrayList<>();
	for (int divisor = 2; n > 1; divisor++)
        for (; n % divisor == 0; n /= divisor)
        	factors.add(divisor);
	return factors;
}
```

这个算法足以计算任何[^4]整数的质因数。

## Clojure 版本

好的，那么用 Clojure 实现会是什么样子呢？

和之前一样，我们从一个简单的测试[^5]开始：

```
(should= [] (prime-factors-of 1))
```

我们通过返回一个空列表使其通过，正如预期的那样：

```
(defn prime-factors-of [n] [])
```

接下来的测试与 Java 版本非常相似：

```
(should= [2] (prime-factors-of 2))
```

解决方案也是一样的：

```
(defn prime-factors-of [n]
	(if (> n 1) [2] []))
```

对第三个测试的解决方法也采用了相同的小技巧，即将 `2` 替换为 `n`：

```
(should= [3] (prime-factors-of 3))

(defn prime-factors-of [n]
	(if (> n 1) [n] []))
```

但到了测试 4 时，Clojure 和 Java 的解决方案开始分化：

```
(should= [2 2] (prime-factors-of 4))

(defn prime-factors-of [n]
	(if (> n 1)
		(if (zero? (rem n 2))
		(cons 2 (prime-factors-of (quot n 2)))
		[n])
	[]))
```

这个解决方案是递归的。`cons` 函数将 `2` 添加到 `prime-factors-of` 返回的列表的开头。仔细想一想，你会理解为什么这样做是合理的！`rem` 和 `quot` 函数分别是整数取余和取商操作。

在 Java 程序中，此时还没有出现迭代。两个 `if(n>1)` 片段虽然暗示了即将到来的迭代，但解决方案依然是线性的逻辑。

然而，在函数式版本中，我们看到了完整的递归实现，甚至没有使用尾调用优化。

接下来的四个测试直接通过，甚至包括测试 `8` 的情况：

```
(should= [5] (prime-factors-of 5))
(should= [2 3] (prime-factors-of 6))
(should= [7] (prime-factors-of 7))
(should= [2 2 2] (prime-factors-of 8))
```

在某种程度上，这有点遗憾，因为在 Java 解决方案中，正是 `8` 这个测试促使我们将 `if` 转换为 `while`。在 Clojure 解决方案中，没有发生这样的优雅转换；不过我不得不说，到目前为止，递归是更好的解决方案。

接下来是 `9` 的测试。此时，Java 和 Clojure 版本都面临着相似的重复代码问题：

```
(should= [3 3] (prime-factors-of 9))

(defn prime-factors-of [n]
	(if (> n 1)
		(if (zero? (rem n 2))
			(cons 2 (prime-factors-of (quot n 2)))
			(if (zero? (rem n 3))

				(cons 3 (prime-factors-of (quot n 3)))
				[n]))
		[]))
```

这个解决方案并不具有可持续性。它会迫使我们为 5、7、11、13……等质数一直添加到语言能够处理的最大质数。但这个解决方案暗示了一个有趣的迭代/递归解决方案：

```
(defn prime-factors-of [n]
	(loop [n n
		divisor 2
		factors []]
	(if (> n 1)
		(if (zero? (rem n divisor))
			(recur (quot n divisor) divisor (conj factors divisor))
			(recur n (inc divisor) factors))
		factors)))
```

`loop` 函数在原地创建了一个新的匿名函数。当 `recur` 嵌套在 `loop` 表达式中时，它会在尾调用优化（TCO）的情况下重新调用这个匿名函数。匿名函数的参数是 `n`、`divisor` 和 `factors`，每个参数都有其初始值。因此，`loop` 内的 `n` 被初始化为 `loop` 外部的 `n` 的值（两个 `n` 标识符是不同的），`divisor` 被初始化为 2，`factors` 被初始化为空列表 `[]`。

这个解决方案中的递归实际上是迭代式的，因为递归调用在尾部。注意，`cons` 被替换为了 `conj`，因为列表构建的顺序发生了变化。`conj` 函数将元素追加[^6]到 `factors` 中。确保你理解为什么顺序发生了变化！

## 总结

有几个值得注意的地方。首先，Java 和 Clojure 版本的测试顺序是相同的。这很重要，因为它暗示着转向函数式编程对我们表达测试的方式几乎没有影响。测试似乎更加基础、抽象，或者说比编程风格更本质。

其次，两个版本的解决策略在迭代需求出现之前就已经发生了偏离。在 Java 中，测试 `4` 并不需要迭代；但在 Clojure 中，它促使我们使用递归。这暗示了递归在某种程度上比使用 `while` 语句的标准循环更具语义上的本质性。

第三，Java 中的推导相对直截了当，从一个测试到下一个测试几乎没有出现意外。然而，Clojure 的推导在进行到 `9` 的测试时出现了一个急转弯，这是因为我们选择使用非尾递归而不是迭代循环结构来解决 `4` 的测试问题。这表明，当我们有选择时，应该优先使用尾递归结构而不是非尾递归。

最终的结果是，一个与 Java 解决方案相似的算法，但有一个令人惊讶的不同：它不是一个双重嵌套循环。Java 解决方案有一个循环递增除数，另一个循环不断将当前除数作为因数添加。而 Clojure 解决方案用两个独立的递归替代了双重嵌套循环。

哪个解决方案更好呢？Java 解决方案速度更快，因为 Java 本身比 Clojure 快得多。但除此之外，我没有看到哪种解决方案有特别明显的优点。对于同时熟悉两种语言的人来说，两者在阅读或理解上都没有更容易的表现。两者在结构上或风险性上也没有优劣之分。从我的角度来看，这两者难分伯仲。除了 Java 固有的速度优势之外，两种风格并没有哪一种明显优于另一种。

然而，这是最后一个结果不明确的例子。从一个例子到下一个例子，差异将变得越来越显著。

[^1]: Robert C. Martin, *Clean Craftsmanship* (Addison-Wesley, 2021)。
[^2]: Robert C. Martin, *Agile Software Development: Principles, Patterns, and Practices* (Pearson, 2002)。
[^3]: Martin, Clean Craftsmanship, p. 52.
[^4]: 只要给足够的时间和空间
[^5]: 使用 speclj 测试框架
[^6]: 在这个例子是因为 `factors` 是矢量