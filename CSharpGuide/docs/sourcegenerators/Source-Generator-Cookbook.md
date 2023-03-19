# Source Generators 指南

## 摘要

> 注意：source generator 的设计方案仍在审查中。本文档只使用了一种可能的语法，随着特性的发展，预计它将不另行通知地进行更改。

本文旨在通过提供一系列通用模式的指导，帮助您创建 source generator。它还旨在阐明在当前的设计下，哪些类型的生成器是可能的，以及在最终的交付特性设计中，哪些类型的生成器可能会明显超出范围。

该文件扩展了[完整设计文档](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md)中的细节，请确保你已经阅读了该文件。

## 提议

提醒一下，source generator 的高级设计目标是：

- 生成器生成一个或多个表示要添加到编译中的 c# 源代码的字符串。
- 只能添加（additive）。生成器可以向编译添加新的源代码，但不能修改现有的用户代码。
- 可以产生诊断。当无法生成源时，生成器可以通知用户这个问题。
- 可以访问其他文件，即非 c# 源文本。
- 如果无序运行，每个生成器将看到相同的输入编译，而不能访问其他源生成器创建的文件。
- 用户通过程序集列表指定要运行的生成器，这很像分析程序。

## 范围之外的设计

我们将简要地以不可解决的问题为例，说明 source generator 无法解决的问题：

### 语言特性

