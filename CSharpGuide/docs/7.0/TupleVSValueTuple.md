# ﻿深入理解 c# 元组与值元组（Tuple，ValueTuple）

## 为什么有此文章

首先要说的是我们公司内部技术框架是用 abp.vnext 框架整合而来的，我们架构师对于 abp 相关的知识都很了然于胸了。并且这个框架的确很优秀，省了我们前期大量基础工作。架构师把主要的架子搭建好了之后，把应用层与核心层让我们去实现，并让我们好熟悉这个框架。

就在前天架构师过来跟我说了代码规范相关的东西，建议我们不要在代码中用元组。我当时听了之后觉得疑惑，为什么不能用元组呢？元组的确很方便啊，特别是 C#7.0 之后支持元组解构，代码阅读性，美观度双双提升。他是说元组在取值的时候会发生装箱，会有性能损耗。再者值元组跟之前的 Tuple 不同，前者是一个结构体，后者则是引用类型，在用值元组的时候会不利于垃圾回收（具体是说 Ioc 管理的生命周期与我在用的值元组变量的生命周期会有矛盾）。

在最开始的话，我并没有这么考虑，因为我心里想着是这样的：

1. `Tuple<T>` 和 `ValueTuple<T>` 是泛型的，是不会发生装箱的（这点在我查看了源代码以及 IL 发现很有意思，后面会有提到）
2. `ValueTuple<T>` 是值对象没错，内存分配在栈中，但还是属于托管资源，CLR 会管理好每个变量的生命周期的，会确保值类型变量在当前作用域无任何引用时会释放资源。比如我在程序中是新建的局部变量，那么哪怕是这个变量未引用，已经引用过后再无引用，CLR 都会自动回收这个局部变量。

而后我查看 [Tuple](https://docs.microsoft.com/en-us/dotnet/api/system.tuple?view=netcore-3.0) 和 [ValueTuple](https://docs.microsoft.com/en-us/dotnet/api/system.valuetuple?view=netcore-3.0) 的api，心情可谓是一波三折啊。所以在有此文章

## ValueTuple

先来看 ValueTuple，查看其成员信息如下：

```c#
public struct ValueTuple : IStructuralComparable, IStructuralEquatable, IComparable, IComparable<ValueTuple>, IEquatable<ValueTuple>, ITuple
```

这里面有一个成员信息特别扎眼，那就是 `ITuple` 类，因为其他的接口都是跟判断相等性相关的。不在我们这次的讨论范围。

