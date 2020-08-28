# .NET 编译器代码分析器

.net 编译器不仅仅可以分析高级语言代码（不是IL），还可以在 VS 这样做，甚至可以提出建议和执行代码重构编辑。稍后会用两个例子建立两个规则，其中一个要求静态字段必须被标记被 `readonly`，另一个规则是调用 `String.ToUpper` 和 `String.ToLower` 的时候给出警告。

关于前提条件请翻阅：[Write your first analyzer and code fix](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)

> 1. 菜单 -> 工具-> 获取工具和功能 -> 搜索 `.NET Compiler Platform SDK` -> 安装





