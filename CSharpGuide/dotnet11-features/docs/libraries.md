# .NET 11 基础库：跟着示例看输入和输出

这篇文章对应 [LibraryDemos.cs](../LibraryDemos.cs)。代码包含文本、集合、数值、JSON、压缩和进程几个例子。先在仓库根目录运行：

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- libraries
```

JSON、压缩数据和 ZIP 都在内存中处理。进程示例会执行本机的 `dotnet --version`，不需要启动网络服务。下面按控制台输出顺序阅读，就能找到每个例子对应的代码。

## 为什么 `"A😀B".Length` 是 4

字符串 `"A😀B"` 看起来是三个字符：字母 A、一个表情、字母 B。但 C# 里：

```csharp
string text = "A😀B";
Console.WriteLine(text.Length); // 4
```

原因是 .NET 的 `string` 使用 UTF-16 存储文本，而 `Length` 数的是其中有多少个 `char`。每个 `char` 存放一个 16 位的数，可以把它看作一格存储空间。A 和 B 各占一格，😀 比较特殊，需要连续两格：

```text
看到的字符：   A       😀       B
char 下标：    0     1    2     3
占用格数：     1       2        1
```

这两格合起来才能表示 😀。UTF-16 把它们分别称为**高代理项**和**低代理项**；这只是两种需要配合使用的 16 位数，不是网络代理，也不是程序中的代理对象。

这个例子中 😀 是 Unicode 字符 U+1F600，在 UTF-16 中写成 `\uD83D\uDE00`：

```csharp
string valid = "\uD83D\uDE00"; // 两半放在一起：😀
string invalid = "\uD800";     // 只有高代理项，缺少后半部分

Console.WriteLine(Utf16.IsValid(valid));   // True
Console.WriteLine(Utf16.IsValid(invalid)); // False
```

第二个字符串里的 `\uD800` 就是**孤立代理项**：它需要另一半配对，但旁边没有合法的另一半。C# 的 `string` 可以装下这样的 `char` 序列，因此字符串能够创建，不代表它一定是格式正确的 UTF-16。`Utf16.IsValid` 用来检查这一点。原示例也检查了这样的单独代理项，输出 `False`。

### Rune 让查找和替换更容易

既然 😀 要占两个 `char`，单独拿一个 `char` 做查找就无法表达整个表情。`Rune` 可以表示一个 Unicode 标量值；这里可以先把它理解为能够装下 **U+1F600 这个完整字符**的值：

```csharp
Rune emoji = new(0x1F600);
Console.WriteLine(text.IndexOf(emoji));                  // 1
Console.WriteLine(text.Replace(emoji, new Rune('*')));  // A*B
```

.NET 11 为 `string` 增加了接受 `Rune` 的查找、替换、拆分和修剪重载。示例中的 `IndexOf` 返回 1，是因为 😀 的第一格 `char` 位于下标 1；返回值仍按 `char` 计算。

`Rune` 也不总等于人眼看到的一个完整字形。例如某些表情由基本表情和肤色修饰符等多个 Unicode 标量组成，屏幕上合在一起显示，内部却可能有多个 `Rune`。如果要按用户看到的字形移动光标或截取文本，还需要考虑文本元素的边界。

### UTF-8 字节也可能不完整

网络和文件里常见的是 UTF-8 字节。先区分两件事：一个数字能否存进 `byte`，以及一串字节能否按 UTF-8 规则组成文本。`byte` 能存放 0 到 255 的整数；UTF-8 则规定了这些字节应当怎样排列。

示例拿到三个字节：

```text
字节下标：  0     1     2
内容：     41    C3    28
```

这三个值都能存进 `byte`：`0x41`、`0xC3`、`0x28` 分别是十进制的 65、195、40。问题出在它们组合成的 UTF-8 文本上：

1. `0x41` 可以单独表示字母 A。
2. `0xC3` 是一个双字节字符的开头，需要紧跟一个符合 UTF-8 规则的后续字节。
3. 后面的 `0x28` 不符合这个要求，所以从 `0xC3` 开始的序列无效。

```csharp
byte[] data = [0x41, 0xC3, 0x28];
int index = Utf8.IndexOfInvalidSubsequence(data); // 1
```

返回值是 1，指向无效序列的**起始字节** `0xC3`，并不是说 `0xC3` 不是合法的 `byte`。

把 `0x28` 换成符合要求的 `0xA9`，就能组成合法文本：

```csharp
byte[] valid = [0x41, 0xC3, 0xA9]; // UTF-8 文本 Aé
int index = Utf8.IndexOfInvalidSubsequence(valid); // -1
```

这里 `0xC3` 和 `0xA9` 合起来表示 `é`。整个输入都是合法 UTF-8，所以返回 `-1`。这个 API 可以在解析前检查字节，并在出错时指出第一个无效序列从哪里开始。

### 正则遇到不同的换行符

并不是所有文本都用 `\n` 换行。示例混用了 Windows 常见的 `\r\n`、NEL（`\u0085`）和 Unicode 行分隔符（`\u2028`）。

正则 `^line\d$` 同时使用 `RegexOptions.Multiline` 和 .NET 11 新增的 `RegexOptions.AnyNewLine`：前者让 `^`、`$` 可以匹配每一行的开头和结尾，后者让正则认识更多换行形式。示例最终找到四行。具体文件格式或网络协议可能只允许其中某些换行符，解析时仍应按协议规定处理。

### 转移 StringBuilder 内容、Base64 和空白搜索

`StringBuilder.MoveChunks` 把一个 builder 保存文本用的内部块转给新的 builder：

```csharp
var source = new StringBuilder("hello ");
source.Append(".NET 11");
StringBuilder destination = StringBuilder.MoveChunks(source);

