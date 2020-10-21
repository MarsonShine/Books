# C# 范型约束 new() 你必须要知道的事

注意：本文不会讲范型如何使用，关于范型的概念和范型约束的使用请移步谷歌。

本文要讲的是关于范型约束无参构造函数 new 的一些底层细节和注意事项。写这篇文章的原因也是因为看到 github 上，以及其他地方看到的代码都是那么写的，而我一查相关的资料，发现鲜有人提到这方面的细节，所以才有了此文。

这里我先直接抛出一段代码，请大家看下这段代码有什么问题？或者说能说出什么问题？

```c#
public static T CreateInstance<T>() where T: new() => new T();
```

先不要想这种写法的合理性（实际上很多人都会诸如此类的这么写，无非就是中间多了一些业务处理，最后还是会 `return new T()`）。先想一下，然后在看下面的分析。

假设这样的问题出现在面试上，其实能有很多要考的点。

# 首先是范型约束的底层细节

如果说我们不知道范型底下到底做了什么操作，我们也不用急，我们可以用 ILSpy 来看查看一下，代码片段如下：

```c#
.method public hidebysig static 
    !!T CreateInstance<.ctor T> () cil managed 
{
    // Method begins at RVA 0x2053
    // Code size 6 (0x6)
    .maxstack 8
    
    IL_0000: call !!0 [System.Private.CoreLib]System.Activator::CreateInstance<!!T>()
    IL_0005: ret
} // end of method C::CreateInstance
```

