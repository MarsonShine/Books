# 非空引用类型——C#8.0

原文地址：https://devblogs.microsoft.com/dotnet/try-out-nullable-reference-types/?utm_source=vs_developer_news&utm_medium=referral

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

## 可 null 先决条件：`AllowNull` 和 `DisallowNull`

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

## Nullable 后置条件：`MaybeNull` 和 `NotNull`

请看下面代码：

```c#
public class MyArray
{
    // 返回结果如果不匹配则默认值
    public static T Find<T>(T[] array, Func<T, bool> match)
    {
        ...
    }

    // 调用的时候不会返回 null
    public static void Resize<T>(ref T[] array, int newSize)
    {
        ...
    }
}
```

这里会有另一个问题。我们希望在 `Find` 方法找到匹配返回 `default`，它是可 null 的引用类型。我们希望 `Resize` 方法接受一个可能为 null 的输入，但是在调用 `Resize` 之后确保我们想要的 `array` 值不是 null。同样，我们用 `notnull` 约束不能解决。

这个时候输入 `[MaybeNull]` 以及 `[NotNull]` 现在就能想象输出的可为 null 了。我们只要做需要做些小修改：

```c#
public class MyArray
{
    [return: MaybeNull]
    public static T Find<T>(T[] array, Func<T, bool> match)
    {
        ...
    }

    public static void Resize<T>([NotNull] ref T[]? array, int newSize)
    {
        ...
    }
}
```

然后我们就调用就会有这样的效果：

```c#
void M(string[] testArray)
{
    var value = MyArray.Find<string>(testArray, s => s == "Hello!");
    Console.WriteLine(value.Length); // Warning: 取消引用可能出现的空引用

    MyArray.Resize<string>(ref testArray, 200);
    Console.WriteLine(testArray.Length); // Safe!
}
```

> 注意：在 .netcore 3.0 preview 7 下，我的VS2019  16.2.3 版本中以上代码不会爆出警告，只有在当前域引用可能为 null 的引用才会暴警告：
>
> ```c#
> string value = defalt;
> Console.WriteLine(value.Length);//这里会爆出警告提示
> ```
>
> 也说明了目前的版本还不完善。

第一个方法指定 T 返回的能是 null 值。也就是说当使用调用这个方法返回的结果时，必须要检查值是否为 null。

第二个方法有一个严格的签名：`[NotNull] ref T[]? array`。意思是 `array` 作为输入能为 null，但是当 `Resize` 被调用时，`array` 不能为 null。也就是如果你调用 `Resize` 之后你在 `array` 有引用（"."），你将不会得到一个警告。因为在 调用`Resize` 时，`array` 值永远不为 null 了。

正式说：

`MaybeNull`特性允许你返回可为 null 的类型，甚至这个类型不允许为 null。`NotNull`特性不允许返回的结果的为 null，甚至是本身这个类允许为 null。他们都能指定以下的任何输出上：

- 方法返回
- `out` 标记参数（在方法调用后）
- `ref` 标记参数（在方法调用后）
- 字段
- 属性
- 索引

**重要提示：**这些特性仅仅只是影响对那些被注解的调用方法的调用者可为 null 性的分析。那些被注解的方法主体和类似接口实现的东西一样不遵循这个这些标签。我们也许会在下一个特性中加入。

## 后置条件：`MaybeNullWhen(bool)` 和 `NotNullWhen(bool)`

考虑如下代码片段：

```c#
public class MyString
{
    // value 为 null 时为 true
    public static bool IsNullOrEmpty(string? value)
    {
        ...
    }
}

public class MyVersion
{
    // 如果转换成功，Version 将不会为 null
    public static bool TryParse(string? input, out Version? version)
    {
        ...
    }
}

public class MyQueue<T>
{
    // 如果我们不能将它出队列，那么 result 能为 null
    public bool TryDequeue(out T result)
    {
        ...
    }
}
```

这些方法在 .NET 随处可见，当返回值是 `true` 或 `false` 对应于参数的可为 null 性（或者可能位 null）。`MyQueue` 这个例子有点特殊，因为他是泛型的。`TryDequeque` 方法如果在它返回 `false` 时应该给 `result` 赋值为 `null`，但是这种情况只有 T 是引用类型下才可以。如果 T 是值类型 `struct` 结构体，那么它不会是 null。

所以针对这种情况，我们想做以下三件事：