Console.WriteLine(destination);   // hello .NET 11
Console.WriteLine(source.Length); // 0
```

转移后原 builder 清空，所以它适合不再需要保留原内容的情况；需要留下两份内容时，就不能把这个方法当成复制。

示例还把“你好 .NET 11”编码成 UTF-8 字节，再用 `Base64.EncodeToString` 得到 Base64 文本，最后解码还原。Base64 只是让二进制数据能够用文本形式传输，并不加密内容。

`IndexOfAnyWhiteSpace` 用来找第一个空白字符。示例输入 `"ab　cd"` 中，`b` 后面是全角空格，返回的位置是 2。这个方法按文本空白规则判断；某些协议只允许几种指定空白，不能不加区分地采用同一规则。

## FullJoin：两边没有匹配上的记录也保留

示例有两件商品和两条订单：

```text
商品：1 键盘       2 鼠标
订单：商品编号 2   商品编号 3
```

按商品编号做 `FullJoin`，结果有三行：

```text
键盘   + 没有订单
鼠标   + 商品编号为 2 的订单
无商品 + 商品编号为 3 的订单
```

商品 2 和订单 2 可以配对；商品 1 没有订单，订单 3 找不到商品，它们也各自保留下来。示例两边都是引用类型，因此没有对应记录的一侧是 `null`。如果一个编号在左边有两条、右边有三条，匹配部分会产生六对，不会自动去重。

对比一下普通 `Join`：它只返回两边匹配的记录，本例只有商品 2 和订单 2 那一对。示例使用了不需要单独写结果选择器的重载。

这些 LINQ 查询在枚举时才开始执行，`foreach` 会触发遍历。比如先用 `var query = products.FullJoin(...)` 创建查询，此时只是记下要怎样匹配；开始 `foreach` 时，才逐步读取商品和订单。再枚举一次，通常会重新读取数据并计算结果。

`Queryable.FullJoin` 面向数据库查询：它先把要做的匹配记录成表达式，再由数据库提供程序尝试翻译成 SQL。C# 能编译这个方法，不代表当前数据库提供程序一定能翻译它。[EF Core SQLite 示例](ef.md)会打印实际生成的 SQL。

### BitArray 和按姓名判断相等

`BitArray` 把一组开关状态存成二进制位：每一位可以是 0 或 1，例如表示某个选项关闭还是开启。它可以从 `ReadOnlySpan<byte>` 创建；这里的 span 可以理解为对一段连续字节的访问视图，不需要先为这段数据再复制一个数组。

示例给它两个字节：

```text
00000011  → 两个 1
10000000  → 一个 1
```

因此 `PopCount()` 返回 3，也就是总共有三位为 1。

位的下标从每个字节的低位开始计算。第二个字节 `10000000` 中最左边那个 1，是该字节的第 7 位；前面还有一个字节的 8 位，因此整个 `BitArray` 的第 15 位为 `true`。

另一个例子创建了一个人员集合，人员数据有姓名和年龄，但只希望按姓名去重。示例使用 `p => p.Name` 从人员数据中取出姓名，再指定 `StringComparer.OrdinalIgnoreCase`，让比较时忽略字母大小写。`EqualityComparer<T>.Create` 将这两部分组合成一个比较器，交给 `HashSet` 使用。

于是，集合判断 `("Ada", 20)` 和 `("ADA", 30)` 时，只比较 `Ada` 与 `ADA`，认为两人相同，最后只保留一项。年龄不同不影响判断，因为这份比较器没有使用年龄；换用其他比较规则，集合的去重结果也可能不同。

## 数值：同样写着 1.23，内部保存的值可能不同

```csharp
double binary = 1.23;
decimal converted = (decimal)binary;
decimal direct = 1.23m;
```

`double` 使用二进制浮点，无法精确保存十进制的 1.23；赋值时保存的是一个非常接近它的二进制数。`1.23m` 则直接表示十进制的 1.23。

可以类比十进制里表示三分之一：不管写 `0.3`、`0.33` 还是 `0.333`，只要位数有限，就只能越来越接近 `1/3`。二进制也有类似情况，有些十进制小数需要无限多位才能精确表示。`double` 的位数有限，保存 1.23 时就要选一个附近的可表示值。

平时打印 `double`，输出经常会做适合阅读的格式化，因此看到 `1.23` 不表示内部恰好存了十进制的 1.23。转换为 `decimal` 时，程序处理的仍是已经保存下来的那个近似值。

.NET 11 改进了从 `double` 到 `decimal` 的转换舍入。本机运行示例时，输出是：

```text
double 转 decimal：1.2299999999999999822364316060
decimal 字面量：  1.23
```

前者反映了 `double` 里实际保存的近似值。升级后，如果程序依赖旧版本转换结果的具体末位，应核对[兼容性说明](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11.0/decimal-biginteger-conversions)。金额或其他原本以十进制给出的数据，通常应直接解析成十进制类型，而不是先转为 `double` 再期望找回最初的值。

### 只读数字开头，不必先截子字符串

例如收到 `"123,456"`，当前只需要先读出逗号前的数字：

```csharp
int.TryParsePartial("123,456".AsSpan(), NumberStyles.Integer,
    CultureInfo.InvariantCulture, out int value, out int consumed);
