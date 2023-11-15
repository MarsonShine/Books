using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace net8_guide.Serialize
{
    public class MyPoco
    {
        public int Id { get; set; }
    }
#if NET8_0_OR_GREATER
    // JsonUnmappedMemberHandling.Disallow 表示所有的字段都要一一匹配。
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class MyPoco2
    {
        public int Id { get; set; }
    }
#endif
    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public int TemperatureCelsius { get; set; }
        public string? Summary { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(WeatherForecast))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {

    }

    public class MyPoco3
    {
        public int Id { get; set; }
        public string? Name { get; init; }
        public required int Age { get; set; }
    }

    [JsonSerializable(typeof(MyPoco3))]
    internal partial class MyPoco3JsonSerializerContext : JsonSerializerContext
    {

    }

    // 序列化对枚举转换的支持
    [JsonConverter(typeof(JsonStringEnumConverter<MyEnum>))]
    public enum MyEnum { Value1, Value2, Value3 }

    public class MyPoco4
    {
        public DateTime Date { get; set; }
        public int TemperatureCelsius { get; set; }
        public MyEnum MyEnum { get; set; }
    }

    [JsonSerializable(typeof(MyPoco4))]
    public partial class MyPoco4JsonSerializerContext : JsonSerializerContext { }

    public class MyPoco5
    {
        public required string Name { get; set; }
        public string? PhoneNumber { get; set;}
    }
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public class CustomerInfo
    {
        public List<string> Names { get; } = new();
        public MyPoco5 Company { get; } = new() { Name = "N/A", PhoneNumber = "N/A" };
    }
}
