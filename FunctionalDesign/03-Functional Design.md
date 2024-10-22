# 数据流

在[第9章面向对象编程](02-Comparative Analysis.md#面向对象编程)中，我提到函数式程序的设计更像是管道而非过程式的。其设计具有明显的数据流倾向。这是因为我们倾向于使用 `map`、`filter` 和 `reduce` 来将列表的内容转换成其他列表，而不是一次处理一个元素来生成结果。

我们可以在之前的许多例子中看到这种倾向，包括[第II部分](02-Comparative Analysis.md)中比较分析的**保龄球游戏**、**八卦的公交车司机**和**工资发放**应用程序。

再举一个例子，考虑 2022 年 Advent of Code 第十天[^1]的这个有趣问题。目标是在一个 6x40 的屏幕上渲染像素。像素从左到右逐个绘制，基于时钟电路。时钟周期从 0 开始计数。如果某个寄存器 `x` 与时钟周期数字匹配，那么适当屏幕位置的像素将被点亮，否则将关闭。

这种方式实际上与老式 CRT[^2] 显示器的工作方式非常类似。你必须在光束扫描屏幕的过程中在正确的时刻激活电子束。因此，你需要将位图中的位与驱动电子束的时钟匹配。如果根据时钟，光束处于第 934 个位置，而位图中的第 934 位被设置，那么你会瞬间激活光束以显示该像素。

Advent of Code 的这个问题更有趣一些。它要求我们模拟一个只有两个指令的简单处理器。第一个指令是 `noop`，它消耗一个时钟周期，但没有其他效果。另一个指令是 `addx`，它接受一个整数参数 `n`，并将其加到处理器的 `x` 寄存器中。这个指令需要两个时钟周期，且仅在这两个周期结束后才会更改 `x` 寄存器。屏幕上的像素会在某个时钟周期内可见，但仅当在该周期开始时，`x` 寄存器与时钟周期号匹配时才会如此。

因此，如果根据时钟，光束位于屏幕位置 23，而在周期 23 的开始时 `x` 寄存器为23，那么该时钟周期将激活光束。

为了稍微增加复杂性，`x` 寄存器与时钟周期的匹配范围被扩展为 22、23 和 24 都会匹配时钟周期 23。换句话说，`x` 寄存器指定了一个三像素宽的窗口。只要时钟周期落在这个窗口内，光束就会被激活。

由于屏幕宽 40 像素、高 6 像素，因此时钟周期与 `x` 的匹配是以 40 为模的。

任务是执行一组指令，并生成六个字符串的列表，每个字符串有 40 个字符，用 `"#"` 表示可见的像素，用 `"."` 表示不可见的像素。

如果你用 Java、C、Go、C++、C# 或其他过程式/面向对象的语言编写这个程序，你可能会创建一个循环，一次迭代一个时钟周期，同时为每个周期积累相应的像素。循环将消费指令并按照指示修改 `x` 寄存器。

以下是一个典型的 Java 示例：

```java
package crt;

public class Crt {
    private int x;
    private String pixels = "";
    private int extraCycles = 0;
    private int cycle = 0;
    private int ic;
    private String[] instructions;
    
    public Crt(int x) {
    	this.x = x;
    }
    
    public void doCycles(int n, String instructions) {
        instructions = instructionsLines.split("\n");
        ic = 0;
        for (cycle = 0; cycle < n; cycle++) {
            setPixel();
            execute();
        }
    }
    
    private void execute() {
        if (instructions[ic].equals("noop"))
        	ic++;
        else if (instructions[ic].startsWith("addx ") && extraCycles == 0) {
        	extraCycles = 1;
    	}
        else if (instructions[ic].startsWith("addx ")
        && extraCycles == 1) {
        	extraCycles = 0;
        	x += Integer.parseInt(instructions[ic].substring(5)^;
        	ic++;
        } else
        	System.out.println("TILT");
    }
    
    private void setPixel() {
        int pos = cycle % 40;
        int offset = pos - x;
        if (offset >= -1 && 1 >= offset)
        	pixels += "#";
        else
        	pixels += ".";
    }
    
    public String getPixels() {
    	return pixels;
    }
    
    public int getX() {
    	return x;
    }
}
```

注意所有被修改的状态。注意它是如何通过逐个时钟周期迭代来填充像素的。还要注意为了考虑 `addx` 需要两个周期来执行，代码中使用了 `extraCycles` 这一机制。

最后，虽然这个程序被很好地分成了几个小的函数，但这些函数都通过可变状态变量相互耦合在一起。这当然是可变类方法的常见情况。

今天我用 Clojure 解决了这个问题，得出的解决方案与上面的 Java 代码非常不同。在阅读时请记得从最后开始，因为 Clojure 程序通常是从底层向上构建的。

```clojure
(ns day10-cathode-ray-tube.core
	(:require [clojure.string :as string]))

(defn noop [state]
	(update state :cycles conj (:x state)))

(defn addx [n state]
	(let [{:keys [x cycles]} state]
		(assoc state :x (+ x n)
					:cycles (vec (concat cycles [x x

(defn execute [state lines]
	(if (empty? lines)
		state
		(let [line (first lines)
			state (if (re-matches #"noop" line)
				(noop state)
				(if-let [[_ n] (re-matches
								#"addx (-?\d+)" line)]
					(addx (Integer/parseInt n) st
					"TILT"))]
		(recur state (rest lines)))))
		
(defn execute-file [file-name]
	(let [lines (string/split-lines (slurp file-name))
		starting-state {:x 1 :cycles []}
		ending-state (execute starting-state lines)]
	(:cycles ending-state)))

(defn render-cycles [cycles]
	(loop [cycles cycles
			screen ""
			t 0]
		(if (empty? cycles)
            (map #(apply str %) (partition 40 40 "" screen))
            (let [x (first cycles)
                    offset (- t x)
                    pixel? (<= -1 offset 1)
                    screen (str screen (if pixel? "#" "."))
                    t (mod (inc t) 40)]
				(recur (rest cycles) screen t)))))

(defn print-screen [lines]
	(doseq [line lines]
		(println line))
	true)

(defn -main []
	(-> "input"
        execute-file
        render-cycles
        print-screen))
```

> TILT 是我最喜欢的错误信息。很久以前，如果你通过物理倾斜弹球机来操纵球，机器就会显示这个信息并取消你的游戏。
>

`execute-file` 函数将文件中的指令列表转换为结果 `x` 值的列表。接着，`render-cycles` 函数将 `x` 值列表转换为像素列表，最后将这些像素分割成 40 字符长度的字符串。

注意，这里没有可变变量。相反，状态值通过各个函数，如同流经管道一样。状态值从 `execute-file` 开始，然后流向 `execute`，接着反复流向 `noop` 或 `addx`，再返回到 `execute`，最后又回到 `execute-file`。在这个流动过程中，每个阶段都从旧的状态值创建出新的状态值，而不改变旧的值。

如果这让你感到熟悉，那就对了。这与我们在命令行 shell 中习惯的管道与过滤器非常相似。数据通过管道进入一个命令，经过该命令转换，然后通过管道流向下一个命令。

下面是我最近在 shell 中使用的一个命令：

```shell
ls -lh private/messages | cut -c 32-37,57-64
```

它列出了 `private/messages` 目录的内容，然后使用 `cut` 命令截取了某些字段。数据从 `ls` 命令流出，通过管道进入 `cut` 命令。这种数据流动的方式与状态值在 `execute`、`addx` 和 `noop` 函数之间流动非常相似。

由于这种流水线处理方式，你应该注意到我的阴极射线管程序被划分为一组小函数，这些函数并没有通过可变状态相互耦合。现存的耦合只是数据格式的耦合，即数据从一个函数流向另一个函数的格式耦合。

最后，注意到在 Java 程序中围绕 `addx` 指令两个周期的那种奇怪处理在这里是不存在的。相反，通过简单地将两个 `x` 值添加到状态的 `:cycles` 元素中，两个周期问题被巧妙地解决了。

当然，我并不一定非要采用数据流风格。我本可以写出一个更接近 Java 算法的 Clojure 算法。但当我在函数式语言中编程时，我不会这样去思考问题。我倾向于数据流解决方案。

Java 和 C# 的一些新功能也适用于数据流风格。但它们显得繁琐，在我看来是生硬地添加到这些语言中的。你的体验可能会有所不同；但当我使用过程式/面向对象语言时，我往往更倾向于迭代而不是使用数据流风格。

换句话说：
***在可变语言中，行为在对象之间流动；在函数式语言中，对象在行为之间流动。***

# 职责

二十多年前，我在面向对象设计的背景下写了关于SOLID原则的文章。由于这一背景，许多人开始将这些原则与面向对象编程联系在一起，并认为这些原则与函数式编程格格不入。这是一个不幸的误解，因为SOLID原则实际上是通用的软件设计原则，它们并不局限于某种特定的编程风格。在本章中，我将努力解释SOLID原则如何应用于函数式编程。

接下来的章节是原则的总结，而不是完整的描述。如果你对更多细节感兴趣，我推荐以下资源：

1. **《敏捷软件开发：原则、模式与实践》**，作者：Robert C. Martin (Pearson, 2002)。
2. **《整洁架构》**，作者：Robert C. Martin (Pearson, 2017)。
3. [Cleancoder.com](http://cleancoder.com/)，这里有很多博客文章和文章可以学习到关于原则和更多内容。
4. [Cleancoders.com](http://cleancoder.com/)，该网站提供了视频，详细解释了每个原则，并通过引人入胜的示例进行讲解。

## 单一职责原则

SRP（单一职责原则）是一个关于关注模块的变更来源的简单声明。而这些变更来源，当然是人。是人提出了软件的变更请求，因此模块需要对这些人负责。

这些人可以被分为称作角色或行为者的群体。一个行为者可以是一个人，或是一群有着相同需求的人。他们提出的变更通常彼此一致。另一方面，不同的行为者有着不同的需求。一位行为者请求的变更会以非常不同的方式影响系统，与其他行为者提出的变更产生的影响大相径庭。这些不同的变更甚至可能互相冲突。

当一个模块需要对多个行为者负责时，来自这些竞争行为者的变更请求可能相互干扰。这种干扰通常导致设计上脆弱性的问题：当进行简单的修改时，系统可能会以意想不到的方式崩溃。

没有什么比系统在做简单功能修改后突然出现惊人的错误更令管理者和客户恐惧的了。如果这种情况反复发生，他们最终的结论只能是开发人员失去了对系统的控制，不知道自己在做什么。

违反 SRP 的情况可以很简单，比如在同一个模块中混合了 GUI 格式化和业务规则代码。也可以很复杂，比如在数据库中使用存储过程来实现业务规则。

以下是一个用 Clojure 编写的违反 SRP 的糟糕示例。我们首先来看测试代码，因为它们能说明问题：

```clojure
(describe "Order Entry System"
	(context "Parsing Customers"
		(it "parses a valid customer"
			(should=
				{:id "1234567"
                :name "customer name"
                :address "customer address"
                :credit-limit 50000}
				(parse-customer
				["Customer-id: 1234567"
                "Name: customer name"
                "Address: customer address"
                "Credit Limit: 50000"])))
                
(it "parses invalid customer"
	(should= :invalid
		(parse-customer
			["Customer-id: X"
            "Name: customer name"
            "Address: customer address"
            "Credit Limit: 50000"]))
	(should= :invalid
		(parse-customer
			["Customer-id: 1234567"
            "Name: "
            "Address: customer address"
            "Credit Limit: 50000"]))
	(should= :invalid
		(parse-customer
			["Customer-id: 1234567"
            "Name: customer name"
            "Address: "
            "Credit Limit: 50000"]))
	(should= :invalid
		(parse-customer
			["Customer-id: 1234567"
            "Name: customer name"
            "Address: customer address"
            "Credit Limit: invalid"])))
(it "makes sure credit limit is <= 50000"
	(should= :invalid
		(parse-customer
            ["Customer-id: 1234567"
            "Name: customer name"
            "Address: customer address"
            "Credit Limit: 50001"])))))
```



## 开闭原则

## 里氏替换原则

## 

[^1]: https://adventofcode.com/2022/day/10
[^2]: 阴极射线管（CRT）。阴极射线指的是电子束。CRT有电子枪，可以产生窄的电子束，并通过定期变化的磁场在屏幕上进行光栅扫描。电子束撞击屏幕上的磷光体，使它们发光，从而形成光栅图像。