```

调用成功后，`value` 是 123，`consumed` 是 3。调用方可以从下标 3 继续处理逗号和后面的 `456`；它们还没有被这次调用解析或验证。

`NumberStyles.Integer` 指定采用整数的解析规则；`CultureInfo.InvariantCulture` 指定不随当前系统语言变化的格式规则。这里的逗号不属于这次整数解析的内容，所以解析停在逗号前面。如果需要读取下一项，还要由调用方检查并跳过分隔符，再解析剩下的部分。

`BigInteger` 也新增了直接解析 UTF-8 字节的能力。若数据本来就是字节，示例可以直接解析，不必先创建一个字符串。

### 十六进制浮点、十进制浮点和复数

示例把 `Math.PI` 用 `"X"` 格式写成十六进制浮点文本，再用 `NumberStyles.HexFloat` 解析回来。它通过比较 `double` 的原始二进制位，确认两次得到的是同一个值；控制台只显示较短的小数时，有些位上的差异可能看不出来。

本机的示例输出为 `0X1.921FB54442D18P+1`。其中 `0X` 表示后面的数字使用十六进制，`P+1` 表示乘以 2 的 1 次方。这是一种文本表示方式，`Math.PI` 在计算时仍然是 `double`。把文本解析回 `double` 后，再用 `BitConverter.DoubleToInt64Bits` 查看保存的二进制位，结果与原值相同。

`Decimal32`、`Decimal64`、`Decimal128` 是三种不同宽度的 IEEE 754 十进制浮点类型。示例分别读取 `0.1`，并显示 `Decimal64` 的正无穷大。它们与 C# 的 `decimal` 是不同类型，精度、范围和对无穷大、NaN 的支持都需要分别考虑。

宽度中的 32、64、128 表示整个数值使用的位数，不是小数点后能保留的位数。无穷大表示超出有限数值范围的特殊结果；NaN 表示“不是一个数字”，可用于表示某些没有数值结果的运算。示例只展示类型能够表示无穷大，不是在比较三种类型的精度。

`Complex<T>` 表示复数，可以选择分量类型。示例用 `Complex<float>` 相加：`(1 + 2i) + (3 + 4i)` 得到 `(4 + 6i)`。

### 生成随机数和排列向量

`Random.Shared.NextInteger<int>(0, 10)` 生成的整数包含 0，不包含 10，所以范围是 0 到 9；每次运行的值可能不同。

`Vector128<int>` 总宽度是 128 位，一个 `int` 占 32 位，因此装得下四个整数。示例用 `CreateGeometricSequence(1, 2)` 生成 `[1, 2, 4, 8]`，再用 `Zip` 把两组数交错排列。若数组长度不能被向量宽度整除，最后剩下的数据如何处理，可看[运行时文档](runtime.md)中的 130 项例子。

`Zip` 的输入是 `[1, 2, 3, 4]` 和 `[10, 20, 30, 40]`。交错排列后共有八个整数，一个向量放不下，于是返回两个：`[1, 10, 2, 20]` 和 `[3, 30, 4, 40]`。这里的 Zip 是排列向量里的元素；前面的 LINQ `Join` 是按键匹配集合，两者用途不同。

## JSON：数组和逐行数据是两种格式

同样要输出数字 1 和 2，JSON 数组与 JSON Lines 看起来不同：

```text
JSON 数组： [1,2]

