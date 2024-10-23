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

第一个测试告诉我们，我们正在将一些文本输入解析为客户记录。该记录包含四个字段：`id`、`name`、`address` 和 `credit-limit`。接下来的四个测试告诉我们有关语法错误的信息，例如输入缺失或格式错误。

最后一个测试是有趣的。它测试了一条业务规则。将业务规则作为解析输入的一部分进行测试，显然违反了**单一职责原则**（SRP）。解析代码可以安全地验证语法错误，但应避免所有语义检查，因为这些检查属于另一个角色的职责。定义输入格式的角色与指定最大允许信用额度[^3]的角色并不相同。

通过这些测试的代码进一步加剧了问题：

```clojure
(defn validate-customer
    [{:keys [id name address credit-limit] :as customer|]
    (if (or (nil? id)
            (nil? name)
            (nil? address)
            (nil? credit-limit))
		:invalid
        (let [credit-limit (Integer/parseInt credit-limit)]
            (if (> credit-limit 50000)
                :invalid
                (assoc customer :credit-limit credit-limit))))
                
(defn parse-customer [lines]

	(let [[_ id] (re-matches #"^Customer-id: (\d{7})$"
							(nth lines 0))
        [_ name] (re-matches #"^Name: (.+)$" (nth lines 1))
        [_ address] (re-matches #"^Address: (.+)$" (nth lines 2))
        [_ credit-limit] (re-matches #"^Credit Limit: (\n+)$
"
									(nth lines 3
(validate-customer
    {:id id
        :name name
        :address address
        :credit-limit credit-limit})))
```

看看 `validate-customer` 函数是如何将语法检查与限制信用额度为 50,000 的语义业务规则混在一起的。这个语义检查应该属于一个完全不同的模块，而不应该与所有那些语法检查纠缠在一起。

更糟的是，设想一个程序员认真地使用 `clojure/spec` 来动态定义 `customer` 类型：

```clojure
(s/def ::id (s/and
				string?
				#(re-matches #"\d+" %)))

(s/def ::name string?)
(s/def ::address string?)
(s/def ::credit-limit (s/and int? #(<= % 50000)))
(s/def ::customer (s/keys :req-un [::id ::name
									::address ::credit-limit]))
```

这个规范适当地限制了客户数据结构，使其在语法上正确；但它同时也强加了一个语义业务规则，即信用额度不得超过 50,000。

为什么我对将信用额度限制与数据结构的语法混在一起感到担忧？原因是我认为数据结构的语法和信用额度的限制可能由不同的角色来指定。而且我预期这些不同的角色会在不同的时间出于不同的原因请求变更。我不希望语法的变更意外地破坏了业务规则。

当然，这引出了一个问题：语义验证应该放在哪里？答案是，语义验证应该放在负责这些验证的角色可能会更改的模块中。例如，如果有一条业务规则规定信用额度不得超过 50,000，那么验证代码应该放在处理其他所有信用额度相关业务的模块中。

**将那些因相同原因、在相同时间发生变化的内容聚集在一起。  
将那些因不同原因或在不同时间发生变化的内容分离开来。**

## 开闭原则

OCP（**开放封闭原则**）最早由 Bertrand Meyer 在他 1988 年的经典著作《面向对象软件构造》中提出。简单来说，它的意思是，软件模块应该**对扩展开放**，但**对修改封闭**。这意味着，你应该设计你的模块，使得在扩展或改变它们的行为时，不需要修改它们的代码。

这听起来可能有点自相矛盾，但实际上这是我们经常做的事情。以 C 语言中的复制程序为例：

```c
void copy() {
    int c;
    while ((c = getchar()) != EOF)
    	putchar(c);
}
```

这个程序从标准输入（stdin）复制字符到标准输出（stdout）。我可以随时向操作系统添加新设备。例如，我可以添加光学字符识别（OCR）和文本转语音合成器。这段程序仍然能够正常运行，并且能够毫无问题地将字符从 OCR 复制到语音合成器，而无需对程序进行修改，甚至不需要重新编译。

