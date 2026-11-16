# 基础库补充：内存流、归档和密钥交换

运行 `libraries-extra` 组（以项目 README 的最终命令为准），同时打开 `ExtraLibraryDemos.cs`。示例除了 tar 临时目录外都只在内存中工作，不需要连接网络。tar 只创建一个临时文件，用完后按文件、目录顺序删除；不会操作用户指定路径。示例中的随机数和压缩大小每次可能不同。

## 不复制数据，如何把内存交给接受 Stream 的 API

很多 API 要求传入 `Stream`，但我们的数据可能已经是一个数组、`Memory<byte>`，或者分成好几段的 `ReadOnlySequence<byte>`。如果先把它们拼成新数组再放入 `MemoryStream`，就多做了一次复制。

`WritableMemoryStream` 把现有可写内存包装成流。示例先把字节 `hello` 放到数组中，再向流开头写入 `H`：打印原数组时会看到 `Hello`。这是同一块内存；流不会为新数据自动扩容。调用方要确保底层缓冲在流使用期间仍然有效，不要提前把池化数组归还。

`ReadOnlyMemoryStream` 同样包装现有内存，但是只提供读取。它禁止通过这个流修改内容，并不妨碍持有原数组的其他代码修改数组。

`ReadOnlySequenceStream` 包装可能由多段内存组成的 `ReadOnlySequence<byte>`。示例把“前半”和“后半”各放一段，再用 `StreamReader` 连续读取，输出“前半后半”。使用流接口的代码无需知道底层有两段，也不需要调用方先合并这两段。读取和 UTF-8 解码本身仍然需要做。

## GroupJoin 返回的是一组匹配，而不是逐对输出

两个客户对应两笔订单，两笔订单都属于小林：

```text
小林 → 书、笔
小夏 → 没有订单
```

示例的 `customers.GroupJoin(purchases, c => c.Id, p => p.CustomerId)` 不提供结果选择器。在当前 .NET 11 SDK 中，返回的是 `IGrouping<Customer, Purchase>`：`group.Key` 是对应的客户，枚举 `group` 得到他的订单。没有匹配时仍有该客户，组内元素为零。它与 `Join` 返回每对匹配记录的语义不同。