> 没有 ILSpy 的同学可以移步[这里在线查看](https://sharplab.io/#v2:C4LglgNgNAJiDUAfAAgJgIwFgBQyDMABGgQMIEDeOB1RhyALAQLIAUAlBVTQL5fV+0i6AGwEAKqQBOAUwCGwaQEkAdgGdgs5QGNpAHjEA+dgQDuAC2kzxIAsuknjAXgO3749gG4c3IA=)

在 IL_0000 就能明显看出范型约束 new() 的底层实现是通过反射来实现的。至于 `System.Activator.CreateInstance<T>` 方法实现我在这里就不提了。只知道这里用的是它就足够了。不知道大家看到这里有没有觉得一丝惊讶，我当时是有被惊到的，因为我的第一想法就是觉得这么简单肯定是直接调用无参 .ctor，居然是用到的反射。毕竟编译器拥有在编译器就能识别具体的范型类了。现在可以马后炮的讲：**正因为是编译器只有在编译期才确定具体范型类型，所以编译器无法事先知道要直接调用哪些无参构造函数类，所以才用到了反射。**

如果本文仅仅只是这样，那我肯定没有勇气写下这片文章的。因为其实已经[有人早在 04 年园子里就提到了这一点](https://www.cnblogs.com/Hush/archive/2004/10/07/49674.html)。但是我查到的资料也就止步于此。

试想一下 ，如果你的框架中有些方法用到了无参构造函数范型约束，并且处于调用的热路径上，其实这样性能是大打折扣的，因为反射 `Activator.CreateInstance` 性能肯定是远远不如直接调用无参构造函数的。

那么有没有什么方法能够在使用范型约束这个特征的同时，又不会让编译器去用反射呢？

答案肯定是有的，这点我想喜欢动手实验肯定早就知道了。其实我们可以用到**委托来初始化类**。

# 范型约束 return new T() 的优化——委托

如果大家对这点都知道的话，可以略过本节（在这里鼓励大家可以写出来造福大家呀，对于这点那些不知道的人（我）要花很长时间才弄清楚 -_-）。

让我们把上面的例子改成如下方式：

```c#
public static Func<Bar> InstanceFactory => () => new Bar();
```

对于委托的底层相信大家还是都知道的，底层是通过生成一个类 C，在这个类中直接实例化类 Bar。下面我只贴出关键的代码片段

```c#
.method public hidebysig specialname static 
    class [System.Private.CoreLib]System.Func`1<class Bar> get_InstanceFactory () cil managed 
{
    // Method begins at RVA 0x205a
    // Code size 32 (0x20)
    .maxstack 8

    IL_0000: ldsfld class [System.Private.CoreLib]System.Func`1<class Bar> C/'<>c'::'<>9__3_0'
    IL_0005: dup
    IL_0006: brtrue.s IL_001f

    IL_0008: pop
    IL_0009: ldsfld class C/'<>c' C/'<>c'::'<>9'
    IL_000e: ldftn instance class Bar C/'<>c'::'<get_InstanceFactory>b__3_0'()
    IL_0014: newobj instance void class [System.Private.CoreLib]System.Func`1<class Bar>::.ctor(object, native int)
    IL_0019: dup
    IL_001a: stsfld class [System.Private.CoreLib]System.Func`1<class Bar> C/'<>c'::'<>9__3_0'

    IL_001f: ret
} // end of method C::get_InstanceFactory

.method assembly hidebysig 
    instance class Bar '<get_InstanceFactory>b__3_0' () cil managed 
{
    // Method begins at RVA 0x2090
    // Code size 6 (0x6)
    .maxstack 8

    IL_0000: newobj instance void Bar::.ctor()
    IL_0005: ret
} // end of method '<>c'::'<get_InstanceFactory>b__3_0'
```

> 同样我们可以通过 ILSpy 或者 [在线查看示例](https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQMwAJboMLoN7LpGYZQAs6AsgBQCU+hxAvo0ayZnAGzoAqOAJwCmAQwAuQgJIA7AM5iR0gMZCAPLwB8ddAHcAFkOF8Q6aUJ3aAvBtPm+dANzs0nHlACsqgEIiBNmfKKKgBiIkpiAPYCAJ7o1uhWNmY66D4CjsgsSMguWGnIBEhMQA==) 查看委托生成的代码。

这里可以明显看出是不存在反射调用的，IL_000e 处直接调用编译器生成的类 C 的方法 `b__3_0` ,在这个方法中就会直接调用类 Bar 的构造函数。所以性能上绝对要比上种写法要高得多。

看到这里可能大家又有新问题了，众所周知，委托要在初始化时就要确定表达式。所以与此处的范型动态调用是冲突的。的确没错，委托**必须要在初始化表达式时就要确定类型**。但是我们现在已经知道了委托是能够避免让编译器不用反射的，剩下的只是解决动态表达式的问题，毫无疑问表达式树该登场了。

# 范型约束 return new T() 的优化——表达式树

对于这部分已经知道的同学可以跳过本节。

把委托改造成表达式树那是非常简单的，我们可以不假思索的写出下面代码：

```c#
private static readonly Expression<Func<T>> ctorExpression = () => new T();
public static T CreateInstance() where T : new() {
  var func = ctorExpression.Compile();
  return func();
}
```

到这里其实就有点”旧酒装新瓶“的意思了。不过有点要注意的是，如果单纯只是表达式树的优化，从执行效率上来看肯定是不如委托来的快，毕竟表达式树多了一层构造表达式然后编译成委托的过程。优化也是有的，再继续往下讲就有点“偏题”了。因为往后其实就是对委托，对表达式树的性能优化问题。跟范型约束倒没关系了

# 总结

其实如果面试真的有问到这个问题的话，其实考的就是对范型约束 new() 底层的一个熟悉程度，然后转而从反射的点来思考问题的优化方案。因为这可以散发出很多问题，比如性能优化，从直接返回 `new T()` 到委托，因为委托无法做到动态变化，所以想到了表达式树。那么我们继而也能举一反三的知道，如果要继续优化的话，在构造表达式树时，我们可以用缓存来节省每次调用方法的构造表达式树的时间（DI 的 CallSite 实现细节就是如此）。如果我们生思熟虑之后还要选择继续优化，那么我们还可以从表达式树转到动态生成代码这一领域，通过编写 IL 代码来生成表达式树，进而缓存下来达到近乎直接调用的性能。这也是为什么我花了很长时间弄清楚这个的原因。

# 最后关于代码

代码地址在：https://github.com/MarsonShine/Books/tree/master/WHPerformanceDotNet/src/GenericOptimization
注意：我上传这一版是下方第一个文章给出的例子的整理之后的版本。建议大家还是自己动手编码自己的版本，最后才看与那些大牛写的版本的差距在哪

# 参考资料

- https://devblogs.microsoft.com/premier-developer/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/
- https://alexandrnikitin.github.io/blog/dotnet-generics-under-the-hood/
- https://www.microsoft.com/en-us/research/wp-content/uploads/2001/01/designandimplementationofgenerics.pdf
- 《编写高性能.NET代码》



# 