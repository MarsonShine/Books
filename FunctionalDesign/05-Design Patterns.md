设计模式[^1]的概念是软件行业最深刻的思想之一，与结构化编程、面向对象编程和函数式编程齐名。它指出应用程序的某些部分由可重复使用的元素组成，这些元素可以解决许多应用中常见的问题。

然而，和很多优秀的软件理念一样，设计模式也曾被误解、滥用，甚至被视为过时或仅适用于特定场景而被弃用。实属可惜，因为设计模式本质上非常实用。

## 设计模式回顾

设计模式是在特定环境下解决常见问题的命名方案。是的，我知道，这又是一个词语堆砌。让我给你讲个故事吧。

很久以前，在一个遥远的年代，我是一个活跃的作者，经常在一个名为 `comp.object`[^2] 的社交网络上发文。在这个小组中，我们讨论面向对象设计的各种问题。

有一天，有人提出了一个简单的问题，建议我们每个人用自己的方式来解决它，然后讨论结果。问题是：

*给定一个开关和一盏灯，让开关可以控制灯的开关。*

这个讨论持续了数月。

最简单的解决方案当然就是图 16.1 中的方案。

![](asserts/16.1.png)

`Switch` 类调用 `Light` 类[^3]的 `TurnOn` 方法。

反对意见是：`Switch` 类还可以用于打开其他设备，如风扇或电视。因此，`Switch` 类不应直接依赖于 `Light` 类。两者之间应该有一个抽象层，如图 16.2 所示。

![](asserts/16.2.png)

现在，`Switch` 类使用了一个名为 `Switchable` 的接口，而 `Light` 类实现了 `Switchable` 接口。

这样一来，我们就可以通过 `Switch` 控制任意数量的设备了。这个方案是 DIP（依赖倒置原则）、OCP（开放封闭原则）和 LSP（里氏替换原则）的最简单表达之一。它也有一个名字，叫做“抽象服务端”（Abstract Server）[^4]。

假如我们团队正在讨论如何避免 `Switch` 类直接耦合到 `Light` 类，那么团队中的某个人可能会提议：“我们可以使用抽象服务端模式。”如果团队所有成员都了解这个名称及其含义，他们就能迅速决定该方案是否合适。

这就是设计模式：在特定情境下解决某个问题的命名方案。设计模式的价值在于名称和方案具有权威性，因此熟悉这一体系的人可以通过使用名称快速理解彼此。你说“抽象服务端”，我立刻明白你的意思是“在客户端和服务端之间引入一个接口”。

那么设计模式中的“特定情境”部分是什么呢？我们再回到团队中。有人刚刚建议使用抽象服务端模式。另一位团队成员说：“不，你没理解，我们无法控制 `Light` 类，它是第三方库的一部分，无法修改它以实现接口。”

因此，问题的情境是我们希望将 `Switch` 和 `Light` 解耦，但无法修改 `Light` 类。于是团队中的另一个人说：“那我们可以使用适配器模式（Adapter）。”

如果你在团队中，但不知道适配器模式是什么，你可能就无法理解他们的建议。但是如果你熟悉设计模式的经典形式，就可以迅速评估这个建议。设计模式的好处就是让你了解这些名称和标准形式，以便能够快速应用它们。

适配器模式如图 16.3 所示。

![](asserts/16.3.png)

`LightAdapter` 实现了 `Switchable` 接口，并将 `TurnOn` 调用转发给 `Light`。还没在白板上画出来，团队的每个人都已经在脑海中看到它的样子，因为他们熟悉设计模式的经典形式。因此，他们都对这个想法表示认同。

就在他们准备讨论下一个问题时，团队中的一人说：“等等，我们该使用哪种适配器形式？”

事实证明，设计模式的标准名称并不一定描述单一的解决方案。有些模式有多个形式，适配器就是其中之一。它可以如图 16.3 所示，也可以如图 16.4 所示。

![](asserts/16.4.png)

前者称为对象形式的适配器，因为 `LightAdapter` 是一个独立的对象；后者则是类形式的适配器，因为 `LightAdapter` 是 `Light` 的子类。

团队成员讨论了一下这两种形式，并决定目前类形式的适配器足够，且能免去创建单独 `LightAdapter` 对象的麻烦。

### 函数式编程中的设计模式

多年来，我们听到过一些奇怪的传言，比如设计模式是为了解决面向对象语言带来的问题，而在函数式语言中则不需要设计模式。

