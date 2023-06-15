using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace MySourceGenerator
{
    // 分析、报告警告，不符合xml格式的编译器会提示异常
    public class MyXmlGenerator : ISourceGenerator
    {
        // private static readonly DiagnosticDescriptor InvalidXmlWarning = new(
        //     id: "MYXMLGEN001",
        //     title: "Couldn't parse XML file",
        //     messageFormat: "Couldn't parse XML file '{0}'",
        //     category: "MyXmlGenerator",
        //     defaultSeverity: DiagnosticSeverity.Warning,
        //     isEnabledByDefault: true);
        public void Execute(GeneratorExecutionContext context)
        {
            // Using the context, get any additional files that end in .xml
            // IEnumerable<AdditionalText> xmlFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            // foreach (AdditionalText xmlFile in xmlFiles)
            // {
            //     XmlDocument xmlDoc = new();
            //     string text = xmlFile.GetText(context.CancellationToken)!.ToString();
            //     try
            //     {
            //         xmlDoc.LoadXml(text);
            //     }
            //     catch (XmlException)
            //     {
            //         // issue warning MYXMLGEN001: Couldn't parse XML file '<path>'
            //         context.ReportDiagnostic(Diagnostic.Create(InvalidXmlWarning, Location.None, xmlFile.Path));
            //         continue;
            //     }

            //     // continue generation...
            // }
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}
