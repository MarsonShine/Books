# CBOR：数据分两次到达时怎样读取

假设网络中传来一个数组，但第一批数据只包含数组开头和第一个元素。旧的处理方式通常要先收集完整消息，再交给读取器；.NET 11 的 `CborReader` 可以读到目前已有的内容，缺数据时暂停，收到下一批后继续。

示例代码在 [cbor/Program.cs](../cbor/Program.cs)。它把数组 `[10, 20]` 的 CBOR 编码分成两段，用来模拟分批收到的数据。CBOR 是一种二进制数据格式，编码规则可参考 [RFC 8949](https://www.rfc-editor.org/rfc/rfc8949)。

需要 .NET 11 SDK 和 `System.Formats.Cbor` 11.0.0。第一次运行要从 nuget.org 的 HTTPS 源恢复包，包已缓存后可以离线运行。项目目录中的 `NuGet.Config` 单独配置了该源：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/cbor -c Release
```

## 第一段读完，数组还没结束

这个数组的编码和示例拆分如下：

```text
第一段：82 0A    82 表示有两个元素的数组，0A 表示整数 10
第二段：14       14 表示整数 20
```

程序先用第一段创建读取器，并指定 `isFinalBlock: false`，表示后面还有可能收到数据：

```csharp
var reader = new CborReader(firstChunk,
    new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax },
    isFinalBlock: false);

int? length = reader.ReadStartArray(); // 2
int first = reader.ReadInt32();        // 10
```

这时数组还有一个元素，但手里的两个字节已经读完。`reader.PeekState()` 返回 `NeedsMoreData`：读取器知道还不能结束，却也没有足够的数据继续。

## 收到第二段后继续

示例先确认 `BytesRemaining == 0`，也就是第一段恰好全部消费，再用 `SlideData` 交入第二段。由于这次知道后面没有数据了，标记 `isFinalBlock: true`：

```csharp
reader.SlideData(lastChunk, isFinalBlock: true);
int second = reader.ReadInt32(); // 20
reader.ReadEndArray();
```

读完数组后，`PeekState()` 返回 `Finished`。因此程序先打印第一个数和 `NeedsMoreData`，再打印第二个数和 `Finished`。

这个例子有意把数据拆在元素边界，第一段没有剩余字节。真实网络分段不一定这么整齐：一个值可能只收到一部分。如果还有未消费的字节，调用方需要把它们与新到的数据一起保留，再交给读取器；不能直接扔掉旧缓冲。

目前增量读取要求 `CborConformanceMode.Lax`，其他严格模式不接受非最终块。消息最大长度、缓冲区管理、取消和连接中断也需要由调用方处理。[.NET 11 增量读取说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#read-cbor-incrementally)