在接下来的内容中，你会看到，确实有些设计模式的某些方面看起来像是为了解决面向对象语言的一些不足而存在的“变通方法”；但这并不适用于所有设计模式。而且，即使是这些特定的设计模式，也有更普遍的形式，使其在函数式语言中也可以适用。

### 抽象服务

那么，抽象服务在函数式语言中是什么样子呢？

让我们再次考虑 `Switch/Light` 的问题。在 Clojure 中可以这样表达：

```clojure
(defn turn-on-light []
  ;turn on the bloody light!
  )

(defn engage-switch []
  ;Some other stuff...
    (turn-on-light))
```

好吧，这并不复杂。然而，原有的问题显而易见。我们的 `engage-switch` 函数直接依赖于 `turn-on-light`，这意味着我们不能用它来打开风扇、电视或其他任何东西。那么，我们该怎么做呢？

当然，我们可以使用抽象服务端模式。我们只需在 `engage-switch` 函数和 `turn-on-light` 函数之间插入一个抽象接口。我们可以通过传递一个函数参数来实现这个目的。我们称其为抽象服务的函数形式：

```clojure
(defn engage-switch [turn-on-function]
  ;Some other stuff. . .
  (turn-on-function))
```

这在最简单的情况下是有效的。但是让我们稍微增加一点难度。假设 `engage-switch` 函数需要在不同时间将灯打开和关闭，也许它是某种家庭安防系统的一部分，带有用于控制灯的特殊定时器。这会将原有的问题变成这样：

```clojure
(defn turn-on-light []
  ;turn on the bloody light!
  )

(defn turn-off-light []
  ;Criminy! just turn it off!
  )

(defn engage-switch []
  ;Some other stuff...
  (turn-on-light)
  ;Some more other stuff...
  (turn-off-light))
```

现在，`engage-switch` 函数对 `light` 的依赖更加紧密了。我们可以使用抽象服务的相同函数形式，但传递两个参数显得有些笨拙。因此，我们可以传递一个包含多个方法的虚表（vtable）参数。我们称其为抽象服务的虚表形式：

```clojure
defn make-switchable-light []
  {:on turn-on-light
   :off turn-off-light})
  
(defn engage-switch [switchable]
  ;Some other stuff...
  ((:on switchable))
  ;Some more other stuff...
  ((:off switchable)))
```

这看起来确实不错。由于 Clojure 是动态类型语言，我们不需要担心继承或实现关系引发的问题。

当然，我们也可以用抽象服务模式的多方法形式来解决这个问题：

```clojure
(defmulti turn-on :type)
(defmulti turn-off :type)

(defmethod turn-on :light [switchable]
  (turn-on-light))

(defmethod turn-off :light [switchable]
  (turn-off-light))

(defn engage-switch [switchable]
  ;Some other stuff...
  (turn-on switchable)
  ;Some more other stuff...
  (turn-off switchable))
```

我使用以下测试对其进行了测试：

```clojure
(describe "switch/light"
  (with-stubs)
  (it "turns light on and off"
	(with-redefs [turn-on-light (stub :turn-on-light)
				  turn-off-light (stub :turn-off-light)]
	  (engage-switch {:type :light})
	  (should-have-invoked :turn-on-light)
	  (should-have-invoked :turn-off-light))))
```

这里的两个存根用于模拟目标函数。我们用 `{:type :light}` 参数调用 `engage-switch` 函数，然后测试两个目标函数是否实际被调用了。

我将抽象服务模式的协议/记录形式作为练习留给你。在这一点上，很清楚该模式在函数式语言中既适用又有用。

### 适配器

适配器模式用于在客户端需要使用服务端时，客户端所期望的接口与服务端提供的接口不兼容的情况。

例如，假设我们有前面讨论的 `engage-switch` 函数，但我们想将一个第三方的 `:variable-light` 传递给它。`:variable-light` 的 `turn-on-light` 函数接受一个用于控制灯光亮度的参数：0 表示关闭，100 表示全亮。

`:variable-light` 的接口与 `engage-switch` 函数的预期接口不匹配，因此我们需要一个适配器。

最简单的适配器形式可能如下所示：

```clojure
(defn turn-on-light [intensity]
  ;Turn it on with intensity.
  )

(defmulti turn-on :type)
(defmulti turn-off :type)

(defmethod turn-on :variable-light [switchable]
  (turn-on-light 100))

(defmethod turn-off :variable-light [switchable]
  (turn-on-light 0))

(defn engage-switch [switchable]
  ;Some other stuff...
  (turn-on switchable)
  ;Some more other stuff...
  (turn-off switchable))
```

