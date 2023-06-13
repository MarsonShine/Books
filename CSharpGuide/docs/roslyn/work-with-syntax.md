# 使用语法

**语法树（syntax tree）**是编译器 API 公开的基本不可变数据结构。这些树代表源代码的词汇（lexical）和句法（syntactic）结构。它们有两个重要目的：

- 允许工具（如 IDE、插件、代码分析工具和重构）查看和处理用户项目中源代码的句法结构。
- 使工具（例如重构和 IDE）能够以自然的方式创建、修改和重新排列源代码，而无需使用直接文本编辑。通过创建和操作树，工具可以轻松地创建和重新排列源代码。

## 语法树

语法树（syntax tree）是用于编译、代码分析、绑定、重构、IDE 功能和代码生成的主要结构。**如果不首先识别源代码并将其归类为许多众所周知的结构语言元素之一，就无法理解源代码的任何部分。**

语法树具有三个关键属性：

- 他们完全保真地保存所有源信息。**完全保真意味着语法树包含在源文本中找到的每条信息、每个语法结构、每个词法标记以及介于两者之间的所有其他内容，包括空格、注释和预处理程序指令**。例如，源代码中提到的每个文字都完全按照键入的方式表示。当程序不完整或格式错误时，语法树还通过表示跳过或丢失的标记来捕获源代码中的错误。
- 他们可以生成与解析出的文本完全相同的文本。从任何语法节点，都可以获取以该节点为根的子树的文本表示。这种能力意味着语法树可以用作构建和编辑源文本的一种方式。通过创建一棵树，您已经暗示创建了等效文本，并且通过对现有树的更改创建一棵新树，您已经有效地编辑了文本。
- 它们是**不可变的**和**线程安全**的。获得树后，它是代码当前状态的快照，永远不会改变。这允许多个用户在不同线程中同时与同一语法树交互，而无需锁定或重复。因为树是不可变的，不能直接对树进行修改，工厂方法通过创建树的额外快照来帮助创建和修改语法树。这些树在重用底层节点的方式上是高效的，因此可以快速重建新版本并且只需要很少的额外内存。

语法树实际上是一种树数据结构，其中非终端结构元素是其他元素的父级。每个语法树都由节点（nodes）、标记（tokens）和注释（trivia）组成。

## 语法节点

语法节点（syntax nodes）是语法树的主要元素之一。**这些节点表示语法结构，例如声明、语句、子句和表达式。**每个类别的语法节点都由派生自 [Microsoft.CodeAnalysis.SyntaxNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode) 的单独类表示。节点类集是不可扩展的。

