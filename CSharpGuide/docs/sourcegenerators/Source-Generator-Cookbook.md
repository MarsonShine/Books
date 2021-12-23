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

        context.AddSource("myGeneratedFile.cs", SourceText.From($@"
namespace GeneratedNamespace
{{
    public class GeneratedClass
    {{
        public static const SerializedContent = {serializedContent};
    }}
}}", Encoding.UTF8));

    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

### 访问分析器配置属性

TODO
