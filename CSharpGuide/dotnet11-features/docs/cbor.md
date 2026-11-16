# 增量读取 CBOR：数据没收齐也能先处理

[CBOR](https://www.rfc-editor.org/rfc/rfc8949) 是一种二进制数据格式。网络数据可能分多次到达，等整条消息收到以后再解析，会延迟处理，也可能要求准备一块完整缓冲区。.NET 11 的 `CborReader` 可以在数据未全部到达时读取，缺数据时告诉调用方，再接收下一段。

项目依赖 `System.Formats.Cbor` 11.0.0。首次运行需要访问 `https://api.nuget.org/v3/index.json`；项目目录单独配置该 HTTPS 源，已缓存依赖时可离线运行。

```powershell
dotnet run --project CSharpGuide/dotnet11-features/cbor -c Release
```

## 跟着两段字节走一遍

示例把数组 `[10, 20]` 的编码拆成两段：

```text
第一段：82 0A    → 82 表示两个元素的数组，0A 是 10
第二段：14       → 20
```

1. 用第一段创建 `CborReader`，明确 `isFinalBlock: false`。读取数组开头和第一个数 10。
2. 再问读取器当前状态，得到 `NeedsMoreData`：数组还有一项，当前缓冲却用完了。
3. `BytesRemaining == 0` 表示这段数据已经完全消费，可以用 `SlideData` 给出第二段，并标为最后一段。
4. 读取第二个数 20，再读数组结尾，状态变成 `Finished`。

`NeedsMoreData` 不代表输入无效，也不表示最终数据已经完整。示例只演示前一段恰好被消费完的情况：如果一段里还有未消费字节，生产代码必须把这些字节与新输入一起妥善保留，不能直接丢掉或假设每次都对齐值边界。

当前增量读取要求 `CborConformanceMode.Lax`；其他严格模式不接受非最终块。内存缓冲、数据大小限制、取消、连接断开和恶意输入处理仍由调用方负责。

参考：[.NET 11 增量 CBOR 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#read-cbor-incrementally)。
