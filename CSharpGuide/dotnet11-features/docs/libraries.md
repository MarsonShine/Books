# 基础库：从数据怎样流动来理解新增 API

配合 `LibraryDemos.cs` 阅读。这个文件从头运行时不联网，也不在项目目录创建文件：ZIP、压缩内容和 JSON 都在内存里，进程示例只调用本机的 `dotnet --version`。随机数会变化，压缩大小也可能随平台和实现变化。

## 字符、Rune 和索引

.NET 字符串存的是 UTF-16。`char` 是一个 16 位的编码单元，不保证恰好代表一个完整字符。`😀` 是一个 Unicode 标量，在 UTF-16 中需要两个 `char`：

```text
字符串：  A   😀       B
索引：    0   1   2    3
char 数： 1   2        1
```

所以 `"A😀B".Length` 是 4，`IndexOf(new Rune(0x1F600))` 返回 1。返回值仍是 UTF-16 的 `char` 索引，而不是“第几个用户看到的字符”。由多个标量组成的字形（如人脸加肤色修饰）也不能保证是一个 `Rune`。

.NET 11 给 `string` 增加接受 `Rune` 的查找、替换、拆分和修剪重载；示例直接查找和替换 😀。需要验证收到的字节时，`Utf8.IndexOfInvalidSubsequence` 会给出首个不合法序列的**字节位置**；`Utf16.IsValid` 判断代理项是否正确配对。示例的 `[0x41, 0xC3, 0x28]` 在索引 1 开始无效，因为 `0xC3` 需要后续字节，而 `0x28` 不是合法的续字节。正常输入返回 `-1`。

`RegexOptions.AnyNewLine` 让正则按更多 Unicode 换行形式理解 `^`、`$` 和 `.`。示例同时启用 `Multiline`，才能在各行分别使用 `^` 和 `$`。这改变了正则对换行的认识，应按输入协议选择，不应假设所有文本协议都把这些字符当换行。

`StringBuilder.MoveChunks` 把已有块转给新的 builder：调用后原 builder 清空。它适合不再需要原内容而要转移所有权的情形，不能当成复制；移动后继续修改源对象与修改目标对象是两回事。

## FullJoin：哪些记录会出现在结果里

示例里的商品编号是 `{1, 2}`，订单中的商品编号是 `{2, 3}`：

```text
商品 1 ── 没有订单              → (商品 1, null)
商品 2 ── 订单商品编号 2         → (商品 2, 订单 2)
          订单商品编号 3 ── 无商品 → (null, 订单 3)
```

`FullJoin` 是完整外连接：匹配项配对，左右两边各自没有匹配的项也保留。它返回二元组；对于示例中的引用类型，缺失一边是 `null`。如果键不唯一，会输出所有匹配组合，例如同一个键左边两项、右边三项，就有六个结果。不要把这个 API 当去重工具。

LINQ 的调用是延迟执行的；构造查询时没有立即遍历。示例用 `foreach` 才开始执行。`Queryable.FullJoin` 是另一回事：能否转换为 SQL 还取决于数据库提供程序版本。

`BitArray(ReadOnlySpan<byte>)` 避免为了构造先调用 `ToArray`；`PopCount` 统计为真的位数。示例第一字节有两个 1，第二字节最高位有一个 1，因此总数为 3。`EqualityComparer<T>.Create` 用键选择器创建相等比较器，`HashSet` 会用同一比较规则判断相等和生成哈希；示例按姓名忽略大小写，两个不同年龄的 Ada 因而只保留一个。

## JSON Lines、命名和复用

普通 JSON 数组是一整个值：`[1,2]`。JSON Lines 则是一行一个完整 JSON 值：

```text
1\n
2\n
```

示例调用 `SerializeAsyncEnumerable(..., topLevelValues: true)` 将异步序列逐项写入流；每个值后都有 LF，包括最后一个。输出适合逐条消费；读到一行不必等待整个数组闭合。它仍需要正确处理取消、写入失败和中途终止，不表示写入一定逐条立刻发送到远端。省略 `topLevelValues` 会输出 JSON 数组。

示例同时展示三个命名层级：全局 PascalCase、类型 snake_case、成员 camelCase。成员显式规则优先，因此 `EventName` 写成 `eventName`；`ReleaseVersion` 使用类型规则，写成 `release_version`。类型上的 `JsonIgnore(Condition = WhenWritingNull)` 让 `Notes` 为 null 时省略，成员单独指定忽略规则可以覆盖它。属性上的 `JsonPropertyName` 又有更高优先级。

`options.MakeReadOnly(populateMissingResolver: true)` 固定配置并准备默认解析器，随后 `GetTypeInfo<LibraryEvent>()` 返回带泛型类型的元数据，不再需要把非泛型结果强转。配置固定后不要再修改 options。

C# union 的 JSON 支持值得单独注意。`JsonDemoReading` 包含 `int` 和 `string` 两种情况，示例把字符串分支序列化成 JSON 字符串，反序列化后从 `Value` 得到 `string`；没有外层包装对象，也没有 `$type` 字段。默认按 JSON 的 token 类型选择分支：数字和字符串容易分辨，两个对象类型都是 `{...}`，无法只看 token 判断；可以使用 `JsonUnionTypeStructuralClassifier` 按根属性识别，零个或多个候选会抛 `JsonException`。它不会检查嵌套内容或属性值，也不能解决任意相同 JSON 形状。不要把默认规则当成完整的业务校验。参见[联合类型序列化](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)。

## 压缩：输入、输出和状态分别由谁负责