这是一个非常强大的理念，它允许我们将高层次的策略与低层次的细节分离，并使得高层次的策略免受低层次细节变更的影响。然而，这要求高层策略通过抽象层来访问低层细节。

在面向对象（OO）程序中，我们通常通过多态接口来创建这个抽象层。在像 Java、C# 和 C++ 这样的静态类型语言中，这些接口是带有抽象方法的类[^4]。高层策略通过这些接口访问实现这些接口或继承这些接口的低层细节。

在像 Python 和 Ruby 这样的动态类型面向对象语言中，这些接口是鸭子类型（duck types）。鸭子类型在语言中没有特定的语法，它们只是高层策略调用、低层细节实现的一组函数签名。动态类型系统通过匹配这些签名在运行时确定多态分派。

一些函数式语言，比如 F# 和 Scala，基于面向对象的基础，因此可以利用该基础的多态接口。但函数式语言长期以来拥有另一种创建 OCP 抽象层的机制：**函数**。

### 函数

考虑一个简单的 Clojure 程序：

```clojure
(defn copy [read write]
    (let [c (read)]
    	(if (= c :eof)
			nil
			(recur read (write c)))))
```

这本质上与用 C 语言编写的复制程序是一样的，只不过读取和写入的函数是作为参数传入的[^5]。然而，开放封闭原则（OCP）的抽象层依然保持完整。

顺便说一下，我使用了以下测试来测试这个程序。我想你会觉得有趣。

```
(def str-in (atom nil))
(def str-out (atom nil))

(defn str-read []
    (let [c (first @str-in)]
        (if (nil? c)
            :eof
            (do
            	(swap! str-in rest)
            c))))
            
(defn str-write [c]
    (swap! str-out str c)
    str-write)
    
(describe "copy"
    (it "can read and write using str-read and str-write"
        (reset! str-in "abcedf")
        (reset! str-out "")
        (copy str-read str-write)
        (should= "abcdef" @str-out)))
```

我使用了 `atom`，因为 I/O 是一个副作用，因此它并不是纯函数式的。毕竟，当你从输入读取或向输出写入时，你实际上是在改变它们的状态。因此，底层的 I/O 函数并非纯函数式，并使用软件事务内存（Software Transactional Memory）来管理状态的变化。

### 带有虚表(Vtables)的对象

对于那些渴望面向对象（OO）的人，你可以使用以下技术将“对象”传递给 `copy`：

```clojure
(defn copy [device]
    (let [c ((:getchar device))]
        (if (= c :eof)
            nil
            (do
                ((:putchar device) c)
                (recur device)))))
```

测试只是用函数加载了设备映射：

```clojure
(it "can read and write using str-read and str-writer"
    (reset! str-in "abcedf")
    (reset! str-out "")
    (copy {:getchar str-read :putchar str-write})
    (should= "abcdef" @str-out))
```

C++ 程序员会认出，`device` 参数实际上就是一个虚表（vtable）——这就是 C++ 中的多态机制。无论如何，显然你可以为 `copy` 程序定义许多不同的设备。你可以扩展 `copy` 的行为，而不需要修改它。

### 多方法

关于这个主题的另一种变体是使用多方法（multi-methods）。许多语言，无论是函数式的还是其他类型的，都以某种方式支持多方法。多方法是另一种形式的鸭子类型，因为它们创建了一组松散的、根据函数签名和参数的“类型”[^6]动态调度的方法。

在 Clojure 中，我们采用了久经考验的调度函数（dispatching function）来指定这种“类型”：

```clojure
(defmulti getchar (fn [device] (:device-type device)))
(defmulti putchar (fn [device c] (:device-type device)))
```

