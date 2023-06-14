# 语义分析入门

在本教程中，你将探索**符号（Symbol）**和**绑定 API（Binding APIs）**。这些 API 提供有关程序语义含义的信息。它们使您能够提出和回答有关程序中任何符号所表示的类型的问题。

## 理解编译和符号

随着您更多地使用 .NET 编译器 SDK，您将熟悉语法 API（syntax api） 和语义 API（semantic api）之间的区别。语法 API 允许您查看程序的结构。但是，您通常希望获得有关程序语义或含义的更丰富的信息。虽然可以单独分析松散的代码文件或 Visual Basic 或 C# 代码片段，但在真空中提出诸如“此变量的类型是什么”之类的问题没有意义。类型名称的含义可能取决于程序集引用、命名空间导入或其他代码文件。这些问题使用 **Semantic API** 来回答，特别是 [Microsoft.CodeAnalysis.Compilation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.compilation) 类。

[编译（Compilation）](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.compilation)实例类似于编译器看到的单个项目，表示编译 Visual Basic 或 C# 程序所需的一切。编译包括要编译的源文件集、程序集引用和编译器选项。您可以使用此上下文中的所有其他信息来推理代码的含义。编译允许您查找符号 - 名称和其他表达式引用的类型、命名空间、成员和变量等实体。**将名称和表达式与符号相关联的过程称为绑定。**

与 [Microsoft.CodeAnalysis.SyntaxTree](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtree) 一样，编译（Compilation）是一个抽象类，具有特定于语言的衍生物。创建编译实例时，必须在 [Microsoft.CodeAnalysis.CSharp.CSharpCompilation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpcompilation)（或 [Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.visualbasic.visualbasiccompilation)）类上调用工厂方法。

## 查询符号

在本教程中，您将再次查看 “Hello World” 程序。这一次，您将查询程序中的符号以了解这些符号表示的类型。您将查询命名空间中的类型，并了解如何查找类型上可用的方法。

