# Runtime Async、JIT、SIMD 和 GC：从示例看运行时做了什么

示例入口是 `RuntimeDemos.RunAsync()`，依次运行异步、JIT、SIMD 和 GC 示例。控制台显示的是程序结果和运行时状态；这些输出本身不能证明 JIT 消除了哪条指令，也不能作为性能测试结果。

## Runtime Async：等待时把什么留下，完成后又怎么继续

运行示例时，先看 `CalculateTotalAsync → AddDeliveryFeeAsync → LoadPriceAsync`：价格 120 加运费 10，再减优惠 20，结果是 110。

`cacheHit` 为 `true` 时，`LoadPriceAsync` 立即返回 120。调用方拿到 `Task<int>` 时操作已经完成，`await` 可以直接取得结果。

`cacheHit` 为 `false` 时，价格来自一个尚未完成的 `TaskCompletionSource<int>`。调用返回后，示例先检查任务是否完成，再调用 `SetResult(120)`。此时异步方法要先让出线程，运行时需要保留恢复位置和后续计算所需的信息；任务完成后才继续算运费和优惠。`TaskCompletionSource` 在这里替代真实网络请求，保证演示可离线、可重复。

传统 C# 编译器会把异步方法变为状态机，通过 `MoveNext` 推进执行，并将方法的结果或异常交给返回的任务。Runtime Async 将挂起和恢复的更多安排交给运行时与 JIT。编译器生成能够表达异步方法和等待点的 IL；JIT 在可识别的直接调用加等待模式下，可以采用内部异步调用方式。同步完成时，上层有机会直接拿到值，而不必为每一层创建中间任务；真正挂起时，仍要保存继续执行所需的状态。

这里的“有机会”很重要：如果业务把中间任务存入集合、交给其他代码、检查状态或调用 `WhenAll`，它是调用方需要观察的对象，不能假定会消失。I/O 本身也可能需要自己的任务和缓冲区。不要凭源码有几个 `await` 就判断实际分配了几个对象。

示例最后将 `FormatException` 注入尚未完成的操作。外层依然通过 `await` 收到相同异常。优化后的运行时可越过没有处理异常的中间恢复记录，但用户编写的 `catch`、`finally` 仍须按语义执行。同步完成时的异常传播和挂起后的恢复路径也不完全相同。

`AsyncLocal` 的输出演示另一个容易混淆的点：`ConfigureAwait(false)` 影响恢复时是否回到捕获的同步上下文或调度器，并不自动禁止 `ExecutionContext` 中的 `AsyncLocal` 流动。无同步上下文的控制台程序中，不能通过这个例子判断是否发生了线程切换。

### 比较两种编译方式

.NET 11 的应用代码需要在项目编译时启用 Runtime Async；框架库使用了它，不代表原来编译好的应用程序集会自动改变。本项目默认启用，它在项目文件中对应：

```xml
<Features>$(Features);runtime-async=on</Features>
```

在项目目录下运行下面的命令，对照两种编译方式：

```powershell
# 默认启用 Runtime Async
dotnet build -c Release -t:Rebuild
dotnet run -c Release --no-build -- runtime

# 不启用 Runtime Async，重新编译整个项目
dotnet build -c Release -t:Rebuild -p:EnableRuntimeAsyncDemo=false
dotnet run -c Release --no-build -- runtime

# 恢复本项目的默认编译方式
dotnet build -c Release -t:Rebuild
```

切换属性后要使用 `-t:Rebuild`。实测普通增量构建可能沿用上一次的编译结果，反射观察也就不会随设置改变。

