using Microsoft.CodeAnalysis;
using System;

/*
 访问语法树或其他文件的分析器配置属性。
 访问自定义生成器输出的键值对。
 自定义生成的代码并覆盖默认值

 还可以使用 MSBuild 属性和元数据，这样可以根据项目文件中包含的值去做不同的逻辑
 通过将项目添加到 CompilerVisibleProperty 和 CompilerVisibleItemMetadata 项目组来指定他们想要提供的属性和元数据。
 */
namespace MySourceGenerator
{
    public class MyGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            bool emitLoggingGlobal = false;
            // 访问分析器配置属性 .editorconfig 文件
            if(context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("mygenerator_emit_logging",out var emitLoggingSwitch))
            {
                emitLoggingGlobal = emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            // 根据 emitLogging 来判断是否记录日志
            if(emitLoggingGlobal)
            {

            }

            // 访问项目的MSBuild配置属性信息
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out emitLoggingSwitch))
            {
                emitLoggingGlobal = emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            foreach (var file in context.AdditionalFiles)
            {
                bool emitLogging = emitLoggingGlobal;
                // 然后在项目文件的 <AdditionalFiles> 中添加的各种文件来说明是否需要开启日志记录
                if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFields.MyGenerator_EnableLogging",out var perFileLoggingSwitch))
                {
                    emitLogging = perFileLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            
        }
    }
}
