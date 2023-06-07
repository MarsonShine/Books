using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MySourceGenerator
{
    // 附加文件转换
    [Generator]
    public class FileTransformGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
            var myFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".xml"));
            foreach (var file in myFiles)
            {
                var text = file.GetText(context.CancellationToken);
                var sourceText = SourceText.From(Transform(text!.ToString()), Encoding.UTF8);
                context.AddSource($"Transformed_{file.Path}generated.txt", sourceText);
            }
        }

        private string Transform(string content)
        {
            //string output = MyXmlToCSharpCompiler.Compile(content);
            return content;
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}