我使用以下测试验证了这个适配器：

```clojure
(describe "Adapter"
  (with-stubs)
  (it "turns light on and off"
	(with-redefs [turn-on-light (stub :turn-on-light)]
	  (engage-switch {:type :variable-light})
	  (should-have-invoked :turn-on-light {:times 1 :with [100]})
	  (should-have-invoked :turn-on-light {:times 1 :with [0]}))))
```

如果用 UML 绘制这个结构，大概会如图16.5所示。

![](asserts/16.5.png)

`defmulti` 函数对应于 `Switchable` 接口。`{:type :variable-light}` 对象与两个 `defmethod` 函数配合使用，对应于 `VariableLightAdapter`。`EngageSwitch` 和 `VariableLight`“类”对应于我们试图适配的两个函数。

也许你对此不以为然，毕竟这只是一个包含几个 `defmulti` 函数的小程序，并没有 UML 图中那样明显的面向对象结构。所以，让我们通过将源文件拆分来引入这样的结构。

我们先从 `switchable` 接口开始。在 `ns` 声明中，我使用了 `turn-on-light` 作为包含 `switchable` 命名空间的项目的总体命名空间：

```clojure
(ns turn-on-light.switchable)

(defmulti turn-on :type)
(defmulti turn-off :type)
```

这是一个多态接口。注意它没有源代码依赖。此外，请记住，在 Clojure 中，`ns` 声明对源文件有与 Java 类类似的要求，源文件和命名空间的名称必须对应[^5]。因此，当我们将代码的各个部分移入不同的命名空间时，也会将它们移入不同的源文件。

接下来，让我们看看 `engage-switch` 和 `variable-light` 命名空间：

```clojure
(ns turn-on-light.engage-switch
  (:require [turn-on-light.switchable :as s]))

(defn engage-switch [switchable]
  ;Some other stuff...
  (s/turn-on switchable)
  ;Some more other stuff...
  (s/turn-off switchable))


(ns turn-on-light.variable-light)
(defn turn-on-light [intensity]
  ;Turn it on with intensity.
  )
```

这里并没有什么真正的意外。`engage-switch` 命名空间依赖于 `switchable` 接口，`variable-light` 命名空间则没有外部源代码依赖。

而 `variable-light-adapter` 命名空间将 `switchable` 接口与 `variable-light` 连接起来。请注意其中的 `make-adapter` 构造函数，测试会使用它：

```clojure
(ns turn-on-light.variable-light-adapter
  (:require [turn-on-light.switchable :as s]
			[turn-on-light.variable-light :as v-l]))
  (defn make-adapter []
	{:type :variable-light})

(defmethod s/turn-on :variable-light [switchable]
  (v-l/turn-on-light 100))

(defmethod s/turn-off :variable-light [switchable]
  (v-l/turn-on-light 0))
```

最后，测试通过依赖所有的具体命名空间，将所有部分整合在一起：

```clojure
(ns turn-on-light.turn-on-spec
  (:require [speclj.core :refer :all]
            [turn-on-light.engage-switch :refer :all]
            [turn-on-light.variable-light :as v-l]
            [turn-on-light.variable-light-adapter
			  :as v-l-adapter]))

(describe "Adapter"
  (with-stubs)
  (it "turns light on and off"
	(with-redefs [v-l/turn-on-light (stub :turn-on-light)]
      (engage-switch (v-l-adapter/make-adapter))
      (should-have-invoked :turn-on-light
						   {:times 1 :with [100]}
	  (should-have-invoked :turn-on-light
						   {:times 1 :with [0]}))))
```

查看这些源代码依赖，并将其与 UML 图进行比较，你会发现它们完全匹配。

那么，这是哪种形式的适配器模式呢？我们可以称其为“多方法”形式，但它也是“对象”形式。

在 Clojure 中构建适配器模式的“类”形式是否可能？不可能，因为 Clojure 没有实现的继承，而类形式的适配器模式依赖于此。

因此，尽管适配器模式并不依赖于特定语言，但其不同形式确实受语言限制。例如，在 Java 中无法创建多方法形式的适配器模式。

#### 这真的是一个适配器对象吗？

