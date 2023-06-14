using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Console;

namespace SyntaxWalker
{
    /// <summary>
    /// 只关心特定指定操作（如 using 指令）
    /// </summary>
    public class UsingCollector : CSharpSyntaxWalker
    {
        public ICollection<UsingDirectiveSyntax> Usings { get; } = new List<UsingDirectiveSyntax>();

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            WriteLine($"\tVisitUsingDirective called with {node.Name}.");
            if (node?.Name?.ToString() != "System" && !node!.Name!.ToString().StartsWith("System."))
            {
                WriteLine($"\t\tSuccess. Adding {node.Name}.");
                Usings.Add(node);
            }
        }
    }
}
