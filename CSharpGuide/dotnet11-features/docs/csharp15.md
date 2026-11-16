# C# 15：把数据范围和控制流写清楚

运行 `dotnet run --project CSharpGuide/dotnet11-features -c Release -- language`，并对照 `LanguageDemos.cs` 阅读。本项目目标为 `net11.0`，默认使用 C# 15；下面的示例不需要开启 preview。

## 集合表达式也能传构造参数

`["CSharp", "csharp"]` 以前只能表达放哪些元素，无法同时指定集合的比较规则。现在 `HashSet<string> tags = [with(StringComparer.OrdinalIgnoreCase), "CSharp", "csharp", "NET"];` 可以把比较器传给集合构造过程。两个大小写不同的 CSharp 因此只保留一个。

`[with(capacity: 8), 10, 20, 30]` 同样可以为 `List<int>` 预留容量。容量是内部可容纳空间，`Count` 仍是实际元素数。`with(...)` 必须位于集合表达式开头，并且目标集合的构造或工厂需要支持对应参数。

## union：返回商品，或者返回找不到的原因

`ProductResult` 有两个分支：`Product` 和 `MissingProduct`。调用者可以对返回值写 switch：一个分支显示商品，另一个解释商品不存在。相比把错误原因藏在 null 里，类型本身告诉调用方有哪些结果。

联合类型提供分支到 union 的隐式转换。switch 穷尽性检查可以提醒我们处理遗漏的分支；默认值及可空性仍需要按实际类型和编译器警告处理。union 不意味着每种情况都绝不分配，也不自动提供跨版本序列化协议。运行 `language` 看两个分支的输出，再试着新增一种分支，观察编译器警告。

## closed：限制哪些类型能直接继承

`closed record PaymentState` 让编译器知道本程序集里的直接子类型。这里 `Pending` 和 `Paid` 覆盖了两个分支，switch 就可以直接写出完整处理。

注意这个限制不是自动向下传递：如果一个直接子类仍然允许被继承，其他程序集仍可能扩展它。需要真正封闭到叶子时，可以让中间类型继续 `closed` 或让叶子类型 `sealed`。本示例将两个叶子类型标为 sealed。

## 扩展索引器：好看不代表随机访问免费

`EnumerableIndexer` 让 `IEnumerable<int>` 支持 `sequence[1]`，内部调用 `ElementAt`。如果底层是普通可枚举序列，找第 100 项可能需要从头遍历；语法看起来像数组，并不改变算法复杂度。已经有实例索引器时，也不能假定扩展索引器会取代实例成员。

## 带标签的 break 和 continue

网格示例找到目标后使用 `break outer`，直接退出外层循环。普通 `break` 只退出当前内层循环。第二个循环遇到 9 时用 `continue rows` 跳到下一行；标签必须标在相应的外层结构上。

## 非虚静态接口成员

`ITemperature.CelsiusToFahrenheit` 是接口上的普通静态辅助方法，通过接口名直接调用。C# 15 放宽了这类方法在旧目标运行时上的使用限制。这与 `static abstract` 泛型数学契约是两类功能；在 net11.0 下运行只能看到调用行为，不能单凭它证明旧运行时兼容性。

## Memory safety 仍是预览

C# 15 的内存安全规则调整仍处于 preview，需要预览语言版本及相应编译开关。它改变指针声明、解引用及 unsafe 调用责任的划分，不是本项目默认启用的正式特性。动手前应阅读 [官方说明](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15#memory-safety) 和目标 SDK 文档，不要把试验代码的限制误当成 GA 合同。

参考：[C# 15 官方发布说明](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)。
