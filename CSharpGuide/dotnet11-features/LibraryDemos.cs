using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

/// <summary>.NET 11 基础库：示例均在本地运行，不要求服务或外部账号。</summary>
public static class LibraryDemos
{
    public static async Task RunAsync()
    {
        TextAndUnicode();
        Collections();
        Numerics();
        await JsonAsync();
        await StreamsAndArchivesAsync();
        await HttpCompressionAsync();
        await ProcessAsync();
    }

    private static void TextAndUnicode()
    {
        Console.WriteLine("\n=== Unicode、文本和搜索 ===");
        // 一个 emoji 是一个 Unicode 标量，但在 UTF-16 字符串中占两个 char。
        string text = "A😀B";
        Rune emoji = new(0x1F600);
        Console.WriteLine($"字符串长度（UTF-16 单元）={text.Length}，😀的位置={text.IndexOf(emoji)}");
        Console.WriteLine($"Rune 替换：{text.Replace(emoji, new Rune('*'))}");

        byte[] invalid = [0x41, 0xC3, 0x28];
        Console.WriteLine($"无效 UTF-8 起始字节位置：{Utf8.IndexOfInvalidSubsequence(invalid)}");
        Console.WriteLine($"有效 UTF-16：{Utf16.IsValid(text)}；孤立代理项有效：{Utf16.IsValid("\uD800")}");
        string mixedLines = "line1\r\nline2\u0085line3\u2028line4";
        var matches = Regex.Matches(mixedLines, @"^line\d$", RegexOptions.Multiline | RegexOptions.AnyNewLine);
        Console.WriteLine($"跨不同换行符找到 {matches.Count} 行");

        var original = new StringBuilder("hello ");
        original.Append(".NET 11");
        StringBuilder moved = StringBuilder.MoveChunks(original);
        Console.WriteLine($"MoveChunks 后原长度={original.Length}，新内容={moved}");
        byte[] bytes = Encoding.UTF8.GetBytes("你好 .NET 11");
        string encoded = Base64.EncodeToString(bytes);
        Console.WriteLine($"Base64：{encoded}；解码：{Encoding.UTF8.GetString(Convert.FromBase64String(encoded))}");
        Console.WriteLine($"第一个空白位置：{"ab\u3000cd".AsSpan().IndexOfAnyWhiteSpace()}");
    }

