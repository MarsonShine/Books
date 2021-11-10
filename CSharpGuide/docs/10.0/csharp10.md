# 全局Using（Global Using）

C#10 支持在 cs 文件顶层添加全局引用功能。这样在整个项目中就可以不用在其它文件中再次添加 using。

```csharp
global using System;
global using static System.Console;
global using static System.Math;
global using Env = System.Environment;

namespace CSharpGuide.LanguageVersions._10._0
{
    internal class GlobalUsing
    {
        static void Start()
        {
            WriteLine(Sqrt(3 * 3 + 4 * 4));
            WriteLine(string.Join(',', Env.GetEnvironmentVariables().Keys));
        }
    }
}
```

我们可以在其它命名空间下可以直接引用 `System、System.Console、System.Math`命名空间下的对象所有方法，同样也支持全局范围设置引用别名。

此外我们可以在项目解决方案（构建解决方案）中设置隐式 using，这样在解决方案中所有的项目都能默认添加全局 using 功能，一处添加，处处都能使用：

```xml
<PropertyGroup>
    <!-- Other properties like OutputType and TargetFramework -->
    <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

那么如果我们在自己的项目中定义的方法与对象由于添加了全局 using 的原因，有很多方法与对象会重名而报二义性的错误。这种情况我们可以针对性的在指定的项目解决方案中添加排除指定的全局引用：

```xml
<ItemGroup>
  <Using Remove="System.Threading.Tasks" />
</ItemGroup>
```

# Lambda表达式支持类型推断

在 C#10 之前，我们定义的 Lambda 必须要显式定义类型：

```
Func<string,int> parse = (string s) => int.Parse(s);
```

如果尝试给 parse 变量申请 var 类型时，在 C#10之前是会报 "推断的委托类型在C#10以前是不可用的"：

```
var parse = (string s) => string.Parse(s);	// C#10以下报错，无法进行lambda委托类型推断
```

上面的写法在 C#10 能完美支持。

当然并不是所有的 lambda 委托都能支持这个特性，有个前提必须是要给编译器提供足够的类型推断信息，如下面的委托表达式就不能推断具体的类型：

```
var parse = s => int.Parse(s);
```

Lambda表达式类型推断也是支持里氏替换原则的（协变）

```
LambdaExpression parseExpr = (string s) => int.Parse(s); // Expression<Func<string, int>>
Expression parseExpr2 = (string s) => int.Parse(s);       // Expression<Func<string, int>>
```

## 申明Lambda返回类型

对于有返回类型的 lambda 表达式，如果没有足够的消息无法推断出类型的，如：

```
var choose = (bool b) => b ? 1 : "two"; // ERROR: 无法进行类型推断
```

在 C#10 就可以显式申明返回类型：

```
// C#10可以显式申明返回类型
var choose2 = object (bool b) => b ? -1 : "one";
```

# 结构体相关的新特性

## 无参构造函数和字段初始化器

C#10以前的结构体都默认有一个无参的构造函数，如果尝试给结构体添加有参构造函数，编译器会报错。现在C#10支持可以添加带参构造函数。

```
internal struct Address
{
    public Address()
    {
        City = "unknown";
    }
    public Address(string city)
    {
        City = city;
    }
    public string City { get; init; }
}
```

如果是申明不变量，则可以添加 `readonly`

```c#
public record struct Person(string FirstName,string LastName);
public readonly record struct RecordPersonStruct(string FirstName,string LastNam);
```

> 关于 record 相关定义与格式化内容，详见：https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/builtin-types/record

# 在结构体与匿名类上的with 表达式

C#10在所有的类型上都支持`with`表达式，它返回的是新对象与新赋值的值：

```csharp
RecordPerson person = new() { FirstName = "Marson", LastName = "Shine" };
var person2 = person with { LastName = "Zhu" };
```

# 字符串插值处理程序

C#内置了字符串插值处理程序，如果我们在程序中使用了字符串插值，那么编译器会自动调用内部的字符串插值处理程序，当然我们也可以自定义插值处理程序：

```c#
public class Logger
{
    public LogLevel EnabledLevel { get; init; } = LogLevel.Error;
    public void LogMessage(LogLevel level, string msg)
    {
        if (EnabledLevel < level) return;
        WriteLine(msg);
    }

    public void LogMessage(LogLevel level,LogInterpolatedStringHandler builder)
    {
        if (EnabledLevel < level) return;
        WriteLine(builder.GetFormattedText());
    }
}
```

这样我们申明了自定义字符串插值处理程序`LogInterpolatedStringHandler`之后，在记录日志的时候如果使用了字符串插值，那么编译器就会自动选择 `LogInterpolatedStringHandler` 处理程序中的方法：

```csharp
var logger = new Logger() { EnabledLevel = LogLevel.Warning };
var time = DateTime.Now;

logger.LogMessage(LogLevel.Error, $"Error Level. CurrentTime: {time}. This is an error. It will be printed.");//自动调用的新增的重载
logger.LogMessage(LogLevel.Trace, $"Trace Level. CurrentTime: {time}. This won't be printed.");//自动调用的新增的重载
logger.LogMessage(LogLevel.Warning, "Warning Level. This warning is a string, not an interpolated string expression.");
```

> 关于自定义字符串插值处理程序与运行过程详见：https://docs.microsoft.com/zh-cn/dotnet/csharp/whats-new/tutorials/interpolated-string-handler

满足上述效果，必须满足下面四个条件：

1. [System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute](https://docs.microsoft.com/zh-cn/dotnet/api/system.runtime.compilerservices.interpolatedstringhandlerattribute) 应用于该类型
2. 必须带有 `literalLength` 和 `formatCount` 两个 int 类型的构造函数。
3. 申明方法签名 `AppendLiteral(string s)`。
4. 申明方法签名 `AppendFormatted<T>(T t)`

# 拓展属性模式

```c#
// 拓展属性模式
object obj = new Person
{
    FirstName = "Marson",
    LastName = "Shine",
    Address = new Address { City = "Seattle" }
};

if (obj is Person { Address: { City:"Seattle"} })
{
    WriteLine("Seattle");
}

if (obj is Person { Address.City:"Seattle"})    // C#10 拓展属性模式
{
    WriteLine("Seattle");
}
```

# CallerArgumentExpression

`CallerArgumentExpression`可以在调用方法时显式原始的参数上下文信息，如下面例子：

```c#
var a = 6;
var b = true;
CheckExpression(true);
CheckExpression(b);
CheckExpression(a > 5);
static void CheckExpression(bool condition,[CallerArgumentExpression("condition")] string? message = null)
{
    WriteLine($"Condition: {message}");
}
// 输出
// Condition: true
// Condition: b
// Condition: a > 5
```

