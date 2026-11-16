# .NET 11 发布内容与学习索引

.NET 11 的变化覆盖运行时、基础库、编译器和多个应用框架。这份索引用来查找发布内容和对应示例。每一行都注明本项目的覆盖情况；运行方式见项目 [README](../README.md)。版本信息以 [.NET 11 官方概览](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)为准。

表中的 **示例**表示已有可运行代码；**原理示例**用于理解行为，验证内部优化还需要反汇编或诊断工具。**资料**表示暂时只有阅读入口；**平台**表示需要特定系统、设备或服务；**预览**表示能力仍处于预览或实验阶段。

## C# 15

语言默认版本随 `net11.0` 选择。完整规则和示例参见 [C# 15 新功能](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)，本项目代码在 [LanguageDemos.cs](../LanguageDemos.cs)，讲解在 [csharp15.md](csharp15.md)。

| 知识点 | 作用和本项目状态 |
| --- | --- |
| 集合表达式 `with(...)` | 给构造方法传容量或比较器；**示例**展示 `List` 容量与大小写不敏感去重。 |
| `union` 联合类型 | 一个值可以是列出的几种类型；**示例**比较商品存在和不存在，配合 `switch` 处理。 |
| `closed` 类型层次 | 把直接派生类型限制在声明程序集；**示例**展示支付状态与穷尽匹配。非 `closed` 子类仍可能被继续派生。 |
| 扩展索引器 | 在 `extension` 块中给接收者提供索引器；**示例**为 `IEnumerable<int>` 提供索引访问，底层 `ElementAt` 可能遍历。 |
| 带标签 `break` 和 `continue` | 从嵌套循环明确指定跳转目标；**示例**分别演示退出外层循环和开始外层下一轮。 |
| 较旧运行时的非虚静态接口成员 | 接口可以提供普通静态辅助方法；**示例**为温度换算。静态抽象、虚成员仍有自己的运行时要求。 |
| 内存安全规则 | 调整指针、`unsafe` 调用方责任和 `safe` 声明；**预览／资料**：需要 `LangVersion=preview`，更新规则还需 `updated-memory-safety-rules` 特性开关，不在默认项目中悄悄开启。 |

## 运行时、JIT、GC 与 SIMD

[RuntimeDemos.cs](../RuntimeDemos.cs) 运行 `dotnet run -- runtime`。这些示例主要帮助理解运行过程，不把控制台输出当成性能证明；查看优化需要 Release、预热并结合反汇编或性能工具。详见 [runtime.md](runtime.md) 和 [官方运行时说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime)。

| 知识点 | 作用和本项目状态 |
| --- | --- |
| Runtime Async | **示例**覆盖同步完成、未完成任务、继续执行、异常和 `AsyncLocal`。启用方式为项目编译开关；旧程序集不会因升级而自动重新编译。 |
| 运行时异步诊断、NativeAOT、ReadyToRun | 调试器和 profiler 可观察调用链，AOT/R2R 支持新的异步代码生成；**资料**，需调试器或发布构建，不以普通输出推断结果。 |
| 分层编译、Task/ValueTask 工厂与 continuation | 热点编译、缓存和较少状态保存改善开销；**资料**，是否节省分配取决于路径和配置。 |
| 空 span、索引和 checked 范围分析 | **原理示例**包含 `IsEmpty`、数组索引、范围已知的 `checked` 转换；需看 JIT 输出确认检查是否消除。 |
| 去虚拟化、内联、常量折叠、分支简化 | **原理示例**含接口调用和连续值判断；不能用相同结果证明 JIT 已发生优化。 |
| SIMD 固定宽度与尾部 | **示例**展示 `Vector128<int>.Count`、130 项相加的标量收尾和重叠向量收尾；掩码收尾依赖平台，重叠收尾依赖别名和重复计算条件。 |
| 向量几何序列、Zip/Unzip、Concat、Reverse | **示例**展示 `CreateGeometricSequence`、`Zip`、`Unzip`、`ConcatLowerLower` 和 `Reverse`；其他重载见 [向量 API](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#cross-lane-vector-operations)。 |
| 硬件加速和指令集 | **原理示例**打印 `Vector128.IsHardwareAccelerated`；AVX、SVE、FP16 等**平台**能力必须检查支持，不可假设每台机器相同。 |
| 分代 GC、引用与写屏障 | **原理示例**展示对象引用、代数和回收计数；这些数字不能直接证明某个写屏障或句柄优化已触发。 |
| dependent handle 老化、Arm64 GC 地址排序 | **资料／平台**：解释见 [GC 与写屏障文章](../../../blogs/2026/performance-improvements-in-net-11.md#gc)。 |
| 最低硬件要求与 R2R 目标 | **平台／资料**：x86/x64 基线升级到 x86-64-v2，Windows Arm64 增加 LSE 基线；R2R 目标和可运行基线是两个不同概念，见官方运行时说明。 |
| 文件锁和可靠性修复 | **平台／资料**：iOS/tvOS 的 advisory 文件锁默认设置变化；Linux cgroup、AssemblyLoadContext 和 Windows 多 CPU 组等修复见运行时说明。 |

## 基础库

代码在 [LibraryDemos.cs](../LibraryDemos.cs)，运行 `dotnet run -- libraries`。下表逐项标明已覆盖与仍需补充的内容。[官方库更新](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) 包含完整 API 和限制。

| 知识点 | 本项目状态 |
| --- | --- |
| Unicode `Rune` 字符串操作 | **示例**：emoji 在 UTF-16 中占两个 `char`，`Rune` 查找与替换。 |
| UTF-8/UTF-16 校验与错误位置 | **示例**：找到无效 UTF-8 的起始位置，检查 UTF-16 中没有配对的字符部分；见[字符与 UTF-16 的讲解](libraries.md)。 |
| 正则 `RegexOptions.AnyNewLine` | **示例**：同一输入含 CRLF、NEL 和 Unicode 行分隔符。 |
| `StringBuilder.MoveChunks` | **示例**：转移内容后原 builder 清空。 |
| Base64 API | **示例**：编码为字符串并还原；字符/字节重载和状态管理见官方库说明。 |
| 空白字符搜索 | **示例**：Unicode 空白的索引；协议允许的空白可能更窄。 |
| `FullJoin`、元组 Join、GroupJoin | **示例**：左右未匹配的完整连接和无结果选择器的 `Join`；`GroupJoin` 示例在 [ExtraLibraryDemos.cs](../ExtraLibraryDemos.cs)，`Queryable` 的翻译取决于提供程序。 |
| BitArray 的 span 构造及计数 | **示例**：从字节构造并计算置位数。 |
| `EqualityComparer<T>.Create` | **示例**：按姓名选择键并忽略大小写。 |
| `BigInteger` UTF-8 解析 | **示例**直接读 UTF-8；内部原生宽度块、模幂和乘法讲解见 [数值章节](../../../blogs/2026/performance-improvements-in-net-11.md#numerics)。 |
| `decimal`、二进制浮点和大整数转换的舍入变化 | **示例**比较 `double` 转 `decimal` 与 `decimal` 字面量；**兼容性**：不同版本的具体末位可能不同，应以实际目标框架验证。 |
| IEEE 754 `Decimal32`/`64`/`128`、`Complex<T>`、BFloat16 | **示例**展示三种十进制浮点类型和 `Complex<float>`；BFloat16 位表示往返也有**示例**。这些十进制浮点类型与传统 `decimal` 的精度、范围和特殊值语义不同。 |
| `TryParsePartial`、浮点十六进制格式 | **示例**：部分解析逗号分隔输入；浮点十六进制往返按位比较。 |
| 泛型随机数 | **示例**：`NextInteger<int>(0, 10)`；`NextBinaryFloat<T>` 见补充基础库示例；普通随机数不应用作密钥生成。 |
| JSON 属性命名和忽略 | **示例**：类型和成员规则的优先级、空属性忽略。 |
| JSON Lines | **示例**：逐项写独立 JSON 值并以 LF 分行；不是 JSON 数组。 |
| C# union JSON、结构分类器、F# discriminated union | **示例**：将字符串或整数 union 序列化并反序列化；对象形状分类器与源生成均有**示例**；F# discriminated union 为**资料**。形状相同的分支需要分类策略；F# union 的反射实现不支持源生成、裁剪或 NativeAOT。 |
| JSON 源生成、多态、开放泛型转换器、只读集合、writer 复用 | **示例**：对象联合类型源生成、closed 层次推断、writer Reset；开放泛型转换器和只读集合为**资料**。 |
| 四种内存流 | **示例**：`StringStream`；`ReadOnlyMemoryStream`、`WritableMemoryStream`、`ReadOnlySequenceStream` 见补充基础库**示例**，注意缓冲生命周期和可写范围。 |
| Deflate/ZLib/GZip 无流编码器与 Reset | **示例**：ZLib 压缩、解压、检查 `OperationStatus` 并重置。输出空间不足或输入未用尽要继续处理。 |
| ZIP 条目访问模式与方法 | **示例**：内存中写入并读取 ZIP 条目及压缩方法。 |
| ZIP CRC32、密码、空 GZip/Deflate 行为 | **示例**：密码 ZIP 往返、损坏数据的 CRC 检查、空 GZip；空 Deflate 为**资料**。 |
| Zstandard、tar 格式及 GNU sparse | **示例**：Zstandard 压缩往返、GNU tar；字典训练及 GNU sparse 为**资料**。 |
| 请求体压缩 | **示例**：`GZipCompressedContent` 本地压缩后解压，不发送网络请求。 |
| HTTP/2 初始并发限制 | **示例**：配置 `InitialHttp2MaxConcurrentStreams`；服务端 SETTINGS 到达后的行为需结合协议配置观察。 |
| DNS 记录、连接回收、QUIC 优先级 | **示例／环境**：可选 MX 记录查询与回环 HTTP 指标；连接回收回调为**实验性**，QUIC 优先级为**资料／平台**。 |
| TLS caller-driven session、混合后量子加密 | **预览／平台**：`TlsBufferSession`、`TlsSocketSession`、部分 ML-KEM/HPKE API 为实验能力，算法和系统支持条件需查看官方文档。 |
| X25519、AES Key Wrap | **示例／平台**：本地生成密钥并演示交换和封装；按操作系统或密码提供程序检测支持。 |
| 管道与 RandomAccess | **示例／平台**：匿名管道的 SafeFileHandle 和异步读取，见[补充基础库说明](extra-libraries.md)。 |
| 进程启动与输出 | **示例**：`Process.RunAndCaptureTextAsync` 执行本机 `dotnet --version`，展示 stdout、stderr 与退出码。 |
| 进程 handle、信号、挂起、继承 | **示例／平台**：进程查找、逐行读取和退出状态；信号、挂起与继承仍为**资料**。 |
| 异步 DataAnnotations、Options 验证 | **示例**：异步属性验证和消息格式化；`IAsyncValidatableObject`、`IAsyncStartupValidator` 为**资料**。 |
| HTTP 可观察指标、Activity 规则、MemoryCache 指标 | **示例**：回环 HTTP 指标、进程状态，以及 Web 项目的 MemoryCache 命中与未命中；Activity 规则为**资料**。 |
| 增量 CBOR | **示例**：[独立项目](../cbor/Program.cs) 分块读取并使用 `NeedsMoreData`、`SlideData`；需要 HTTPS NuGet 源。 |

## SDK 与部署

SDK 变化主要体现在命令、构建和工具行为，详情见 [SDK 和工具更新](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)。以下是阅读清单，不代表本项目已经演示每一项。

| 知识点 | 说明 |
| --- | --- |
| SDK 体积 | Linux/macOS 安装包通过重复程序集去重缩小；Windows 不属于同一实现。**平台／资料**。 |
| 原生 AOT CLI、MSBuild server | 原生 CLI 和 MSBuild server 默认启用；启动、外部工具与完整命令行为以 SDK 说明为准。**资料**。 |
| 文件式应用 | **示例**：[main.cs](../file-apps/main.cs) 用 `#:include` 引入辅助文件；引用程序集、NativeAOT 输出复用和 `dotnet format` 支持为**资料**。 |
| `dotnet run -e` | **示例**：README 的文件式应用命令向程序传环境变量。 |
| 解决方案筛选器 | **示例**：`sdk-examples/cli-demo.ps1` 在临时目录创建和编辑 `.slnf`。 |
| `dotnet watch` | 应用崩溃恢复、Aspire、移动端设备选择等。**平台／资料**。 |
| 分析器 | CA1873 消息与误报改进；可结合代码诊断使用。**资料**。 |
| 容器发布 | 多架构 Podman、平台本地容器运行时、可复现摘要与去除重复上传。**平台／资料**。 |
| `dotnet test` 和模板 | 测试平台、设备、超时及输出选项有变化；本项目按需求不创建测试项目。**资料**。 |
| 文件式项目和属性展开 | **示例**：[SDK 脚本](../sdk-examples/cli-demo.ps1) 演示项目引用、DLL 引用、引号属性及启动配置属性展开。 |
| 构建和包源 | MSBuild 通配符、文件指令、工具并发操作及 NuGet 最小发布时长等；以官方 SDK 更新为准。**资料**。 |

## Web、数据库与桌面

这些功能通常需要独立应用、浏览器、数据库或桌面环境。Web、EF Core 和 Windows Forms 已有单独项目；其余主题可以从官方说明继续阅读：

| 领域 | 代表性变化与入口 |
| --- | --- |
| ASP.NET Core / Blazor | **示例**：独立 web 项目展示 Minimal API 异步验证、union、过滤器和短路端点；Blazor 等其余内容为**资料**：Blazor `DisplayName`、相对导航、TempData、虚拟化和网关等；还要查看服务器、API、认证与性能部分。[ASP.NET Core 更新](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11) 页面本身仍写有预览期说明，采用具体功能前应核对所用 SDK。 |
| EF Core 11 | **示例／依赖**：[SQLite 内存项目](../ef-sqlite/Program.cs) 演示 TPT + JSON 复杂类型、MaxBy 和 FullJoin SQL；[说明](ef.md)列出首次包恢复条件。[EF Core 更新](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew) 当前页面仍写“开发中”，与 .NET 核心 GA 页面标注不同。 |
| Windows Forms | **示例／Windows**：[独立项目](../winforms/Program.cs) 演示新视觉样式、开关和批量更新，见[说明](winforms.md)。[WinForms 更新](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net110) 当前标注 RC 1。 |
| WPF | **Windows／资料**：混合模式程序集、DWM 和弱事件表修复。[WPF 更新](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net110) 当前标注 RC 1。 |
| MAUI | **平台／资料**：CoreCLR、移动设备编译和平台限制随目标及 SDK 配置变化。[MAUI 更新](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-11)。 |
| Extensions | **资料**：配置、验证、诊断等扩展包按各自版本发布，查阅 [dotnet/extensions 发布记录](https://github.com/dotnet/extensions/releases)。 |

本表按官方概览归类，细分页面会继续更新。实际升级时，还应查看 [.NET 11 破坏性变更](https://learn.microsoft.com/en-us/dotnet/core/compatibility/11.0) 和目标平台、数据库提供程序的版本说明。