所有语法节点都是语法树中的非终端节点，这意味着它们总是有其他节点和标记作为子节点。作为另一个节点的子节点，每个节点都有一个可以通过 [SyntaxNode.Parent](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.parent#microsoft-codeanalysis-syntaxnode-parent) 属性访问的父节点。因为节点和树是不可变的，所以节点的父节点永远不会改变。树的根有一个空父节点。

每个节点都有一个 [SyntaxNode.ChildNodes()](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.childnodes#microsoft-codeanalysis-syntaxnode-childnodes) 方法，该方法根据子节点在源文本中的位置按顺序返回子节点列表。此列表不包含标记。每个节点还具有检查后代的方法，例如 [DescendantNodes](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.descendantnodes)、[DescendantTokens](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.descendanttokens) 或 [DescendantTrivia](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.descendanttrivia) - 表示以该节点为根的子树中存在的所有节点、标记或注释的列表。

此外，每个语法节点子类都通过强类型属性公开所有相同的子节点。例如，[BinaryExpressionSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax) 节点类具有三个特定于二元运算符的附加属性：[Left](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax.left#microsoft-codeanalysis-csharp-syntax-binaryexpressionsyntax-left)、[OperatorToken](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax.operatortoken#microsoft-codeanalysis-csharp-syntax-binaryexpressionsyntax-operatortoken) 和 [Right](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax.right#microsoft-codeanalysis-csharp-syntax-binaryexpressionsyntax-right)。 Left 和 Right 的类型是 [ExpressionSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.expressionsyntax)，OperatorToken 的类型是 [SyntaxToken](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken)。

一些语法节点有可选的子节点。例如，[IfStatementSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.ifstatementsyntax) 有一个可选的 [ElseClauseSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.elseclausesyntax)。如果孩子不存在，则该属性返回 null。

## 语法标记

语法标记（syntax tokens）是语言文法的终结符，代表代码的最小语法片段。它们永远不是其他节点或代币的父代。语法标记由关键字、标识符、文字和标点符号组成。

为了提高效率，[SyntaxToken](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken) 类型是 CLR 值类型。因此，与语法节点不同的是，所有类型的标记只有一种结构，具有混合的属性，这些属性的含义取决于所表示的标记的种类。

例如，整数文字标记表示数值。除了标记跨越的原始源文本之外，文字标记还有一个 [Value](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.value#microsoft-codeanalysis-syntaxtoken-value) 属性，它告诉您准确的解码整数值。此属性类型为 Object，因为它可能是许多基本类型之一。

[ValueText](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.valuetext#microsoft-codeanalysis-syntaxtoken-valuetext) 属性告诉您与 [Value](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.value#microsoft-codeanalysis-syntaxtoken-value) 属性相同的信息；然而，此属性的类型始终为字符串。 C# 源文本中的标识符可能包含 Unicode 转义字符，但转义序列本身的语法不被视为标识符名称的一部分。因此，虽然标记跨越的原始文本确实包含转义序列，但 ValueText 属性不包含。相反，它包括由转义标识的 Unicode 字符。例如，如果源文本包含写为 `\u03C0` 的标识符，则此标记的 [ValueText](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.valuetext#microsoft-codeanalysis-syntaxtoken-valuetext) 属性将返回 `π` 。

## 语法注释

语法注释（syntax trivia）表示源文本中对代码的正常理解基本上无关紧要的部分，例如空格、注释和预处理程序指令。与语法标记一样，注释（trivia）是值类型。单个 [Microsoft.CodeAnalysis.SyntaxTrivia](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtrivia) 类型用于描述各种注释。

因为注释不是正常语言语法的一部分，并且可以出现在任意两个标记之间的任何位置，所以它们不会作为节点的子节点包含在语法树中。然而，因为它们在实现重构等功能以及保持与源文本的完全保真度时很重要，所以它们确实作为语法树的一部分存在。

您可以通过检查标记的 [SyntaxToken.LeadingTrivia](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.leadingtrivia#microsoft-codeanalysis-syntaxtoken-leadingtrivia) 或 [SyntaxToken.TrailingTrivia](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.trailingtrivia#microsoft-codeanalysis-syntaxtoken-trailingtrivia) 集合来访问注释。解析源文本时，注释序列与标记相关联。通常，一个标记在同一行中拥有它之后的所有注释，直到下一个标记。该行之后的任何注释都与以下标记相关联。源文件中的第一个标记获取所有初始注释，文件中的最后一个注释序列被附加到文件结尾标记上，否则它的宽度为零。

与语法节点和标记不同，语法注释没有父节点。然而，因为它们是树的一部分并且每个都与一个标记关联，所以您可以使用 [SyntaxTrivia.Token](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtrivia.token#microsoft-codeanalysis-syntaxtrivia-token) 属性访问它关联的标记。

## 跨度（Spans）

每个节点、标记或注释都知道它在源文本中的位置以及它包含的字符数。文本位置表示为 32 位整数，它是从零开始的 `char` 索引。 [TextSpan](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.text.textspan) 对象是起始位置和字符数，均表示为整数。如果 TextSpan 的长度为零，则它指的是两个字符之间的位置。

每个节点都有两个 TextSpan 属性：[Span](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.span) 和 [FullSpan](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.fullspan)。

Span 属性是从节点子树中第一个标记开始到最后一个标记结束的文本跨度。此跨度不包括任何前导或尾随注释。

FullSpan 属性是文本跨度，包括节点的正常跨度，加上任何前导或尾随注释的跨度。

例如：

```c#
if (x > 3)
{
｜｜		// this is bad
    |throw new Exception("Not right.");|  // better exception?||
}
```

块内的语句节点的跨度由单竖线 (|) 指示。它包括字符 `throw new Exception("Not right.");` 。完整跨度由双竖线 (||) 表示。它包括与跨度相同的字符以及与前导和尾随注释关联的字符。

## 种类（Kinds）

每个节点、令牌或注释都有一个 System.Int32 类型的 [SyntaxNode.RawKind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.rawkind#microsoft-codeanalysis-syntaxnode-rawkind) 属性，用于标识所表示的确切语法元素。该值可以转换为特定于语言的枚举。每种语言（C# 或 Visual Basic）都有一个 `SyntaxKind` 枚举（分别为 [Microsoft.CodeAnalysis.CSharp.SyntaxKind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind) 和 [Microsoft.CodeAnalysis.VisualBasic.SyntaxKind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.visualbasic.syntaxkind)），它列出了语法中所有可能的节点、标记和注释元素。这种转换可以通过访问 [CSharpExtensions.Kind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpextensions.kind) 或 [VisualBasicExtensions.Kind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.visualbasic.visualbasicextensions.kind) 扩展方法自动完成。

[RawKind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken.rawkind#microsoft-codeanalysis-syntaxtoken-rawkind) 属性允许轻松消除共享同一节点类的语法节点类型的歧义。对于标记和注释，此属性是将一种元素与另一种元素区分开来的唯一方法。

例如，单个 [BinaryExpressionSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax) 类有 [Left](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax.left#microsoft-codeanalysis-csharp-syntax-binaryexpressionsyntax-left)、[OperatorToken](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax.operatortoken#microsoft-codeanalysis-csharp-syntax-binaryexpressionsyntax-operatortoken) 和 [Right](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.binaryexpressionsyntax.right#microsoft-codeanalysis-csharp-syntax-binaryexpressionsyntax-right) 作为子类。 [Kind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpextensions.kind) 属性区分它是 [AddExpression](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind#microsoft-codeanalysis-csharp-syntaxkind-addexpression)、[SubtractExpression](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind#microsoft-codeanalysis-csharp-syntaxkind-subtractexpression) 还是 [MultiplyExpression](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind#microsoft-codeanalysis-csharp-syntaxkind-multiplyexpression) 类型的语法节点。

> 注意⚠️
>
> 建议使用 [IsKind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharpextensions.iskind)（对于 C#）或 [IsKind](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.visualbasicextensions.iskind)（对于 VB）扩展方法来检查类型。

## 错误

即使源文本包含语法错误，也会公开可往返于源的完整语法树。当解析器遇到不符合语言定义语法的代码时，它会使用两种技术之一来创建语法树：

- 如果解析器需要一种特定类型的标记但没有找到它，它可能会将丢失的标记插入到语法树中需要该标记的位置。缺少的标记表示预期的实际标记，但它有一个空跨度，并且其 [SyntaxNode.IsMissing](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxnode.ismissing#microsoft-codeanalysis-syntaxnode-ismissing) 属性返回 `true` 。
- 解析器可能会跳过标记，直到找到可以继续解析的标记。在这种情况下，跳过的标记作为类型为 [SkippedTokensTrivia](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind#microsoft-codeanalysis-csharp-syntaxkind-skippedtokenstrivia) 的注释节点附加。

下一篇：[使用语义](work-with-semantics.md)