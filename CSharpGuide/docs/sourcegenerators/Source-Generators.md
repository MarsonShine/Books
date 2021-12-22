# Source Generator 文档

## 摘要

Source Generator 的目标是支持编译时元编程，即可以**在编译时创建并添加到编译中的代码**。Source Generator 将能够在运行之前读取编译的内容，以及访问任何附加文件，从而使生成器能够内省（introspect）用户的c#代码和生成器特定的文件。

## 高级设计目标

- 生成器生成一个或多个表示要添加到编译中的c#源代码的字符串。
- 只能添加（additive）。生成器可以向编译添加新的源代码，但不能修改现有的用户代码。
- 可以产生诊断。当无法生成源时，生成器可以通知用户这个问题。
- 可以访问其他文件，即非 c# 源文本。
- 如果无序运行，每个生成器将看到相同的输入编译，而不能访问其他源生成器创建的文件。
- 用户通过程序集列表指定要运行的生成器，这很像分析程序。

## 实现

最简单的生成器例子就是实现 `Microsoft.CodeAnalysis.ISourceGenerator`

```c#
namespace Microsoft.CodeAnalysis
{
    public interface ISourceGenerator
    {
        void Initialize(GeneratorInitializationContext context);
        void Execute(GeneratorExecutionContext context);
    }
}
```

生成器的实现定义在传递给编译器的外部程序集中，使用与诊断分析程序相同的 `-analyzer:` 选项。实现需要用 `Microsoft.CodeAnalysis.GeneratorAttribute` 属性进行注释。

一个组件可以包含诊断分析仪和 `source generator` 的混合。**由于生成器是从外部程序集加载的，因此生成器不能用于构建定义它的程序集。**

`ISourceGenerator` 有一个 `Initialize` 方法，该方法由宿主（host）( IDE 或命令行编译器) 明确只调用一次。`Initialize` 传递一个`GeneratorInitializationContext` 的实例，生成器可以使用它来注册一组回调，这些回调会影响将来的生成传递的方式。

*主要的生成过程是通过 `Execute` 方法进行的。`Execute` 传递 `GeneratorExecutionContext` 的一个实例，该实例提供对当前 `Compilation` 的访问，并允许生成器通过添加源和报告诊断来更改生成的输出 `Compilation`。*

生成器还能够通过 `AdditionalFiles` 集合访问所有的 `AnalyzerAdditionalFiles` 来传递编译器的，允许基于用户的 c# 代码以外的其他决策。

```c#
namespace Microsoft.CodeAnalysis
{
    public readonly struct GeneratorExecutionContext
    {
        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

        public CancellationToken CancellationToken { get; }

        public Compilation Compilation { get; }

        public ISyntaxReceiver? SyntaxReceiver { get; }

        public void ReportDiagnostic(Diagnostic diagnostic) { throw new NotImplementedException(); }

        public void AddSource(string fileNameHint, SourceText sourceText) { throw new NotImplementedException(); }
    }
}
```

假设一些生成器想要生成多个 `SourceText`，例如在为其他文件生成 1:1 映射时。`AddSource` 的 `fileNameHint` 参数旨在解决这个问题：

1. 如果生成的文件被发送到磁盘，那么能够放置一些有区别的文本可能会很有用。举个例子，如果您有两个 .resx 文件，那么仅生成名称为`ResxGeneratedFile1.cs` 和 `ResxGeneratedFile2.cs` 的文件就不是非常有用了。如果有两个文件并想用名称来区别，那么你可能想要像 `ResxGeneratedFile-Strings.cs` 和 `ResxGeneratedFile-Icons.cs` 这样的名字。
2. IDE 需要一些“稳定”标识符的概念。source generator 为 IDE 创建了几个有趣的问题：例如，用户希望能够在生成的文件中设置断点。如果一个 source generator 输出多个文件，我们需要知道具体生成的是哪一个，这样我们就可以知道断点在对应的文件中。当然，如果一个 source generator 的输入发生了变化（如您删除了一个.resx，那么与它相关联的生成的文件也将消失），那么它就可以停止发出文件，但是它这里给了我们一些控制。

这被称为“提示（hint）”，因为编译器被隐式地允许以它最终需要的方式控制文件名，并且如果两个源生成器给出相同的“提示”，它仍然可以根据需要使用任何类型的前缀/后缀来区分它们。

### IDE 集成

支持生成器的一个更复杂的方面是在 Visual Studio 中实现高保真（high-fidelity experience）体验。为了确定代码的正确性，预期所有的生成器都必须运行。显然，在每次敲击键时都运行每个生成器，并且在 IDE中 仍然保持一个可接受的性能水平这是不现实的。

#### 渐进复炸可选（Progressive complexity opt-in）

人们期望 source generator 能够以一种“选择加入”的方式的 IDE 支持。

默认情况下，只实现 `ISourceGenerator` 的生成器将看不到 IDE 集成，并且只在构建时是正确的。根据与第一方客户的对话，在一些情况下，这就足够了。

然而，对于代码优先的 gRPC 场景，特别是 Razor 和 Blazor，IDE 需要能够在编辑这些文件类型时实时生成代码，并将更改反映到 IDE 中的其他文件中。

提议就是有一组可选实现的高级回调，这将允许 IDE 查询生成器，以决定在任何特定的编辑情况下需要运行什么。

例如，一个扩展会导致生成运行后，保存一个第三方文件可能看起来像：

```c#
namespace Microsoft.CodeAnalysis
{
    public struct GeneratorInitializationContext
    {
        public void RegisterForAdditionalFileChanges(EditCallback<AdditionalFileEdit> callback){ }
    }
}
```

这将允许生成器在初始化期间注册一个回调，**该回调将在每次其他文件更改时被调用**。

它预计将有各种级别的选择，可以添加到一个生成器，以实现要求它特定水平的性能。

这些 api 究竟是什么样子的仍然是一个悬而未决的问题，我们希望在知道它们的确切形状之前，我们需要对一些真实世界的生成器进行原型化。

### 输出文件

所生成的源文本在生成后可以用于检查，或者作为创建生成器的一部分，或者查看由第三方生成器生成的代码，这是很理想的。

默认情况下，生成的文本将被持久化到 `CommandLineArguments.OutputDirectory` 下的 `GeneratedFiles/{GeneratorAssemblyName}` 子文件夹中。`GeneratorExecutionContext.AddSource` 的参数 `fileNameHint` 是用来创建一个唯一的名称，如果需要，可以应用适当的冲突重命名。如在从 `MyGeneratro.dll` 在 Windows 上调用 `AddSource("MyCode", ...);`，这对于 C# 项目来说可能持久化到 `obj/debug/GeneratedFiles/MyGenerator.dll/MyCode.cs`。

命令行或基于 IDE 的生成的正确功能都不需要文件输出，如果需要，可以完全禁用文件输出。IDE 将处理生成的源文本的内存副本（用于“查找所有引用”、断点等），并定期将任何更改刷新到磁盘。

为了支持用户希望生成源文本，然后将生成的文件提交到源代码控制的用例，我们将允许通过适当的命令行开关和匹配的 MSBuild 属性（命名尚待确定）更改生成文件的位置。

在这些情况下，用户是否希望在将来再次生成文件（在这种情况下，文件仍然会生成，但会输出到一个受源代码控制的位置），或者删除生成器并将操作作为一个一次性步骤执行，都取决于用户。

例如，在基于磁盘生成的文件中设置断点的操作将如何发挥作用，这是目前一个悬而未决的问题。

TK：我们如何保存 PDBs/Source 链接等？

### 对第三方语言有编辑经验

