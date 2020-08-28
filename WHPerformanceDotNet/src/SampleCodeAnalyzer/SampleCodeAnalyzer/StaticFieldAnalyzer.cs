using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace SampleCodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StaticFieldAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "StaticFieldAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.StaticReadOnlyAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.StaticReadOnlyAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.StaticReadOnlyAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Thread Safety";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSymbolAction(AnalyzeFieldSymbol, SymbolKind.Field);
        }

        private void AnalyzeFieldSymbol(SymbolAnalysisContext context)
        {
            IFieldSymbol field = (IFieldSymbol)context.Symbol;
            if (field.IsStatic && !field.IsReadOnly)
            {
                var diagnostic = Diagnostic.Create(Rule, field.Locations[0], field.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
