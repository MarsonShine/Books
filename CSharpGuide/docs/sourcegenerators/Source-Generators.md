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

source generator 支持的一个有趣的场景是 c# 在其他语言中的“嵌入”(反之亦然)。这就是今天 Razor 的工作方式，并且 Razor 团队在 Visual Studio 中维护了一个重要的语言服务来支持它。

这个项目一个可能的目标是找到一种通用的方式来表示这一点：这将允许 Razor 团队减少他们的工具投资，同时允许第三方以相对便宜的价格实现同样的体验(包括“去定义”，“查找所有引用”等)。

当前的想法是为生成器提供某种形式的“侧通道（side channel）”。当生成器生成源文本时，它将指示此文本从原始文档的何处生成。这将允许编译器 API 跟踪，例如，一个生成的 `Symbol` 有一个 `OriginalDefinition`，它代表了一组第三方源文本(如 .cshtml 文件中的 Razor 标签)。

我们讨论过通过 `#pragma` 将其直接嵌入到源文本中，但这将需要更改语言特性，并将该特性限制在特定的 c# 版本中。其他考虑事项可以是特殊格式的注释或 `#if FALSE --` 语句块。一般来说，在生成的文本中，“侧通道”方法似乎比特别设计的语法更可取。

这并不是 Source generator 成功的必要目标；如果事实证明 Razor 的语言服务是不可行的，那么它可以被升级到与 source generator 一起工作，但它确实是我们想要考虑的工作的一部分。

### MSBuild 集成

预计生成器将需要某种形式的配置系统，我们打算通过 MSBuild 允许某些属性来实现这一点。

> 注意：这仍处于设计阶段，可以随时改变

### 性能目标

最终，该特性的性能将在一定程度上依赖于客户编写的生成器的性能。渐进选择加入和默认情况下仅构建时将允许 IDE 减轻许多由第三方生成器造成的潜在性能问题。但是，仍然存在第三方生成器会导致 IDE 无法接受的性能问题的风险，该特性的设计需要记住这一点。

对于第一方的生成器，特别是 Razor 和 Blazor，我们的目标是最小化，以达到当前用户所看到的性能。预计即使是基于原生 generator 的实现也将比现有的工具执行得更快，因为通信开销更少，重复的工作也更少，但提高这些体验的速度并不是这个项目的主要目标。

### 语言变更

这个设计目前并不打算改变语言，它纯粹是一个编译器特性。前面的 source generator 设计中介绍了 `replace` 和 `original` 关键词。这次的提议删除了这些，因为生成的源代码纯粹是附加的，所以不需要它们。我们希望大多数场景都可以使用现有的部分定义；作为 V1，我们希望以这种状态发布。如果稍后显示了使用 V1 方法无法实现的具体场景，我们将考虑允许将其修改为 V2。

## 用例

我们已经确定了几个第一和第三部分候选人，他们将从 source generator 中受益：

- ASP.NET 提高启动时间
- Blazor 和 Razor：减少工具的伤害
- Azure Functios：启动期间的正则编译
- Azure SDK
- [gRPC](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-3.1)
- Resx 文件生成
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- 序列化
- [SWIG](http://www.swig.org/)

## 讨论 / 打开 Issue /  TODOs：

**接口 VS ISourceGenerator 类：**

我们讨论过这是一个接口或类。分析器选择有一个抽象基类，但我们不确定最终会需要什么，因为最终我们只有一个方法。保持它是一个接口也更自然，因为我们有其他接口实现了这个接口以及可选择的使用（light-up）。

**IDependsOnCompilationGenerator：**

我们确实讨论了是否应该有一个 IDependsOnCompilationGenerator 来正式声明你实际上使用了编译。毕竟，如果您不使用编译，那么我们知道您在 IDE 中的性能会大大简化。然而，我们使用的每个读取额外文件的场景都需要编译，所以我们不确定这会带来什么。

**在生成的文件断点：**

我们把它映射回内存文件吗？

**生成的文件是否应该拉取或推送：**

Source generator 是基于拉取的，分析器是基于推送的(基于注册)。我们也应该为生成器使用基于推送的模型吗？

- 如果我们继续使用基于推送的模型，遍历树应该确保继续为尽可能多的节点产生事件，即使有错误，因为生成器通常会在存在的情况下工作
- 我们今天用于分析程序的事件可能需要更多的工作来产生，因为我们期望分析程序在完整编译期间运行，而生成器可能甚至不想构造符号表
- 渐进性能选择加入模型（progressive-performance-opt-in model）可能在基于推送的模型中工作得更好，因为您将只注册您关心的东西

**我们是否应该与分析器类型层次结构共享更多? ：**

我们仍然需要区分分析器和生成器，因为它们将在不同的时间生成(生成器诊断仅在第一次编译时产生，分析器诊断仅在第二次编译时产生)

**我们能预测我们的一些样本客户(Razor?)多久需要运行一次生成器吗?：**

他们目前无法预测这一点，而将计时器整合到当前生成器，只基于事件生成的结果会非常难以预测

**我们有最重要客户的优先列表吗?：**

不，我们应该制定出优先级来为特征划分优先级。

**安全检索：**

生成器是否会产生任何新的安全风险，而不是由分析器和 nuget 带来的?
