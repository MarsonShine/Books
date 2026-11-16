using System.Buffers;
using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32.SafeHandles;

/// <summary>可离线运行的 .NET 11 补充示例。</summary>
public static class ExtraLibraryDemos
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== 补充基础库示例 ===");
        await MemoryStreamsAsync();
        GroupJoinAndNumbers();
        JsonObjectUnion();
        await ArchiveAsync();
        Crypto();
        await PipeAsync();
    }

    private static async Task MemoryStreamsAsync()
    {
        byte[] data = Encoding.UTF8.GetBytes("hello");
        using (var writer = new WritableMemoryStream(data))
        {
            await writer.WriteAsync("H"u8.ToArray());
            Console.WriteLine($"WritableMemoryStream 修改原数组：{Encoding.UTF8.GetString(data)}");
        }
        using (var reader = new ReadOnlyMemoryStream(data))
        using (var text = new StreamReader(reader, Encoding.UTF8))
            Console.WriteLine($"ReadOnlyMemoryStream 读取：{await text.ReadToEndAsync()}");

        // 模拟网络数据分两段到达；不先拼成一个大数组。
        var first = new ByteSegment("前半"u8.ToArray());
        ByteSegment last = first.Append("后半"u8.ToArray());
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        using var sequenceStream = new ReadOnlySequenceStream(sequence);
        using var sequenceReader = new StreamReader(sequenceStream, Encoding.UTF8);
        Console.WriteLine($"ReadOnlySequenceStream 跨两段读取：{await sequenceReader.ReadToEndAsync()}");
    }

    private static void GroupJoinAndNumbers()
    {
        Customer[] customers = [new(1, "小林"), new(2, "小夏")];
        Purchase[] purchases = [new(1, "书"), new(1, "笔")];
        Console.WriteLine("GroupJoin：");
        foreach (var group in customers.GroupJoin(purchases, c => c.Id, p => p.CustomerId))
            Console.WriteLine($"  {group.Key.Name}：{string.Join('、', group.Select(p => p.Item))}（{group.Count()} 项）");

        float random = Random.Shared.NextBinaryFloat<float>();
        Half half = Random.Shared.NextBinaryFloat<Half>();
        BFloat16 value = BFloat16.Parse("1.5", CultureInfo.InvariantCulture);
        ushort bits = BitConverter.BFloat16ToUInt16Bits(value);
        BFloat16 roundtrip = BitConverter.UInt16BitsToBFloat16(bits);
        Console.WriteLine($"随机浮点：float={random}, Half={half}；BFloat16={value}, 位模式=0x{bits:X4}, 还原={roundtrip}");
    }

    private static void JsonObjectUnion()
    {
        const string catJson = """{"Name":"Misty","Lives":9}""";
        const string dogJson = """{"Name":"Rex","Breed":"Collie"}""";
        PetChoice cat = JsonSerializer.Deserialize<PetChoice>(catJson);
        PetChoice dog = JsonSerializer.Deserialize<PetChoice>(dogJson);
        Console.WriteLine($"JSON 根属性分类：Lives → {cat.Value?.GetType().Name}，Breed → {dog.Value?.GetType().Name}");
        try
        {
            _ = JsonSerializer.Deserialize<PetChoice>("""{"Name":"未区分"}""");
        }
        catch (JsonException)
        {
            Console.WriteLine("只有 Name，无法区分两个分支：JsonException");
        }

        DeliveryEvent sent = new Delivered("PKG-1");
        string eventJson = JsonSerializer.Serialize<DeliveryEvent>(sent);
        DeliveryEvent? restored = JsonSerializer.Deserialize<DeliveryEvent>(eventJson);
        Console.WriteLine($"closed 类型推断多态：{eventJson}；恢复类型：{restored?.GetType().Name}");

        using var output = new MemoryStream();
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
        writer.WriteStringValue("first");
        writer.Flush();
        output.SetLength(0);
        writer.Reset(output, new JsonWriterOptions { Indented = false });
        writer.WriteStringValue("second");
        writer.Flush();
        Console.WriteLine($"重置后复用 JSON writer：{Encoding.UTF8.GetString(output.ToArray())}");
    }

    private static async Task ArchiveAsync()
    {
        byte[] payload = Encoding.UTF8.GetBytes(".NET 11 Zstandard 示例");
        try
        {
            using var compressed = new MemoryStream();
            await using (var encoder = new ZstandardStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                await encoder.WriteAsync(payload);
            compressed.Position = 0;
            using var decoder = new ZstandardStream(compressed, CompressionMode.Decompress, leaveOpen: true);
            using var text = new StreamReader(decoder, Encoding.UTF8);
            Console.WriteLine($"Zstandard 压缩/解压：{await text.ReadToEndAsync()}");
        }
        catch (PlatformNotSupportedException ex)
        {
            Console.WriteLine($"当前平台不支持 Zstandard：{ex.Message}");
        }

        using var emptyGzip = new MemoryStream();
        using (var gzip = new GZipStream(emptyGzip, CompressionLevel.Fastest, leaveOpen: true)) { }
        Console.WriteLine($"空 GZip 关闭后仍有格式头尾：{emptyGzip.Length} 字节");

        // 只创建和删除本示例自己的临时文件。
        DirectoryInfo source = Directory.CreateTempSubdirectory("dotnet11-tar-");
        string item = Path.Combine(source.FullName, "note.txt");
        try
        {
            await File.WriteAllTextAsync(item, "tar 内容");
            using var tarBytes = new MemoryStream();
            TarFile.CreateFromDirectory(source.FullName, tarBytes, includeBaseDirectory: false, format: TarEntryFormat.Gnu);
            tarBytes.Position = 0;
            using var tar = new TarReader(tarBytes);
            TarEntry? entry = tar.GetNextEntry();
            Console.WriteLine($"指定 GNU tar：条目={entry?.Name}，格式={entry?.Format}");
        }
        finally
        {
            File.Delete(item);
            source.Delete();
        }

        const string password = "demo-password"; // 仅用于展示 API，不用于生产密码。
        using var zipBytes = new MemoryStream();
        using (var zip = new ZipArchive(zipBytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry entry = zip.CreateEntry("message.txt", password, ZipEncryptionMethod.Aes256);
            using var text = new StreamWriter(entry.Open(password), Encoding.UTF8);
            text.Write("加密的归档内容");
        }
        zipBytes.Position = 0;
        using var input = new ZipArchive(zipBytes, ZipArchiveMode.Read);
        using var content = new StreamReader(input.GetEntry("message.txt")!.Open(password), Encoding.UTF8);
        Console.WriteLine($"密码 ZIP：{await content.ReadToEndAsync()}");
        VerifyZipCrc();
    }

    private static void VerifyZipCrc()
    {
        byte[] content = "unique-crc-content-123"u8.ToArray();
        using var original = new MemoryStream();
        using (var archive = new ZipArchive(original, ZipArchiveMode.Create, leaveOpen: true))
        {
            using Stream entry = archive.CreateEntry("crc.txt", CompressionLevel.NoCompression).Open();
            entry.Write(content);
        }
        byte[] corrupted = original.ToArray();
        int offset = corrupted.AsSpan().IndexOf(content);
        if (offset < 0) throw new InvalidOperationException("没有找到未压缩内容，无法演示 CRC 校验。");
        corrupted[offset] ^= 1; // 只破坏内存中的副本，不修改磁盘文件。
        try
        {
            using var zip = new ZipArchive(new MemoryStream(corrupted), ZipArchiveMode.Read);
            using Stream entry = zip.GetEntry("crc.txt")!.Open();
            entry.CopyTo(Stream.Null); // 读到结尾才触发校验。
            Console.WriteLine("损坏 ZIP 未检测到 CRC 错误，请核对运行时版本。");
        }
        catch (InvalidDataException)
        {
            Console.WriteLine("ZIP 内容被篡改，读取到结尾时触发 CRC 校验失败。");
        }
    }

    private static void Crypto()
    {
        byte[] dataKey = RandomNumberGenerator.GetBytes(24);
        using var aes = Aes.Create();
        aes.Key = RandomNumberGenerator.GetBytes(32);
        byte[] wrapped = aes.EncryptKeyWrap(dataKey);
        byte[] unwrapped = aes.DecryptKeyWrap(wrapped);
        Console.WriteLine($"AES Key Wrap 往返一致：{CryptographicOperations.FixedTimeEquals(dataKey, unwrapped)}");
        byte[] shortKey = RandomNumberGenerator.GetBytes(19);
        byte[] wrappedPadded = aes.EncryptKeyWrapPadded(shortKey);
        byte[] unwrappedPadded = aes.DecryptKeyWrapPadded(wrappedPadded);
        Console.WriteLine($"19 字节密钥使用带填充包装：{CryptographicOperations.FixedTimeEquals(shortKey, unwrappedPadded)}");
        CryptographicOperations.ZeroMemory(dataKey);
        CryptographicOperations.ZeroMemory(unwrapped);
        CryptographicOperations.ZeroMemory(shortKey);
        CryptographicOperations.ZeroMemory(unwrappedPadded);

        if (!X25519DiffieHellman.IsSupported)
        {
            Console.WriteLine("当前平台不支持 X25519，跳过密钥交换。");
            return;
        }
        using var alice = X25519DiffieHellman.GenerateKey();
        using var bob = X25519DiffieHellman.GenerateKey();
        // 导出公钥后导入，避免示例暗示一方可以访问另一方的私钥。
        using var bobPublic = X25519DiffieHellman.ImportPublicKey(bob.ExportPublicKey());
        using var alicePublic = X25519DiffieHellman.ImportPublicKey(alice.ExportPublicKey());
        byte[] aliceSecret = alice.DeriveRawSecretAgreement(bobPublic);
        byte[] bobSecret = bob.DeriveRawSecretAgreement(alicePublic);
        Console.WriteLine($"X25519 两方协商结果一致：{CryptographicOperations.FixedTimeEquals(aliceSecret, bobSecret)}");
        CryptographicOperations.ZeroMemory(aliceSecret);
        CryptographicOperations.ZeroMemory(bobSecret);
    }

    private static async Task PipeAsync()
    {
        SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readEnd, out SafeFileHandle writeEnd, asyncRead: true, asyncWrite: false);
        using (readEnd)
        using (writeEnd)
        {
            Console.WriteLine($"匿名管道句柄类型：{readEnd.Type} / {writeEnd.Type}");
            RandomAccess.Write(writeEnd, "pipe"u8, fileOffset: 0);
            byte[] buffer = new byte[4];
            int received = 0;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            while (received < buffer.Length)
            {
                int count = await RandomAccess.ReadAsync(readEnd, buffer.AsMemory(received), fileOffset: 0, timeout.Token);
                if (count == 0) break;
                received += count;
            }
            Console.WriteLine($"RandomAccess 从管道读取 {received} 字节：{Encoding.UTF8.GetString(buffer, 0, received)}");
        }
    }

    private sealed record Customer(int Id, string Name);
    private sealed record Purchase(int CustomerId, string Item);

    private sealed class ByteSegment : ReadOnlySequenceSegment<byte>
    {
        public ByteSegment(ReadOnlyMemory<byte> memory) => Memory = memory;
        public ByteSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ByteSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union PetChoice(DogDetails, CatDetails);
public sealed record DogDetails(string Name, string Breed);
public sealed record CatDetails(string Name, int Lives);

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record DeliveryEvent;
public sealed record Delivered(string TrackingId) : DeliveryEvent;
public sealed record DeliveryFailed(string Reason) : DeliveryEvent;