> Clojure 中的**多方法**（multimethods）是一种强大的多态机制，允许根据任意的条件（而不仅仅是对象类型或数量）来选择不同的函数实现。这比传统的面向对象语言中的**方法重载**更加灵活。
>
> ### 多方法的核心概念
>
> 1. **判定函数**（dispatch function）：多方法的判定是基于一个判定函数的结果。这个函数会根据传入的参数，返回一个值，Clojure 根据该值选择合适的具体方法。
> 2. **方法定义**：根据不同的判定函数结果（称为 **dispatch 值**），我们可以定义多个不同的函数实现。每个实现对应一个特定的判定结果。
>
> 可以使用 `defmulti` 定义一个多方法，指定一个判定函数。如：
>
> ```clojure
> (defmulti area 
>   (fn [shape] (:type shape)))  ;; 这是判定函数，根据 shape 的 :type 属性决定具体调用哪个实现
> ```
>
> 这里的 `area` 是一个多方法，判定函数 `(fn [shape] (:type shape))` 将根据 `shape` 的 `:type` 属性决定调用哪个具体的 `area` 实现。
>
> 具体实现如下：
>
> ```clojure
> (defmethod area :circle
>   [shape]
>   (* Math/PI (:radius shape) (:radius shape)))  ;; 针对圆形定义面积计算公式
> 
> (defmethod area :rectangle
>   [shape]
>   (* (:width shape) (:height shape)))  ;; 针对矩形定义面积计算公式
> ```
>
> 调用：
>
> ```clojure
> (area {:type :circle :radius 10})  ;; 返回圆形的面积
> (area {:type :rectangle :width 5 :height 7})  ;; 返回矩形的面积
> ```
>
> 在这里，多方法会根据传入数据的 `:type` 属性值来选择具体的 `area` 实现。

在这里，我们看到 `getchar` 和 `putchar` 被声明为多方法（multi-methods）。每个多方法都有一个调度函数，该函数接受与 `getchar` 和 `putchar` 相同的参数。我们可以修改 `copy` 程序，使其调用这些多方法：

```clojure
(defn copy [device]
    (let [c (getchar device)]
        (if (= c :eof)
        nil
        (do
            (putchar device c)
            (recur device)))))
```

下面是新 `copy` 函数的测试。注意，测试设备不再是包含函数指针的虚表（vtable）。相反，它现在包含输入和输出的 `atom`，以及一个 `:device-type`。多方法将根据这个 `:device-type` 进行调度。

```clojure
(it "can read and write using multi-method"
    (let [device {:device-type :test-device
                    :input (atom "abcdef")
                    :output (atom nil)}]
        (copy device)
        (should= "abcdef" @(:output device))))
```

剩下的就是多方法的实现了，这不应该令人感到惊讶。

```clojure
(defmethod getchar :test-device [device]
    (let [input (:input device)
        	c (first @input)]
        (if (nil? c)
            :eof
            (do
                (swap! input rest)
                c))))
                
(defmethod putchar :test-device [device c]
  (let [output (:output device)]
    (swap! output str c)))
```

这些是当 `:device-type` 为 `:test-device` 时将被调度的实现方法。显然，可以为各种不同的设备创建许多类似的实现方法。这些新设备将扩展 `copy` 程序，而无需进行任何修改。

### 独立部署

我们期望从开放封闭原则（OCP）中获得的一个好处是能够将高层策略和低层细节编译到单独的模块中，并独立部署。在 Java 和 C# 中，这意味着将它们编译为单独的 `jar` 或 `dll` 文件，并能够动态加载。在 C++ 中，我们会编译这些模块并将其二进制文件放入动态加载的共享库中。

上面展示的 Clojure 解决方案未能实现这个目标。高层策略和低层细节无法从两个独立的 `jar` 文件中动态加载。

这在 Java 或 C# 中可能是个大问题，但在 Clojure 中却不是那么严重，因为“加载” Clojure 程序几乎总是涉及到编译[^7]。因此，虽然高层策略和低层细节可能无法从 `jar` 文件中动态加载，但它们是从源文件中动态编译和加载的。因此，大部分独立部署 `jar` 文件的好处仍然得以保留。

然而，如果你绝对必须拥有完全独立的部署能力，还有另一个选择。你可以使用 Clojure 的协议（protocols）和记录（records）：

```clojure
(defprotocol device
    (getchar [_])
    (putchar [_ c]))
```

协议（protocol）将成为一个 Java 接口，可以独立编译为 `jar` 文件以供动态加载。协议的实现（如下所示）同样可以独立编译和加载：

