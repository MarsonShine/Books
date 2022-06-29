# 函数式概念

## 函子-functor

## 单子-monad

## 偏函数应用-partial-function-application（PFA）

在计算机科学中，偏函数应用是指在一个函数中固定一些参数，产生另一个更小的函数的过程。给定一个函数：$f=(x \times y \times z) \rightarrow N$，我们继续修正（绑定）第一个参数，就会产生一个新的函数：$partial(f):(Y \times Z)\rightarrow N$。我们将这个函数表示为$f_{partial}(2,3)$。注意，在本例中，偏函数应用的结果是一个带有两个参数的函数。注意偏函数不同于柯里化（curring），这两者概念是不同的。

举个例子：给定一个函数：$div(x,y) = x/y$，固定参数x固定为1的div是另一个函数：$div_1(y)=div(1,y)=1/y$。这个函数与`inv`函数是相同的，即`inv`函数也返回其参数乘法的逆：$inv(y)=1/y$

详见[偏函数](https://en.wikipedia.org/wiki/Partial_application)