示例用反射打印 `CalculateTotalAsync` 是否带 `AsyncStateMachineAttribute`：对于该示例方法，这能帮助观察编译方式；不同代码形态可能走传统回退，不能仅凭一个属性判断整个应用是否启用。`Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 是 Runtime Async 支持的方法返回类型；`async void`、异步迭代器和自定义 task-like builder 不应直接套用这一机制。

对照实验使用同一个 SDK、同一个运行时和相同输入。分别测立即完成、确实挂起、异常三条路径；将计时和分配交给 BenchmarkDotNet 等工具，不用控制台输出的耗时推断吞吐量。

参考：[.NET 11 Runtime Async](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime#runtime-async)、[运行时规范草案](https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md)、[代码生成说明](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/runtime-async-codegen.md)。

## JIT：先知道哪些事情已经成立

`StartsWith` 先判断 `span.IsEmpty`，只有非空才读取第一个元素。非空意味着长度至少为 1，所以进入右侧时访问 `[0]` 必然安全。JIT 可以利用这条信息，不再生成重复的下标检查。空输入仍然返回 `false`；安全语义没有改变。

`ToByte` 先限制输入不大于 80。`uint` 本身不可能为负，而 `byte` 能表示 0 到 255，因此 `checked((byte)value)` 在该分支中不会溢出。JIT 可能删掉不必要的溢出检查。`IsSmall` 的连续值判断也可能合并成范围比较。

接口调用示例展示正常的面向接口代码。JIT 如果能够证明调用对象的具体类型，就能把接口调用改成直接调用，并有机会进一步内联；不能证明时仍要保留一般调用。只看到结果是 100 并不能证明去虚拟化发生了。

### 查看生成的代码

用 Release 构建，在项目目录下打开 PowerShell，观察指定方法的反汇编：

```powershell
$env:DOTNET_TieredCompilation = '0'
$env:DOTNET_JitDisasm = 'RuntimeDemos:StartsWith'
dotnet run -c Release -- runtime
Remove-Item Env:DOTNET_JitDisasm
Remove-Item Env:DOTNET_TieredCompilation
```

如果主程序使用了不同的章节参数，请以项目 README 为准。关闭分层编译只是让这次查看更容易聚焦优化后的代码，不能拿这个配置直接代表应用默认性能。实际输出随架构、构建方式和运行时版本而变化。可比较守卫和访问相关的机器指令，也可以在不同版本分别查看，不能保证一定出现特定指令或 `CORINFO_HELP_RNGCHKFAIL` 名称。

[官方 JIT 改进说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime#jit-improvements)

## SIMD：130 个 int 为什么最后还剩两个

`Vector128<int>` 的 128 是**位数**；一个 `int` 是 32 位，因此每次处理 4 个整数，而不是 128 个整数。两个长度为 130 的数组相加时，示例的完整向量循环处理下标 `0..127`，再用普通循环处理下标 `128`、`129`。目标数组单独分配，避免与输入发生重叠。

.NET 11 新增了向量元素组合 API。示例用 `CreateGeometricSequence(1, 2)` 得到 `[1, 2, 4, 8]`，再把 `[1, 2, 3, 4]` 与 `[10, 20, 30, 40]` 交错，得到 `[1, 10, 2, 20]` 和 `[3, 30, 4, 40]`。这些 API 用来表达数据排列，不会自动替数组相加循环处理尾部。

示例接着用 `Unzip` 把两份交错结果还原成原来的两个向量。`Reverse` 把 `[1, 2, 3, 4]` 反转为 `[4, 3, 2, 1]`；`ConcatLowerLower` 把两个向量各自的前半段拼在一起，得到 `[1, 2, 10, 20]`。这些操作处理的是向量内已有元素的位置，不是对整段任意长度数组自动执行循环。

尾部除了逐项处理，还可以重算末尾一整个向量。示例的 `AddWithOverlappingTail` 对同一份输入运行第二次：

```text
完整向量最后一轮：下标 [124, 125, 126, 127]
重叠收尾一轮：    下标 [126, 127, 128, 129]
                            ↑ 重复 ↑    ↑ 原本剩下 ↑
```

源数组不会被修改，结果写进新数组。126、127 虽然再次计算，第二次写入的仍是相同结果，因此与标量收尾的完整数组一致。程序会打印这个比较结果；这是演示正确性，不能据此断言重叠收尾更快。

这一做法依赖运算和数据不受重复处理影响。假如计算总和，126、127 会被加进总和两次；如果原地执行 `a[i] += b[i]`，这两个位置也会被加两遍。输入和目标发生其他破坏性重叠时，已写出的数据还可能改变后续读取。输入短于一个向量时，`length - width` 会是负数，示例退回逐项处理；没有尾部时也不会重复运行最后一组。

另一种办法是使用硬件支持的掩码加载与存储，只让有效位置访问内存。例如最后一轮可用 `[有效, 有效, 无效, 无效]` 表示只访问下标 128、129。掩码必须控制读写；先完整读取四个元素、再遮住后两项已经越界。

Arm SVE 的谓词可以表达这样的循环，.NET 11 扩充了相关谓词 API 和代码生成。SVE 是 Arm 的可伸缩向量指令集，不是所有 Arm64 CPU 都有；调用特定硬件 API 前要检查对应的 `IsSupported`，并提供不支持时的替代路径。本项目展示的是跨平台 `Vector128<T>` 的两种收尾，不假装普通 `Vector128<T>` 已经自动变成 SVE 掩码循环。是否值得使用掩码，还需要在目标 CPU 和真实数据长度上测量。

参考：[.NET 11 向量 API](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime#simd-lane-construction-and-composition)、[SIMD 指南](https://learn.microsoft.com/en-us/dotnet/standard/simd)。

## 写屏障和 GC：为什么写一个引用还需要记账

示例把新对象放入 `Dictionary`，使它在字典存活期间仍可访问。真正的分代场景是：老年代对象的字段指向年轻代对象。如果只扫描年轻代，GC 可能漏掉这条从老对象过来的引用；如果每次都扫描整个老年代，年轻代回收又会变得很贵。

写屏障是 JIT 在需要的引用写入处生成的辅助操作，用来标记值得检查的堆区域。后续年轻代回收可检查这些区域，找到跨代引用。写屏障不等同于线程内存屏障，也不表示这次赋值立刻触发 GC；具体哪些写入需要屏障，由运行时和 JIT 决定。

`GC.GetGeneration` 和 `GC.CollectionCount` 只是当前进程的观察值。程序启动后发生过哪些分配、什么时候回收、对象是否已经晋升，会影响输出；这个示例既不强制回收，也不宣称从输出看到了卡表或某条写屏障指令。

.NET 11 的相关改进包括把数组存储辅助逻辑展开，使 JIT 能在证明安全时去掉多余的协变检查或写屏障；复制含引用的结构体时分开处理引用和普通数据；为 `ConditionalWeakTable` 使用的 dependent handle 增加年龄管理，使长期存活的关系不必在每次年轻代回收时反复检查。这些内部优化的触发与收益都要结合实际负载分析。

参考：[数组存储](https://github.com/dotnet/runtime/pull/126547)、[结构体复制](https://github.com/dotnet/runtime/pull/128542)、[dependent handle](https://github.com/dotnet/runtime/pull/78746)、[GC 设计说明](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/garbage-collection.md)。
