# 函数式概念

## 函子-functor



## 单子-monad

单子组合：有两个函数

f: A -> B

g: B -> C

我们可以通过管道组合的方式，将f函数的输出结果作为g函数的输入，这样我们就得到了另一个函数：

g': A -> C

还有两个函数

f'': A -> (B, K)

g'': B -> (C, K)

那么同样可以通过组合的方式，将函数f''的结果提取B，然后调用函数g''，这样就组成了新的函数：

g: A -> (C, K)

这就是单子组合。

## 偏函数应用-partial-function-application（PFA）

在计算机科学中，偏函数应用是指在一个函数中固定一些参数，产生另一个更小的函数的过程。给定一个函数：$f=(x \times y \times z) \rightarrow N$，我们继续修正（绑定）第一个参数，就会产生一个新的函数：$partial(f):(Y \times Z)\rightarrow N$。我们将这个函数表示为$f_{partial}(2,3)$。注意，在本例中，偏函数应用的结果是一个带有两个参数的函数。注意偏函数不同于柯里化（curring），这两者概念是不同的。

举个例子：给定一个函数：$div(x,y) = x/y$，固定参数x固定为1的div是另一个函数：$div_1(y)=div(1,y)=1/y$。这个函数与`inv`函数是相同的，即`inv`函数也返回其参数乘法的逆：$inv(y)=1/y$

详见[偏函数](https://en.wikipedia.org/wiki/Partial_application)

## 应用函子-applicative functor

在函数式编程语言里，是介于函子（functor）和单子（monad）之间的一种中间结构。应用函子允许函子计算是有序的(不像普通函子)，但不允许使用之前的计算结果在后续的定义(不像函子)。但不允许在后续计算的定义中使用先前计算的结果（不像单子）。应用函子是范畴论中具有张量强度的宽松的monoidal函子的编程等价物。

详见：[应用函子](https://en.wikipedia.org/wiki/Applicative_functor)
