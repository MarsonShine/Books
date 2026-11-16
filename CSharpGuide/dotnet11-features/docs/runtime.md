# Runtime Async、JIT、SIMD 与 GC

在仓库根目录运行下面的命令，程序会依次执行本文介绍的四组示例。对应代码在 [RuntimeDemos.cs](../RuntimeDemos.cs)。

```powershell
dotnet run --project CSharpGuide/dotnet11-features -c Release -- runtime
```

控制台中的值可以帮助我们看清程序行为；想知道 JIT 有没有删除检查或减少分配，还需要看生成的代码或做性能测量。

## Runtime Async：一次计算，两种完成方式

示例用三个异步方法计算价格。`LoadPriceAsync` 取得商品价格，`AddDeliveryFeeAsync` 加上运费，`CalculateTotalAsync` 再减去优惠：

```text
商品价格 120 → 加运费 10 → 减优惠 20 → 最终价格 110
```

价格来自哪里，决定了这条调用链会不会暂停。

### 缓存命中：结果已经有了

当 `cacheHit` 为 `true`，`LoadPriceAsync` 直接返回 120。上层的 `await` 拿到的结果已经完成，于是接着计算运费和优惠。调用返回时，最外层任务已经完成，程序会打印结果 110。

源码中有三个 `async` 方法，并不意味着执行时一定会暂停三次。`await` 先看要等的操作是否已完成；完成了就直接继续。

### 缓存未命中：先返回，稍后继续

当 `cacheHit` 为 `false`，`LoadPriceAsync` 等待一个尚未完成的 `Task<int>`。示例用 `TaskCompletionSource<int>` 控制它何时完成，这样不用真的请求网络，也能稳定地看到等待过程：

`TaskCompletionSource<int>` 可以理解成一个由代码控制完成时机的任务来源：`gate.Task` 是交给 `await` 等待的任务，`gate.SetResult(120)` 则让这个任务以结果 120 完成。在真实程序里，任务可能由文件读取或网络请求创建；这里用它来模拟“价格还没拿到”的状态。

```text
调用 CalculateTotalAsync
  → LoadPriceAsync 开始等待价格
  → 调用方拿到尚未完成的任务，IsCompleted 为 False
  → 调用方执行 gate.SetResult(120)
  → 等待价格的方法继续，依次算出 130 和 110
```

等待期间线程可以去做其他工作。以后继续计算时，程序还得知道从哪一步继续，以及运费是 10 这样的信息。无论采用哪种异步实现，这些需要跨等待保留的数据都不能丢失。

### Runtime Async 改变了哪一步

传统方式下，C# 编译器为异步方法生成状态机。状态机记录执行进度，`MoveNext` 负责推进；方法的结果或异常通过它返回的任务交给调用方。

例如执行到等待价格时，方法后面还要用到运费。编译器生成的代码需要记下“等到价格后继续算运费”，也要保存运费值。价格完成时，运行时再次调用状态机的 `MoveNext`，从保存的位置往下执行。若价格早已准备好，就可以直接继续，不必暂停后再恢复。

Runtime Async 把更多挂起和恢复的安排交给运行时与 JIT。编译器在 IL 中保留异步方法和等待点的信息，JIT 看到“调用一个异步方法后立即等待其结果”时，有机会使用内部异步调用方式。

这里的 IL 是 C# 编译后交给 .NET 的中间代码，JIT 则在运行时把它转换成当前机器执行的指令。过去 JIT 接手时，C# 编译器已把异步方法展开成状态机；现在适用的方法在 IL 中保留了更多关于等待点的信息，JIT 因而有机会重新安排结果传递和恢复代码。

拿缓存命中来说：`AddDeliveryFeeAsync` 只想取得价格，随后立刻加上运费。JIT 如果识别并优化了这次直接调用，就有机会直接把 120 交给它，减少为中间一层创建任务、再从任务取出结果的工作。最外面的调用方仍按 `Task<int>` 的公开签名使用 `CalculateTotalAsync`。

缓存未命中时则不同：120 还没算出来，运行时仍须保存恢复位置、运费等信息。Runtime Async 能改变这些步骤的组织方式，不会让一次真正的等待凭空消失。

如果调用方把一个任务存起来、传给别的代码，或者用它检查状态，那个任务对象就是程序要使用的结果，不能假定它会被省掉。I/O 自身也可能需要任务和缓冲区。仅凭源码中 `await` 的数量，无法算出实际分配了多少对象。

### 异常和上下文

示例还创建了一个未完成的价格任务，然后用 `SetException` 让它以 `FormatException` 结束。异常会沿调用链传播，最终由调用方的 `await` 抛出，并被示例中的 `catch` 捕获。

传统状态机可能在每一层把异常记入本层任务，再由上一层 `await` 取出并重新抛出。Runtime Async 在符合条件的调用链中，可以越过没有处理异常的中间恢复记录，减少这些中转。代码里自己写的 `catch` 和 `finally` 仍然要照常执行。异常发生在暂停之前，还是在暂停后恢复时发生，内部传播路径也会不同。