1. 如果 `IsNullOrEmpty` 返回 `false`，那么 `value` 不会为 null
2. 如果 `TryParse` 返回 true，那么 version 不为 null
3. 如果 `TryDequeue` 返回 `false`，那么 `result` 能为 null，如果被提供的参数类型是引用类型的话

很遗憾，C# 编译器并不会将这些方法返回的结果对于参数的可空性关联起来。

现在有了 `NotNullWhen(bool)` 和 `MaybeNullWhen(bool)` 就能对参数进行更细致的处理：

```c#
public class MyString
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] string? value)
    {
    	...    
    }
}

public class MyVersion
{
    public static bool TryParse(string? input, [NotNullWhen(true)]out Version? version)
    {
        ...
    }
}

public class MyQueue<T>
{
	public bool TryDequeue([MaybeNullWhen(false)] out T result)
	{
		...
	}
}
```

然后我们就可以这样调用了：

```c#
void StringTest(string? s)
{
    if(MyString.IsNullOrEmpty(s))
    {
        //这里会有警告
        //Console.WriteLine(s.Length);
        return;
    }
    Console.WriteLine(s.Length);	//安全
}

void VersionTest(string? s)
{
    if (!MyVersion.TryParse(s, out var version))
    {
        // 警告
        // Console.WriteLine(version.Major);
        return;
    }

    Console.WriteLine(version.Major); // Safe!
}
//注意 在我的实验下,以下代码并不会像文中注释中一样会产生警告
void QueueTest(MyQueue<string> q)
{
    if (!q.TryDequeue(out var s))
    {
        // This would generate a warning:
        // Console.WriteLine(s.Length);	//实际上在我VS中是没有警告的
        return;
    }

    Console.WriteLine(s.Length); // Safe!
}
```

调用者可以使用与往常一样的模式处理 api，没有来自编译器的任何警告：

- 如果 `IsNullOrEmpty` 返回 `true`，那么可以安全的在 `value` 使用 "." 
- 如果 `TryParse` 返回 `true`，`version` 成功转换并可以安全使用 “.”
- 如果 `TryDequeue` 返回 `false`，`result` 可能为 null，所以要根据实际需要检查值（例如：当一个类型是值类型结构体时，返回 `false` 不为 null，但如果为引用类型，那么它就为 null）

正式的讲：

`NotNullWhen(bool)` 签名的参数是不为 null 的，甚至这个类型本身不允许为 null，条件依赖于 `bool` 方法返回的值。`MaybeNullWhen(bool)` 签名的参数能为 null，甚至是参数类型本身不允许，条件依赖于方法返回 `bool` 值。他们能指定任何参数类型。

## 输入输出之间的无 Null 依赖：`NotNullIfNotNull(string)`

考虑下面代码片段：

```c#
class MyPath
{
    public static string? GetFileName(string? path)
    {
        ...
    }
}
```

在这个例子，我们希望能够返回 null 字符串，并且我们也应该接受一个 null 值作输入。所以这个方法能完成我想要的效果。

但是，如果 `path` 不为 null，我们希望确保返回总是能返回一个字符串。即我想要 `GetFileName` 返回一个不为 null 的值，条件是 path 不为 null。这里是没有方法去表达这个意思的。

而 `[NotNullIfNotNull]` 就登场了。这个特性能使你的代码变得更花哨，所以小心的使用它！

这里我将展示我使用这个 API 的代码：

```c#
class MyPath
{
    [return: NotNullIfNotNull("path")]
    public static string? GetFileName(string? path)
    {
        ...
    }
}
```

那么我们调用这个方法就有这样的效果：

```c#
void PathTest(string? path)
{
    var possiblyNullPath = MyPath.GetFileName(path);
    Console.WriteLine(possiblyNullPath);// Warning: 取消引用可能出现空引用
    if(!string.IsNullOrEmpty(path))
    {
        var goodPath = MyPath.GetFileName(path);
        Console.WriteLine(goodPath);// safe	注意：在我的验证下仍然有警告
    }
}
```

正式的：

`NotNullIfNotNull(string)` 特性签名表示任何输出值是不为 null 的，条件依赖于给出的特性指定的名称参数的可 null 性。它们可以在以下具体结构指定：

- 方法返回
- `ref` 标明的参数

## 流特性：`DoesNotReturn` 和 `DoesNotReturnIf(bool)`

你可以使用多个方法影响程序的控制流。例如，一个异常帮助类方法，调用它将抛出一个异常，或者一个断言方法，它根据你的输入是 `true` 还是 `false` 来抛出异常。

