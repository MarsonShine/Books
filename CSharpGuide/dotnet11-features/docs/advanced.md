# JSON 联合类型与异步验证

[AdvancedDemos.cs](../AdvancedDemos.cs) 包含两个例子：根据 JSON 字段判断联合类型的分支，以及等待一条异步验证规则。它们都在本机运行；示例里的查询用短暂延时代替，不连接数据库或远端服务。

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- advanced
```

命令在仓库根目录执行。

## 收到一个 JSON 对象，应该还原成哪种通知？

物流系统有两种通知：`ParcelArrived` 记录单号和收件人，`ParcelDelayed` 记录单号和延迟小时数。代码用联合类型 `DeliveryNotice` 表示这两种结果：

```csharp
public union DeliveryNotice(ParcelArrived, ParcelDelayed);
```

.NET 11 可以把联合类型当前的值直接写成 JSON，再读回来。如果一个分支是数字、另一个是字符串，读取时看到 `123` 或 `"abc"` 就能判断类型。但本例的两个分支都是对象，都以 `{` 开头，光看 JSON 值的类型还不够。

代码在 `DeliveryNotice` 上指定 `JsonUnionTypeStructuralClassifier`。它查看对象的根级字段，尝试找出唯一匹配的分支。运行后可以看到三种情况：

```text
有 TrackingId 和 Recipient  → ParcelArrived
有 TrackingId 和 DelayHours → ParcelDelayed
只有 TrackingId             → 无法唯一判断，抛出 JsonException
```

第三种输入只有单号，而单号是两种通知都有的字段。序列化器没有足够信息确定分支，示例捕获异常并打印出来。

分类器根据根级字段名称和必需字段筛选候选类型，不会检查字段值或深入嵌套对象。例如两个分支的字段完全相同、只靠某个字段的值区分时，不能指望它自动理解这个业务规则；需要考虑明确的类型标识或自定义分类器。分类成功也不等于字段值通过了业务验证。

### 类型信息从哪里来？

示例还声明了一个由源生成器处理的上下文：

```csharp
[JsonSerializable(typeof(DeliveryNotice))]
internal partial class DeliveryJsonContext : JsonSerializerContext;
```

调用 `JsonSerializer.Deserialize` 时，代码传入 `DeliveryJsonContext.Default.DeliveryNotice`。这份元数据由源生成器在编译时生成，告诉序列化器如何处理 `DeliveryNotice`；分类器再根据输入选择具体分支。

源生成解决的是类型元数据的准备方式，分类器解决的是两个对象分支如何区分。两者配合使用，并不能让形状相同的对象自动变得可区分。字段是否必需、JSON 命名规则等也会影响匹配。完整限制和自定义方式见 [System.Text.Json 联合类型文档](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)。

## 验证会员编号时，需要等一次查询

`MemberApplication` 有一个 `MemberCode` 属性。示例给它加了两条规则：`[Required]` 检查是否提供了值，`[RegisteredCode]` 则模拟查询编号是否存在。

后者继承 `AsyncValidationAttribute`，在 `IsValidAsync` 中等待一个短暂延时。这里延时只是为了模拟异步查询：`VIP-42` 被当作存在，其他编号返回“会员编号不存在”。实际程序可以在这个方法里调用数据库或远端服务。

调用方使用异步验证入口：

```csharp
await Validator.ValidateObjectAsync(
    model,
    new ValidationContext(model),
    validateAllProperties: true);
```

示例分别验证 `VIP-42` 和 `UNKNOWN`。前者通过，后者抛出 `ValidationException`，`catch` 读取并打印错误信息。如果调用方希望收集验证结果而不是通过异常处理，可以使用对应的 `TryValidateObjectAsync`。

`RegisteredCodeAttribute` 的同步 `IsValid` 有意抛异常：这条规则需要等待，若误用同步验证入口，程序应明确报错，而不是让调用方误以为规则已经执行。异步方法接收取消令牌，示例将它传给 `Task.Delay`；换成数据库或 HTTP 查询时，也应继续传递。

“编号存在”的预检查只能反映查询当时的状态。它不代替真正写入数据时的约束或事务；如果业务要求编号唯一，仍要由数据存储保证。

### 让验证规则填充错误消息

最后一个例子使用 `RangeAttribute(1, 10)` 和 `FormatMessage`：

```csharp
var range = new RangeAttribute(1, 10);
string message = range.FormatMessage("{0} 必须在 {1} 到 {2} 之间", "数量");
```

输出是“数量 必须在 1 到 10 之间”。消息模板由调用方提供，最小值和最大值由 `RangeAttribute` 填入，适合需要统一或本地化错误消息的验证入口。[.NET 11 异步验证说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#asynchronous-validation-with-dataannotations)
