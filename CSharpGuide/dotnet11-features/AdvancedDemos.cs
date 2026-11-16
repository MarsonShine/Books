using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union DeliveryNotice(ParcelArrived, ParcelDelayed);
public sealed record ParcelArrived(string TrackingId, string Recipient);
public sealed record ParcelDelayed(string TrackingId, int DelayHours);

[JsonSerializable(typeof(DeliveryNotice))]
internal partial class DeliveryJsonContext : JsonSerializerContext;

public sealed class RegisteredCodeAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("请使用异步验证入口");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        // 模拟异步仓库查询；实际应用中把请求令牌传入数据库或 HTTP 调用。
        await Task.Delay(10, cancellationToken);
        return value is string code && code == "VIP-42"
            ? ValidationResult.Success
            : new ValidationResult("会员编号不存在", [context.MemberName!]);
    }
}

public sealed class MemberApplication
{
    [Required, RegisteredCode]
    public string MemberCode { get; init; } = "";
}

public static class AdvancedDemos
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== JSON 对象联合类型：根据字段选择分支 ===");
        foreach (string json in new[]
        {
            """{"TrackingId":"P123","Recipient":"小王"}""",
            """{"TrackingId":"P123","DelayHours":3}""",
            """{"TrackingId":"P123"}"""
        })
        {
            try
            {
                DeliveryNotice notice = JsonSerializer.Deserialize(json, DeliveryJsonContext.Default.DeliveryNotice);
                Console.WriteLine($"{json} → {notice.Value?.GetType().Name}: {notice.Value}");
            }
            catch (JsonException ex) { Console.WriteLine($"{json} → 无法唯一选择分支：{ex.GetType().Name}"); }
        }

        Console.WriteLine("\n=== DataAnnotations 异步验证 ===");
        foreach (string code in new[] { "VIP-42", "UNKNOWN" })
        {
            var model = new MemberApplication { MemberCode = code };
            try
            {
                await Validator.ValidateObjectAsync(model, new ValidationContext(model), validateAllProperties: true);
                Console.WriteLine($"{code}：通过");
            }
            catch (ValidationException ex) { Console.WriteLine($"{code}：{ex.ValidationResult?.ErrorMessage}"); }
        }
        var range = new RangeAttribute(1, 10);
        Console.WriteLine(range.FormatMessage("{0} 必须在 {1} 到 {2} 之间", "数量"));
    }
}
