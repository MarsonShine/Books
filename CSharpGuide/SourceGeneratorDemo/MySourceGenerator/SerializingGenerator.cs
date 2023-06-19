using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MySourceGenerator;
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

        //context.AddSource("Serializer.g.cs", SourceText.From(GeneratorHelper.Attribute, Encoding.UTF8));
        var serializationSyntaxReceiver = (SerializationSyntaxReceiver)context.SyntaxContextReceiver!;
        if (serializationSyntaxReceiver == null)
            return;

        //string result = GeneratorHelper.GenerateExtensionClass(enumSyntaxReceiver.EnumsToGenerate);
        //context.AddSource("EnumExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static string Generate(ClassDeclarationSyntax c)
    {
        var sb = new StringBuilder();
        int indent = 8;
        foreach (var member in c.Members)
        {
            if (member is PropertyDeclarationSyntax p)
            {
                var name = p.Identifier.ToString();
                appendWithIndent($"addWithIndent($\"\\\"{name}\\\": ");
                if (p.Type.ToString() != "int")
                {
                    sb.Append("\\\"");
                }
                sb.Append($"{{this.{name}.ToString()}}");
                if (p.Type.ToString() != "int")
                {
                    sb.Append("\\\"");
                }
                sb.AppendLine(",\");");
            }
        }

        return $@"
using System.Text;
partial class {c.Identifier}
{{
    public string Serialize()
    {{
        var sb = new StringBuilder();
        sb.AppendLine(""{{"");
        int indent = 8;

        // Body
{sb.ToString()}

        sb.AppendLine(""}}"");

        return sb.ToString();

        void addWithIndent(string s)
        {{
            sb.Append(' ', indent);
            sb.AppendLine(s);
        }}
    }}
}}";
        void appendWithIndent(string s)
        {
            sb.Append(' ', indent);
            sb.Append(s);
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SerializationSyntaxReceiver());
    }

    internal class SerializationSyntaxReceiver : ISyntaxContextReceiver
    {
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            throw new NotImplementedException();
        }
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