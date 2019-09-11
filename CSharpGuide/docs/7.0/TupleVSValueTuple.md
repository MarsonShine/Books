# ﻿深入理解 c# 元组与值元组（Tuple，ValueTuple）

## 为什么有此文章

首先要说的是我们公司内部技术框架是用 abp.vnext 框架整合而来的，我们架构师对于 abp 相关的知识都很了然于胸了。并且这个框架的确很优秀，省了我们前期大量基础工作。架构师把主要的架子搭建好了之后，把应用层与核心层让我们去实现，并让我们好熟悉这个框架。

就在我们在讨论代码规范相关的东西，就说到了值元祖这个点，并提议不要在代码中用元组。我当时听了之后觉得疑惑，为什么不能用元组呢？元组的确很方便啊，特别是 C#7.0 之后支持元组解构，代码阅读性，美观度双双提升。他是说元组在取值的时候会发生装箱，会有性能损耗。再者值元组跟之前的 Tuple 不同，前者是一个结构体，后者则是引用类型，在用值元组的时候会不利于垃圾回收（具体是说 Ioc 管理的生命周期与我在用的值元组变量的生命周期会有矛盾）。

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

我们 F12 到 ITuple 进去看看具体成员信息

```c#
object? this[int? index] { get; }

int? Length { get; }
```

不过我们在暴露出来的 API 中没有看到这两个实现属性，说明这个实现类的这这两个属性只是内部实现用的，不会给我们开发者用（当然，我们可以选择强转来使用接口的这两个属性）。这从源码也是可以很容易知道的：

```c#
[Nullable(2)]
object ITuple.this[int index]
{
	get
    {
        if(index != 0){
            throw new IndexOutOfRangeException();
        }
        return Item1;
    }
}
int ITuple.Length
{
    get{
		return 1;	//如果 ValueTuple 多个参，则是 switch case
    }
}
```

当我在官网看到 ValueTuple 有两个属性是实现接口 ITuple 的，并且 `ITuple.Item[Int32]` 返回的是一个 `object` 对象，我下意识的反映就是难道真的会发生装箱么？仔细想想其实完全不是这样，如果是发生装箱的话，那么这个 ValueTuple 泛型就是一个多余的东西，那就跟 java 中的泛型擦除效果一样了，只是起到了一个编译期的检测作用，不能做到实质的性能提升。

其实再仔细查看便会发现，我们平常引用的 ValueTuple 、Tuple 实例对象引用的 Item1、Item2 等值实际上是字段而不是属性，而这些字段在你初始化或用 `Tuple.Creat,ValueTuple.Create` 函数创建的元组 / 值元组对象时，类型以及 Item 的个数以及值就已经确定了。所以根本不会发生装箱。这一点我们从 IL 代码中就能从中得知

在看 IL 之前我们先来看与 IL 对应的 C# 代码

```c#
var t = ValueTuple.Create(2, 3);
Console.WriteLine(t.Item1);
Console.WriteLine(t.Item2);
Console.WriteLine($"Item1 = {t.Item1}, Item2= ${t.Item2}");
```

IL 代码：

```c#
.method private hidebysig static 
    void Main (
        string[] args
    ) cil managed 
{
    // Method begins at RVA 0x2094
    // Code size 72 (0x48)
    .maxstack 3
    .entrypoint
    .locals init (
        [0] valuetype [System.Runtime]System.ValueTuple`2<int32, int32> t
    )

    IL_0000: nop
    IL_0001: ldc.i4.2
    IL_0002: ldc.i4.3
    IL_0003: call valuetype [System.Runtime]System.ValueTuple`2<!!0, !!1> [System.Runtime]System.ValueTuple::Create<int32, int32>(!!0, !!1)
    IL_0008: stloc.0
    IL_0009: ldloc.0
    IL_000a: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
    IL_000f: call void [System.Console]System.Console::WriteLine(int32)
    IL_0014: nop
    IL_0015: ldloc.0
    IL_0016: ldfld !1 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item2
    IL_001b: call void [System.Console]System.Console::WriteLine(int32)
    IL_0020: nop
    IL_0021: ldstr "Item1 = {0}, Item2= ${1}"
    IL_0026: ldloc.0
    IL_0027: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
    IL_002c: box [System.Runtime]System.Int32
    IL_0031: ldloc.0
    IL_0032: ldfld !1 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item2
    IL_0037: box [System.Runtime]System.Int32
    IL_003c: call string [System.Runtime]System.String::Format(string, object, object)
    IL_0041: call void [System.Console]System.Console::WriteLine(string)
    IL_0046: nop
    IL_0047: ret
} // end of method Program::Main
```

这样我们就能很清楚的知道元组里面的细节了，我们平常取的都是元组 / 值元组的字段，并且 Main 函数开头的 `managed` 标识就代表这是托管资源。值得注意的是 IL_002c 处的装箱只是由于 Console.WriteLine 导致的装箱。

## 元组解构

我们知道 C#7 支持了元组结构了，可以支持我们对元组字段 Item 进行表意话，这样更能提高阅读性和代码美观。那么元组结构跟之前直接引用的字段值变量 Item 有什么区别呢？这一点我们也可以直接从 IL 上轻易得知。

```c#
var (pd, id) = ValueTuples.Create(2, 3);
Console.WriteLine(pd);
Console.WriteLine(id);
Console.WriteLine($"元组解构：Item1 = {pd}, Item2= ${id}");

//IL
// Methods
.method private hidebysig static 
    void Main (
        string[] args
    ) cil managed 
{
    // Method begins at RVA 0x2094
    // Code size 64 (0x40)
    .maxstack 3
    .entrypoint
    .locals init (
        [0] int32 pd,
        [1] int32 id
    )

    IL_0000: nop
    IL_0001: ldc.i4.2
    IL_0002: ldc.i4.3
    IL_0003: call valuetype [System.Runtime]System.ValueTuple`2<!!0, !!0> CSharpGuide.LanguageVersions._7._0.ValueTuples::Create<int32>(!!0, !!0)
    IL_0008: dup
    IL_0009: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
    IL_000e: stloc.0
    IL_000f: ldfld !1 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item2
    IL_0014: stloc.1
    IL_0015: ldloc.0
    IL_0016: call void [System.Console]System.Console::WriteLine(int32)
    IL_001b: nop
    IL_001c: ldloc.1
    IL_001d: call void [System.Console]System.Console::WriteLine(int32)
    IL_0022: nop
    IL_0023: ldstr "元组解构：Item1 = {0}, Item2= ${1}"
    IL_0028: ldloc.0
    IL_0029: box [System.Runtime]System.Int32
    IL_002e: ldloc.1
    IL_002f: box [System.Runtime]System.Int32
    IL_0034: call string [System.Runtime]System.String::Format(string, object, object)
    IL_0039: call void [System.Console]System.Console::WriteLine(string)
    IL_003e: nop
    IL_003f: ret
} // end of method Program::Main
```

发现了没有，这段 IL 与之前的一模一样，没任何区别。

## ITuple

如果你想用把元组转换成 `ITuple` 类型，那么取的值就一定会发生装箱，因为 Item 是一个 `object` 类型。我们能从这个类获知这个元组有多少个值，能通过索引遍历所有的值。除此之外，这个类并没有其他使用场景了。

# 总结

`Tuple`、`ValueTuple` 平常的使用完全不用担心 Item 值的装箱，因为根本不会发生装箱拆箱。元组解构生成的代码跟之前直接引用元组是没任何区别的。只是编译器增加这么一个功能，给 item 命名的功能而已。如果你想要遍历这个元组对象的值的话，那么就建议转化成 ITuple 进一步操作。