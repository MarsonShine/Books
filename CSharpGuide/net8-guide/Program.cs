// See https://aka.ms/new-console-template for more information
// https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-8
using net8_guide.CollectionExpression;
using net8_guide.Randoms;
using net8_guide.Serialize;
using net8_guide.Times;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO.Hashing;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

Console.WriteLine("Hello, World!");

// 数组简化声明
#if NET8
int[] arry = [1, 2, 3]; // yes
						//var array2 = [1, 2, 3]; // error; 必须显示指定类型
						// 支持直接对内置类型的序列化
Console.WriteLine(JsonSerializer.Serialize(arry));
//Console.WriteLine(JsonSerializer.Serialize([Half.MaxValue, Int128.MaxValue, UInt128.MaxValue]));
JsonSerializer.Serialize<ReadOnlyMemory<byte>>(new byte[] { 1, 2, 3 }); // "AQID"
JsonSerializer.Serialize<Memory<int>>(value: new int[] { 1, 2, 3 }); // [1,2,3]
																	 // 序列化部分
var poco = JsonSerializer.Deserialize<MyPoco>("""{"Id":42, "Name": "Marson Shine"}"""); 
#endif

//var poco2 = JsonSerializer.Deserialize<MyPoco2>("""{"Id":42, "Name": "Marson Shine"}"""); // 因为配置了属性映射规则：JsonUnmappedMemberHandling.Disallow，所以会报错

// serialize source generator
WeatherForecast wf = new() { Date = DateTime.Now, Summary = "备注", TemperatureCelsius = 27 };
string jsonString = JsonSerializer.Serialize(value: wf, typeof(WeatherForecast), SourceGenerationContext.Default);

// .net8 序列化sg支持了 init,required 关键字的字段属性
// 以下代码 .net8 以下会报错
//var poco3 = new MyPoco3 { Age = 30, Id = 1, Name = "Marson Shine" };
//string poco3String = JsonSerializer.Serialize(poco3, typeof(MyPoco3), MyPoco3JsonSerializerContext.Default);
//JsonSerializer.Deserialize<MyPoco3>(poco3String);

var poco4 = new MyPoco4 { MyEnum = MyEnum.Value3, Date = DateTime.Now, TemperatureCelsius = 27 };
string poco4String = JsonSerializer.Serialize(poco4, typeof(MyPoco4), MyPoco4JsonSerializerContext.Default);

// JsonConverter 新增 Type 属性
Dictionary<Type, JsonConverter> CreateDictionary(IEnumerable<JsonConverter> converters) => converters.Where(converter => converter.Type != null).ToDictionary(converter => converter.Type!);

// 只读属性的反序列赋值，再没有使用[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]时，反序列化是无法给只读属性赋值的。
//CustomerInfo customer = JsonSerializer.Deserialize<CustomerInfo>("""{"Names":["John Doe"], "Company": {"Name":"Contoso"}}""")!;

// TimeProvider
DateTimeOffset utcNow = TimeProvider.System.GetUtcNow();
DateTimeOffset localNow = TimeProvider.System.GetLocalNow();
// create timeprovider
TimeProvider timeProvider = ZoneTimeProvider.FromLocalTimeZone(TimeZoneInfo.Local);
ITimer timer = timeProvider.CreateTimer((obj) =>
{
    Console.WriteLine("执行中...");
}, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);

long providerTimestamp1 = timeProvider.GetTimestamp();
long providerTimestamp2 = timeProvider.GetTimestamp();
var period = GetElapsedTime(providerTimestamp1, providerTimestamp2);

var period2 = timeProvider.GetElapsedTime(providerTimestamp1, providerTimestamp2);

static TimeSpan GetElapsedTime(long providerTimestamp1, long providerTimestamp2)
{
    return TimeSpan.FromTicks(providerTimestamp2 - providerTimestamp1);
}

// UTF8 improvements
ReadOnlySpan<byte> u8 = "123123123"u8;

