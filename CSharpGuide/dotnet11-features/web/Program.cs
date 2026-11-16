using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5087");
builder.Services.AddValidation();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
var app = builder.Build();

app.MapGet("/reading/{id:int}", Reading (int id) => id % 2 == 0 ? new Reading(id) : new Reading("设备离线"));
app.MapPost("/members", (Registration request) => TypedResults.Ok(new { request.Code, Accepted = true }));
app.MapGet("/items", (int count) => TypedResults.Ok(new { count }))
    .AddEndpointFilter(async (context, next) =>
    {
        var result = await next(context);
        return context.HttpContext.Response.StatusCode == 400
            ? TypedResults.BadRequest(new { error = "count 必须是整数" })
            : result;
    });
app.MapGet("/health", [ShortCircuit] () => TypedResults.Ok(new { status = "healthy" }));
app.MapGet("/cache-metrics", () =>
{
    var measurements = new List<string>();
    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, current) =>
    {
        if (instrument.Meter.Name == "Microsoft.Extensions.Caching.Memory.MemoryCache")
            current.EnableMeasurementEvents(instrument);
    };
    listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        measurements.Add($"{instrument.Name}={value} [{string.Join(", ", tags.ToArray().Select(t => $"{t.Key}={t.Value}"))}]"));
    listener.Start();
    using var cache = new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });
    cache.Set("book", "C#");
    cache.TryGetValue("book", out _);
    cache.TryGetValue("missing", out _);
    listener.RecordObservableInstruments();
    return TypedResults.Ok(measurements);
});

app.Run();

public union Reading(int, string);
public sealed class Registration
{
    [Required, KnownCode]
    public string Code { get; init; } = "";
}
public sealed class KnownCodeAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("此规则需要异步验证");
    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return Equals(value, "VIP-42") ? ValidationResult.Success : new ValidationResult("会员编号不存在");
    }
}
