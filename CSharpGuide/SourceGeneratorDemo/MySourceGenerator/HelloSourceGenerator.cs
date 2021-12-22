using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MySourceGenerator
{
    [Generator]
    public class HelloSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // 代码生成在这里执行
            // 查找入口方法
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken)!;

            // 生成的具体内容
            string source = $@"// 自动生成代码，此文件无法编辑
using System;
namespce {mainMethod.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {mainMethod.ContainingType.Name}
    {{
        static partial void HelloFrom(string name) =>
            Console.WriteLine($""Generator says: Hi from '{{name}}'"");
    }}
}}
";
            var typeName = mainMethod.ContainingType.Name;
            // 添加资源编译
            context.AddSource($"{typeName}.g.cs", source);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // 这个不需要初始化
            System.Diagnostics.Debugger.Launch();
        }
    }
}
