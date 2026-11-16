# .NET 11 基础库示例

本篇对应 [LibraryDemos.cs](../LibraryDemos.cs)。在仓库根目录运行：

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- libraries
```

示例处理的 JSON、压缩数据和 ZIP 都在内存里，进程部分会调用本机的 `dotnet --version`，不需要网络服务。下面按程序的执行顺序介绍，建议一边看代码一边对照输出。

## 文本：一个字符不一定只占一个 `char`

先看 `"A😀B"`。它看起来有三个字符，`.Length` 却返回 4：.NET 字符串使用 UTF-16，`char` 表示一个 16 位编码单元；其中的 😀 需要两个 `char` 才能表示。

```text
字符串：A  😀    B
索引：  0  1  2  3
```

如果按 `char` 查找或替换，需要小心不要把这个表情拆成一半。.NET 11 给字符串的查找、替换、拆分和修剪增加了接受 `Rune` 的重载。`Rune` 表示一个 Unicode 标量，示例用 `new Rune(0x1F600)` 查找 😀，又把它替换成 `*`。`IndexOf` 返回 1，这仍然是 **UTF-16 索引**；它不会把字符串改成按人眼看到的字符计数。一个字形还可能由多个 Unicode 标量组成，例如表情和肤色修饰符。

处理外部输入时，还需要知道编码是否完整。示例里的 UTF-8 数据是 `[0x41, 0xC3, 0x28]`：`0x41` 是字母 A，`0xC3` 表示一个多字节序列的开头，后面的 `0x28` 却不是合法的续字节。`Utf8.IndexOfInvalidSubsequence` 返回 1，指出第一个无效序列的起始字节位置；输入合法时返回 `-1`。`Utf16.IsValid` 则可检查 UTF-16 的代理项是否正确配对，示例中的孤立 `\uD800` 就不能单独构成合法字符。

同一段文本也可能混用不同换行符。示例中除了 Windows 的 `\r\n`，还包含 NEL 和 Unicode 行分隔符。正则同时使用 `Multiline` 和 .NET 11 的 `AnyNewLine` 后，可以用 `^line\d$` 找出四行：前者让 `^` 和 `$` 匹配行首、行尾，后者扩展了它们识别的换行形式。解析具体协议时，仍要以该协议允许的换行符为准。

这一组还有两个小工具。`StringBuilder.MoveChunks(source)` 将内容块转给新的 builder，调用后 `source.Length` 变成 0；需要保留原内容时不能把它当作复制。`Base64.EncodeToString` 把 UTF-8 字节直接编码成 Base64 字符串，示例随后解码，得到原来的中文文本。`IndexOfAnyWhiteSpace` 则直接查找文本中的 Unicode 空白，示例返回全角空格的位置 2。

## 集合：FullJoin 和比较器

示例有两件商品，编号分别是 1、2；两条订单引用的商品编号是 2、3。执行 `FullJoin` 后会得到：

```text
商品 1 + 没有订单
商品 2 + 订单 2
没有商品 + 订单 3
```

完整外连接既保留匹配的记录，也保留两边各自没有匹配的记录。本例中两边元素都是引用类型，缺失的一侧是 `null`。如果一个键在左侧出现两次、右侧出现三次，匹配部分会得到六对结果。`Join` 只返回匹配的记录；示例使用不需要结果选择器的重载，得到一对匹配。

LINQ 查询在枚举时执行，所以修改数据后再枚举，看到的是枚举时的数据。对于 `Queryable.FullJoin`，还要由数据库提供程序把表达式翻译成 SQL；C# 编译通过并不代表任意数据库提供程序都支持它。[SQLite 示例](ef.md)会打印实际生成的 SQL。

另外两个例子分别处理位和相等规则。`BitArray` 可以直接从 `ReadOnlySpan<byte>` 构造，示例中两个字节总共包含三个置位，`PopCount()` 返回 3。`EqualityComparer<T>.Create` 则通过键选择器生成比较器：示例用姓名作为键，并忽略大小写，所以 `("Ada", 20)` 与 `("ADA", 30)` 被 `HashSet` 视作同一个人。年龄不参与这份比较规则。

## JSON：一行一个值，和一个数组有什么区别

`SerializeAsyncEnumerable` 默认输出一个 JSON 数组；传入 `topLevelValues: true` 后，会逐项写成 JSON Lines：

```text
1\n
2\n
```

每一行都是独立的 JSON 值，每个值后都有 LF，包括最后一项。消费者可以按行处理，不必等整个数组闭合。不过写入流不保证数据立刻到达网络另一端，缓冲、取消和写入失败仍需由实际应用处理。

命名规则示例同时设置了三个层级：全局 PascalCase、类型上的 snake_case，以及 `EventName` 成员上的 camelCase。成员规则优先，所以结果是：

```json
{"eventName":"launch","release_version":"11"}
```

`Notes` 为 `null`，被类型上的忽略规则省略。成员若单独指定规则，可以覆盖类型规则；`JsonPropertyName` 则可为某个属性指定更高优先级的固定名称。

示例还调用 `MakeReadOnly(populateMissingResolver: true)`，固定 options 并准备默认类型解析器，然后用 `GetTypeInfo<LibraryEvent>()` 取得强类型元数据。options 固定后不能再修改。

`JsonDemoReading` 展示简单的 C# 联合类型序列化：它可以是整数，也可以是字符串。当前值为字符串时，输出就是一个 JSON 字符串，没有额外包装对象或 `$type` 字段。反序列化时，数字和字符串可以根据 JSON 值的类型区分；如果两个分支都是对象，就都以 `{` 开头，需要其他办法。这个情况在 [对象联合类型示例](advanced.md)中展开说明，完整规则见 [System.Text.Json 联合类型文档](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)。

## 压缩：输出缓冲区由调用方提供

.NET 11 的 `ZLibEncoder` 等 API 可以直接使用 span，不必先把输入包装成流。示例准备一份 UTF-8 输入和一块输出数组，然后调用 `Compress`。有三个返回信息需要一起看：

- `bytesConsumed`：这次处理了多少输入；
- `bytesWritten`：向输出缓冲写了多少字节；
- `OperationStatus`：操作是否完成，还是需要继续处理。

示例提供了足够大的输出缓冲，并设置 `isFinalBlock: true`，告诉编码器后面没有输入了。它确认状态为 `Done` 且全部输入已被消费，再把实际写出的那段数据交给解码器。解码时同样检查状态和消费量，并只使用实际产生的字节。

真实应用不能假定一次调用就完成。如果输出空间先用完，调用方需要处理已写出的部分，准备下一块缓冲，再从未消费的输入继续。编码器处理完一条独立消息后，可以 `Reset()` 再处理下一条，或者释放它并创建新实例。

`StringStream` 解决的是另一个常见问题：数据已经是字符串，下游 API 却要求 `Stream`。它按指定编码提供字节，省去调用方先创建整份 UTF-8 数组的步骤；读取过程中仍需要编码。其他几种包装已有内存的流见[补充示例](extra-libraries.md)。

ZIP 示例先创建归档，释放写入端后把内存流移回开头，再按 `FileAccess.Read` 打开条目并输出内容和 `CompressionMethod`。.NET 11 在读取 ZIP 条目时检查 CRC32，损坏或截断的数据可能抛出 `InvalidDataException`。CRC 用来发现数据损坏，不能替代防篡改认证。密码 ZIP、tar 和 Zstandard 见[补充示例](extra-libraries.md)及[官方压缩说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#compression-and-archive-formats)。

## HTTP：压缩的是请求体

`GZipCompressedContent` 包装原有的 `HttpContent`。序列化请求时，它压缩内容并设置 `Content-Encoding: gzip`。示例没有发送请求，而是把压缩字节读到内存，再用 `GZipStream` 解压，确认内容能还原。

服务端需要能够接收 gzip 请求体，才能使用这种包装。它与 `SocketsHttpHandler.AutomaticDecompression` 不同：后者处理的是服务器返回的压缩**响应**。

示例还设置了 `InitialHttp2MaxConcurrentStreams = 20`。新 HTTP/2 连接在收到服务器 SETTINGS 之前，先按这个数量限制可并发的流；收到 SETTINGS 后使用服务器公布的限制。它不是整个应用的请求限流器，设置值至少为 1。真实 DNS 和本地 HTTP 指标放在可选的[环境示例](environment.md)中；连接回收回调及 TLS 会话 API 的实验状态和平台条件，请参见[网络发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#networking-and-transport-security)。

## 数值：看清输入的表示方式

C# 中 `1.23` 是 `double`，`1.23m` 才是 `decimal`。二进制浮点无法精确保存十进制的 1.23；.NET 11 改进了二进制浮点到 `decimal` 等转换的舍入，示例会显示转换后更接近原始二进制值的结果，而不是简单显示为 `1.23`。如果金额原本来自十进制文本，应直接按十进制解析。升级时也要留意[转换行为变化](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11.0/decimal-biginteger-conversions)。

浮点数现在可以用 `"X"` 格式写成十六进制，再用 `NumberStyles.HexFloat` 解析。示例对 `Math.PI` 做往返后比较原始位模式，确认结果与输入一致。这比只比较控制台上显示的短十进制文本更直接。

`TryParsePartial` 用于只解析输入开头的一部分。输入 `123,456` 时，示例读出 123，报告消费了三个字符；逗号和后面的内容仍要由调用方继续处理。`BigInteger` 也可以直接从 UTF-8 字节解析，省去先转成字符串的步骤。

`Decimal32`、`Decimal64`、`Decimal128` 是不同宽度的 IEEE 754 十进制浮点类型，支持无穷大和 NaN；它们不是 C# 的 `decimal`，选择时需要分别考虑精度、范围和特殊值。`Complex<T>` 则允许选择复数分量的数值类型：示例把两个 `Complex<float>` 相加，得到 `(4 + 6i)`。

`Vector128<int>` 有 128 位，能装四个 32 位整数。示例用 `CreateGeometricSequence` 生成 1、2、4、8，再用 `Zip` 交错两组数据；数组长度不能被向量宽度整除时怎样处理，见[运行时示例](runtime.md)。`Random.Shared.NextInteger<int>(0, 10)` 返回的上界 10 不包含在内。

## 启动进程并收集输出

过去要同时重定向标准输出、标准错误并等待退出，通常需要自己安排读取，避免一侧管道写满。`Process.RunAndCaptureTextAsync` 把常见的启动、收集和等待过程组合起来。示例直接执行 `dotnet --version`，读取 `StandardOutput`、`StandardError` 和退出状态，并没有经过 shell。

启动成功不代表子进程执行成功，仍需检查退出状态。这个 API 收集完整输出；面对很大或持续不断的输出，应考虑逐行或流式读取，并设置取消和超时。可运行的逐行读取示例见[进程、DNS 与 HTTP 指标](environment.md)。

其他新增 API、适用平台及预览状态可查阅 [.NET 11 基础库发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries)。