JSON Lines：
1
2
```

数组整体是一个 JSON 值；JSON Lines 每行是一个独立的 JSON 值。示例使用 `SerializeAsyncEnumerable` 读取异步序列，设置 `topLevelValues: true` 后输出后者，并在每个值后写入换行符 LF，包括最后一项。消费者可以一行一行处理，不必等待整个数组闭合。写进流也不保证立即发到网络另一端，具体还取决于流和网络缓冲。

### 一个对象为什么有两种命名格式

示例给序列化器设置全局 PascalCase，又给 `LibraryEvent` 类型指定 snake_case，给 `EventName` 成员单独指定 camelCase。最终输出：

```json
{"eventName":"launch","release_version":"11"}
```

`EventName` 有自己的成员规则，所以是 `eventName`；`ReleaseVersion` 没有单独指定，就采用类型上的规则，变成 `release_version`。`Notes` 的值是 `null`，类型上的忽略规则让它不出现在输出中。成员可以覆盖类型规则，`JsonPropertyName` 可以为具体属性指定固定名称。

这几种名称分别指：PascalCase 每个单词开头大写，例如 `ReleaseVersion`；camelCase 的第一个单词小写，例如 `eventName`；snake_case 用下划线分隔单词，例如 `release_version`。规则改变的是 JSON 中的字段名，不会重命名 C# 类中的属性。

代码接着调用 `MakeReadOnly(populateMissingResolver: true)`，固定这份配置并准备默认类型解析器；之后通过 `GetTypeInfo<LibraryEvent>()` 取得该类型的序列化元数据。配置变为只读后，就不能再修改它。

序列化器要知道 `LibraryEvent` 有哪些属性、JSON 字段叫什么，以及遇到 `null` 时怎么处理。这些信息就是这里所说的“类型元数据”。解析器负责提供它们；`GetTypeInfo<LibraryEvent>()` 取回的是描述该类型如何序列化的一份信息，不是 `LibraryEvent` 实例本身。

### 联合类型直接写出当前值

`JsonDemoReading` 可以装一个整数或一个字符串。示例装入字符串“传感器离线”，序列化得到的就是 JSON 字符串，不额外包一个对象，也没有 `$type` 字段。反序列化时，JSON 数字与字符串容易区分；如果两个候选类型都会写成 JSON 对象，就需要其他信息来判断。[对象联合类型示例](advanced.md)演示了按字段区分，[官方文档](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types)列出了完整规则。

## 压缩：输入和输出缓冲区怎么交给编码器

`ZLibEncoder` 可以直接读取字节并写入调用方提供的缓冲区。示例将一段中文转成 UTF-8 字节，准备 256 字节的输出数组，再调用 `Compress`。调用后需要看三项结果：

| 结果 | 含义 |
| --- | --- |
| `bytesConsumed` | 本次读取并处理了多少输入字节 |
| `bytesWritten` | 本次向输出缓冲写入了多少字节 |
| `OperationStatus` | 是否完成，还是需要更多空间或输入 |

示例把 `isFinalBlock` 设为 `true`，告诉编码器这条消息后面没有更多输入。它检查状态为 `Done` 且输入已全部处理，再把输出数组中实际写出的部分交给解码器；解码后也只读取实际产生的字节。

输出数组不一定总够大。实际处理时如果空间用完，应先处理已写出的部分，再准备空间并从尚未处理的输入继续。编码器用完可以释放；如果要处理一条独立的新消息，也可以先调用 `Reset()` 后重用。

可以把它想成从一桶水往一个小杯子里倒：杯子满了，先把杯里的内容交出去，再继续处理桶里剩下的部分。`bytesConsumed` 告诉我们输入处理到哪里，`bytesWritten` 告诉我们输出缓冲里有多少有效数据。若只顾着拿走输出，却又从输入开头继续，之前的数据就会被重复压缩。

### 已有字符串或内存，怎样交给只接受 Stream 的代码

`StringStream` 让调用方按指定编码把字符串作为字节流读取。示例把 `"配置=启用"` 交给 `StringStream`，再用 `StreamReader` 读回来。调用方不必提前创建整份 UTF-8 数组，但流在读取时仍需要编码。

如果原始数据已经是字节内存，另外几种流也能把已有内存交给 `Stream` API；它们的区别见[补充基础库示例](extra-libraries.md)。

### ZIP 内容什么时候检查 CRC

示例先在内存中创建 ZIP，写入 `hello.txt`；关闭写入端后，把流位置移回开头，重新读取内容，并显示条目的 `CompressionMethod`。

.NET 11 读取 ZIP 条目时会验证 CRC32，损坏或截断的数据可能抛出 `InvalidDataException`。CRC 用于发现数据损坏，不是防止恶意篡改的认证手段。[补充示例](extra-libraries.md)演示了修改内存中的 ZIP 内容，再读取到结尾触发校验；密码 ZIP、tar 和 Zstandard 也在那里介绍。

可以把 CRC32 理解为对内容算出的一个短校验值。写入 ZIP 时保存这个值；读取时再对读到的内容计算一次，如果对不上，就知道数据出了问题。因为它不是用秘密密钥生成的，有人故意修改内容时，也可能重新计算 CRC，所以不能用它证明数据没有被恶意修改。

## HTTP：压缩请求体，需要服务端也认识

`GZipCompressedContent` 包装已有的 `HttpContent`。内容被写出时，包装器进行 gzip 压缩，并设置 `Content-Encoding: gzip`。

例如客户端原本要发送一段 JSON，可以先把 JSON 放进 `StringContent`，再用 `GZipCompressedContent` 包住。服务端收到的是压缩后的请求体；请求头里的 `Content-Encoding: gzip` 告诉它，应先按 gzip 解压，再读取里面的 JSON。示例中的原始内容是 `"请求数据"`，便于直接观察解压结果。

示例没有向网络发送请求，而是读取压缩后的字节，再用 `GZipStream` 解压，确认得到原始文本。真实请求的服务端必须支持 gzip 请求体。`SocketsHttpHandler.AutomaticDecompression` 处理的是另一方向：自动解压服务器返回的响应。

示例还设置 `InitialHttp2MaxConcurrentStreams = 20`。建立 HTTP/2 连接后、收到服务器的 SETTINGS 之前，客户端先按这个值限制该连接上的并发流；收到设置后再使用服务器公布的限制。它不是整个应用的请求限流器，设置值至少为 1。真实 DNS 和本地 HTTP 指标可运行[环境示例](environment.md)；其他网络 API 的平台和实验状态见[官方发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#networking-and-transport-security)。

HTTP/2 的一条连接可以同时承载多个请求和响应，每组请求与响应称为一条流。连接刚建立时，客户端还没收到服务器发来的 SETTINGS，因此还不知道对方允许多少条流同时进行；这个初始值就是在等待设置期间使用的上限。应用还可能建立其他连接，所以它不能代替针对整个应用的并发控制。

## 启动进程并收集输出

启动进程后，如果既要读取标准输出、又要读取标准错误，还要等待程序退出，就需要协调两条管道的读取。`Process.RunAndCaptureTextAsync` 把常见操作组合起来：示例直接运行本机的 `dotnet --version`，得到标准输出、标准错误和退出状态，没有经过 shell。

可以把标准输出看作命令正常打印结果的通道，标准错误看作命令报告问题的另一条通道。管道有缓冲容量：如果父进程一直等待子进程退出，却迟迟不读取某个已经写满的管道，子进程可能卡在写入处，双方就都等下去了。这个 API 替调用方处理常见的输出收集过程；示例随后从结果对象读取版本和退出状态。

成功启动进程不意味着命令执行成功，仍要检查退出状态。这个方法会收集完整输出；如果子进程持续输出或结果可能很大，可以考虑逐行或流式读取，并设置取消和超时。[进程示例](environment.md)展示了逐行读取。

其他新增 API 及其平台要求可查阅 [.NET 11 基础库发布说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries)。