您可以在我们的 [GitHub 存储库](https://github.com/dotnet/samples/tree/main/csharp/roslyn-sdk/SemanticQuickStart)中看到此示例的完成代码。

> 注意
>
> 语法树（Syntax Tree）类型使用继承来描述在程序中不同位置有效的不同语法元素。使用这些 API 通常意味着将属性或集合成员强制转换为特定的派生类型。在下面的示例中，赋值和强制转换是单独的语句，使用显式类型变量。您可以阅读代码以查看 API 的返回类型和返回对象的运行时类型。在实践中，更常见的是使用隐式类型变量并依赖 API 名称来描述所检查对象的类型。

```c#
const string programText =
@"using System;
using System.Collections.Generic;
using System.Text;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";
```

接下来，添加以下代码，为 `programText` 常量中的代码文本生成语法树。将以下行添加到 `Main` 方法中：

```c#
SyntaxTree tree = CSharpSyntaxTree.ParseText(programText);

CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
```

接下来，从已创建的树构建 CSharpCompilation。“Hello World” 示例依赖于字符串和控制台类型。您需要在编译中引用声明这两种类型的程序集。将以下行添加到 `Main` 方法中以创建语法树的编译，包括对相应程序集的引用：

```c#
var compilation = CSharpCompilation.Create("HelloWorld")
    .AddReferences(MetadataReference.CreateFromFile(
        typeof(string).Assembly.Location))
    .AddSyntaxTrees(tree);
```

[CSharpCompilation.AddReferences](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpcompilation.addreferences) 方法添加对编译的引用。[MetadataReference.CreateFromFile](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.metadatareference.createfromfile) 方法将程序集加载为引用。

## 查询语义模型

一旦你有一个[编译](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.compilation)，你可以要求它为该编译中包含的任何[语法树](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtree)提供一个[语义模型](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel)。您可以将语义模型视为通常从智能感知获得的所有信息的来源。SemanticModel 可以回答诸如“此位置的作用域中有哪些名称？”、“此方法可以访问哪些成员？”、“此文本块中使用了哪些变量？” 和 “此名称/表达式引用什么？” 等问题。添加此语句以创建语义模型：

```c#
SemanticModel model = compilation.GetSemanticModel(tree);
```

## 绑定名称

编译从语法树创建语义模型。创建模型后，可以查询它以查找第一个 `using` 指令，并检索 `System` 命名空间的符号信息。将这两行添加到 `Main` 方法中以创建语义模型并检索第一个 using 语句的符号：

```c#
// Use the syntax tree to find "using System;"
UsingDirectiveSyntax usingSystem = root.Usings[0];
NameSyntax systemName = usingSystem.Name;

// Use the semantic model for symbol information:
SymbolInfo nameInfo = model.GetSymbolInfo(systemName);
```

上面的代码演示如何绑定第一个 `using` 指令中的名称以检索 `System` 命名空间的 [Microsoft.CodeAnalysis.SymbolInfo](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.symbolinfo)。前面的代码还说明了如何使用语法模型来查找代码的结构;您可以使用语义模型来理解其含义。语法模型在 using 语句中查找字符串 `System` 。语义模型包含有关 `System` 命名空间中定义的类型的所有信息。

从 [SymbolInfo](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.symbolinfo) 对象中，您可以使用 [SymbolInfo.Symbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.symbolinfo.symbol#microsoft-codeanalysis-symbolinfo-symbol) 属性获取 [Microsoft.CodeAnalysis.ISymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol)。此属性返回此表达式引用的符号。对于不引用任何内容（如数字文本）的表达式，此属性为 `null` 。当 SymbolInfo.Symbol 不为空时，[ISymbol.Kind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.kind#microsoft-codeanalysis-isymbol-kind) 表示符号的类型。在此示例中，ISymbol.Kind 属性是一个 [SymbolKind.Namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.symbolkind#microsoft-codeanalysis-symbolkind-namespace)。将以下代码添加到 `Main` 方法中。它检索 `System` 命名空间的符号，然后显示在 `System` 命名空间中声明的所有子命名空间：

```c#
var systemSymbol = (INamespaceSymbol?)nameInfo.Symbol;
if (systemSymbol?.GetNamespaceMembers() is not null)
{
    foreach (INamespaceSymbol ns in systemSymbol?.GetNamespaceMembers()!)
    {
        Console.WriteLine(ns);
    }
}
```

运行该程序，您应该看到以下输出：

```
System.Collections
System.Configuration
System.Deployment
System.Diagnostics
System.Globalization
System.IO
System.Numerics
System.Reflection
System.Resources
System.Runtime
System.Security
System.StubHelpers
System.Text
System.Threading
Press any key to continue . . .
```

> 注意
>
> 输出不包括作为 `System` 命名空间的子命名空间的每个命名空间。它显示此编译中存在的每个命名空间，该命名空间仅引用声明 `System.String` 的程序集。此编译不知道在其他程序集中声明的任何命名空间

## 绑定表达式

前面的代码演示如何通过绑定到名称来查找符号。C# 程序中还有其他可以绑定的表达式，这些表达式不是名称。为了演示此功能，让我们访问对简单字符串文本的绑定。

“Hello World”程序包含一个 [Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.literalexpressionsyntax)，即“Hello， World！”字符串显示给控制台。

您可以通过在程序中找到单个字符串文字来查找“Hello， World！”字符串。然后，找到语法节点后，从语义模型中获取该节点的类型信息。将以下代码添加到 `Main` 方法：

```c#
// Use the syntax model to find the literal string:
LiteralExpressionSyntax helloWorldString = root.DescendantNodes()
.OfType<LiteralExpressionSyntax>()
.Single();

// Use the semantic model for type information:
TypeInfo literalInfo = model.GetTypeInfo(helloWorldString);
```

[Microsoft.CodeAnalysis.TypeInfo](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.typeinfo) 结构包含一个 [TypeInfo.Type](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.typeinfo.type#microsoft-codeanalysis-typeinfo-type) 属性，该属性允许访问有关文本类型的语义信息。在此示例中，这是 `string` 类型。添加将此属性分配给局部变量的声明：

```c#
var stringTypeSymbol = (INamedTypeSymbol?)literalInfo.Type;
```

为了完成本教程，让我们生成一个 LINQ 查询，该查询创建在 `string` 类型上声明的所有返回 `string` 的公共方法的序列。此查询变得复杂，因此让我们逐行构建它，然后将其重新构造为单个查询。此查询的源是在 `string` 类型上声明的所有成员的序列：

```c#
var allMembers = stringTypeSymbol?.GetMembers();
```

该源序列包含所有成员，包括属性和字段，因此请使用 [ImmutableArray<T>.OfType](https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablearray-1.oftype) 对其进行筛选。用于查找 [Microsoft.CodeAnalysis.IMethodSymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.imethodsymbol) 对象的元素：

```c#
var methods = allMembers?.OfType<IMethodSymbol>();
```

接下来，添加另一个筛选器以仅返回那些公共方法并返回 `string` ：

```c#
var publicStringReturningMethods = methods?
    .Where(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, stringTypeSymbol) &&
    m.DeclaredAccessibility == Accessibility.Public);
```

还可以使用 LINQ 查询语法生成完整查询，然后在控制台中显示所有方法名称：

```c#
foreach (string name in (from method in stringTypeSymbol?
                         .GetMembers().OfType<IMethodSymbol>()
                         where SymbolEqualityComparer.Default.Equals(method.ReturnType, stringTypeSymbol) &&
                         method.DeclaredAccessibility == Accessibility.Public
                         select method.Name).Distinct())
{
    Console.WriteLine(name);
}
```

生成并运行程序。应会看到以下输出：

```
Join
Substring
Trim
TrimStart
TrimEnd
Normalize
PadLeft
PadRight
ToLower
ToLowerInvariant
ToUpper
ToUpperInvariant
ToString
Insert
Replace
Remove
Format
Copy
Concat
Intern
IsInterned
Press any key to continue . . .
```

你已使用语义 API 查找和显示有关属于此程序的符号的信息。

下一篇：[语法转换入门](syntax-transformation.md)