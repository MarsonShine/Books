// See https://aka.ms/new-console-template for more information
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Console;

Console.WriteLine("Hello, World!");

const string programText =
@"using System;
using System.Collections.Generic;
using System.Text;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";

SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(programText);
CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

var compilation = CSharpCompilation.Create("HelloWorld")
    .AddReferences(
        MetadataReference.CreateFromFile(
            typeof(string).Assembly.Location)
    )
    .AddSyntaxTrees(syntaxTree);

// 语义模型查询
SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

// 绑定名称
UsingDirectiveSyntax usingSystem = root.Usings[0];
NameSyntax systemName = usingSystem.Name!;
// 根据语义模型查询符号信息
SymbolInfo systemSymbolInfo = semanticModel.GetSymbolInfo(systemName);
var systemSymbol = (INamespaceSymbol)systemSymbolInfo.Symbol!;
if (systemSymbol?.GetNamespaceMembers() is not null) {
    foreach (INamespaceSymbol ns in systemSymbol?.GetNamespaceMembers()!) {
        WriteLine(ns.Name);
    }
}

// 通过使用语义模型查找字符串字面量
LiteralExpressionSyntax helloWorldString = root.DescendantNodes()
.OfType<LiteralExpressionSyntax>()
.Single();
// 使用语义模型获取类型信息
TypeInfo litertalInfo = semanticModel.GetTypeInfo(helloWorldString);

var stringTypeSymbol = (INamedTypeSymbol)litertalInfo.Type!;

// 在 string 类型上申明的所有成员信息
// 包含很多信息：属性和字段
var allMembers = stringTypeSymbol?.GetMembers();
// 过滤
var methods = allMembers?.OfType<IMethodSymbol>();

// 返回公开的方法并返回string
var publicStringReturnMethods = methods?
    .Where(m=>SymbolEqualityComparer.Default.Equals(m.ReturnType, stringTypeSymbol) &&
        m.DeclaredAccessibility == Accessibility.Public);
// 筛选 name
var distinctMethods = publicStringReturnMethods?.Select(p=>p.Name).Distinct();

// 用 linq 查询上面等价的代码
var methods2 = from m in stringTypeSymbol?.GetMembers().OfType<IMethodSymbol>()
               where SymbolEqualityComparer.Default.Equals(m.ReturnType, stringTypeSymbol) &&
               m.DeclaredAccessibility == Accessibility.Public
               select m.Name;
foreach (var name in methods2)
{
    WriteLine(name);
}