Source generator 并不是为了取代新的语言特性而设计的：例如，可以想象将 [record](https://github.com/dotnet/roslyn/blob/main/docs/features/records.md) 实现为一个 source generator，它将指定的语法转换为可编译的 c# 表示。

我们明确地认为这是一个反模式；该语言将继续发展并添加新特性，我们不指望源代码生成器能实现这一点。这样做会创建新的 c# “方言（dialects）”，与没有生成器的编译器不兼容。此外，由于生成器在设计上不能相互交互，以这种方式实现的语言特性很快就会与语言中其他添加的特性不兼容。

### 代码重写

如今，用户在程序集上执行许多后处理任务，这里我们将其广义地定义为“代码重写”。这些包括但不限于：

- 优化
- 日志注入
- IL 织入
- 调用点重写（call site rewriting）

虽然这些技术有许多有价值的用例，但它们并不适合 source generator 的思想。根据定义，代码更改操作这种操作它们是 source generator 提议明确排除的。

已经有很好的支持工具和技术来实现这些类型的操作，并且 source generator 的提议并不是为了取代它们。

## 约定

TODO：下面列出一组适用于所有设计的通用约定。例如，重用命名空间，生成的文件名等。

## 设计

本节按用户场景细分，首先列出通用解决方案，稍后列出更具体的示例。

### 生成类

**用户场景：**作为一名生成器作者，我希望能够在编译中添加一个可以被用户代码引用的类型。

**解决方案：**让用户编写代码，就像该类型已经存在一样。根据编译中可用的信息生成缺失的类型。

**例子：**

给定下面用户代码：

```c#
public partial class UserClass
{
    public void UserMethod()
    {
        // call into a generated method
        GeneratedNamespace.GeneratedClass.GeneratedMethod();
    }
}
```

上面的 `GeneratedNamespace.GeneratedClass.GeneratedMethod()` 就是由生成器生成的类型放法。在编译器编译之前这些信息都是缺失的。

然后我们创建一个生成器，在程序运行时它会创建这些丢失的类型：

```c#
[Generator]
public class CustomGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) {}

    public void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("myGeneratedFile.cs", SourceText.From(@"
namespace GeneratedNamespace
{
    public class GeneratedClass
    {
        public static void GeneratedMethod()
        {
            // generated code
        }
    }
}", Encoding.UTF8));
    }
}
```

### 附加文件转换

**用户场景：**作为一名生成器作者，我希望能够将外部非 c# 文件转换为等效的 c# 表示。

**解决方案：**使用 `GeneratorExecutionContext` 的附加文件属性来检索文件的内容，将其转换为 c# 表示并返回。

**例子：**

```c#
[Generator]
public class FileTransformGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) {}

    public void Execute(GeneratorExecutionContext context)
    {
        // find anything that matches our files
        var myFiles = context.AnalyzerOptions.AdditionalFiles.Where(at => at.Path.EndsWith(".xml"));
        foreach (var file in myFiles)
        {
            var content = file.GetText(context.CancellationToken);

            // do some transforms based on the file context
            string output = MyXmlToCSharpCompiler.Compile(content);

            var sourceText = SourceText.From(output, Encoding.UTF8);

            context.AddSource($"{file.Name}generated.cs", sourceText);
        }
    }
}
```

### 扩充用户代码

**用户场景：**作为一名生成器作者，我希望能够使用新功能检查和扩充用户的代码。

**解决方案：**要求用户将你想要增加的类设置为分部类（partial class），并将其标记为唯一属性或名称。注册一个 `SyntaxReceiver`，用来查找为生成的所有被标记的类并记录它们。在生成阶段检索填充的 `SyntaxReceiver`，并使用记录的信息生成包含附加功能的匹配部分类。

**例子：**

```c#
public partial class UserClass
{
    public void UserMethod()
    {
        // call into a generated method inside the class
        this.GeneratedMethod();
    }
}
```

```c#
[Generator]
public class AugmentingGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // the generator infrastructure will create a receiver and populate it
        // we can retrieve the populated instance via the context
        MySyntaxReceiver syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

        // get the recorded user class
        ClassDeclarationSyntax userClass = syntaxReceiver.ClassToAugment;
        if (userClass is null)
        {
            // if we didn't find the user class, there is nothing to do
            return;
        }

        // add the generated implementation to the compilation
        SourceText sourceText = SourceText.From($@"
public partial class {userClass.Identifier}
{{
    private void GeneratedMethod()
    {{
        // generated code
    }}
}}", Encoding.UTF8);
        context.AddSource("UserClass.Generated.cs", sourceText);
    }

    class MySyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax ClassToAugment { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Business logic to decide what we're interested in goes here
            if (syntaxNode is ClassDeclarationSyntax cds &&
                cds.Identifier.ValueText == "UserClass")
            {
                ClassToAugment = cds;
            }
        }
    }
}
```

### 问题分析

**用户场景：**作为生成器作者，我希望能够向用户编译添加诊断。

**解决方案：**诊断可以通过 `GeneratorExecutionContext.ReportDiagnostic()` 添加到编译中。这些可以用于响应用户编译的内容：例如，如果生成器期望得到一个格式良好的 `AdditionalFile`，但无法解析它，则生成器可以发出警告，通知用户不能继续生成。

对于基于代码的问题，生成器作者还应该考虑实现一个诊断分析程序，它可以识别问题，并提供一个代码修复程序来解决问题。

**例子：**

```c#
[Generator]
public class MyXmlGenerator : ISourceGenerator
{

    private static readonly DiagnosticDescriptor InvalidXmlWarning = new DiagnosticDescriptor(id: "MYXMLGEN001",
                                                                                              title: "Couldn't parse XML file",
                                                                                              messageFormat: "Couldn't parse XML file '{0}'.",
                                                                                              category: "MyXmlGenerator",
                                                                                              DiagnosticSeverity.Warning,
                                                                                              isEnabledByDefault: true);

    public void Execute(GeneratorExecutionContext context)
    {
        // Using the context, get any additional files that end in .xml
        IEnumerable<AdditionalText> xmlFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        foreach (AdditionalText xmlFile in xmlFiles)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string text = xmlFile.GetText(context.CancellationToken).ToString();
            try
            {
                xmlDoc.LoadXml(text);
            }
            catch (XmlException)
            {
                // issue warning MYXMLGEN001: Couldn't parse XML file '<path>'
                context.ReportDiagnostic(Diagnostic.Create(InvalidXmlWarning, Location.None, xmlFile.Path));
                continue;
            }

            // continue generation...
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

### INotifyPropertyChanged

**用户场景：**作为一个生成器作者，我希望能够为用户自动实现 `INotifyPropertyChanged` 模式。

**解决方案：**设计租户“仅显式添加”似乎与实现这一点的能力有直接的冲突，似乎要求用户修改代码。然而，我们可以利用显式字段，而不是编辑用户属性，直接为列出的字段提供它们。

**例子：**

给定下面用户类：

```c#
using AutoNotify;

public partial class UserClass
{
    [AutoNotify]
    private bool _boolProp;

    [AutoNotify(PropertyName = "Count")]
    private int _intProp;
}
```

生成器会生成如下代码：

```c#
using System;
using System.ComponentModel;

namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}