或许你认为，由于 `variable-light-adapter` 中唯一的数据元素是 `:type`，它还不够资格被称为对象。那么，这里有一个稍微不同的 `variable-light-adapter` 版本，可能更具说服力：

```clojure
(ns turn-on-light.variable-light-adapter
  (:require [turn-on-light.switchable :as s]
			[turn-on-light.variable-light :as v-l]))

(defn make-adapter [min-intensity max-intensity]
  {:type :variable-light
   :min-intensity min-intensity
   :max-intensity max-intensity})

(defmethod s/turn-on :variable-light [variable-light]
  (v-l/turn-on-light (:max-intensity variable-light)))

(defmethod s/turn-off :variable-light [variable-light]
  (v-l/turn-on-light (:min-intensity variable-light)))


(ns turn-on-light.turn-on-spec
  (:require [speclj.core :refer :all]
            [turn-on-light.engage-switch :refer :all]
            [turn-on-light.variable-light :as v-l]
            [turn-on-light.variable-light-adapter
				:as v-l-adapter]))

(describe "Adapter"
  (with-stubs)
  (it "turns light on and off"
	(with-redefs [v-l/turn-on-light (stub :turn-on-light)]
	  (engage-switch (v-l-adapter/make-adapter 5 90))
	  (should-have-invoked :turn-on-light
						   {:times 1 :with [90]})
      (should-have-invoked :turn-on-light
						   {:times 1 :with [5]}))))
```

现在，你应该确信这就是适配器模式，直接出自 GOF[^6] 书籍。此外，你应该开始期待许多其他的 GOF 模式也可以在像 Clojure 这样的函数式语言中实现。而更重要的是，你应该开始考虑将命名空间和源文件结构视为函数式程序设计和架构的一部分。

### 命令模式

在 GOF 书籍的所有设计模式中，命令模式（Command）最让我着迷。并不是因为它复杂，而是因为它非常简单，极其简单。

顺带一提，这也是让我对 Clojure 着迷的原因之一。正如我在本书开头所说，Clojure 语义丰富但语法简单。而命令模式也具备相同的特质：它的丰富性在于其极致的简单。

在 C++ 中，我们可以像下面这样实现命令模式：

```cpp
class Command {
public:
    virtual void execute() = 0;
};
```

就这样，一个抽象类（接口）包含一个纯虚（抽象）函数。如此简单，但你可以用这个模式做许多有趣的事情。关于这方面的深入探讨，请参考《敏捷软件开发：原则、模式与实践》的相关章节。[^7]

在像 Clojure 这样的函数式语言中，你可能认为这个模式就此消失了。毕竟，如果你想传递一个命令给其他函数，只需要传递命令函数就好了，不需要将其封装成对象，因为在函数式语言中，函数本身就是对象：

```clojure
(ns command.core)

(defn execute []
  )

(defn some-app [command]
  ;Some other stuff. . .
  (command)
  ;Some more other stuff. . .
  )


(ns command.core-spec
  (:require [speclj.core :refer :all]
			[command.core :refer :all]))

(describe "command"
  (with-stubs)
  (it "executes the command"
	(with-redefs [execute (stub :execute)]
	  (some-app execute)
	  (should-have-invoked :execute))))
```

> 细心的读者可能会发现，这里的命令并不是纯函数（即没有引用透明性）。然而，可以清楚地看到，纯函数也可以按照这种方式传递。
>
> **`with-redefs`**：重定义 `execute` 函数，使其成为一个 stub。这一行代码的作用是将 `command.core/execute` 替换为 `(stub :execute)`，从而创建一个模拟函数 `:execute`，用于监控 `execute` 是否被调用。

如上所示，测试将 `execute` 函数传递给 `some-app`，然后 `some-app` 调用该命令。没什么特别的。

那么，如果你想创建一个带有数据元素的命令，并将该数据作为参数传递给 `execute` 函数呢？在 C++ 中，我们会这样做（请原谅我使用内联函数）：

```c++
class CommandWithArgument : public Command {
public:
	CommandWithArgument(int argument)
	:argument(argument)
	{}
	
	virtual void execute()
	{theFunctionToExecute(argument);}

private:
int argument;

    void theFunctionToExecute(int argument)
    {
    //do something with that argument!
    }
};
```

在 Clojure 中，我们可以这样做，再次证明在函数式语言中，函数实际上就是对象：

```clojure
(defn execute-command [command]
  (command))

(execute-command (fn [state] (println "Executing command with state:" state)))
```

