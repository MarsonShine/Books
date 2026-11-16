# CBOR：数据分两次到达时怎样读取

CBOR 和 JSON 一样，可以用来表示整数、字符串、数组和对象等数据。两者的区别在于编码方式：JSON 是文本，CBOR 使用二进制字节。发送方和接收方都支持 CBOR 时，就可以用它交换这些结构化数据。

这篇文章先用数组 `[10, 20]` 看看 CBOR 和 JSON 有什么区别，再通过 [cbor/Program.cs](../cbor/Program.cs) 观察 .NET 11 如何分批读取 CBOR。CBOR 的编码规则由 [RFC 8949](https://www.rfc-editor.org/rfc/rfc8949) 定义。

## 同一个数组，JSON 和 CBOR 怎样表示

如果用 JSON 表示这个数组，可以写成文本 `[10,20]`。把它编码为 UTF-8 后，共有七个字节：

```text
文本：   [  1  0  ,  2  0  ]
字节：  5B 31 30 2C 32 30 5D
```

使用 CBOR，示例中的同一个数组可以表示为三个字节：

```text
字节：  82 0A 14
含义：  │  │  └─ 整数 20
        │  └──── 整数 10
        └─────── 包含两个元素的数组
```

这里的字节以十六进制书写；`0A` 等于十进制的 10，`14` 等于十进制的 20。接收方按照 CBOR 规则读取 `82`，便知道后面有两个数组元素；再读取 `0A` 和 `14`，就能还原出 `[10, 20]`。

这个例子里 CBOR 比 JSON 少用了几个字节。数据量大、消息发送频繁或者网络带宽有限时，较紧凑的表示可能有帮助，但实际大小仍取决于数据内容和是否压缩。CBOR 还可以直接保存字节数据；JSON 没有字节串类型，常见做法是先把字节编码成 Base64 字符串。

代价是 JSON 文本可以直接打开阅读，CBOR 字节通常需要解码工具。CBOR 规定的是数据如何编码；它可以放在 HTTP 请求体、TCP 消息或文件中，并不负责建立连接或传输数据。日常 Web API 如果双方都已使用 JSON，未必需要为了这个小例子的大小差异切换格式。

## 示例：数据分两次到达

假设网络中传来上面的 CBOR 数组，但第一批只包含数组开头和第一个元素。程序不一定要等整条数据都到齐才开始解析：.NET 11 的 `CborReader` 可以先读取已有内容，遇到数据不足时等待调用方提供下一批，再从之前的位置继续。

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

`PeekState()` 相当于先问“接下来能读到什么”，不会替我们把下一个值读走。这里它告诉程序继续之前需要更多数据；网络连接本身不会因为这个返回值自动读取下一包，还要由调用方取得并交给读取器。

## 收到第二段后继续

示例先确认 `BytesRemaining == 0`，也就是第一段恰好全部读完，再用 `SlideData` 交入第二段。由于这次知道后面没有数据了，标记 `isFinalBlock: true`：

```csharp
reader.SlideData(lastChunk, isFinalBlock: true);
int second = reader.ReadInt32(); // 20
reader.ReadEndArray();
```

读完数组后，`PeekState()` 返回 `Finished`。因此程序先打印第一个数和 `NeedsMoreData`，再打印第二个数和 `Finished`。

这个例子有意把数据拆在元素边界，第一段没有剩余字节。真实网络分段不一定这么整齐：一个值可能只收到一部分。如果还有尚未读完的字节，调用方需要把它们与新到的数据一起保留，再交给读取器；不能直接扔掉旧缓冲。

目前增量读取要求 `CborConformanceMode.Lax`，其他严格模式不接受非最终块。消息最大长度、缓冲区管理、取消和连接中断也需要由调用方处理。[.NET 11 增量读取说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#read-cbor-incrementally)

## 如果数据来自 PipeReader

`PipeReader` 也能分批读取数据，看起来与 `CborReader` 很像。区别在于它们负责不同的事情：

| 组件 | 负责什么 |
| --- | --- |
| `PipeReader` | 提供当前收到的字节，并管理哪些缓冲数据可以释放；它不需要知道数据是什么格式。 |
| `CborReader` | 按 CBOR 规则解释字节，记住当前读到数组、整数还是其他结构；它不会自己去网络读取下一批数据。 |

可以把两者接在一起：数据先进入管道，应用从 `PipeReader` 取得字节，再交给 `CborReader` 解析。以本例为例，第一批 `82 0A` 已经让 CBOR 读取器知道“进入了一个两项数组，第一项是 10”。这时如果还有第二项没收到，应用就需要等待管道提供更多数据，而不是重新创建读取器、从数组开头再读一次。

### AdvanceTo 和 SlideData 有什么不同

调用 `PipeReader.ReadAsync()` 后，返回的 `Buffer` 表示当前可读的数据。处理完这一批后，`AdvanceTo(consumed, examined)` 向管道说明两个位置：

- `consumed`：这个位置之前的数据已处理完，以后不再需要，管道可以释放或复用相应内存。
- `examined`：这次检查到了哪里。管道据此判断下次读取是否需要等待更多数据。

它们可以不同。比如缓冲开头已经读完，后面是一个只收到一半的 CBOR 值。应用可以将 `consumed` 停在这个不完整值的开头，将 `examined` 标记到本次确实检查过的位置。这样未读完的字节得以保留，等新数据到达后继续处理。

`CborReader.SlideData(...)` 做的是另一件事：给同一个 CBOR 读取器换一段数据，同时保留它的解析进度。它不会替 `PipeReader` 调用 `AdvanceTo`，也不会替应用接收网络数据；反过来，`AdvanceTo` 只通知管道缓冲区的使用情况，不会替 CBOR 读取器完成解析。

本项目的 demo 没有使用 `PipeReader`，而是手动准备两份 `byte[]`。第一段恰好完全读完，所以可以直接将第二段传给 `SlideData`。真实网络分段可能落在一个值的中间；如果旧数据还有未读部分，调用方需要保留它，并与新数据一起提供给读取器。

还要留意两种 API 对内存的要求：`PipeReader` 的缓冲可能分成多个片段，而本示例中的 `CborReader` 接收的是一段连续的 `ReadOnlyMemory<byte>`。必要时需要将不完整的数据整理到合适的连续内存中。调用 `AdvanceTo` 后，不能再访问本次 `ReadAsync` 返回的旧 `Buffer`，也不能让 `CborReader` 继续引用已交还给管道复用的内存；下一轮应使用新返回的缓冲或应用自己保留的副本。

关于 `consumed`、`examined` 和缓冲生命周期的完整说明，可参阅 [System.IO.Pipelines 指南](https://learn.microsoft.com/en-us/dotnet/standard/io/pipelines)与 [PipeReader.AdvanceTo API](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipelines.pipereader.advanceto)。
