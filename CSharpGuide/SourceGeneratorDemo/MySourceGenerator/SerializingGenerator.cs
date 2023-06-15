using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

public class SerializingGenerator : ISourceGenerator
{
    private static readonly DiagnosticDescriptor MissingReferenceLib = new(
            id: "REFLIBGEN001",
            title: "未加载依赖项",
            messageFormat: "未加载依赖项:{0}",
            category: "SerializingGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    public void Execute(GeneratorExecutionContext context)
    {
        // 检查用户编译是否引入预期的依赖库
        if (!context.Compilation.ReferencedAssemblyNames.Any(ai => ai.Name.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase)))
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingReferenceLib, Location.None, "Newtonsoft.Json"));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {

    }
}

public class JsonUsingGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var serializedContent = Newtonsoft.Json.JsonConvert.SerializeObject(new { a = "a", b = 4 });
        context.AddSource("myGeneratedFile.cs", SourceText.From($@"
namespace GeneratedNamespace
{{
    public class GeneratedClass
    {{
        public static const SerializedContent = {serializedContent};
    }}
}}", Encoding.UTF8));
    }

    public void Initialize(GeneratorInitializationContext context)
    {

    }
}