static bool FormatHexVersion(short major, short minor, short build, short revision, Span<byte> utf8Bytes, out int bytesWriten) => Utf8.TryWrite(utf8Bytes, CultureInfo.InvariantCulture, $"{major:X4}.{minor:X4}.{build:X4}.{revision:X4}", out bytesWriten);

// 随机数模块，新增方法 GetItems
// 从给定的数据源那种随机选择
ReadOnlySpan<ButtonEnum> s_allButtons = [ButtonEnum.Red, ButtonEnum.Green, ButtonEnum.Blue, ButtonEnum.Yellow];
ButtonEnum[] thisRound = Random.Shared.GetItems(s_allButtons, 31);
ButtonEnum[] thisRound2 = RandomNumberGenerator.GetItems(s_allButtons, 31);
// 混淆，打乱顺序
ButtonEnum[] orderedRound = [.. thisRound.Order()];
ButtonEnum[] orderedRound2 = [.. thisRound2.Order()];
Random.Shared.Shuffle(orderedRound);
RandomNumberGenerator.Shuffle(orderedRound2.AsSpan());

// 高性能类型
// SearchValues<T> 用于传递给在传递的集合中查找任何值的首次出现的方法。
var searcher = SearchValues.Create(thisRound.Select(p => (byte)p).ToArray().AsSpan<byte>());
Console.WriteLine(searcher.Contains((byte)ButtonEnum.Blue));
// CompositeFormat 自定义格式化
CompositeFormat compositeFormat = CompositeFormat.Parse("the {0} jumped over the {1}");
string[] animals = ["fox", "dog"];

Console.WriteLine(string.Format(CultureInfo.InvariantCulture, compositeFormat, "fox", "dog"));
Console.WriteLine(string.Format(CultureInfo.InvariantCulture, compositeFormat, args: animals));

// XxHash,非加密哈希算法,需要引入包：System.IO.Hashing
ReadOnlySpan<byte> source = "this is example"u8;
var hashCode = new HashCode();
hashCode.AddBytes(source);
int code = hashCode.ToHashCode();
// 速度要比hashcode快4倍
int xxhash32 = unchecked((int)XxHash3.HashToUInt64(source));

int sum = 0;
for (int i = 0; i < 100; i++)
{
    sum += i;
}
Vector<int> vector = Vector<int>.Zero;
int lastBlockIndex = 100 - 100 % Vector<int>.Count;
int index = 0, sum2 = 0;
for (index = 0; index < lastBlockIndex; index += Vector<int>.Count)
{
    vector += new Vector<int>(Enumerable.Range(index, index + Vector<int>.Count).ToArray());
}
for (int n = 0; n < Vector<int>.Count; n++)
{
    sum2 += vector[n];
}
// 剩下的相加
while (index < 100)
{
    sum2 += index++;
}

// metrics
//MeterOptions options = new("name")
//{
//    Version = "version",
//    Tags = new TagList() { { "MeterKey1", "MeterValue1" }, { "MeterKey2", "MeterValue2" } }
//};
//Meter meter = meterFactory.Create(options);
//Counter<int> instrument = meter.CreateCounter<int>("counter", null, null, new TagList() { { "counterKey1", "counterValue1" } });
//instrument.Add(1);

// 加密
//if (SHA3_256.IsSupported)
//{
//    byte[] hash = SHA3_256.HashData(inputs);
//}
//if (SHA3_256.IsSupported)
//{
//    using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
//    byte[] signature = ec.SignData(inputs, HashAlgorithmName.SHA3_256);
//}

// 自定义集合表达式
MyCollection myCollection = [1, 2, 3];
MyCollection<int> myCollection2 = [1, 2, 3];
MyBetterCollection collection3 = [1, 2, 3];
MyBetterCollection<int> collection4 = [1, 2, 3];
IMyCollection<int> c5 = [1, 2, 3, 4];

IList<int> a = [1, 2, 3, 4];
ICollection<int> b = [1, 2, 3, 4];
Console.ReadLine();
