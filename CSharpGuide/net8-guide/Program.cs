// See https://aka.ms/new-console-template for more information
using net8_guide.Serialize;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

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
var poco3 = new MyPoco3 { Age = 30, Id = 1, Name = "Marson Shine" };
string poco3String = JsonSerializer.Serialize(poco3, typeof(MyPoco3), MyPoco3JsonSerializerContext.Default);
JsonSerializer.Deserialize<MyPoco3>(poco3String);

var poco4 = new MyPoco4 { MyEnum = MyEnum.Value3,Date = DateTime.Now,TemperatureCelsius = 27 };
string poco4String = JsonSerializer.Serialize(poco4,typeof(MyPoco4), MyPoco4JsonSerializerContext.Default);

// JsonConverter 新增 Type 属性
Dictionary<Type, JsonConverter> CreateDictionary(IEnumerable<JsonConverter> converters) => converters.Where(converter => converter.Type != null).ToDictionary(converter => converter.Type!);

// 只读属性的反序列赋值，再没有使用[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]时，反序列化是无法给只读属性赋值的。
CustomerInfo customer = JsonSerializer.Deserialize<CustomerInfo>("""{"Names":["John Doe"], "Company": {"Name":"Contoso"}}""")!;

Console.ReadLine();