示例还在调用前设置了 `AsyncLocal` 的 `RequestId`。即使 `LoadPriceAsync` 使用 `ConfigureAwait(false)`，恢复后仍能读到它。这是因为 `ConfigureAwait(false)` 控制的是是否返回捕获的同步上下文或调度器，它不会自动阻止 `ExecutionContext` 中的 `AsyncLocal` 流动。控制台程序一般没有同步上下文，示例的输出也不能用来判断是否切换了线程。

可以把 `RequestId` 想成给这次操作贴上的编号：代码不需要在每个方法参数中都传入编号，异步流程继续时也能读取它。`ExecutionContext` 负责让这类上下文信息沿异步执行流程流动；同步上下文则涉及继续执行的代码应被安排到哪里，比如 UI 程序可能需要回到界面线程。两者解决的问题不同。

### 在本机比较两种编译方式

项目默认开启 Runtime Async，项目文件中对应的设置是：

```xml
<Features>$(Features);runtime-async=on</Features>
```

在 `CSharpGuide/dotnet11-features` 目录下，先运行默认版本，再关闭开关重新构建：

```powershell
# 默认启用 Runtime Async
dotnet build -c Release -t:Rebuild
dotnet run -c Release --no-build -- runtime

# 使用传统编译方式
dotnet build -c Release -t:Rebuild -p:EnableRuntimeAsyncDemo=false
dotnet run -c Release --no-build -- runtime

# 恢复默认设置
dotnet build -c Release -t:Rebuild
```

程序会用反射检查 `CalculateTotalAsync` 是否带有 `AsyncStateMachineAttribute`。对这个示例方法，默认编译结果通常显示 `False`，关闭开关后显示 `True`。切换时使用 `Rebuild`：普通增量构建可能继续使用上一次的产物。不同方法可能因为代码形态而回退到传统方式，因此一个方法的属性不能代表整个应用。

这个编译开关针对自己的应用代码；框架库采用哪种方式，取决于框架自身怎样编译。Runtime Async 支持返回 `Task`、`Task<T>`、`ValueTask` 和 `ValueTask<T>` 的适用方法。`async void`、异步迭代器和自定义 task-like builder 不应直接套用上面的实现过程。

要比较性能，应使用相同 SDK、运行时和输入，分别测量同步完成、真正挂起和异常三条路径。可以用 BenchmarkDotNet 记录耗时和分配量，控制台输出本身不是基准测试。