示例使用 `ZLibEncoder.Compress`，输入是现有字节数组，输出写入调用方准备的缓冲。返回值 `OperationStatus` 和两个长度必须一起检查：

- `bytesConsumed` 表示这次实际处理了多少输入，`bytesWritten` 表示输出了多少字节。
- `Done` 且确认输入全部消耗，才表示这条最终数据处理完毕。
- 如果目标缓冲不足，需要处理已经写出的部分，再准备新空间继续；不能把不完整的输出当作完整压缩数据。
- `isFinalBlock: true` 表示没有后续输入，需要写完格式的结尾。处理另一条独立消息前调用 `Reset()`，或者创建新实例；结束时释放实例。

示例也检查解码状态、消费字节数，然后按实际 `produced` 长度解释输出。真实分块流不能假定一次就完成，还需要根据 `OperationStatus`、剩余数据和最终块状态继续推进。

`StringStream` 把字符串按指定编码作为只读字节流提供，适合已有文本、下游只接受 `Stream` 的场景。它避免调用方先手动创建整份 UTF-8 数组；读取时仍要进行编码。`ReadOnlyMemoryStream`、`WritableMemoryStream`、`ReadOnlySequenceStream` 则分别包装已有内存或分段序列，视图的可写性和底层数据生命周期仍要由调用方管理。

ZIP 示例先创建归档，再重新定位流，用 `Open(FileAccess.Read)` 读取，并显示 `CompressionMethod`。在 .NET 11 读取 ZIP 会检查 CRC32，损坏或截断的条目可能抛 `InvalidDataException`；CRC 用于检测意外损坏，不是防篡改认证。密码保护条目、tar 格式选择、Zstandard 以及不同格式的适用条件见[官方发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#compression-and-archive-formats)。

## HTTP：请求体压缩不等于响应自动解压

`GZipCompressedContent` 包装原始 `HttpContent`，在发送请求时压缩请求体并添加 `Content-Encoding: gzip`。示例只在内存中读取其压缩结果，再用 `GZipStream` 解开，因此不需要真实服务。

服务器必须支持 gzip 请求体，才应该发送这种内容。它与 `SocketsHttpHandler.AutomaticDecompression` 处理服务器**响应**是两件事。请求压缩也不会默认让所有 HTTP 消息自动压缩。

`InitialHttp2MaxConcurrentStreams = 20` 表示新 HTTP/2 连接收到服务器 SETTINGS 之前，先按最多 20 条并发流安排。收到 SETTINGS 后使用服务器公布的限制；它不是控制全局请求数的限流器，值至少为 1。连接回收可用 `ShouldEvictConnection` 按连接信息作决定；DNS 的 `ResolveSrv`、`ResolveMx`、`ResolveTxt` 等 API 则涉及真实 DNS，不放进默认离线流程。实验性 TLS 会话 API 有 `SYSLIB5007` 诊断，也不会因为普通 `SslStream` 调用而自动启用。[网络发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#networking-and-transport-security)

## 浮点转换、部分解析与向量排列

`1.23` 是 `double` 字面量，二进制无法精确表示十进制 1.23。.NET 11 改善了 `double` 到 `decimal` 的转换：示例显示更接近二进制输入实际值的十进制结果。写 `1.23m` 则直接得到十进制字面量。旧版本升级后相关结果可能变化；对于金额，输入本来是十进制时应直接解析或使用 `decimal`，而不是期望二进制浮点能恢复原来的十进制意图。[转换行为说明](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11.0/decimal-biginteger-conversions)

浮点 `"X"` 格式及 `NumberStyles.HexFloat` 可精确往返浮点位模式。示例用 `BitConverter.DoubleToInt64Bits` 比较，避免只显示较短十进制文本造成“看起来一样”的错觉。`TryParsePartial` 则从 `123,456` 消耗 `123`，返回已消费字符数 3，调用方据此继续处理逗号之后的数据；剩余输入并没有被解析或验证。

`Decimal32`、`Decimal64`、`Decimal128` 是新的 IEEE 754 十进制浮点格式，分别提供不同的存储宽度与精度，也支持无穷大和 NaN。它们与 C# 的 `decimal` 不是同一个类型，不能只因为名字都含 decimal 就假定范围、精度和异常行为完全一样；选择时应看数据格式及运算规则。`Complex<T>` 则允许指定复数实部、虚部的数值类型，示例使用 `float`，把 `(1 + 2i)` 和 `(3 + 4i)` 相加得到 `(4 + 6i)`，无需自己拆开两个分量计算。

`Vector128<int>` 总共 128 位，容纳四个 32 位整数。`CreateGeometricSequence(1, 2)` 得到 `<1, 2, 4, 8>`；`Zip` 交错两组元素，得到两份向量。它演示元素如何重排，不代表数组尾部会自动处理。`Random.NextInteger<int>(0, 10)` 的上界 10 不包含在内。

## Process：同时拿到输出和退出状态

过去启动进程、重定向 stdout/stderr、等待退出时，需要自己安排读取和等待，避免一侧管道填满导致双方互相等待。`Process.RunAndCaptureTextAsync` 将常见的启动、收集输出和取得退出状态组合起来。示例不通过 shell，直接运行本机 `dotnet --version`，最后分别读取 `StandardOutput`、`StandardError` 和 `ExitStatus`。

仍应检查退出状态；成功启动进程不等于子进程执行成功。读取全部输出也会占用内存。处理无限输出或非常大的输出时，应改为逐行或流式处理，并根据需要设置取消和超时。

[.NET 11 基础库正式版说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) 列出了其他平台专属、需要额外包或仍处于实验阶段的能力；使用前请核对运行平台、诊断 ID 和部署约束。