#### 撤销功能

命令模式的一个更有用的变体可以通过以下 C++ 代码看出：

```cpp
class UndoableCommand : public Command {
public:
  virtual void undo() = 0;
};
```

这个 `undo()` 函数带来了许多有趣的可能性。

很久以前，我曾参与开发一个类似 AutoCAD 的 GUI 应用程序。它是一个绘图工具，用于建筑平面图、屋顶图、地界图等。GUI 采用典型的调色板/画布模式，用户可以在调色板中点击选择他们想要的功能，例如“添加房间”，然后在画布上点击来确定位置和大小。

每次在调色板中点击时，都会实例化并执行一个 `UndoableCommand` 的派生类对象。执行过程管理画布上的鼠标/键盘手势，然后对内部数据模型进行相应的修改。因此，调色板中的每个不同功能都有一个相应的 `UndoableCommand` 派生类。

当一个 `UndoableCommand` 对象完成执行后，它会被推入撤销栈。每当用户点击调色板中的撤销图标时，撤销栈顶部的 `UndoableCommand` 被弹出并调用其 `undo` 函数。

在执行 `UndoableCommand` 对象时，它会记录所做的操作，以便 `undo` 函数可以逆转这些更改。在 C++ 中，这种记录保存在特定的 `UndoableCommand` 对象的成员变量中：

```cpp
class AddRoomCommand : public UndoableCommand {
    public:
        virtual void execute() {
        // manage canvas events to add room
        // record what was done in theAddedRoom
        }
        virtual void undo() {
        // remove theAddedRoom from the canvas
        }
    private:
        Room* theAddedRoom;
};
```

这并非函数式，因为 `AddRoomCommand` 对象是可变的。但在函数式语言中，我们可以让 `execute` 函数创建一个新的 `UndoableCommand` 实例。类似于这样：

```clojure
(ns command.undoable-command)

(defmulti execute :type)
(defmulti undo :type)


(ns command.add-room-command
  (:require [command.undoable-command :as uc]))

(defn add-room []
  ;stuff that adds rooms to the canvas
  ;and returns the added room
)

(defn delete-room [room]
  ;stuff that deletes the specified room from the
  )

(defn make-add-room-command []
  {:type :add-room-command})

(defmethod uc/execute :add-room-command [command]
  (assoc (make-add-room-command) :the-added-room (and-room)))

(defmethod uc/undo :add-room-command [command]
  (delete-room (:the-added-room command)))


(ns command.core
  (:require [command.undoable-command :as uc]
			[command.add-room-command :as ar]))

(defn gui-app [actions]
  (loop [actions actions
		 undo-list (list)]
	(if (empty? actions)
	  :DONE
	  (condp = (first actions)
	  	:add-room-action
		(let [executed-command (uc/execute
								(ar/make-add-room-command))]
		  (recur (rest actions)
				 (conj undo-list executed-command
          :undo-action
          (let [command-to-undo (first undo-list)]
            (uc/undo command-to-undo)
            (recur (rest actions)
                    (rest undo-list)))
           :TILT))))

(ns command.core-spec
(:require [speclj.core :refer :all]
          [command.core :refer :all]
          [command.add-room-command :as ar]))
          
(describe "command"
  (with-stubs)
  (it "executes the command"
	(with-redefs [ar/add-room (stub :add-room {:return :a-room})
				  ar/delete-room (stub :delete-room)]
	  (gui-app [:add-room-action :undo-action])
	  (should-have-invoked :add-room)
	  (should-have-invoked :delete-room {:with [:a-room]})))
```

我们通过 `defmulti` 函数创建 `undoable-command` 接口。在 `add-room-command` 命名空间中实现该接口，并在 `command.core` 命名空间的 `gui-app` 函数中模拟 GUI。

测试中会替换掉 `add-room-command` 的底层函数，并确保它们被正确调用。它会使用一个 `palette-actions` 列表调用 `gui-app`。

`add-room-command` 的两个方法通过多态方式调度。对于 `execute` 方法，这种多态调度看似并不必要，因为 `gui-app` 刚刚创建了 `add-room-command` 对象。但如果我们向系统中添加更多命令，那么 `execute` 的多态调度会变得更有必要。

而对于 `undo` 的多态调度，即使在这个小示例中也是显然必要的，因为在从调色板接收到 `:undo-action` 时，我们不知道要撤销的是哪个命令。

