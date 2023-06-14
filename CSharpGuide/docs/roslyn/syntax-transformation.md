# 语法转换入门

在本快速入门中，你将探索创建和转换语法树（transforming syntax tree）的技术。结合在前面的快速入门中学到的技术，创建第一个命令行重构！

## 不变性和 .NET 编译器平台

**不变性（Immutability）**是 .NET 编译器平台的基本原则。不可变的数据结构在创建后无法更改。不可变的数据结构可以由多个消费者同时安全地共享和分析。不存在一个消费者以不可预测的方式影响另一个消费者的危险。分析器不需要锁或其他并发度量。此规则适用于语法树、编译、符号、语义模型以及您遇到的所有其他数据结构。API 不是修改现有结构，而是根据与旧对象的指定差异创建新对象。将此概念应用于语法树，以使用转换创建新树。

## 创建和转换树

您可以选择两种语法转换策略之一。在搜索要替换的特定节点或要插入新代码的特定位置时，最好使用**工厂方法**。当您想要扫描整个项目以查找要替换的代码模式时，**重写**是最佳选择。

### 使用工厂方法创建节点

第一个语法转换演示了工厂方法。您将用 `using System.Collections.Generic;` 语句替换 `using System.Collections;` 语句。此示例演示如何使用 [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxfactory) 工厂方法创建 [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxnode) 对象。对于每种节点、令牌或注释，都有一个工厂方法可以创建该类型的实例。您可以通过以自下而上的方式分层组合节点来创建语法树。然后，您将通过将现有节点替换为您创建的新树来转换现有程序。

创建项目，将 using 指令添加到 Program.cs 的顶部：

```c#
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static System.Console;
```

您将创建名称语法节点来构建表示 `using System.Collections.Generic;` 语句的树。[NameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.namesyntax) 是 C# 中出现的四种名称类型的基类。将这四种类型的名称组合在一起，以创建可在 C# 语言中显示的任何名称：

- [Microsoft.CodeAnalysis.CSharp.Syntax.NameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.namesyntax)，表示简单的单个标识符名称，如 `System` 和 `Microsoft`。
- [Microsoft.CodeAnalysis.CSharp.Syntax.GenericNameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.genericnamesyntax)，表示泛型类型或方法名称，如 `List<int>`。
- [Microsoft.CodeAnalysis.CSharp.Syntax.QualifiedNameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.qualifiednamesyntax)，表示形式为 `<left-name>.<right-identifier-or-generic-name>` 的限定名称，例如 `System.IO`。
- [Microsoft.CodeAnalysis.CSharp.Syntax.AliasQualifiedNameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.aliasqualifiednamesyntax)，它表示使用程序集外部别名（如 `LibraryV2::Foo`）的名称。

