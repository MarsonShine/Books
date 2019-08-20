# 非空引用类型——C#8.0﻿

该新增的特性最关键的作用是处理泛型和更高级 API 的使用场景。这些都是我们从 .NETCore 上注解衍生过来的经验。

## 通用不为 NULL 约束

通常的做法是不允许泛型类型为 NULL。我们给出下面代码：

```c#
interface IDoStuff<Tin, Tout>
{
	Tout DoStuff(Tin input);
}
```

这种做法对为空引用和值类型也许令人满意的。也就是说对 `string` 或者 `or` 来说很好，但是对 `string?` 或 `or` 却不是。

这样可以通过 `notnull` 约束来实现。

```c#
interface IDoStuff<Tin, Tout>
	where Tin: notnull
	where Tout: notnull
{
	Tout DoStuff(Tin input);
}
```

像下面这样的实现类如果没有应用相同的泛型约束还是会生成一个警告。

```c#
//警告：CS8714 - 可空类型参数 TIn 无法匹配 notnull 约束
//警告：CS8714 - 可空类型参数 TOut 无法匹配 notnull 约束
public class DoStuffer<TIn, TOut>: IDoStuff<TIn, TOut>
{
	public TOut DoStuff(TIn input)
	{
		...
	}
}
```

修改成下面版本则无此问题

```c#
// No warnings!
public class DoStuffer<TIn, TOut>: IDoStuff<TIn, TOut>
	where TIn : notnull
	where TOut : notnull
{
	public TOut DoStuff(TIn input)
	{
		...
	}
}
```

当创建这个类的实例时，如果你用可空引用类型时，也会生成一个警告

```c#
//警告：CS8714 - 可空类型参数 string? 无法匹配 notnull 约束
var doStuffer = new DoStuff<string?,string?>();
// no wanings
var doStufferRight = new DoStuff<string,string>();
```

针对于值类型同样如此：

```c#
// Warning: CS8714 - 类型参数 'int?' 是可为 null 的 无法匹配 'notnull' 约束
var doStuffer = new DoStuff<int?, int?>();

// No warnings!
var doStufferRight = new DoStuff<int, int>();
```

> 注意：如果有人编写如上代码没有发生警告，是因为在解决方案下没有开启可空验证，具体在 `.csproj` 配置如下：
>
> ```xml
> <Project>
> 	<PropertyGroup>
>     <CheckForOverflowUnderflow>false</CheckForOverflowUnderflow>
>     <LangVersion>8.0</LangVersion>
>     <!-- New name -->
>     <Nullable>enable</Nullable>
>     
>     <!-- Old name while we wait for new name to be everywhere -->
>     <NullableContextOptions>enable</NullableContextOptions>
>     <Features>strict</Features>
>   </PropertyGroup>
> </Project>
> ```

这个约束条件对于泛型代码来说是很有用的，当你想要确保使用一个不为 null 的引用类型。一个显著的例子是 `Dictionary<TKey,TValue>` 其中的 `TKey` 现在被约束为 `notnull` 它不允许使用为 null 的类型作为 key：

```c#
// Warning: CS8714 警告内容同上
var d1 = new Dictionary<string?, string>(10);
// 正因如此，不为空的键类型使用 null 作为 key 是会有一个警告
var d2 = new Dictionary<string, string>(10);
// Warning: CS8625 - 无法将 null 文本转换为不为 null 引用类型
var nothing = d2[null];
```

然而，不是所有的为 null 泛型问题都能通过这种方式解决。这里我们添加一些属性标签，来允许你们在编译器中影响可 null 分析。

## *T?*  的问题

你可能想知道：当指定一个可 null 引用或值类型的泛型类的时候，为什么不仅仅只用 `T?` 不久好了么？这个答案非常复杂。

从 `T?` 的定义上来看，意思就是说它是 “任何可为 null 类型”。但是，这就潜意识在暗示意味着 T 将是 “任何不可为 null 的类型”，这是错误的。这在今天用可 null 值类型的 T 是可能的（例如 `bool?`）。这是因为 T 早就是一个没有限制的反省类型。使用 T 作为一个无限制的泛型类，这个变化在语义上是不是期望的，并且在已经存在大量的代码中无疑是灾难。

在一个，值得注意的是可 null 引用类型与为 null 的值的类型是不等价的。Nullable 值类型具体映射到会生成一个类，在 .NET 中 `int?` 实际上 `Nullable<int>`。但是对于 `string?`，它实际上还是 `string`，但是编译器会生成鼠标标签来标记它。这样做是为了向后兼容。换句话说，`string?` 是一个“冒牌类型”（fake type），`int?` 却不是。

