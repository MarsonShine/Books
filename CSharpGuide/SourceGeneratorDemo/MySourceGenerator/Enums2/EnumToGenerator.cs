using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MySourceGenerator.Enums;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace MySourceGenerator.Enums2;
/// <summary>
/// 以前的版本是直接集成 ISourceGenerator，现在最佳时间改为 IIncrementalGenerator
/// IIncrementalGenerator 只需要实现一个方法 Initialize()。在这个方法中，你可以注册你的"静态"源代码（如标记属性），也可以建立一个管道来识别感兴趣的语法，并将这些语法转化为源代码。
/// </summary>
[Generator]
internal class EnumToGenerator : IIncrementalGenerator
{
    private const string EnumExtensionsAttribute = "MySourceGenerator.Enums2.EnumExtensionsAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 添加标记特性给编译器
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "EnumExtensionsAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)
            ));
        // 管道化
        // 为枚举增加简单的过滤器
        IncrementalValuesProvider<EnumDeclarationSyntax?> enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // 通过特性选择目标枚举
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // 使用 [EnumExtensions] 属性选择枚举并提取详细信息
            .Where(static m => m is not null); // 不需要关心过滤器输出的错误

        IncrementalValueProvider<(Compilation Compilation, ImmutableArray<EnumDeclarationSyntax?> EnumDeclarationSyntaxes)> compilationAndEnums = context.CompilationProvider.Combine(enumDeclarations.Collect());

        // 上面的两部可以用 ForAttributeWithMetadataName 一步完成
        //IncrementalValuesProvider<EnumToGenerate?> enumsToGenerate = context.SyntaxProvider
        //    .ForAttributeWithMetadataName(
        //        "EnumExtensionsAttribute",
        //        predicate: static (s, _) => true,
        //        transform: static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode))
        //    .Where(static m => m is not null);

        // 为找到的每个枚举生成源代码
        context.RegisterSourceOutput(compilationAndEnums, static (spc, source) => Execute(source.Compilation, source.EnumDeclarationSyntaxes!, spc));




        #region 私有方法

        #endregion
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is EnumDeclarationSyntax m && m.AttributeLists.Count > 0;
    /// <summary>
    /// 在 GetSemanticTargetForGeneration() 中，我们会循环浏览通过前一个测试的每个节点，并查找我们的标记属性。如果节点具有该属性，我们就用它来创建一个 EnumToGenerate，即我们的数据模型类型。如果枚举没有标记属性，我们将返回空值，并在下一阶段将其过滤掉。
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    static EnumDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // 通过 IsSyntaxTargetForGeneration，我们知道该节点是 EnumDeclarationSyntax。
        var enumDeclarationSyntax = (EnumDeclarationSyntax)context.Node;
        // 循环遍历方法的所有属性
        foreach (AttributeListSyntax attributeListSyntax in enumDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                // 找到目标特性 [EnumExtensions]?
                if (fullName == EnumExtensionsAttribute)
                {
                    return enumDeclarationSyntax;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 运行完管道的这一阶段后，我们将拥有一个 EnumToGenerate 提供者，它会为每个用我们的属性装饰过的枚举返回一个新的 EnumToGenerate 实例。在 Execute 方法中，我们将其传递给 SourceGenerationHelper 类，以生成源代码，并将其添加到编译输出中。
    /// </summary>
    /// <param name="enumToGenerate"></param>
    /// <param name="context"></param>
    static void Execute(Compilation compilation, ImmutableArray<EnumDeclarationSyntax> enums, SourceProductionContext context)
    {
        if (enums.IsDefaultOrEmpty)
        {
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        IEnumerable<EnumDeclarationSyntax> distinctEnums = enums.Distinct();

        // Convert each EnumDeclarationSyntax to an EnumToGenerate
        List<EnumToGenerate> enumsToGenerate = GetTypesToGenerate(compilation, distinctEnums, context.CancellationToken);

        // If there were errors in the EnumDeclarationSyntax, we won't create an
        // EnumToGenerate for it, so make sure we have something to generate
        if (enumsToGenerate.Count > 0)
        {
            // generate the source code and add it to the output
            string result = SourceGenerationHelper.GenerateExtensionClass(enumsToGenerate);
            context.AddSource("EnumExtensions2.g.cs", SourceText.From(result, Encoding.UTF8));
        }

    }


    static List<EnumToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<EnumDeclarationSyntax> enums, CancellationToken ct)
    {
        var enumsToGenerate = new List<EnumToGenerate>();
        INamedTypeSymbol? enumAttribute = compilation.GetTypeByMetadataName(EnumExtensionsAttribute);
        if (enumAttribute == null)
        {
            // nothing to do if this type isn't available
            return enumsToGenerate;
        }

        foreach (var enumDeclarationSyntax in enums)
        {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            SemanticModel semanticModel = compilation.GetSemanticModel(enumDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            {
                // something went wrong
                continue;
            }

            string enumName = enumSymbol.ToString();
            string extensionName = "EnumExtensions";

            foreach (AttributeData attributeData in enumSymbol.GetAttributes())
            {
                if (!enumAttribute.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                {
                    continue;
                }

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

            foreach (ISymbol member in enumMembers)
            {
                if (member is IFieldSymbol field && field.ConstantValue is not null)
                {
                    members.Add(member.Name);
                }
            }

            enumsToGenerate.Add(new EnumToGenerate(extensionName, enumName, members));
        }

        return enumsToGenerate;
    }
}