参考：[.NET 11 Runtime Async](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime#runtime-async)、[运行时规范草案](https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md)、[代码生成说明](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/runtime-async-codegen.md)。

## JIT：前面的判断可以帮后面省一次检查

示例里的 `StartsWith` 先检查 span 是否为空，再读取第一个元素：

```csharp
!values.IsEmpty && values[0] == expected
```

`&&` 左边为 `false` 时，右边不会执行。能读到 `values[0]`，说明前面的检查已经确认 span 非空。JIT 可以利用这个条件，省去访问第一个元素时重复的边界检查。空输入仍然返回 `false`，不会因为优化而去访问不存在的元素。

`ToByte` 也是类似的例子。它先判断 `uint` 值是否不大于 80，再执行 `checked((byte)value)`。进入转换分支时，值一定在 0 到 80 之间；而 `byte` 可以容纳 0 到 255，因此这次转换不可能溢出。JIT 有机会去掉多余的溢出检查。`IsSmall` 的 `0 or 1 or 2 or 3 or 4` 则可能被合并成范围判断。

接口示例将 `Discount` 作为 `IRule` 调用。若 JIT 能确定实际类型是 `Discount`，就有机会改成直接调用，甚至把方法内容内联；无法确定时，需要保留一般的接口调用。程序打印 100 只说明算出了预期值，不能证明采用了哪种机器代码。

接口调用需要根据对象的实际类型找到应执行的方法。直接调用则已经知道目标是 `Discount.Apply`；如果 JIT 进一步把方法体放进调用方，就叫内联。这样除了省下部分调用工作，还可能让 JIT 看见更多可以一起优化的计算。实际是否发生仍取决于生成代码时掌握的信息和优化条件。

### 看看 JIT 生成了什么

在项目目录下，可以用 PowerShell 查看 `StartsWith` 的反汇编：

```powershell
$env:DOTNET_TieredCompilation = '0'
$env:DOTNET_JitDisasm = 'RuntimeDemos:StartsWith'
dotnet run -c Release -- runtime
Remove-Item Env:DOTNET_JitDisasm
Remove-Item Env:DOTNET_TieredCompilation
```

这里关闭分层编译，是为了更容易集中观察优化后的代码，不代表应用默认运行配置。不同 CPU 和运行时版本生成的指令可能不同；比较时应关注前面的非空判断和后面的读取之间有没有重复检查，而不是只寻找某个固定的辅助方法名。[官方 JIT 改进说明](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime#jit-improvements)

## SIMD：130 个整数，最后两个怎么处理

`Vector128<int>` 中的 128 是位数。一个 `int` 占 32 位，所以一个向量能放四个整数，`Vector128<int>.Count` 返回 4。

示例把两个长度为 130 的数组逐项相加。完整向量循环每次处理四项，处理完下标 0 到 127 后，还剩下 128 和 129。第一种写法用普通循环逐项计算这两个值。

示例还用 `AddWithOverlappingTail` 算出另一份结果。它处理完完整向量后，从数组末尾往前取四项，再计算一次：

```text
完整向量的最后一轮：124、125、126、127
重叠收尾的这一轮：  126、127、128、129
```

126 和 127 被计算了两次，但这里的输入数组没有被修改，结果写入新数组。对同一组输入再做一次加法，写入的值相同，所以最终结果与逐项收尾一致。程序会比较两份完整结果；它验证的是这个例子的结果，不能说明哪种写法更快。

重复计算并不总是安全。如果正在累加总和，126 和 127 会被加两次；如果原地执行 `a[i] += b[i]`，这两个位置也会多加一次。输入和输出发生其他重叠时，先写出的值还可能影响后面的读取。输入不足四项时，示例退回逐项处理；长度恰好是四的倍数时，没有尾部，也就不需要重叠处理。

另一种办法是使用硬件支持的掩码加载和存储。最后一轮可以指定只有前两个位置有效，只读取和写入下标 128、129。掩码必须控制内存访问：先读取四项再遮住后两项，读取时已经越界。

Arm SVE 用一组称为“谓词”的开关表示哪些元素有效，例如 `[有效, 有效, 无效, 无效]`。合适的指令可以根据这些开关只访问有效位置。.NET 11 扩充了相关谓词 API 和代码生成。但 SVE 不是所有 Arm64 CPU 都支持；使用专用硬件 API 前需要检查 `IsSupported` 并准备回退。本项目实际运行的是跨平台 `Vector128<T>` 的标量收尾和重叠收尾，普通向量循环不会因为升级到 .NET 11 就自动变成 SVE 掩码循环。

### 交错、拆分和反转向量

示例还展示了 .NET 11 的向量组合方法。它们操作的是向量里已有的元素，与上面的数组收尾是两回事。

```text
CreateGeometricSequence(1, 2)：[1, 2, 4, 8]

Zip 输入： [1,  2,  3,  4]    [10, 20, 30, 40]
Zip 输出： [1, 10,  2, 20]    [ 3, 30,  4, 40]
Unzip：    再拆回原来的两个向量
```

`Reverse` 把 `[1, 2, 3, 4]` 变成 `[4, 3, 2, 1]`。`ConcatLowerLower` 取两份向量各自的前半段，拼成 `[1, 2, 10, 20]`。处理图像、音频等交错数据时，这些操作可以直接表达需要的排列。

参考：[.NET 11 向量 API](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime#simd-lane-construction-and-composition)、[SIMD 指南](https://learn.microsoft.com/en-us/dotnet/standard/simd)。

## GC：老对象指向新对象时，怎样避免漏掉引用

示例创建一个对象并放入 `Dictionary`。只要字典还保留它，程序就能从字典取得同一个对象。`GC.GetGeneration(item)` 显示对象当前的代数，`GC.CollectionCount` 显示进程中各代已经发生的回收次数。这些值会受到之前的分配和回收时机影响。

分代回收中有一个需要解决的问题：一个已经进入老年代的对象，可能持有指向年轻代对象的引用。只回收年轻代时，GC 不能漏掉这条引用；但每次为了找它而扫描整个老年代，又会增加回收成本。

.NET 按对象经历回收后的存活情况划分代数。普通新建的小对象通常从第 0 代开始；经历回收仍存活后可能晋升到第 1 代、第 2 代。GC 可以只回收年轻代，因为很多对象存活时间很短；第 2 代回收涉及更大范围。示例刚创建的 `item` 通常处于第 0 代，但输出会受运行时活动影响，不应把它当作永久保证。

写屏障是 JIT 在需要的引用写入处生成的辅助操作。它会帮助 GC 标记值得检查的堆区域，让后续年轻代回收能够找到跨代引用。写屏障与线程同步中的内存屏障不同，也不意味着每次赋值都会立刻触发 GC。

.NET 11 对相关路径做了几项改进：展开数组存储辅助逻辑，让 JIT 在证明安全时有机会去掉多余的类型检查或写屏障；复制包含引用的结构体时，分别处理引用和普通数据；为 `ConditionalWeakTable` 使用的 dependent handle 增加年龄管理，减少年轻代回收时对长期存活关系的重复检查。

示例没有强制回收，也不会从代数或回收次数推断某条写屏障指令是否执行。要研究具体优化，需要结合实际负载、诊断工具或生成的机器代码。

参考：[数组存储](https://github.com/dotnet/runtime/pull/126547)、[结构体复制](https://github.com/dotnet/runtime/pull/128542)、[dependent handle](https://github.com/dotnet/runtime/pull/78746)、[GC 设计说明](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/garbage-collection.md)。