为了区分为 null 值类型预计为 null 引用类型，我们用如下模式：

```c#
void M<T>(T? t) where T: notnull
```

这段代码是说这个参数 `T` 为 null，并且 `T` 被约束为 `notnull`。如果 T 是一个 `string`，那么实际上 M 的签名方法将会成为 `M<string>([NullableAttribute] T t)`。但是如果 T 是个 `int`，M 则会变成 `M<int>(Nullable<int> t)`。这两个签名方法本质上就是不通过的，并且这种差异是互相矛盾的。

因为这个问题，null 引用类型以及 null 值类型这两者具体表现的差异，任何使用 `T?` 都必须要求你所引用的这个 T 的 `class` 和 `struct` 都要有这个约束。

最后，T? 的存在，都能在 null 值类型和 null 引用类型之间工作，但是不能解决泛型所有的问题。你也许想在单一方向用可 null 类型（比如只在输入或只在输出）并且它既不能用 `notnull` 也不能用 T 和 T? 来表示分开，除非你认为对于输入输出分离泛型类。

## 可 null 先决条件：AllowNull 和 DisallowNull

看下面这段代码

```c#
public class MyClass
{
    public string MyValue { get; set; }
}
```

这个 API 在 C#8.0 之前可能也是支持的。但是 `string` 的意思现在是不为 null 的 `string` ！我们也许希望实际上任然会允许 null 值，但是在 `get` 总是返回一些 `string` 的值。这个地方你就可以使用 `AllowNull`：

```c#
public class MyClass
{
    private string _innerValue = string.Empty;
    [AllowNull]
    public string MyValue
    {
        get
        {
            return _innerValue;
        }
        set
        {
            _innerValue = value ?? string.Empty;
        }
    }
}
```

这样我就能确保我们得到的值总是不为 null 的，并且我类型还是为 `string` 类型。但是为了向后兼容，我们还是想接受 null 值。`AllowNull` 标签就能让你特指这个 setter 接受 null 值。那样调用就能得到预期结果：

```c#
void M1(MyClass mc)
{
    mc.MyValue = null;	//没有 AllowNull 标签会有警告
}

void M2(MyClass mc)
{
    Console.WriteLine(mc.MyValue.Length); // ok, 注意这里没有警告
}
```

**注意**：这里存在一个 [bug](https://github.com/dotnet/roslyn/issues/37313) ，null 值的分配与 可为 null 的分析冲突了。这在编译器后续的更新中解决。

考虑下面代码：

```c#
public static class HandleMethods
{
    public static void DisposeAndClear(ref MyHandle handle)
    {
        ...
    }
}
```

在这个例子中，`MyHandle`引用某个资源的句柄。这个 API 的典型用法是我们有一个引用传递的不为 null 的实例，但是当它被清除时，这个引用就为 null 了。我们用 `DisallowNull` 来很好的表示：

```c#
public static class HandleMethods
{
    public static void DisposeAndClear([DisallowNull] ref MyHandle? handle)
    {
        ...
    }
}
```

这个效果就是任何调用者通过传递 null 值，那么就会引发一个警告，但是你企图用 . 来处理这个被调用的方法也将会发出警告：

```c#
void M(MyHandle handle)
{
    MyHandle? local = null; // Create a null value here
    HandleMethods.DisposeAndClear(ref local); // Warning: CS8601 - Possible null reference assignment
    
    // Now pass the non-null handle
    HandleMethods.DisposeAndClear(ref handle); // No warning! ... But the value could be null now
    
    Console.WriteLine(handle.SomeProperty); // Warning: CS8602 - Dereference of a possibly null reference
}
```

对于那些案例在我们需要的地方，这两个特性允许我们单方向可为 null 或不为 null。

更正式的讲：

`AllowNull` 标签允许调用者传递 null 值，甚至是如果这个类型不允许为 null。

`DisallowNull` 标签不允许调用者传递 null 值，甚至是如果这个类型允许为 null。

他们都能任何输入上指定：

- 值类型参数
- `in` 参数
- `ref` 参数
- 字段
- 属性
- 索引

**重要：**这些标签只影响那些注解了的方法调用的 null 分析。这些被注解的方法以及接口实现类的主体不遵循这些特性。

## Nullable 后置条件：MaybeNull 和 NotNull

// TODO



[使用可为空引用类型]: https://docs.microsoft.com/zh-cn/dotnet/csharp/tutorials/nullable-reference-types