public partial class UserClass : INotifyPropertyChanged
{
    public bool BoolProp
    {
        get => _boolProp;
        set
        {
            _boolProp = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserBool"));
        }
    }

    public int Count
    {
        get => _intProp;
        set
        {
            _intProp = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
```

### 生成器打包成 NuGet 包

**用户场景：**作为一个生成器的作者，我想把我的生成器打包成一个 NuGet 包供使用。

**解决方案：**生成器可以像 Analyzer 一样使用相同的方法打包。确保生成器放在软件包的 `analyzers\dotnet\cs` 文件夹中，以便在安装时自动添加到用户项目中。

例如，要在构建时将你的生成器项目转换为 NuGet 包，在你的项目文件中添加以下内容：

```xml
<PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
</PropertyGroup>

<ItemGroup>
    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```

### 使用 NuGet 包中的功能

**用户场景：**作为一名生成器作者，我希望依靠生成器内部 NuGet 包提供的功能。

**解决方案：**可以依赖于生成器内部的 NuGet 包，但在分发时必须特别考虑。

任何运行时依赖项(即最终用户程序需要依赖的代码)都可以通过通常的引用机制作为生成器 NuGet 包的依赖项简单地添加进去。

例如，考虑一个生成器，它创建依赖于 Newtonsoft.Json 的代码。生成器并不直接使用依赖项，它只是发出依赖于用户编译中引用的库的代码。作者将添加对`Newtonsoft.Json` 作为公共依赖，当用户添加生成器包时，它将被自动引用。

生成器可以检查编译是否存在 `Newtonsoft.Json` 程序集，如果不存在，会发出警告或错误。

```xml
<Project>
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Take a public dependency on Json.Net. Consumers of this generator will get a reference to this package -->
    <PackageReference Include="Newtonsoft.Json" Version="12.0.1" />

    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
```

```c#
using System.Linq;

[Generator]
public class SerializingGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // check that the users compilation references the expected library 
        if (!context.Compilation.ReferencedAssemblyNames.Any(ai => ai.Name.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase)))
        {
            context.ReportDiagnostic(/*error or warning*/);
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

但是，任何生成时依赖项(即生成器在运行和生成代码时使用的依赖项)都必须直接打包在生成器 NuGet 包中。这方面没有自动工具，您需要手动指定要包含的依赖项。

考虑一个使用 `Newtonsoft.Json` 的生成器。在生成过程中，将某些内容编码为 json，但不会发出任何依赖于它在运行时存在的代码。**作者将添加对 `Newtonsoft.Json` 的引用，但使其所有资产为私有；这确保了生成器的使用者不会继承对库的依赖。**

然后，作者必须将 `Newtonsoft.Json` 打包到生成器里面一起作为 NuGet 包。这可以通过以下方式实现：通过设置 `GeneratePathProperty="true"` 来设置依赖来生成一个路径属性。这将创建一个新的 MSBuild 属性，格式为 `PKG<PackageName>`，其中 `<PackageName>` 是使用 `.` 替换 `_` 的包名。在我们的例子中，会有一个名为 `PKGNewtonsoft_Json` 的 MSBuild 属性，它的值指向在磁盘路径上的 NuGet 文件的二进制内容。然后，我们可以使用它将二进制文件添加到生成的 NuGet 包中，就像我们使用生成器一样：

```xml
<Project>
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Take a private dependency on Newtonsoft.Json (PrivateAssets=all) Consumers of this generator will not reference it.
         Set GeneratePathProperty=true so we can reference the binaries via the PKGNewtonsoft_Json property -->
    <PackageReference Include="Newtonsoft.Json" Version="12.0.1" PrivateAssets="all" GeneratePathProperty="true" />

    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />

    <!-- Package the Newtonsoft.Json dependency alongside the generator assembly -->
    <None Include="$(PkgNewtonsoft_Json)\lib\netstandard2.0\*.dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
```

```c#
[Generator]
public class JsonUsingGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // use the newtonsoft.json library, but don't add any source code that depends on it

        var serializedContent = Newtonsoft.Json.JsonConvert.SerializeObject(new { a = "a", b = 4 });
        // 是为了避免Liquid模板引擎对双括号的报错的问题
        // Liquid模板引擎不支持双大括号内部的大括号，而在你的代码中，serializedContent变量的值包含了双大括号。可以通过在大括号前加上一个反斜杠来转义它们，以避免这个问题，例如：
		var escapedContent = serializedContent.Replace("{", "{{").Replace("}", "}}").Replace("{{", "{{\"{{\"}}").Replace("}}", "{{\"}}\"}}");

        context.AddSource("myGeneratedFile.cs", SourceText.From($@"
namespace GeneratedNamespace
{{
    public class GeneratedClass
    {{
        public static const SerializedContent = @""{escapedContent}"";
    }}
}}", Encoding.UTF8));

    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

### 访问分析器配置属性

**实现状态：**VS 16.7 preview3 及以上可用

**用户场景：**

- 作为生成器作者，我希望访问语法树或其他文件的分析器配置属性。
- 作为生成器作者，我希望访问自定义生成器输出的键值对。
- 作为一个生成器的用户，我希望能够自定义生成的代码和覆盖默认值。

**解决方案：**生成器可以通过 `GeneratorExecutionContext` 的 `AnalyzerConfigOptions` 属性访问分析器的配置值。分析器配置值可以在 `SyntaxTree`、`AdditionalFile` 的上下文中访问，也可以通过 `GlobalOptions` 全局访问。全局选项是“外界的（ambient）”，因为它们不适用于任何特定的上下文，但将包括在一个特定的上下文请求选项。

生成器可以自由地使用全局选项来定制其输出。例如，考虑一个可以选择性地发出日志记录的生成器。作者可以选择检查全局分析器配置值的值，以控制是否发出日志代码。然后用户可以通过 `.editorconfig` 文件启用每个项目的设置：

```
mygenerator_emit_logging = true
```

```c#
[Generator]
public class MyGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // control logging via analyzerconfig
        bool emitLogging = false;
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("mygenerator_emit_logging", out var emitLoggingSwitch))
        {
            emitLogging = emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // add the source with or without logging...
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

### 使用 MSBuild 属性和元数据

**实现状态：**VS 16.7 preview3 及以上可用

**用户场景：**

- 作为一个生成器作者，我希望根据项目文件中包含的值做出决策
- 作为一个生成器的用户，我希望能够自定义生成的代码和覆盖默认值。

**解决方案：**MSBuild 将自动将指定的属性和元数据转换为一个全局分析器配置，生成器可以读取该配置。生成器作者通过向 `CompilerVisibleProperty` 和 `CompilerVisibleItemMetadata` 条目组（item group）添加条目来指定它们想要提供的属性和元数据。当将生成器打包为 NuGet 包时，这些可以通过那些属性或目标文件添加。

例如，考虑一个基于附加文件创建源的生成器，并希望允许用户通过项目文件启用或禁用日志记录。作者会在他们的 props 文件中指定他们想让指定的 MSBuild 对编译器可见的属性：

```xml
<ItemGroup>
    <CompilerVisibleProperty Include="MyGenerator_EnableLogging" />
</ItemGroup>
```

在构建之前，属性 `MyGenerator_EnableLogging` 的值将被发送到生成的分析器配置文件中，其名称为 `build_property.MyGenerator_EnableLogging`。生成器可以通过 `GeneratorExecutionContext` 的 `AnalyzerConfigOptions` 属性读取这个属性：

```c#
context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out var emitLoggingSwitch);
```

因此，用户可以通过在项目文件中设置属性来启用或禁用日志记录。

现在，考虑到生成器作者希望有选择地允许在每个附加文件的基础上选择进入/退出日志。作者可以通过添加到 `CompilerVisibleItemMetadata` 条目组，请求 MSBuild 发出指定文件的元数据值。作者指定了他们想要从其中读取元数据的 `MSBuild` 条目类型（itemType）(在本例中为 `AdditionalFiles`)，以及他们想要为他们检索的元数据的名称。

```xml
<ItemGroup>
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="MyGenerator_EnableLogging" />
</ItemGroup>
```

对于编译中的每个附加文件，`MyGenerator_EnableLogging` 的值将被发送到生成的分析器配置文件中，并带有一个名为 `build_metadata.AdditionalFiles.MyGenerator_EnableLogging` 的项。生成器可以在每个附加文件的上下文中读取这个值：

```c#
foreach (var file in context.AdditionalFiles)
{
    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.MyGenerator_EnableLogging", out var perFileLoggingSwitch);
}
```

在用户项目文件中，用户现在可以注释单独的附加文件，以说明他们是否希望启用日志记录：

```xml
<ItemGroup>
    <AdditionalFiles Include="file1.txt" />  <!-- logging will be controlled by default, or global value -->
    <AdditionalFiles Include="file2.txt" MyGenerator_EnableLogging="true" />  <!-- always enable logging for this file -->
    <AdditionalFiles Include="file3.txt" MyGenerator_EnableLogging="false" /> <!-- never enable logging for this file -->
</ItemGroup>
```

**完整的例子：**

MyGenerator.props:

```xml
<Project>
    <ItemGroup>
        <CompilerVisibleProperty Include="MyGenerator_EnableLogging" />
        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="MyGenerator_EnableLogging" />
    </ItemGroup>
</Project>
```

MyGenerator.csproj:

```xml
<Project>
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />

    <!-- Package the props file -->
    <None Include="MyGenerator.props" Pack="true" PackagePath="build" Visible="false" />
  </ItemGroup>
</Project>
```

MyGenerator.cs:

```c#
[Generator]
public class MyGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // global logging from project file
        bool emitLoggingGlobal = false;
        if(context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out var emitLoggingSwitch))
        {
            emitLoggingGlobal = emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        foreach (var file in context.AdditionalFiles)
        {
            // allow the user to override the global logging on a per-file basis
            bool emitLogging = emitLoggingGlobal;
            if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.MyGenerator_EnableLogging", out var perFileLoggingSwitch))
            {
                emitLogging = perFileLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // add the source with or without logging...
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

### 生成器的单元测试

**用户场景：**作为生成器作者，我希望能够对生成器进行单元测试，以简化开发并确保正确性。

**解决方案A：**

推荐的方式就是使用 [Microsoft.CodeAnalysis.Testing](https://github.com/dotnet/roslyn-sdk/tree/main/src/Microsoft.CodeAnalysis.Testing#microsoftcodeanalysistesting) 包：

- `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.MSTest`
- `Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.MSTest`
- `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.NUnit`
- `Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.NUnit`
- `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit`
- `Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.XUnit`

这与分析程序和代码修复程序测试的工作方式相同。你可以像下面这样添加一个类：

```c#
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : ISourceGenerator, new()
{
    public class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
    {
        public Test()
        {
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
           var compilationOptions = base.CreateCompilationOptions();
           return compilationOptions.WithSpecificDiagnosticOptions(
                compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
        }

        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = { "/warnaserror:nullable" };
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

            return nullableWarnings;
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
        }
    }
}
```

然后，在你的测试文件中：

```c#
using VerifyCS = CSharpSourceGeneratorVerifier<YourGenerator>;
```

接着在测试文件中使用下面的代码：

```c#
var code = "initial code"
var generated = "expected generated code";
await new VerifyCS.Test
{
    TestState = 
    {
        Sources = { code },
        GeneratedSources =
        {
            (typeof(YourGenerator), "GeneratedFileName", SourceText.From(generated, Encoding.UTF8, SourceHashAlgorithm.Sha256)),
        },
    },
}.RunAsync();
```

**解决方案B：**

另一种方法是不使用测试库，用户可以直接在单元测试中托管 `GeneratorDriver`，这使得代码的生成器部分相对容易进行单元测试。用户需要为生成器提供一个编译来进行操作，然后可以探测结果编译，或者驱动程序的 `GeneratorDriverRunResult`，以查看由生成器添加的各个项。

从添加一个源文件的基本生成器开始：

```c#
[Generator]
public class CustomGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) {}

    public void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("myGeneratedFile.cs", SourceText.From(@"
namespace GeneratedNamespace
{
    public class GeneratedClass
    {
        public static void GeneratedMethod()
        {
            // generated code
        }
    }
}", Encoding.UTF8));
    }
}
```

作为用户，我们可以在单元测试中寄宿（host）它，像下面这样：

```c#
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GeneratorTests.Tests
{
    [TestClass]
    public class GeneratorTests
    {
        [TestMethod]
        public void SimpleGeneratorTest()
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

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            CustomGenerator generator = new CustomGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            Debug.Assert(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

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
```

注意:上面的例子使用了 MSTest，但是测试的内容很容易适应其他框架，比如 XUnit。

### 参与 IDE 体验

**实现状态：**没有实现

**用户场景：**作为一个生成器作者，我希望能够在用户编辑文件时交互式地重新生成代码。

**解决方案：**我们希望能够实现一组交互式回调，以允许越来越复杂的生成策略。预计将会有一种机制来**提供符号映射（symbol mapping）**来点亮如“查找所有引用”这样的特性。

```c#
[Generator]
public class InteractiveGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register for additional file callbacks
        context.RegisterForAdditionalFileChanges(OnAdditionalFilesChanged);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // generators must always support a total generation pass
    }

    public void OnAdditionalFilesChanged(AdditionalFilesChangedContext context)
    {
        // determine which file changed, and if it affects this generator
        // regenerate only the parts that are affected by this change.
    }
}
```

注意：在这些接口可用之前，生成器作者不应该尝试模拟缓存到磁盘和自定义最新检查的“增量”。编译器目前没有为生成器提供可靠的方法来检测它是否适合使用以前的运行，任何尝试这样做的尝试都可能导致用户很难诊断错误。生成器作者应该总是假设这是第一次发生的“完整”生成。

### 序列化

**用户场景**

序列化通常使用动态分析（dynamic analysis）来实现，即序列化器通常使用反射来检查给定类型的运行时状态并生成序列化逻辑。这可能既昂贵又脆弱。如果编译时类型和运行时类型相似，那么将大部分成本转移到编译时而不是运行时，这是有用的。

source generator 提供了一种方法来做到这一点。由于 source generator 可以像分析器一样通过 NuGet 交付，我们预计这将是一个 source generator 库的用例，而不是每个人都构建自己的源代码生成器库。

**解决方案**

首先，生成器需要一些方法来发现哪些类型应该是可序列化的。这个指标可以是一个属性，例如。

```c#
[GeneratorSerializable]
partial class MyRecord
{
    public string Item1 { get; }
    public int Item2 { get; }
}
```

当该特性的全部范围都被设计好时，这个属性也可以用于[参与IDE体验](#参与 IDE 体验)。在这种情况下，不是让生成器找到每个用给定属性标记的类型，而是由编译器通知生成器每个用给定属性标记的类型。现在，我们假设这些类型是提供给我们的。

第一个任务是决定我们想要序列化返回什么。假设我们做了一个简单的 JSON 序列化，它生成如下所示的字符串：

```json
{
    "Item1": "abc",
    "Item2": 11,
}
```

为此，我们可以像下面这样为记录类型添加一个 `Serialize` 方法：

```c#
public string Serialize()
{
    var sb = new StringBuilder();
    sb.AppendLine("{");
    int indent = 8;

    // Body
    addWithIndent($"\"Item1\": \"{this.Item1.ToString()}\",");
    addWithIndent($"\"Item2\": {this.Item2.ToString()},");

    sb.AppendLine("}");

    return sb.ToString();

    void addWithIndent(string s)
    {
        sb.Append(' ', indent);
        sb.AppendLine(s);
    }
}
```

显然，这是非常简单的 —— 这个示例只正确地处理字符串和 int 类型，在 json 输出中添加一个末尾逗号，并且没有错误恢复，但它应该用来演示源代码生成器可以添加到编译中的代码类型。

我们下一个任务就是生成器生成上面的代码，因为上面的代码是根据类的实际属性在 `// Body` 部分中自定义的。换句话说，我们需要生成将生成 JSON 格式的代码。这是一个生成器生成的。

让我们从一个基本的模板开始。我们添加了一个完整的 source generator，因此我们需要生成一个与输入类同名的类，它有一个名为 `Serialize` 的公共方法，还有一个填充区，在这里我们可以写出属性。

```c#
string template = @"
using System.Text;
partial class {0}
{{
    public string Serialize()
    {{
        var sb = new StringBuilder();
        sb.AppendLine(""{{"");
        int indent = 8;

        // Body
{1}

        sb.AppendLine(""}}"");

        return sb.ToString();

        void addWithIndent(string s)
        {{
            sb.Append(' ', indent);
            sb.AppendLine(s);
        }}
    }}
}}";
```

现在我们已经知道了代码的一般结构，我们需要检查输入类型并找到要填写的所有正确信息。这些信息在我们示例中的 c# SyntaxTree 中都是可用的。假设我们得到了一个 `ClassDeclarationSyntax`，它被确认附加了一个 generation 属性。然后我们可以获取类的名称和它的属性名称，如下所示：

```c#
private static string Generate(ClassDeclarationSyntax c)
{
    var className = c.Identifier.ToString();
    var propertyNames = new List<string>();
    foreach (var member in c.Members)
    {
        if (member is PropertyDeclarationSyntax p)
        {
            propertyNames.Add(p.Identifier.ToString());
        }
    }
}
```

这就是我们所需要的。如果属性的序列化值是它们的字符串值，生成的代码只需要对它们调用 `ToString()`。剩下的唯一问题是在文件的顶部放置 `using` 。因为我们的模板使用了字符串构建器，所以我们需要 `System.Text`，但所有其他类型看起来都是基元类型，所以这就是我们需要的。把它们放在一起：

```c#
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
            break;
        }
    }

    return $@"
using System.Text;
partial class {c.Identifier.ToString()}
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
```

这与其他序列化示例清晰地联系在一起。通过在编译的 SyntaxTrees 中找到所有合适的类声明，并将它们传递给上面的 Generate 方法，我们可以为选择生成序列化的每种类型构建新的分部类。与其他技术不同的是，这种序列化机制完全发生在编译时，可以专门针对在用户类中编写的内容。

### 自动接口实现

TODO：

## 破坏性变更

**实现状态：**Visual Studio 16.8 preview 3 / roslyn 3.8.0-3.final 实现

在预览和发布之间，引入了以下破坏性变更：

**`SourceGeneratorContext`** 重命名为 **`GeneratorExecutionContext`**

**`IntializationContext`** 重命名为 **`GeneratorInitializationContext`**

这将影响到用户创建的生成器，因为这意味着基本界面将变成：

```c#
public interface ISourceGenerator
{
    void Initialize(GeneratorInitializationContext context);
    void Execute(GeneratorExecutionContext context);
}
```

用户试图使用针对 Roslyn 的更新版本的预览 api 的生成器，将会看到类似的异常：

```bash
CSC : warning CS8032: An instance of analyzer Generator.HelloWorldGenerator cannot be created from Generator.dll : Method 'Initialize' in type 'Generator.HelloWorldGenerator' from assembly 'Generator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' does not have an implementation. [Consumer.csproj]
```

用户需要执行的操作是重命名 Initialize 和 Execute 方法的参数类型得以匹配。

**`RunFullGeneration`** 重命名为 **`RunGeneratorsAndUpdateCompilation`**

**`CSharpGeneratorDriver`** 增加静态方法 **`Create()`** 以及过时的构造函数。

这将影响所有使用 `CSharpGeneratorDriver` 编写单元测试的生成器作者。要创建一个新的生成器驱动实例，用户不应该再调用 new，而是使用`CSharpGeneratorDriver.Create()` 重载。用户应该不再使用 `RunFullGeneration` 方法，而是使用相同的参数调用 `RunGeneratorsAndUpdateCompilation`。

## 已知问题

本节跟踪其他杂项 TODO 项：

**目标框架**如果我们对生成器有框架要求，可能需要提一下，例如，它们必须以 `netstandard2.0` 或类似的标准为目标。

**约定：**（查看上面的[约定](#约定)小节）。我们向用户建议什么标准约定？

**部分方法：**我们是否应该提供一个包含部分方法的场景？理由：

- 命名控制。开发人员可以控制成员的名称
- 生成是可选的/依赖于其他状态。根据其他信息，生成器可能决定不需要该方法。

**特性检测**：演示如何创建依赖于特定目标框架特性的生成器，而不依赖于 TargetFramework 属性。
