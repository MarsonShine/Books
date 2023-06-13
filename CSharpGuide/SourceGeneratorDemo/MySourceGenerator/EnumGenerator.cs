using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace MySourceGenerator
{
    [Generator]
    public class EnumGenerator : ISourceGenerator
    {
        public const string EnumExtensionsAttribute = "MySourceGenerator.EnumExtensionsAttribute";
        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("EnumExtensionsAttribute.g.cs", SourceText.From(GeneratorHelper.Attribute, Encoding.UTF8));
            var enumSyntaxReceiver = (EnumSyntaxReceiver)context.SyntaxContextReceiver!;
            if (enumSyntaxReceiver == null)
                return;
            if (enumSyntaxReceiver.EnumsToGenerate.Count > 0)
            {
                string result = GeneratorHelper.GenerateExtensionClass(enumSyntaxReceiver.EnumsToGenerate);
                context.AddSource("EnumExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EnumSyntaxReceiver());
        }
    }

    internal class EnumSyntaxReceiver : ISyntaxContextReceiver
    {
        private readonly List<EnumToGenerate> enumsToGenerate = new();

        public List<EnumToGenerate> EnumsToGenerate => enumsToGenerate;

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is EnumDeclarationSyntax m && m.AttributeLists.Count > 0)
            {
                var enumDeclarationSyntax = GetSemanticTargetForGeneration(context);
                if (enumDeclarationSyntax == null)
                    return;

                if (context.SemanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
                {
                    // something went wrong
                    return;
                }

                string enumName = enumSymbol.ToString();
                string extensionName = "EnumExtensions";

                foreach (AttributeData attributeData in enumSymbol.GetAttributes())
                {
                    foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
                    {
                        if (namedArgument.Key == "ExtensionClassName"
                            && namedArgument.Value.Value?.ToString() is { } n)
                        {
                            extensionName = n;
                        }
                    }
                    break;
                }

                ImmutableArray<ISymbol> enumMembers = enumSymbol.GetMembers();
                var members = new List<string>(enumMembers.Length);
                var descriptions = new List<string>(enumMembers.Length);

                foreach (ISymbol member in enumMembers)
                {
                    if (member is IFieldSymbol field && field.ConstantValue is not null)
                    {
                        members.Add(member.Name);
                    }
                    string description = member.Name; // 没有注释则默认value
                    var descriptionAttribute = member.GetAttributes().FirstOrDefault(p => p.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute");
                    if (descriptionAttribute != null)
                        description = descriptionAttribute.ConstructorArguments.FirstOrDefault().Value!.ToString();
                    descriptions.Add(description);
                }

                enumsToGenerate.Add(new EnumToGenerate(extensionName, enumName, members, descriptions));
            }
        }

        static EnumDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // we know the node is a EnumDeclarationSyntax thanks to IsSyntaxTargetForGeneration
            var enumDeclarationSyntax = (EnumDeclarationSyntax)context.Node;

            // loop through all the attributes on the method
            foreach (AttributeListSyntax attributeListSyntax in enumDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                    string? fullName = context.SemanticModel.GetTypeInfo(attributeSyntax).Type?.ToDisplayString();
                    // Is the attribute the [EnumExtensions] attribute?
                    if (fullName == "EnumExtensions")
                    {
                        // return the enum
                        return enumDeclarationSyntax;
                    }
                }
            }

            // we didn't find the attribute we were looking for
            return null;
        }
    }

    public readonly struct EnumToGenerate
    {
        public readonly string ExtensionName;
        public readonly string Name;
        public readonly List<string> Values;
        public readonly List<string> Descriptions;

        public EnumToGenerate(string extensionName, string name, List<string> values, List<string> descriptions)
        {
            Name = name;
            Values = values;
            ExtensionName = extensionName;
            Descriptions = descriptions;
        }
    }
}