    private static void Collections()
    {
        Console.WriteLine("\n=== LINQ FullJoin ===");
        Product[] products = [new(1, "键盘"), new(2, "鼠标")];
        Order[] orders = [new(2, 3), new(3, 1)];
        foreach (var (product, order) in products.FullJoin(orders, p => p.Id, o => o.ProductId))
            Console.WriteLine($"商品={product?.Name ?? "（无）"}，订单商品编号={order?.ProductId.ToString() ?? "（无）"}");
        // 结果包含：只在左侧、两侧匹配、只在右侧。重复键会产生多对结果。
        var count = products.Join(orders, p => p.Id, o => o.ProductId).Count();
        Console.WriteLine($"不写结果选择器的 Join 匹配对数：{count}");
        ReadOnlySpan<byte> bytes = [0b_0000_0011, 0b_1000_0000];
        var bits = new BitArray(bytes);
        Console.WriteLine($"BitArray：第 15 位={bits[15]}，置位总数={bits.PopCount()}");
        var comparer = EqualityComparer<(string Name, int Age)>.Create(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var people = new HashSet<(string Name, int Age)>(comparer) { ("Ada", 20), ("ADA", 30) };
        Console.WriteLine($"按姓名忽略大小写去重：{people.Count} 人");
    }

    private static void Numerics()
    {
        Console.WriteLine("\n=== 数值与向量 ===");
        double d = 1.23;
        Console.WriteLine($"double 转 decimal：{(decimal)d}；decimal 字面量：{1.23m}");
        string hex = Math.PI.ToString("X", CultureInfo.InvariantCulture);
        double roundTrip = double.Parse(hex, NumberStyles.HexFloat, CultureInfo.InvariantCulture);
        Console.WriteLine($"浮点十六进制 {hex}，按位往返：{BitConverter.DoubleToInt64Bits(roundTrip) == BitConverter.DoubleToInt64Bits(Math.PI)}");
        Console.WriteLine($"有界泛型随机数（0 到 9）：{Random.Shared.NextInteger<int>(0, 10)}");
        if (int.TryParsePartial("123,456".AsSpan(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int first, out int consumed))
            Console.WriteLine($"部分解析：值={first}，已读取={consumed}，剩余={"123,456".AsSpan(consumed).ToString()}");
        Vector128<int> powers = Vector128.CreateGeometricSequence(1, 2);
        var (lower, upper) = Vector128.Zip(Vector128.Create(1, 2, 3, 4), Vector128.Create(10, 20, 30, 40));
        Console.WriteLine($"几何数列：{powers}；交错：{lower}、{upper}");
        BigInteger big = BigInteger.Parse("123456789012345678901234567890"u8, CultureInfo.InvariantCulture);
        Console.WriteLine($"UTF-8 直接解析大整数：{big}");
        Decimal32 dec32 = Decimal32.Parse("0.1", CultureInfo.InvariantCulture);
        Decimal64 dec64 = Decimal64.Parse("0.1", CultureInfo.InvariantCulture);
        Decimal128 dec128 = Decimal128.Parse("0.1", CultureInfo.InvariantCulture);
        Console.WriteLine($"IEEE 十进制格式：{dec32}, {dec64}, {dec128}；Decimal64 无穷大={Decimal64.PositiveInfinity}");
        Complex<float> firstComplex = new(1f, 2f);
        Complex<float> secondComplex = new(3f, 4f);
        Console.WriteLine($"泛型复数 (1+2i)+(3+4i)={firstComplex + secondComplex}");
    }

    private static async Task JsonAsync()
    {
        Console.WriteLine("\n=== JSON Lines 与命名规则 ===");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.PascalCase };
        var item = new LibraryEvent { EventName = "launch", ReleaseVersion = "11", Notes = null };
        Console.WriteLine($"成员规则覆盖类型规则，null 被忽略：{JsonSerializer.Serialize(item, options)}");
        options.MakeReadOnly(populateMissingResolver: true);
        Console.WriteLine($"强类型元数据：{options.GetTypeInfo<LibraryEvent>().Type.Name}");
        JsonDemoReading reading = new("传感器离线");
        string unionJson = JsonSerializer.Serialize(reading);
        JsonDemoReading decoded = JsonSerializer.Deserialize<JsonDemoReading>(unionJson);
        Console.WriteLine($"联合类型直接序列化为 JSON 值：{unionJson}；恢复类型：{decoded.Value?.GetType().Name}");
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsyncEnumerable(stream, GenerateValues(), topLevelValues: true);
        Console.WriteLine("逐行 JSON：");
        Console.Write(Encoding.UTF8.GetString(stream.ToArray()));
        // 每行是一个独立的 JSON 值，最后一个值后也有 LF；不是一个 JSON 数组。
    }

    private static async IAsyncEnumerable<int> GenerateValues()
    {
        yield return 1;
        await Task.Yield();
        yield return 2;
    }

    private static async Task StreamsAndArchivesAsync()
    {
        Console.WriteLine("\n=== 内存流、压缩和 ZIP ===");
        using (Stream stream = new StringStream("配置=启用", Encoding.UTF8))
        using (var reader = new StreamReader(stream))
            Console.WriteLine($"StringStream：{await reader.ReadToEndAsync()}");

        byte[] source = Encoding.UTF8.GetBytes("同一份缓冲区，不需要先包装成 MemoryStream。 ");
        byte[] compressed = new byte[256];
        using var encoder = new ZLibEncoder();
        OperationStatus status = encoder.Compress(source, compressed, out int consumed, out int written, isFinalBlock: true);
        if (status != OperationStatus.Done || consumed != source.Length)
            throw new InvalidOperationException($"压缩未完成：{status}");
        using var decoder = new ZLibDecoder();
        byte[] restored = new byte[source.Length];
        OperationStatus decoded = decoder.Decompress(compressed.AsSpan(0, written), restored, out int read, out int produced);
        if (decoded != OperationStatus.Done || read != written)
            throw new InvalidOperationException($"解压未完成：{decoded}");
        Console.WriteLine($"ZLib 消耗 {consumed} 字节，输出 {written} 字节；还原：{Encoding.UTF8.GetString(restored, 0, produced)}");
        encoder.Reset(); // 对独立的下一条消息重用对象。
        decoder.Reset();

        using var archiveBytes = new MemoryStream();
        using (var archive = new ZipArchive(archiveBytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("hello.txt");
            using var writer = new StreamWriter(entry.Open(FileAccess.Write), Encoding.UTF8);
            writer.Write("你好 ZIP");
        }
        archiveBytes.Position = 0;
        using var zip = new ZipArchive(archiveBytes, ZipArchiveMode.Read);
        var saved = zip.GetEntry("hello.txt")!;
        using var content = new StreamReader(saved.Open(FileAccess.Read), Encoding.UTF8);
        Console.WriteLine($"ZIP 方法={saved.CompressionMethod}，内容={await content.ReadToEndAsync()}");
    }

    private static async Task ProcessAsync()
    {
        Console.WriteLine("\n=== 进程输出 ===");
        // 调用同机已安装的 dotnet，不启动 shell，也不依赖网络。
        ProcessTextOutput result = await Process.RunAndCaptureTextAsync("dotnet", ["--version"]);
        Console.WriteLine($"子进程版本={result.StandardOutput.Trim()}，退出码={result.ExitStatus.ExitCode}");
        if (!string.IsNullOrWhiteSpace(result.StandardError))
            Console.WriteLine($"标准错误：{result.StandardError.Trim()}");
    }

    private static async Task HttpCompressionAsync()
    {
        Console.WriteLine("\n=== HTTP 请求体压缩（不发送网络请求） ===");
        using var content = new GZipCompressedContent(new StringContent("请求数据"), CompressionLevel.Fastest);
        byte[] compressed = await content.ReadAsByteArrayAsync();
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        Console.WriteLine($"Content-Encoding={string.Join(',', content.Headers.ContentEncoding)}，解压后={await reader.ReadToEndAsync()}");
        using var handler = new SocketsHttpHandler { InitialHttp2MaxConcurrentStreams = 20 };
        Console.WriteLine($"HTTP/2 初始并发限制={handler.InitialHttp2MaxConcurrentStreams}");
    }

    private sealed record Product(int Id, string Name);
    private sealed record Order(int ProductId, int Quantity);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonNamingPolicy(JsonKnownNamingPolicy.SnakeCaseLower)]
    public sealed class LibraryEvent
    {
        [JsonNamingPolicy(JsonKnownNamingPolicy.CamelCase)]
        public string EventName { get; init; } = "";
        public string ReleaseVersion { get; init; } = "";
        public string? Notes { get; init; }
    }
}

public union JsonDemoReading(int, string);
