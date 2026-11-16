# 联合类型 JSON 与异步数据验证

在仓库根目录运行：

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- advanced
```

代码在 [AdvancedDemos.cs](../AdvancedDemos.cs)。示例用本地延时模拟查询，不访问数据库或远端服务。

## JSON 里两个分支都是对象，怎么知道该创建哪个？

物流通知可能是 `ParcelArrived`（已送达，包含收件人），也可能是 `ParcelDelayed`（已延迟，包含延迟小时数）。C# 的 `DeliveryNotice` 联合类型允许它装下其中任意一个。

序列化时，.NET 11 会直接写出当前分支的 JSON。问题出在反序列化：两个分支都是 `{ ... }`，只看第一个字符无法区分。

示例在 union 上配置 `JsonUnionTypeStructuralClassifier`，让序列化器根据根对象的字段缩小候选范围：

```text
{"TrackingId":"P123","Recipient":"小王"}  → ParcelArrived
{"TrackingId":"P123","DelayHours":3}      → ParcelDelayed
{"TrackingId":"P123"}                     → 无法唯一判断，抛 JsonException
```

最后一种输入只提供了两种通知都有的单号；调用方不能猜测它是哪一种。实际字段规则还受到必填字段、JSON 命名配置影响。分类器检查的是根字段与要求，不会根据字段值或嵌套对象内容推断业务含义。

示例的 `DeliveryJsonContext` 由 JSON 源生成器创建类型元数据；`JsonSerializer.Deserialize(json, DeliveryJsonContext.Default.DeliveryNotice)` 明确使用生成结果。它展示了源生成和分类器如何配合，不意味着任何两个结构相同的对象类型都能被区分。协议允许增加标识字段时，显式且稳定的标识往往更容易维护。

## 异步验证：规则需要等数据库怎么办？

普通 `[Required]` 可以立即检查字段，但“会员编号是否存在”通常需要访问数据库或外部服务。用同步验证时阻塞等待会占住线程；把 `async` 方法当作普通同步方法调用又无法正确等待结果。

`RegisteredCodeAttribute` 继承 `AsyncValidationAttribute`，在 `IsValidAsync` 里等待一个模拟查询。编号 `VIP-42` 通过，其他编号返回 `ValidationResult`。调用方必须使用 `Validator.ValidateObjectAsync`；失败时该方法抛 `ValidationException`。如果想自行收集多个错误，可以使用对应的 `TryValidateObjectAsync`。

`IsValid` 在示例里明确抛异常，防止有人改用同步验证时悄悄跳过异步规则。模拟延时接收 `CancellationToken`；实际数据库或 HTTP 调用也应继续传递它。这个示例只演示一次模型验证，不承担注册、事务或并发唯一性保证：即便预检查通过，真正写入时仍要由数据存储保证唯一性。

`RangeAttribute.FormatMessage` 是另一项改进：验证规则知道自己的最小值和最大值，调用方可以提供本地化模板，规则再把参数填进去，不必自己猜测不同属性的格式参数。

参考：[System.Text.Json 联合类型](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)、[.NET 11 异步验证](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#asynchronous-validation-with-dataannotations)。