```clojure
(defrecord str-device [in-atom out-atom]
    device
    (getchar [_]
        (let [c (first @in-atom)]
            (if (nil? c)
                :eof
                (do
                    (swap! in-atom rest)
                	c))))
    (putchar [_ c]
    	(swap! out-atom str c)))
    	
(describe "copy"
	(it "can read and write using str-read and str-write"
		(let [device (->str-device (atom "abcdef") (atom nil))]
            (copy device)
            (should= "abcdef" @(:out-atom device)))))
```

> 在 Clojure 中，**协议**（protocols）和**记录**（records）是用于处理多态和数据结构的概念。它们提供了一种简洁且高效的方式来实现类似面向对象语言中的接口和类的功能，但更加灵活且具函数式编程的风格。
>
> ### 1. **协议（Protocols）**
>
> Clojure 中的协议类似于面向对象编程中的接口。它定义了一组行为（方法），不同的数据类型可以实现这些行为。这为多态提供了一种高效的实现方式。
>
> #### 定义协议
>
> 你可以用 `defprotocol` 定义一个协议，协议中包含一个或多个方法签名（但不包含具体实现）。
>
> ```clojure
> (defprotocol Shape
>   (area [this])  ;; 计算面积
>   (perimeter [this]))  ;; 计算周长
> ```
>
> **`Shape`** 是一个协议，定义了两个方法：`area` 和 `perimeter`。
>
> 每个方法的第一个参数通常是 `this`，表示调用方法的对象（类似于面向对象编程中的 `this` 或 `self`）。
>
> #### 实现协议
>
> 可以用 `extend-protocol` 或 `extend-type` 来为不同的数据类型实现该协议的方法。
>
> ```clojure
> (defrecord Circle [radius])
> 
> (extend-type Circle
>   Shape
>   (area [this]
>     (* Math/PI (* (:radius this) (:radius this))))  ;; 计算圆的面积
>   (perimeter [this]
>     (* 2 Math/PI (:radius this))))  ;; 计算圆的周长
> ```
>
> - `Circle` 是一个记录（稍后会解释）。
> - 使用 `extend-type` 实现了 `Shape` 协议中的 `area` 和 `perimeter` 方法。对于 `Circle` 类型的数据，`area` 会计算圆的面积，而 `perimeter` 会计算周长。
>
> #### 使用协议
>
> 定义并实现协议后，可以像这样使用协议的方法：
>
> ```clojure
> (def my-circle (Circle. 5))  ;; 创建一个半径为5的圆
> (area my-circle)  ;; 计算面积，返回 78.53981633974483
> (perimeter my-circle)  ;; 计算周长，返回 31.41592653589793
> ```
>
> Clojure 会根据 `my-circle` 的类型自动调用对应的 `Shape` 实现。
>
> ### 2. **记录（Records）**
>
> **记录** 是 Clojure 中的一种数据结构，用于定义带有固定字段的高效数据类型。记录是基于 Java 对象实现的，具有比普通 Clojure 的 `map` 更高的性能，并且可以与协议配合使用来提供类似于面向对象编程中的类和接口的行为。
>
> #### 定义记录
>
> 使用 `defrecord` 来定义一个记录。记录类似于 Clojure 的 `map`，但字段是固定的，并且性能更高。
>
> ```clojure
> (defrecord Circle [radius])
> ```
>
> 这里定义了一个名为 `Circle` 的记录，包含一个字段 `radius`，表示圆的半径。
>
> #### 创建记录实例
>
> 你可以通过调用记录的构造函数来创建记录实例：
>
> ```clojure
> (def my-circle (Circle. 10))  ;; 创建一个半径为10的圆
> ```
>
> 记录实例可以像 `map` 一样通过字段名来访问字段值：
>
> ```clojure
> (:radius my-circle)  ;; 返回 10
> ```
>
> - 记录是不可变的，类似于 `map`，但它的字段是固定的。
> - 记录的字段访问效率比 `map` 高。
> - 记录可以实现协议（正如前面所展示的），为特定的数据类型提供行为（类似于面向对象语言中的类实现接口）。
>
> ### 总结
>
> - **协议** 提供了一种类似于面向对象编程中接口的机制，允许定义一组行为，不同类型的数据结构可以实现这些行为。
> - **记录** 是一种高效的数据结构，类似于 `map`，但字段固定，并且可以通过协议赋予行为。记录通常与协议结合使用，形成类似于面向对象编程中的“类+接口”模式。
>
> 通过协议和记录，Clojure 提供了一种既有函数式编程风格，又支持灵活多态的编程模型，适合处理复杂的数据和行为抽象。