[.NET 11 发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#tuple-returning-join-and-groupjoin-overloads)把这一类新重载概括为 tuple-returning；本机 GA SDK 中这个 `GroupJoin` 重载实际返回 `IGrouping`。查看 API 签名比照搬这段文字更可靠。`Queryable` 的执行与 SQL 翻译还要看具体数据库提供程序。

## BFloat16 和随机浮点

`Random.Shared.NextBinaryFloat<float>()` 和 `NextBinaryFloat<Half>()` 是泛型浮点随机数入口；每次输出会不同。`Random` 是普通伪随机数生成器，不适合生成密码、密钥或安全令牌。需要安全随机数时可以使用 `RandomNumberGenerator`。

`BFloat16` 是 16 位二进制浮点格式。示例将 1.5 转成 BFloat16，再用 `BitConverter.BFloat16ToUInt16Bits` 读取位模式，最后按位恢复。十六进制 `0x3FC0` 是数值的编码，不是十进制 1.5 的文本；按位还原只是保存同一个 BFloat16 值，并不会恢复转换前已经丢失的精度。

## JSON union：两个对象分支怎样选

假设一个 `PetChoice` 既可能是狗，也可能是猫：两者序列化后都以 `{` 开头。只看 JSON token 是“对象”，无法区分它们。

示例为 union 指定 `JsonUnionTypeStructuralClassifier`：

```text
{"Name":"Misty","Lives":9}     → 有 Lives，选择猫
{"Name":"Rex","Breed":"Collie"} → 有 Breed，选择狗
{"Name":"未区分"}                → 两者都可能，抛 JsonException
```

分类器根据根级属性名称和必需属性筛选类型，不会根据属性的值或嵌套对象的内容推理。没有候选、或者候选不唯一时必须处理异常；不能把它当作业务字段的完整验证。有关自定义分类和限制，参见 [System.Text.Json 的 union 说明](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)。

`closed` 类型层次走的是另一条路径：示例在 `DeliveryEvent` 上启用 `InferClosedTypePolymorphism`，序列化时自动发现派生类型，输出 `$type`；反序列化时根据该标记还原成 `Delivered`。类型名或标记会成为交换数据的一部分，需要兼容性时应明确管理协议。显式注册派生类型时，注册列表会取代推断列表；源生成模式也要有对应元数据。

示例还展示 `Utf8JsonWriter.Reset` 的新重载。第一份 JSON 写完并 `Flush` 之后，清空内存流，再带新的 `JsonWriterOptions` 重置 writer，就可以写第二份文档。重置 writer 不会替调用方清空目标流；如果流的位置和长度需要重用，也得由调用方管理。

## Zstandard、tar 和 ZIP

### Zstandard 压缩流

示例创建 `ZstandardStream` 把文本压进内存流，关闭压缩流使结尾写完，再把内存流的位置移回开头，创建解压流读取。`leaveOpen: true` 让底层内存流在压缩器释放后仍可继续使用。

`ZstandardStream` 不支持 Browser/WASI；示例遇到 `PlatformNotSupportedException` 会说明并跳过。本机 Windows x64 已验证，其余平台需要对应运行环境。示例演示普通压缩/解压，没有训练字典：字典训练返回原始字节，仍需用 `ZstandardDictionary.Create` 创建字典才能使用。[正式版压缩 API](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#zstandard-compression)

### 空 GZip 也是一个有效压缩流

代码创建 `GZipStream` 后不写数据，直接释放。关闭后底层流仍有格式头尾，程序会打印字节数。需要等压缩流关闭或完成处理之后再观察，否则格式的尾部可能尚未写入。具体字节长度不是应用应依赖的固定值。

### 指定 tar 格式

示例创建只属于本次运行的临时目录和 `note.txt`，调用 `TarFile.CreateFromDirectory(..., format: TarEntryFormat.Gnu)`，然后通过 `TarReader` 查看条目与格式。`includeBaseDirectory: false` 让归档从目录内容开始，而不是再包一层源目录名。支持哪些文件名、元数据和目标工具，应结合 GNU、Pax、Ustar、V7 格式选择；GNU sparse 1.0 的读取支持不等于这里创建了 sparse 文件。

### 带密码的 ZIP 与 CRC

示例用 `ZipEncryptionMethod.Aes256` 建立条目，再用相同密码打开。演示密码写在代码中只是为了方便观察，实际应用应从安全来源获取，不应将真实密码写入源码。旧式 `ZipCrypto` 仅适合兼容遗留工具，安全性较弱；目标解压工具也需要支持所选 ZIP 加密方式。[ZIP 加密说明](https://learn.microsoft.com/en-us/dotnet/standard/io/zip-tar-best-practices#encryption-considerations-net-11)

CRC 示例另建一个未压缩条目，复制它的字节到内存，故意改坏内容，再把条目读到结尾。运行时发现内容与保存的 CRC 不符，抛出 `InvalidDataException`。读到一半就停止不代表校验已经完成。CRC 主要用于发现意外损坏，不能当作防恶意篡改的认证手段。

## AES Key Wrap 与 X25519：两种不同的事

### AES Key Wrap 是用一个已有密钥保护另一个密钥

示例随机生成一个 32 字节的 AES 密钥加密密钥，再随机生成一个 24 字节的数据密钥。`EncryptKeyWrap` 用前者包装后者，`DecryptKeyWrap` 还原，最后用 `FixedTimeEquals` 比较：

```text
密钥加密密钥 + 数据密钥 → 包装后的数据密钥
密钥加密密钥 + 包装后的数据密钥 → 原来的数据密钥
```

无填充的 AES Key Wrap 适用于长度至少 16 字节且为 8 字节倍数的输入；示例另用 19 字节数据展示带填充的 `EncryptKeyWrapPadded`。示例用随机密钥并在使用后清除数组，未展示密钥的持久化和权限管理。参考 [AES Key Wrap](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#aes-key-wrap)。

### X25519 是双方各自算出同一个共享秘密

Alice 和 Bob 各自生成自己的私钥与公钥。示例只把 Bob 的公钥导入 Alice 一方，并把 Alice 的公钥导入 Bob 一方；双方分别用自己的私钥和对方公钥计算共享秘密。打印的 `True` 表明本次运行得到的字节相同，不会把秘密本身输出到控制台。

程序先看 `X25519DiffieHellman.IsSupported`；当前系统或密码提供程序不支持时会跳过。协商出的原始共享秘密不是可以直接拿来当协议密钥的完整方案：实际协议还需要认证对方公钥，并通过合适的 KDF 派生会话密钥。示例不建立连接、不发送密钥。[官方 X25519 说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#x25519-diffie-hellman-key-exchange)

## 匿名管道：RandomAccess 也能用非可定位的句柄

`SafeFileHandle.CreateAnonymousPipe` 建立一对连接的读端与写端。示例为读端开启异步方式，打印两个句柄的 `Type`，然后从写端写入 `pipe`，通过 `RandomAccess.ReadAsync` 从读端读出来。读取可能返回比请求更少的字节，因此例子会按已读数量继续，而不是假设一次读取一定得到全部四字节。

管道不是文件，不能把它当磁盘文件那样跳到任意位置。对于不可定位句柄，`RandomAccess` 的 `fileOffset` 被忽略，但仍需给非负值；示例传 0。完成时两个安全句柄都会释放，读取设置了短超时以免演示中意外阻塞。[RandomAccess 说明](https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess?view=net-11.0)
