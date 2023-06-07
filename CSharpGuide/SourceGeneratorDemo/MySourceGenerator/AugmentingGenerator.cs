using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public class AugmentingGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Add any initialization code here
        // 注册工厂类，能创建自定义的语法接收器
        context.RegisterForSyntaxNotifications(() => new AugmentingSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // 生成器基础设施将创建一个接收器并填充它
        // 我们可以通过上下文检索已填充的实例
        AugmentingSyntaxReceiver receiver = (AugmentingSyntaxReceiver)context.SyntaxReceiver!;
        // 获取记录的用户类
        ClassDeclarationSyntax userClass = receiver.ClassToAugment!;
        if (userClass is null)
        {
            return;
        }
        // 创建一个新的类，它将扩展用户类
        SourceText sourceText = SourceText.From(
$@"
public partial class {userClass.Identifier}
{{
    private void GeneratedMethod()
    {{
        // generated code
        Console.WriteLine(""Hello from generated code!"");
    }}
}}
", Encoding.UTF8);
        context.AddSource("UserClass.g.cs", sourceText);
    }
}

internal class AugmentingSyntaxReceiver : ISyntaxReceiver
{
    public ClassDeclarationSyntax? ClassToAugment { get; private set; }
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // 决定是否我们对这里的业务逻辑感兴趣
        if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.Identifier.ValueText == "UserClass")
        {
            ClassToAugment = classDeclarationSyntax;
        }
    }
}