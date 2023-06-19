using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace SourceGeneratorTests
{
    public class SourceGeneratorTests
    {
        [Fact]
        public void GeneratorTests()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}
");
            CustomGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // ����������ͨ��
            //��ע�⣺���������������ǲ��ɸı�ģ����еĵ��ö��᷵��һ�����µ������汾����Ӧ�������ĵ�����ʹ������汾����
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
            // ����
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            Debug.Assert(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            // ����ֱ�Ӳ鿴���
            var runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Debug.Assert(runResult.GeneratedTrees.Length == 1);
            Debug.Assert(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            Debug.Assert(generatorResult.Generator == generator);
            Debug.Assert(generatorResult.Diagnostics.IsEmpty);
            Debug.Assert(generatorResult.GeneratedSources.Length == 1);
            Debug.Assert(generatorResult.Exception is null);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}