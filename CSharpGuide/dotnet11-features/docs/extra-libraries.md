# 基础库补充示例

[ExtraLibraryDemos.cs](../ExtraLibraryDemos.cs) 接着演示内存流、归档、密钥交换等 API。在仓库根目录运行：

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- extra
```

示例不访问网络。tar 示例会创建自己的临时目录和一个文件，结束后删除；其余数据主要放在内存中。随机数和压缩结果的大小可能随运行环境变化。

## 把现有内存交给 Stream API

假设上游已经给出一块 `byte[]`，下游却只接受 `Stream`。以前可以把数据复制到新的 `MemoryStream`，现在也可以让流直接使用已有内存。

这里的 `Stream` 可以理解为带当前位置的字节读写接口。例如读取时，每读走几个字节，当前位置就向后移动；写入时，数据从当前位置开始覆盖。它不一定对应磁盘文件，也可以操作内存里的数据。

示例先用 `WritableMemoryStream` 包装存有 `hello` 的数组，再从流的开头写入 `H`：

```text
原数组：hello
写入后：Hello
```

变化会出现在原数组中，因为流和调用方使用的是同一块内存。它的容量由这块内存决定，不会像可扩容的 `MemoryStream` 一样自动增长。如果内存来自数组池，也要等流用完后才能归还数组。

接着，`ReadOnlyMemoryStream` 把同一数组作为只读流提供给 `StreamReader`。只读限制的是通过这个流写入；其他代码如果还持有原数组，仍能修改内容。

`ReadOnlySequenceStream` 用来处理不连续的内存。示例把“前半”和“后半”分别放在两个片段里，再交给流读取。`StreamReader` 得到的是完整的“前半后半”，调用方不必先把两段拼成一个新数组。读取和 UTF-8 解码仍然会发生。

可以把两段想成网络接收时先后拿到的两个缓冲：第一块保存“前半”，第二块保存“后半”。`ReadOnlySequence<byte>` 记录这些缓冲以及它们的先后顺序；`ReadOnlySequenceStream` 则让只认识 `Stream` 的代码也能依次读取。它不保证底层数组永远不变，因此读取尚未结束时，提供缓冲的一方还不能回收或随意修改它们。

## GroupJoin：先找到每个客户，再看他有哪些订单

示例中的小林有“书”和“笔”两笔订单，小夏没有订单。`GroupJoin` 为每个客户产生一个分组：

```text
小林：书、笔（2 项）
小夏：（0 项）
```

这里使用不传结果选择器的重载：

```csharp
customers.GroupJoin(purchases, c => c.Id, p => p.CustomerId)
```

当前 .NET 11 SDK 中，这个调用返回 `IGrouping<Customer, Purchase>`：`group.Key` 是客户，枚举 `group` 得到他的订单。小夏没有匹配项，但客户这一组仍然存在。普通 `Join` 则只输出匹配上的客户与订单。

这条调用里有两个函数，分别告诉 LINQ 用什么来配对：`c => c.Id` 取客户编号，`p => p.CustomerId` 取订单上的客户编号。小林的 `Id` 为 1，两条订单的 `CustomerId` 也是 1，于是这两条订单被放进小林的分组。小夏仍有自己的分组，只是组里没有订单。

[发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#tuple-returning-join-and-groupjoin-overloads)把相关新重载概括为 tuple-returning；这里描述的是本机 SDK 中这个 `GroupJoin` 重载的实际类型。换成 `Queryable` 时，还要看数据库提供程序能否翻译查询。

## 随机浮点数与 BFloat16 的位表示

`Random.Shared.NextBinaryFloat<T>()` 可以按指定的二进制浮点类型生成随机值。示例分别生成 `float` 和 `Half`，输出每次都可能不同。`Random` 适合一般伪随机用途；生成密钥等敏感数据时，应使用 `RandomNumberGenerator`。

`float` 使用 32 位，`Half` 使用 16 位；能保存的范围和精度不同。泛型参数 `<float>` 或 `<Half>` 决定返回值的类型，不是生成后再统一转换。示例没有指定上下界，这些调用返回大于等于 0 且小于 1 的值。

示例还把十进制文本 `1.5` 解析为 `BFloat16`。它是 16 位二进制浮点格式，`BitConverter.BFloat16ToUInt16Bits` 把这个浮点数的 16 个二进制位按整数读出来，结果写成十六进制是 `0x3FC0`；`UInt16BitsToBFloat16` 再把相同的位还原为浮点数。如果原来的数在转换成 BFloat16 时已经丢失精度，之后不能凭这 16 位找回来。

## 两种 JSON 多态方式

### 联合类型：根据对象字段选择分支

`PetChoice` 有猫和狗两个分支。它们序列化后都是 JSON 对象，单看开头的 `{` 无法判断该创建哪个类型。示例配置 `JsonUnionTypeStructuralClassifier` 后，可以根据根字段区分：

```text
{"Name":"Misty","Lives":9}      → CatDetails
{"Name":"Rex","Breed":"Collie"} → DogDetails
{"Name":"未区分"}                 → 无法唯一选择，抛 JsonException
```

只有 `Name` 时，两种类型都可能符合。分类器会根据根级属性名称和必需属性筛选候选，不会读取属性值或分析嵌套内容。它帮助序列化器选择类型，业务仍需验证字段值是否合法。[联合类型序列化文档](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)

“根级属性”指对象最外层的字段。例如 `{"Name":"Misty","Details":{"Lives":9}}` 里，`Name` 和 `Details` 在根级；`Lives` 藏在 `Details` 里面，不会被当作根级的 `Lives` 来帮助区分猫和狗。

### closed 层次：用标记记住派生类型

另一个示例将 `Delivered` 作为 `DeliveryEvent` 序列化。基类声明为 `closed` 并启用了 `InferClosedTypePolymorphism`，序列化器据此发现派生类型，输出中包含 `$type`：

```json
{"$type":"Delivered","TrackingId":"PKG-1"}
```

反序列化后，结果的实际类型仍是 `Delivered`。这个例子与前面的 union 分类不同：这里由 `$type` 指明类型。标记会进入交换的数据，如果 JSON 需要长期保存或供其他服务使用，应明确管理标记的兼容性。显式注册派生类型时，注册列表会取代推断列表；使用源生成时也需要对应的类型元数据。

示例还复用了一次 `Utf8JsonWriter`：先完成并 `Flush` 第一份 JSON，清空目标 `MemoryStream`，再调用带新 `JsonWriterOptions` 的 `Reset` 写第二份。`Reset` 不会替调用方清空流，流的长度和位置仍由调用方管理。

`Flush` 的作用是把 writer 尚未交出的内容写到目标流；流本身可能还有自己的缓冲，并不保证网络另一端已经收到。示例写的是内存流，因此随后可直接读取字节。清空流、重置 writer 后，第二次输出是独立的 `"second"`，而不是把新值接到 `"first"` 后面。

## 压缩与归档

### Zstandard 和空 GZip

示例用 `ZstandardStream` 把文本写进内存流。压缩流关闭时会写完结尾；由于设置了 `leaveOpen: true`，底层内存流仍能继续使用。将位置移回开头后，再创建解压流读取文本。

Zstandard 不支持 Browser/WASI；代码捕获 `PlatformNotSupportedException`，在不支持的平台显示原因并跳过。示例没有使用字典。若要使用字典，训练 API 返回的是原始字节，还需要调用 `ZstandardDictionary.Create` 创建字典。[Zstandard 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#zstandard-compression)

空的 `GZipStream` 也会生成压缩格式的头和尾。示例创建后不写任何内容，关闭流，再打印底层流的长度。应在压缩流完成或释放后观察，否则尾部可能还没写出；应用也不应依赖示例输出某个固定字节数。

### 创建 GNU tar

示例创建临时目录和 `note.txt`，调用：

```csharp
TarFile.CreateFromDirectory(source.FullName, tarBytes,
    includeBaseDirectory: false, format: TarEntryFormat.Gnu);
```

`format` 选择 GNU 格式，`includeBaseDirectory: false` 表示归档从目录内容开始，不再包一层源目录名。随后示例读取条目并打印格式，最后删除自己创建的文件和目录。选择 GNU、Pax、Ustar 或 V7 时，要考虑文件名、元数据和目标解压工具的支持；本例没有创建 sparse 文件。

### 密码 ZIP 与 CRC 校验

示例使用 `ZipEncryptionMethod.Aes256` 创建加密条目，再用同一密码读取内容。代码中的密码只是演示值，真实密码应从安全来源取得；接收方的解压工具也必须支持所选加密方式。旧式 `ZipCrypto` 的安全性较弱，主要用于兼容遗留工具。[ZIP 加密注意事项](https://learn.microsoft.com/en-us/dotnet/standard/io/zip-tar-best-practices#encryption-considerations-net-11)

CRC 示例则另建一个未压缩条目，在内存副本中改坏内容，再读取到结尾。内容与归档保存的 CRC 不一致时会抛 `InvalidDataException`。如果读取到一半就停止，不能据此认为整条内容已通过校验。CRC 适合发现意外损坏，不能作为防止恶意篡改的认证方式。

## 用密钥保护密钥：AES Key Wrap

示例随机生成一个 32 字节的 AES 密钥，再生成一个 24 字节的数据密钥。前者用来包装后者：

```text
AES 密钥 + 数据密钥 → 包装后的数据
AES 密钥 + 包装后的数据 → 原来的数据密钥
```

可以把前者叫作“密钥加密密钥”：数据密钥需要保存或交给另一处使用时，先用它保护，再保存包装后的结果。收到结果的一方必须持有正确的 AES 密钥，才能还原原数据密钥。示例只演示 API 调用，两个密钥都临时生成并保存在当前进程里。

程序调用 `EncryptKeyWrap` 和 `DecryptKeyWrap`，然后用 `FixedTimeEquals` 比较还原后的密钥。无填充的 Key Wrap 要求输入至少 16 字节，且长度为 8 字节的倍数；示例还用 19 字节输入演示带填充的 `EncryptKeyWrapPadded`。用过的密钥数组会被清除，但示例没有实现密钥存储、权限控制或密钥轮换。[AES Key Wrap 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#aes-key-wrap)

## 通过公钥协商共享秘密：X25519

Alice 和 Bob 各自生成密钥对，交换公钥。Alice 用自己的私钥和 Bob 的公钥计算；Bob 用自己的私钥和 Alice 的公钥计算。示例分别导出、导入公钥，再比较双方得到的秘密，输出 `True` 表示本次结果一致，程序不会打印秘密本身。

“公钥”可以发给对方，“私钥”由各自保管。两边分别用自己的私钥和收到的对方公钥计算，虽然输入的顺序和持有的私钥不同，结果会相同。示例先导出公钥，再导入一份公钥对象交给对方计算，模拟交换过程；没有把 Alice 或 Bob 的私钥传给另一方。

代码先检查 `X25519DiffieHellman.IsSupported`，不支持的平台会跳过。实际通信还要认证对方公钥：否则可能和冒充对方的人算出另一个共享秘密。协商出的原始秘密还通常要经过密钥派生函数（KDF），结合协议要求生成后续加密使用的会话密钥。本例只在本机演算，不建立连接。[X25519 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#x25519-diffie-hellman-key-exchange)

## 通过管道读取数据

`SafeFileHandle.CreateAnonymousPipe` 创建一条管道，并返回一对读写句柄。句柄是操作系统交给程序的访问入口：写句柄用于向管道放数据，读句柄用于取数据。示例从写端写入 `pipe`，再通过 `RandomAccess.ReadAsync` 从读端读取。

一次读取可能只返回部分数据，所以代码根据已读字节数继续读取，直到取完四字节或管道结束；读取还设置了短超时。管道不能像磁盘文件那样定位到任意位置。对不可定位句柄，`RandomAccess` 会忽略 `fileOffset`，但参数仍需非负；这里传入 0。读写完成后，两端安全句柄都会释放。[RandomAccess 说明](https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess?view=net-11.0)