在这里我们再次看到，随着应用程序复杂度的增加，GOF 模式的标准形式开始显现出来。在单一方法命令的情况下，我们可以简单地使用普通函数（实际上是函数对象）。但当应用程序需要更丰富的命令类型时，我们回归到 GOF 风格。

### 组合模式

组合模式（Composite）延续了语义丰富和语法简单的主题。这是一个绝佳的“句柄/主体”方法的例子，我第一次在 Jim Coplien 的书[^9]中读到这个方法。组合模式的结构在图 16.6 的 UML 图中展示。

![](asserts/16.6.png)

我们的老朋友 `Switchable` 接口由其他熟悉的朋友 `Light` 和 `VariableLight` 实现。而 `CompositeSwitchable` 也实现了 `Switchable` 接口，并包含了一个 `Switchable` 实例的列表。

`CompositeSwitchable` 中 `TurnOn` 和 `TurnOff` 的实现只是将相同函数的调用传递给列表中的所有实例。因此，当你在一个 `CompositeSwitchable` 实例上调用 `TurnOn` 时，它会调用它所包含的所有 `Switchable` 实例的 `TurnOn` 方法。

在 Java 中，我们可以这样实现 `CompositeSwitchable`：

```java
public class CompositeSwitchable implements Switchable {
    private List<Switchable> switchables = new ArrayList<>();

    public void addSwitchable(Switchable switchable) {
        switchables.add(switchable);
    }

    @Override
    public void turnOn() {
        for (Switchable switchable : switchables) {
            switchable.turnOn();
        }
    }

    @Override
    public void turnOff() {
        for (Switchable switchable : switchables) {
            switchable.turnOff();
        }
    }
}
```

在像 Clojure 这样的函数式语言中，我们可能会倾向于避免使用组合模式，而是简单地使用 `map` 或 `doseq` 函数，正如下面的测试所示：

```clojure
(ns composite-example.switchable)

(defmulti turn-on :type)
(defmulti turn-off :type)

(ns composite-example.light
  (:require [composite-example.switchable :as s])

(defn make-light [] {:type :light})

(defn turn-on-light [])
(defn turn-off-light [])

(defmethod s/turn-on :light [switchable]
  (turn-on-light))

(defmethod s/turn-off :light [switchable]
  (turn-off-light))


(ns composite-example.variable-light
  (:require [composite-example.switchable :as s])

(defn make-variable-light [] {:type :variable-light})

(defn set-light-intensity [intensity])

(defmethod s/turn-on :variable-light [switchable]
  (set-light-intensity 100))

(defmethod s/turn-off :variable-light [switchable]
  (set-light-intensity 0))


(ns composite-example.core-spec
  (:require [speclj.core :refer :all]
			[composite-example
            [light :as l]
            [variable-light :as v]
            [switchable :as s]]))

(describe "composite-switchable"
  (with-stubs)
  (it "turns all on"
	(with-redefs
	  [l/turn-on-light (stub :turn-on-light)
	   v/set-light-intensity (stub :set-light-intensity)]
	  (let [switchables [(l/make-light) (v/make-variable-light)]]
		(doseq [s-able switchables] (s/turn-on s-able))
        (should-have-invoked :turn-on-light)
        (should-have-invoked :set-light-intensity
							{:with [100]})))))
```

这样做可以实现点亮所有灯的目标，但代价是将“灯”的复数概念外部化了。组合模式的意义在于隐藏这种复数性。因此，让我们使用实际的组合模式：

`composite-switchable` 实现了 `switchable` 接口。`add` 函数是函数式的，它返回一个新的 `composite-switchable`，其中添加了新的 `:switchables` 列表项。`turn-on` 和 `turn-off` 方法使用 `doseq` 遍历 `:switchables` 列表，并传播相应的函数调用。最后，测试中创建了 `composite-switchable`，添加了一个灯和一个 `variable-light`，然后调用 `turn-on`，我们可以看到两个灯都被成功点亮。

#### 函数式？

此时，你可能会认为组合模式非常适合具有副作用的对象，例如灯和可变灯。的确，整个 `switchable` 接口都是围绕着开关操作的副作用而设计的。那么这个模式是否仅适用于有副作用的对象呢？

让我们考虑一个看起来像这样的形状抽象：

```clojure
(ns composite-example.shape
  (:require [clojure.spec.alpha :as s]))

(s/def ::type keyword?)
(s/def ::shape-type (s/keys :req [::type]))

(defmulti translate (fn [shape dx dy] (::type shape)))
(defmulti scale (fn [shape factor] (::type shape)))
```

