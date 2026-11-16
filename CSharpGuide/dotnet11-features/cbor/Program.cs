using System.Formats.Cbor;

// CBOR 字节 82 0A 14：长度为 2 的数组 [10, 20]；模拟分两次收到。
ReadOnlyMemory<byte> firstChunk = new byte[] { 0x82, 0x0A };
ReadOnlyMemory<byte> lastChunk = new byte[] { 0x14 };
var reader = new CborReader(firstChunk,
    new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax }, isFinalBlock: false);
int? length = reader.ReadStartArray();
int first = reader.ReadInt32();
Console.WriteLine($"收到第一段：数组长度={length}，第一个数={first}，当前状态={reader.PeekState()}");
if (reader.PeekState() != CborReaderState.NeedsMoreData || reader.BytesRemaining != 0)
    throw new InvalidOperationException("示例预期第一段已全部消费，且还需要更多数据。");
reader.SlideData(lastChunk, isFinalBlock: true);
int second = reader.ReadInt32();
reader.ReadEndArray();
Console.WriteLine($"收到最后一段：第二个数={second}，当前状态={reader.PeekState()}");