使用 [IdentifierName（String）](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxfactory.identifiername#microsoft-codeanalysis-csharp-syntaxfactory-identifiername(system-string)) 方法创建一个 [NameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.namesyntax) 节点。在 `Program.cs` 中的 `Main` 方法中添加以下代码：

```c#
NameSyntax name = IdentifierName("System");
WriteLine($"\tCreated the identifier {name}");
```

前面的代码创建一个标识符名称语法对象，并将其分配给变量 `name` 。许多 Roslyn API 返回基类，以便更轻松地使用相关类型。变量 `name`（NameSyntax）可以在构建 [QualifiedNameSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.qualifiednamesyntax) 时重用。生成示例时不要使用类型推断。您将在此工程中自动执行该步骤。

您已经创建了名称。现在，是时候通过构建 QualifiedNameSyntax 在树中构建更多节点了。新树使用 `name` 作为名称的左侧，并使用 `Collections` 命名空间的新标识符名称语法作为限定名称语法的右侧。将以下代码添加到 `program.cs` ：

```c#
name = QualifiedName(name, IdentifierName("Collections"));
WriteLine(name.ToString());
```

再次运行代码，并查看结果。您正在构建一个表示代码的节点树。你将继续使用此模式为命名空间 `System.Collections.Generic` 生成限定名称语法。将以下代码添加到 `Program.cs` ：

```c#
name = QualifiedName(name, IdentifierName("Generic"));
WriteLine(name.ToString());
```

再次运行该程序，查看是否已为要添加的代码构建树。

### 创建修改后的树

你已经建立了一个包含一条语句的小语法树。创建新节点的 API 是创建单个语句或其他小代码块的正确选择。然而，要建立更大的代码块，你应该使用替换节点或在现有的树中插入节点的方法。请记住，语法树是不可改变的。**Syntax API** 不提供任何机制来修改构建后的现有语法树。相反，它提供了一些方法，可以根据对现有树的修改产生新的树。`With*` 方法被定义在派生自 [SyntaxNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode) 的具体类中，或在 [SyntaxNodeExtensions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnodeextensions) 类中声明的扩展方法中。这些方法通过对现有节点的子属性进行修改来创建一个新的节点。此外， [ReplaceNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnodeextensions.replacenode) 扩展方法可以用来替换子树中的一个下级节点。这个方法也会更新父节点以指向新创建的子节点，并在整个树上重复这个过程--这个过程被称为重新旋转树（re-spinning tree）。

下一步是创建一个表示整个（小）程序的树，然后对其进行修改。将以下代码添加到 `Program` 类的开头：

```c#
private const string sampleCode =
@"using System;
using System.Collections;
using System.Linq;
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

> 注意
>
> 示例代码使用 `System.Collections` 命名空间，而不是 `System.Collections.Generic` 命名空间。

接下来，将以下代码添加到 `Main` 方法的底部以解析文本并创建树：

```c#
SyntaxTree tree = CSharpSyntaxTree.ParseText(sampleCode);
var root = (CompilationUnitSyntax)tree.GetRoot();
```

此示例使用 [WithName（NameSyntax）](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.usingdirectivesyntax.withname#microsoft-codeanalysis-csharp-syntax-usingdirectivesyntax-withname(microsoft-codeanalysis-csharp-syntax-namesyntax))方法将 [UsingDirectiveSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.usingdirectivesyntax) 节点中的名称替换为前面代码中构造的名称。

使用 WithName（NameSyntax） 方法创建一个新的 UsingDirectiveSyntax 节点，以使用在上述代码中创建的名称更新 `System.Collections` 名称。将以下代码添加到 `Main` 方法的底部：

```c#
var oldUsing = root.Usings[1];
var newUsing = oldUsing.WithName(name);
WriteLine(root.ToString());
```

运行程序并仔细查看输出。 `newUsing` 尚未放置在根树中。原始树尚未更改。

使用 [ReplaceNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnodeextensions.replacenode) 扩展方法添加以下代码以创建新树。新树是将现有导入替换为更新的 `newUsing` 节点的结果。将此新树分配给现有的 `root` 变量：

```c#
root = root.ReplaceNode(oldUsing, newUsing);
WriteLine(root.ToString());
```

再次运行该程序。这一次，树现在可以正确导入 `System.Collections.Generic` 命名空间。

### 使用 `SyntaxRewriters` 转换树

`With*` 和 [ReplaceNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnodeextensions.replacenode) 方法提供了转换语法树的各个分支的便捷方法。[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxrewriter) 类在语法树上执行多个转换。Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter 类是 [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor<T>](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxvisitor-1) 的一个子类。[CSharpSyntaxRewriter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxrewriter) 将转换应用于特定类型的 [SyntaxNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode)。可以将转换应用于多种类型的 SyntaxNode 对象，无论它们出现在语法树中的哪个位置。本快速入门中的第二个项目创建一个命令行重构，用于删除可以使用类型推断的局部变量声明中的显式类型。

创建新项目，新创建 `TypeInferenceRewriter.cs`，将以下 using 指令添加到该文件中：

```c#
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

接下来，使 `TypeInferenceRewriter` 类扩展 [CSharpSyntaxRewriter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxrewriter) 类：

```c#
public class TypeInferenceRewriter : CSharpSyntaxRewriter
```

添加以下代码以声明一个私有只读字段，以保存 [SemanticModel](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel) 并在构造函数中对其进行初始化。稍后将需要此字段来确定可以在何处使用类型推断：

```c#
private readonly SemanticModel SemanticModel;

public TypeInferenceRewriter(SemanticModel semanticModel) => SemanticModel = semanticModel;
```

重写 [VisitLocalDeclarationStatement（LocalDeclarationStatementSyntax）](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxrewriter.visitlocaldeclarationstatement#microsoft-codeanalysis-csharp-csharpsyntaxrewriter-visitlocaldeclarationstatement(microsoft-codeanalysis-csharp-syntax-localdeclarationstatementsyntax))方法：

```c#
public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
{

}
```

> 注意
>
> 许多 Roslyn API 声明返回类型，这些返回类型是返回的实际运行时类型的基类。在许多情况下，一种节点可能会被另一种节点完全替换，甚至被删除。在此示例中，[VisitLocalDeclarationStatement（LocalDeclarationStatementSyntax）](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxrewriter.visitlocaldeclarationstatement#microsoft-codeanalysis-csharp-csharpsyntaxrewriter-visitlocaldeclarationstatement(microsoft-codeanalysis-csharp-syntax-localdeclarationstatementsyntax))方法返回一个 SyntaxNode，而不是派生类型的 LocalDeclarationStatementSyntax。此重写器基于现有节点返回一个新的 [LocalDeclarationStatementSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.localdeclarationstatementsyntax) 节点。

本快速入门处理局部变量声明。您可以将其扩展到其他声明，例如 `foreach` 循环、`for` 循环、LINQ 表达式和 lambda 表达式。此外，此重写器将仅转换最简单形式的声明：

```c#
Type variable = expression;
```

如果要自行探索，请考虑扩展以下类型的变量声明的已完成示例：

```c#
// Multiple variables in a single declaration.
Type variable1 = expression1,
     variable2 = expression2;
// No initializer.
Type variable;
```

将以下代码添加到 `VisitLocalDeclarationStatement` 方法的主体中，以跳过重写这些形式的声明：

```c#
if (node.Declaration.Variables.Count > 1)
{
    return node;
}
if (node.Declaration.Variables[0].Initializer == null)
{
    return node;
}
```

该方法通过返回未修改的 `node` 参数来指示不进行重写。`if` 表达式都不为 true，则节点表示具有初始化的可能声明。添加这些语句以提取声明中指定的类型名称，并使用 [SemanticModel](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel) 字段绑定它以获取类型符号：

```c#
var declarator = node.Declaration.Variables.First();
var variableTypeName = node.Declaration.Type;

var variableType = (ITypeSymbol)SemanticModel
    .GetSymbolInfo(variableTypeName)
    .Symbol;
```

现在，添加此语句以绑定初始值设定项表达式：

```c#
var initializerInfo = SemanticModel.GetTypeInfo(declarator.Initializer.Value);
```

最后，如果初始值设定项表达式的类型与指定的类型匹配，则添加以下 `if` 语句以将现有类型名称替换为 `var` 关键字：

```c#
if (SymbolEqualityComparer.Default.Equals(variableType, initializerInfo.Type))
{
    TypeSyntax varTypeName = SyntaxFactory.IdentifierName("var")
        .WithLeadingTrivia(variableTypeName.GetLeadingTrivia())
        .WithTrailingTrivia(variableTypeName.GetTrailingTrivia());

    return node.ReplaceNode(variableTypeName, varTypeName);
}
else
{
    return node;
}
```

条件是必需的，因为声明可能会将初始值设定项表达式强制转换为基类或接口。如果需要，条件左侧和右侧的类型不匹配。在这些情况下删除显式类型将更改程序的语义。`var` 被指定为标识符而不是关键字，因为 `var` 是上下文关键字。前导和尾随注释（空格）从旧类型名称转移到 `var` 关键字，以保持垂直空格和缩进。使用 `ReplaceNode` 而不是 `With*` 来转换 [LocalDeclarationStatementSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.localdeclarationstatementsyntax) 更简单，因为类型名称实际上是声明语句的孙子。

您已完成 `TypeInferenceRewriter`。现在返回到您的 `Program.cs` 文件以完成示例。创建测试编译并从中获取语义模型。使用该语义模型来尝试您的 `TypeInferenceRewriter`。您将最后执行此步骤。同时声明一个表示测试编译的占位符变量：

```c#
Compilation test = CreateTestCompilation();
```

暂停片刻后，您应该会看到出现一个错误波浪线，报告不存在 `CreateTestCompilation` 方法。按 Ctrl+ 句点打开小灯泡，然后按 Enter 调用“生成方法存根”命令。此命令将为 `Program` 类中的 `CreateTestCompilation` 方法生成方法存根。

编写以下代码以循环访问测试编译中的每个语法树。对于每个树，使用该树的语义模型初始化一个新的 `TypeInferenceRewriter`：

```c#
foreach (SyntaxTree sourceTree in test.SyntaxTrees)
{
    SemanticModel model = test.GetSemanticModel(sourceTree);

    TypeInferenceRewriter rewriter = new TypeInferenceRewriter(model);

    SyntaxNode newSource = rewriter.Visit(sourceTree.GetRoot());

    if (newSource != sourceTree.GetRoot())
    {
        File.WriteAllText(sourceTree.FilePath, newSource.ToFullString());
    }
}
```

在创建的 `foreach` 语句中，添加以下代码以对每个源树执行转换。如果进行了任何编辑，此代码将有条件地写出新的转换树。重写器只有在遇到一个或多个可以使用类型推断简化的局部变量声明时，才应修改树：

```c#
SyntaxNode newSource = rewriter.Visit(sourceTree.GetRoot());

if (newSource != sourceTree.GetRoot())
{
    File.WriteAllText(sourceTree.FilePath, newSource.ToFullString());
}
```

您应该在 `File.WriteAllText` 代码下看到波浪线。选择小灯泡，并添加必要的 `using System.IO;` 语句。

还剩下一个步骤：创建测试编译。由于在本快速入门中根本没有使用类型推断，因此它将是一个完美的测试用例。遗憾的是，从 C# 项目文件创建编译超出了本演练的范围。但幸运的是，如果你一直认真遵循说明，就有希望。将 `CreateTestCompilation` 方法的内容替换为以下代码。它创建一个测试编译，恰好与本快速入门中所述的项目匹配：

```c#
String programPath = @"..\..\..\Program.cs";
String programText = File.ReadAllText(programPath);
SyntaxTree programTree =
               CSharpSyntaxTree.ParseText(programText)
                               .WithFilePath(programPath);

String rewriterPath = @"..\..\..\TypeInferenceRewriter.cs";
String rewriterText = File.ReadAllText(rewriterPath);
SyntaxTree rewriterTree =
               CSharpSyntaxTree.ParseText(rewriterText)
                               .WithFilePath(rewriterPath);

SyntaxTree[] sourceTrees = { programTree, rewriterTree };

MetadataReference mscorlib =
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
MetadataReference codeAnalysis =
        MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location);
MetadataReference csharpCodeAnalysis =
        MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location);

MetadataReference[] references = { mscorlib, codeAnalysis, csharpCodeAnalysis };

return CSharpCompilation.Create("TransformationCS",
    sourceTrees,
    references,
    new CSharpCompilationOptions(OutputKind.ConsoleApplication));
```

运行项目。在 Visual Studio 中，选择“调试”>“启动调试”。Visual Studio 应该会提示您项目中的文件已更改。单击“全部是”以重新加载修改后的文件。检查它们以观察您的出色之处。请注意，如果没有所有这些显式和冗余的类型说明符，代码看起来会更干净。

祝贺！你已使用**编译器 API** 编写自己的重构，以搜索 C# 项目中的所有文件以查找某些语法模式，分析与这些模式匹配的源代码的语义，然后对其进行转换。您现在正式成为重构作者！