你也许希望做一些能像 `assert` 这样，这个值不为 null，并且我们认为你们会喜欢它，如果编译器能理解它。

输入 `DoseNotReturn` 和 `DoseNotReturnIf(bool)`。这里有一些例子来告诉你怎样使用：

```c#
internal static class ThrowHelper
{
    [DoseNotReturn]
    public static void ThrowArgumentNullException(ExceptionArgument arg)
    {
        ...
    }
}

public static class MyAssertionLibrary
{
    public static void MyAssert([DoesNotReturnIf(false)] bool condition)
    {
        ...
    }
}
```

当 `ThrowArgumentNullException` 在方法中被调用时，它会抛出异常。注解在签名上的 `DoesNotReturn` 将发出信号给编译器表示在之后不要进行非 null 分析，因为代码将会不可达。

当`MyAssert` 被调用时，并且条件传递为 `false`，它会抛出异常。注解的 `DoesNotReturnIf(false)` 以及里面的条件参数能够让编译器知道程序流不会继续往下走，如果条件为 `false`。当你想要断言一个值的可空性的时候，这是非常有用的。在代码 `MyAssert(value != null)` 路径之后，那么编译器会假使 `value` 不是 null。

`DoesNotReturn` 用在方法上。`DoesNotReturnIf(bool)` 用在输入参数上。

## 进化（Evolving）你的注解

一旦你在公开的 API 使用了注解，那你就要考虑一个事实，那就是更新 API 会影响下游：

- 增加可空性的注解地方，它们可能会给用户的代码带来警告。
- 移除可空性注解能如此（比如接口实现）

可空注解在你的公有 API 一部分。增加或移除都会带来警告。我们建议在预览版本开始这个，并且征求你们的反馈，目标是在一个完整的版本之后不改变任何注解。尽管这不总是可能的，但是我们还是推荐。

## Microsoft framwork 和 库目前的状态

因为可空型引用类型是新的，主要的 C# Microsoft framwork 和 lib 的作者也还没有进行合适的注解。

就是说，.NET Core 中的 “Core Lib”，它代表了 .NET Core 共享的 framwork 的 25% 都完整的做了更新。包括了`System`，`System.IO` 以及 `System.Collections.Generic`。我们正在关注对我们的决定的反馈，以便我们做合适的做出调整，在他们的使用变得更加广泛之前。

尽管任然还有大约 80% 的 CoreFX 需要注解，但是最常用的 APIs 都有了注解。

## 可空引用类型的线路

目前，我们把整个可空引用类型视为预览版。它是稳定，但是这个特性涉及到我们自己技术和更好的 .NET 生态。还需要一些时间来完成。

也就是我们鼓励库作者使用在他们的库中开始注解。这个特性只会让库变得更好的使用可空能力，帮忙 .NET 更加安全。

在未来的一年左右，我们将继续提高这个特性以及在 Microsoft frameworks 和 libs 中传播。

针对语言，特别是编译器分析，我们将使它增强以至于我们能使你需要做的事最小化，就像使用非空操作符(`!`)。这些增强都在[这里](https://github.com/dotnet/roslyn/issues?q=is%3Aissue+is%3Aopen+label%3A%22New+Language+Feature+-+Nullable+Reference+Types%22)追踪。

针对 CoreFX，我们将会维护注解到 80% 的 api，同样也会根据反馈做出适当的调整。

对于 ASP.NET Core 和 Entity Framework，我们一旦添加到 CoreFX 和 编译器我们都会添加注解。

我们没有计划去给 WinForms 和 WPFs 的 APIs 注解，但是我们乐意听到你们的反馈在不同的事情上。

最后，我们将继续提高 C# Visual Studio 工具。我们对于这些特性有很多建议去帮助使用这些特性，我们也很喜欢你们的建议。

## 下一步

如果你仍然在读以及在你的代码中没有尝试这个特性，特别是你的库代码，还请尝试以及给我们反馈在你感觉有任何异样。在 .NET 中消除令人意外的 `NullReferenceException` 还需要漫长的过程，但是我们希望在长时间运行的之后，开发者不用担心被隐式的空值影响。你们能帮助导我们。试试这个特性，在你的类库中使用注解。反馈你的经验





[使用可为空引用类型]: https://docs.microsoft.com/zh-cn/dotnet/csharp/tutorials/nullable-reference-types