请注意测试中的 `->str-device` 函数。这实际上是实现了 `device` 协议的 `str-device` 类的 Java 构造函数。还请注意，我像之前的示例一样将 `atom` 加载到设备中。

事实上，我并没有更改 `copy` 程序来使该示例正常运行。`copy` 程序与多方法示例中的代码完全相同。现在，这就是开放封闭原则（OCP）的作用！

如果 Clojure 的协议/记录机制让你觉得像面向对象（OO），那是因为它确实是面向对象的。JVM 是面向对象的基础，而 Clojure 很好地构建在这个基础之上。

## 里氏替换原则

任何支持开放封闭原则 (OCP) 的语言也必须支持里氏替换原则 (LSP)。这两个原则是相互关联的，因为每次违反 LSP 都会隐含着违反 OCP 的可能性。

LSP 首次由 Barbara Liskov 于 1988[^8] 年提出，她对子类型给出了一个或多或少的形式定义。本质上，她的定义指出，子类型在任何使用其基类型的程序中必须可以被替换为基类型。

为了进一步说明这一点，假设我们有一个使用类型 `employee` 的程序 `pay`：

```clojure
(defn pay [employee pay-date]
    (let [is-payday? (:is-payday employee)
        calc-pay (:calc-pay employee)
        send-paycheck (:send-paycheck employee)]
    (when (is-payday? pay-date)
        (let [paycheck (calc-pay)]
        	(send-paycheck paycheck)))))
```

请注意，我在这里使用了虚函数表（vtable）的方法来创建类型。还要注意，`pay` 函数无法看到类型中的任何数据。`pay` 函数唯一能看到的就是 `employee` 类型中的方法。这难道还不够面向对象吗？

下面是使用该类型的测试代码。注意 `make-test-employee` 函数是如何创建一个对象的，该对象使用鸭子类型来符合 `employee` 类型的要求：

```clojure
(defn test-is-payday [employee-data pay-date]
	true)
	
(defn test-calc-pay [employee-data]
	(:pay employee-data))
	
(defn test-send-paycheck [employee-data paycheck]
	(format "Send %d to: %s at: %s"
            paycheck
            (:name employee-data)
            (:address employee-data)))
(defn make-test-employee [name address pay]
	(let [employee-data {:name name
                        :address address
                        :pay pay}
                        
		employee {:employee-data employee-data
                :is-payday (partial test-is-payday
                					employee-data
                :calc-pay (partial test-calc-pay employee-data)
                :send-paycheck (partial test-send-paycheck
										employee-data)}]
	employee))

(describe "Payroll"
	(it "pays a salaried employee"
		(should= "Send 100 to: name at: address"
				(pay (make-test-employee "name" "address" 100)
					:now))))
```



[^1]: https://adventofcode.com/2022/day/10
[^2]: 阴极射线管（CRT）。阴极射线指的是电子束。CRT有电子枪，可以产生窄的电子束，并通过定期变化的磁场在屏幕上进行光栅扫描。电子束撞击屏幕上的磷光体，使它们发光，从而形成光栅图像。

[^3]: 即使这两者是同一个人也是如此。在这种情况下，该人只是扮演了两个不同的角色。
[^4]: Java和C#中的关键字`interface`定义了每个方法都是抽象的类。
[^5]: 作为参数传递或从函数中返回的函数有时被称为高阶函数（higher-order functions）。
[^6]: 我这里用了引号，因为参数的“类型”并不一定与它们的具体数据类型相关。事实上，这个“类型”可以是一个完全不同的概念。
[^7]: 在某些情况下，Clojure 允许预编译。
[^8]: 巧合的是，这也是 Bertrand Meyer 提出 OCP 的同一年。