# C# 15：从项目示例开始

在仓库根目录运行：

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- language
```

示例代码在 [LanguageDemos.cs](../LanguageDemos.cs)。项目以 `net11.0` 为目标，并显式指定 C# 15，因为仓库上层的 `Directory.build.props` 将语言版本设为 C# 10。本页介绍的示例不需要 preview 语言版本。

## 创建集合时，指定比较规则和容量

集合表达式可以写出元素：

```csharp
HashSet<string> tags = ["CSharp", "csharp", "NET"];
```

默认的字符串比较会区分大小写，因此前两个字符串是不同元素。如果希望它们被当成同一个值，创建 `HashSet` 时就要传入比较器。C# 15 允许在集合表达式开头用 `with(...)` 传参：

```csharp
HashSet<string> tags =
    [with(StringComparer.OrdinalIgnoreCase), "CSharp", "csharp", "NET"];
```

现在两个 CSharp 只保留一个，程序输出的集合数量是 2。比较器属于集合创建时的设置，必须在添加元素之前确定；`with(...)` 因此也必须位于集合表达式的开头。

示例还创建了一个列表：

```csharp
List<int> prices = [with(capacity: 8), 10, 20, 30];
```

容量 8 表示列表预先准备了容纳元素的空间，实际放入的仍只有三个数，所以 `Capacity` 是 8，`Count` 是 3。能传哪些参数取决于目标集合的构造方法或工厂，不能给所有集合随意指定同一组参数。

## 查询结果可以是商品，也可以是“未找到”

查看 `ProductResult` 的定义：

```csharp
public record Product(string Name, decimal Price);
public record MissingProduct(string Id);
public union ProductResult(Product, MissingProduct);
```

它声明了两种可能的结果。找到商品时返回 `Product`；没有找到时返回 `MissingProduct`，里面还能带上请求的编号。

两种值可以隐式转换成 `ProductResult`：

```csharp
ProductResult found = new Product("键盘", 120m);
ProductResult missing = new MissingProduct("P-404");
```

调用方再通过 `switch` 分别处理：

```csharp
string message = found switch
{
    Product p => $"找到商品 {p.Name}，价格 {p.Price}",
    MissingProduct m => $"商品不存在：{m.Id}"
};
```

比起用一个 `null` 既表示找不到商品，又把原因放在别处，联合类型把结果的几种情况放进了方法的类型声明。新增一种分支后，可以通过编译器的穷尽性检查发现没有处理的 `switch`。

实际使用时仍要考虑默认值和可空性，留意编译器警告；联合类型本身也不承诺每个分支都不分配对象。若结果需要跨服务传输，还要单独决定 JSON 等协议怎样区分这些情况。

## closed 类型让编译器知道直接子类型

支付状态示例使用的是继承：

```csharp
public closed record PaymentState;
public sealed record Pending : PaymentState;
public sealed record Paid(decimal Amount) : PaymentState;
```

`closed` 限定 `PaymentState` 的直接派生类型只能在声明它的程序集中定义。编译器因此能看到这里列出的直接子类型，处理状态时可以分别写 `Pending` 和 `Paid`：

```csharp
string description = payment switch
{
    Pending => "待支付",
    Paid p => $"已支付 {p.Amount}"
};
```

这个限制不会自动传递给每一层。如果某个直接子类本身允许继续继承，其他程序集仍可能继承那个子类。本例把 `Pending` 和 `Paid` 都标为 `sealed`；层级更深时，也可以在需要限制的中间类上使用 `closed`。

联合类型和封闭继承都能帮助调用方列出需要处理的情况，但它们的建模方式不同：前者声明一个值可以是哪几种类型，后者声明一组有继承关系的类型。

## 给现有类型增加索引器

项目在 `extension` 块里为 `IEnumerable<int>` 定义了一个索引器，内部调用 `ElementAt`。因此，下面的 `sequence` 虽然只声明为可枚举序列，也可以写 `sequence[1]`：

```csharp
IEnumerable<int> sequence = Enumerable.Range(10, 5).Where(x => x % 2 == 0);
Console.WriteLine(sequence[1]); // 12
```

结果依次是 10、12、14，因此下标 1 取得 12。需要注意的是，这个索引器仍然调用 `ElementAt`：如果底层序列不能直接按索引访问，取得较后面的元素可能要从头遍历。写法像数组，不代表访问成本也和数组一样。

扩展成员也不会随意取代类型已有的实例成员。声明和调用规则见 [C# 扩展成员文档](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/extension)。

## 从嵌套循环退出，或者直接开始下一行

网格示例逐行查找数字 9。普通 `break` 只能退出当前内层循环；找到目标后，如果还要停止外层循环，可以给外层循环命名：

```csharp
outer: for (int row = 0; row < grid.Length; row++)
{
    for (int column = 0; column < grid[row].Length; column++)
    {
        if (grid[row][column] == 9)
            break outer;
    }
}
```

`break outer` 直接退出 `outer` 循环。项目中的完整示例在退出前记录了行、列，输出 `(1, 1)`。

另一个循环统计不含 9 的行。内层一旦发现 9，就执行 `continue rows`，跳到外层的下一行，不再把当前行计入结果。标签需要标在相应的外层循环或 `switch` 上；不带标签的 `break` 和 `continue` 仍按原来的规则作用于最近的适用结构。

## 接口上的普通静态方法

`ITemperature` 定义了一个静态辅助方法，将摄氏温度转成华氏温度：

```csharp
public interface ITemperature
{
    public static double CelsiusToFahrenheit(double celsius) => celsius * 9 / 5 + 32;
}

Console.WriteLine(ITemperature.CelsiusToFahrenheit(25)); // 77
```

调用通过接口名完成，不需要创建实现该接口的对象。C# 15 放宽了普通的非虚静态接口成员在旧目标运行时上的使用限制。它与要求实现类型提供成员的 `static abstract` 泛型数学契约不同；本项目运行在 .NET 11 上，只演示调用方式，没有验证旧运行时兼容性。

## 内存安全规则仍处于预览

C# 15 的内存安全规则调整仍是 preview。它涉及哪些指针操作需要 `unsafe` 上下文，以及调用带有相关安全要求的方法时由谁承担检查责任。尝试这些规则需要预览语言版本；启用更新后的安全规则还需要 `updated-memory-safety-rules` 编译特性。本项目没有默认开启它们。

完整语言规则和配置方式见 [C# 15 官方发布说明](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)，其中包括[内存安全预览说明](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15#memory-safety)。