这是一个简单的接口，包含两个方法：`translate` 和 `scale`。我还添加了类型规范以增强安全性。（此时复习一下双冒号 `::` 语法，表示命名空间关键字会是个好主意。）每个形状都是一个包含 `::shape/type` 元素的 map。

`circle` 和 `square` 的实现也很简单，包括它们的类型规范：

```clojure
(ns composite-example.circle
  (:require [clojure.spec.alpha :as s]
			[composite-example.shape :as shape]))

(s/def ::center (s/tuple number? number?))
(s/def ::radius number?)
(s/def ::circle (s/keys :req [::shape/type
                              ::radius
                              ::center]))

(defn make-circle [center radius]
  {:post [(s/valid? ::circle %)]}
  {::shape/type ::circle
                ::center center
                ::radius radius})

(defmethod shape/translate ::circle [circle dx dy
  {:pre [(s/valid? ::circle circle)
		(number? dx) (number? dy)]
   :post [(s/valid? ::circle %)]}
  (let [[x y] (::center circle)]
    (assoc circle ::center [(+ x dx) (+ y dy)])))
                                     
(defmethod shape/scale ::circle [circle factor]
  {:pre [(s/valid? ::circle circle)
		 (number? factor)]
   :post [(s/valid? ::circle %)]}
  (let [radius (::radius circle)]
    (assoc circle ::radius (* radius factor))))
                                     
(ns composite-example.square
  (:require [clojure.spec.alpha :as s]
			[composite-example.shape :as shape]))
                                     
(s/def ::top-left (s/tuple number? number?))
(s/def ::side number?)
(s/def ::square (s/keys :req [::shape/type
                              ::side
                              ::top-left]))
                                     
(defn make-square [top-left side]
  {:post [(s/valid? ::square %)]}
  {::shape/type ::square
   ::top-left top-left
   ::side side})
                                     
(defmethod shape/translate ::square [square dx dy]
  {:pre [(s/valid? ::square square)
		 (number? dx) (number? dy)]
   :post [(s/assert ::square %)]}
  (let [[x y] (::top-left square)] ;;解构 square 对象中的 ::top-left 属性，得到 x 和 y 坐标。
	(assoc square ::top-left [(+ x dx) (+ y dy)]) ;;创建一个新的映射，将 square 的 ::top-left 值更新为偏移后的新坐标 [(+ x dx) (+ y dy)]
      
(defmethod shape/scale ::square [square factor]
  {:pre [(s/valid? ::square square)
		 (number? factor)]
   :post [(s/valid? ::square %)]}
  (let [side (::side square)]
	(assoc square ::side (* side factor))))
```

注意方法上的 `:pre` 和 `:post` 条件。我使用这些来检查传入和传出函数的类型。你可能会担心这些检查会带来运行时开销。一旦我确认类型管理妥当，我要么全局禁用[^10]它们，要么有选择地注释掉它们。

注意到 `translate` 和 `scale` 函数会返回新的形状实例。它们在行为上是完全函数式的。



[^1]: 这一领域的权威著作为 Erich Gamma、Richard Helm、Ralph Johnson 和 John Vlissides 所著的《设计模式：可复用面向对象软件的基础》（Addison-Wesley，1994年）。
[^2]: `comp.object` 是在网络新闻传输协议（NNTP）下的一个新闻组，借助 Unix-to-Unix 复制协议（UUCP）和互联网传播。
[^3]: 请记住，这是在一个面向对象的论坛上，别被“类”这个词困住了。
[^4]: Robert C. Martin，《敏捷软件开发：原则、模式与实践》（Pearson，2002），第318页。
[^5]: 特别是，`turn-on-light.switchable` 命名空间必须位于一个名为 `turn_on_light` 的目录中的 `switchable.clj` 文件中。
[^6]: GOF 是我们在 90 年代对《设计模式》一书的昵称，意为“Gang of Four”（四人组），因为该书的四位作者是 Erich Gamma、John Vlissides、Ralph Johnson 和 Richard Helm。
[^7]: Martin，《敏捷软件开发》，第181页。
[^9]: James O. Coplien，《高级 C++ 编程风格与惯用法》（Addison-Wesley, 1991）。
[^10]: 编译时可以通过开关禁用所有的断言，包括 `:pre` 和 `:post`。