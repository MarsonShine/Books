using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

[Generator]
public class CustomGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("myGeneratedFile.cs", SourceText.From(
"""
namespace GeneratedNamespace
{
    public class GeneratedClass
    {
        public static void GeneratedMethod()
        {
            Console.WriteLine("GeneratedMethod...");
        }
    }
}
"""
        , Encoding.UTF8));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        
    }
}