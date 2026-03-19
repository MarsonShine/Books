性能分析（Profiling）
===================

本文档中，“性能分析（profiling）”指的是在公共语言运行时（Common Language Runtime，CLR）上执行程序时，对其执行过程进行监控。本文档详细介绍了运行时提供的接口，以便访问此类信息。

尽管它被称为 Profiling API，但其提供的能力不仅适用于传统的性能分析工具。传统 profiler 主要关注对程序执行进行度量——例如每个函数花费的时间，或程序随时间变化的内存使用情况。然而，Profiling API 的目标面向的是更广泛的一类诊断工具，例如代码覆盖率工具，甚至高级调试辅助工具。

所有这些用途的共同点在于：它们本质上都是诊断性质的——工具被编写出来用于监控程序的执行。Profiling API 绝不应由程序自身使用，程序执行的正确性也不应依赖于（或因）存在 profiler 而受到影响。

要对 CLR 程序进行 profiling，需要比对传统编译后的机器码进行 profiling 更多的支持。这是因为 CLR 具有诸如应用程序域（application domain）、垃圾回收（garbage collection）、托管异常处理（managed exception handling）以及对代码进行 JIT 编译（将中间语言 Intermediate Language 转换为本机机器码）等概念，而现有的传统 profiling 机制无法识别这些概念并提供有用信息。Profiling API 以高效方式补齐了这些缺失信息，并且对 CLR 与被分析程序的性能影响最小。

另外，运行时对例程进行 JIT 编译也提供了很好的机会：API 允许 profiler 修改某个例程在内存中的 IL 代码流，然后请求重新对其进行 JIT 编译。通过这种方式，profiler 可以动态为需要更深入调查的特定例程加入插桩代码。虽然在传统场景中也能做到类似事情，但在 CLR 上实现要容易得多。

Profiling API 的目标
====================

- 暴露现有 profiler 需要的信息，使用户能够确定并分析在 CLR 上运行的程序性能。具体包括：

	- Common Language Runtime 启动与关闭事件
	- 应用程序域创建与关闭事件
	- 程序集（assembly）加载与卸载事件
	- 模块（module）加载/卸载事件
	- COM VTable 创建与销毁事件
	- JIT 编译与代码回收（code pitching）事件
	- 类加载/卸载事件
	- 线程创建/销毁/同步
	- 函数进入/退出事件
	- 异常
	- 托管与非托管执行之间的转换
	- 不同运行时 _上下文（contexts）_ 之间的转换
	- 运行时挂起（suspension）相关信息
	- 运行时内存堆与垃圾回收活动相关信息

- 可从任意（非托管）且与 COM 兼容的语言调用
- 高效：在 CPU 与内存消耗方面，profiling 的行为不应对被分析程序造成过大的变化，以至于结果产生误导
- 同时对 _采样（sampling）_ 与 _非采样（non-sampling）_ profiler 有用。[_采样_ profiler 会以固定时钟 tick 定期检查被分析进程——比如每 5 毫秒一次。_非采样_ profiler 会在事件发生时与触发事件的线程同步接收通知]

Profiling API 的非目标
======================

- Profiling API **不**支持对非托管代码做 profiling。对非托管代码的 profiling 应改用既有机制。CLR profiling API 只适用于托管代码。不过，profiler 会收到托管/非托管转换事件，从而可以确定托管与非托管代码的边界。
- Profiling API **不**支持编写会修改自身代码的应用程序，例如用于面向切面编程（AOP）。
- Profiling API **不**提供用于边界检查的信息。CLR 对所有托管代码都提供内建（intrinsic）的边界检查支持。

CLR 的代码 profiler 接口不支持远程 profiling，原因如下：

- 使用这些接口时需要尽量降低执行时间，避免 profiling 结果被不当影响。这在监控执行性能时尤为重要。不过，当接口用于监控内存使用或获取运行时栈帧、对象等信息时，这并不是限制。
- 代码 profiler 需要在被分析应用运行的本机上向运行时注册一个或多个回调接口。这限制了创建远程代码 profiler 的能力。

Profiling API – 概览
=====================

CLR 内的 Profiling API 允许用户监控运行中应用程序的执行与内存使用。通常该 API 用于编写一个代码 profiler 包。在后续章节中，我们会将 profiler 视为一个用于监控 _任何_ 托管应用程序执行的包。

Profiling API 由一个 profiler DLL 使用，该 DLL 会被加载到被分析程序的同一进程内。profiler DLL 实现一个回调接口（ICorProfilerCallback2）。运行时调用该接口的方法，以通知 profiler 被分析进程中发生的事件。profiler 也可以通过 ICorProfilerInfo 上的方法反向调用运行时，以获取被分析应用的状态信息。

注意：profiler 解决方案中只有数据采集部分应当在被分析应用进程内运行——UI 与数据分析应当在单独的进程中完成。

![Profiling Process Overview](./asserts/profiling-overview.png)

_ICorProfilerCallback_ 与 _ICorProfilerCallback2_ 接口由一组方法组成，这些方法名类似 ClassLoadStarted、ClassLoadFinished、JITCompilationStarted。每当 CLR 加载/卸载一个类、编译一个函数等，就会调用 profiler 的 _ICorProfilerCallback/ICorProfilerCallback2_ 接口中对应的方法。（其他通知同理；后文会给出细节）

因此，例如 profiler 可以通过 FunctionEnter 与 FunctionLeave 两个通知来度量代码性能：它只需对每次通知打时间戳、累计结果，然后输出列表，指出哪些函数在应用执行期间消耗了最多 CPU 时间或最多墙钟时间。

_ICorProfilerCallback/ICorProfilerCallback2_ 接口可以被视为“通知 API（notifications API）”。

profiling 的另一个相关接口是 _ICorProfilerInfo_。profiler 会按需调用它，以获取更多信息来辅助分析。例如，每当 CLR 调用 FunctionEnter 时，会提供一个 FunctionId 值。profiler 可以调用 _ICorProfilerInfo::GetFunctionInfo_ 以查询该 FunctionId 的更多信息：所属父类、函数名等。

到目前为止的图景描述的是：应用与 profiler 运行后发生了什么。但当应用启动时，二者如何关联在一起？CLR 在每个进程初始化时建立连接：它会决定是否连接 profiler，以及连接哪个 profiler。这个决定取决于两个环境变量的值，并按顺序检查：

- CORECLR_ENABLE_PROFILING - 仅当该环境变量存在且被设置为非零值时才连接 profiler。
- CORECLR_PROFILER - 连接到具有该 CLSID 或 ProgID 的 profiler（它必须事先写入注册表）。CORECLR_PROFILER 环境变量定义为字符串：
	- set CORECLR_PROFILER={32E2F4DA-1BEA-47ea-88F9-C5DAF691C94A}，或
	- set CORECLR_PROFILER="MyProfiler"
- profiler 类必须实现 _ICorProfilerCallback/ICorProfilerCallback2_。要求 profiler 必须实现 ICorProfilerCallback2；若未实现，则不会被加载。

当上述两项检查均通过后，CLR 会以类似 _CoCreateInstance_ 的方式创建 profiler 实例。之所以不直接调用 _CoCreateInstance_，是为了避免调用 _CoInitialize_（它需要设置线程模型）。随后 CLR 会调用 profiler 的 _ICorProfilerCallback::Initialize_ 方法，其签名为：

```cpp
HRESULT Initialize(IUnknown *pICorProfilerInfoUnk)
```

profiler 必须对 pICorProfilerInfoUnk 调用 QueryInterface 获取一个 _ICorProfilerInfo_ 接口指针，并保存起来，以便后续 profiling 时调用获取更多信息。随后 profiler 调用 ICorProfilerInfo::SetEventMask，声明它对哪些类别的通知感兴趣。例如：

```cpp
ICorProfilerInfo* pInfo;

pICorProfilerInfoUnk->QueryInterface(IID_ICorProfilerInfo, (void**)&pInfo);

pInfo->SetEventMask(COR_PRF_MONITOR_ENTERLEAVE | COR_PRF_MONITOR_GC)
```

该 mask 表示 profiler 只对函数进入/退出通知以及 GC 通知感兴趣。随后 profiler 只需返回，就开始运行了。

以这种方式设置通知 mask，profiler 可以限制自己接收的通知。这显然有助于用户构建更简单或更专用的 profiler；同时也减少了向 profiler 发送其会直接“丢到地上”的通知所浪费的 CPU 时间（后文详述）。

TODO: 这段文字有点令人困惑。它似乎把“需要创建不同的 environment（环境变量意义上的环境）来指定不同 profiler”和“同一时刻一个进程只能附加一个 profiler”这两点混在一起了。它也可能把 launch 与 attach 场景混淆了。是这样吗？？

注意：在同一 environment 中，一个进程在同一时间只能被一个 profiler 分析。在不同 environment 中，可以在各自 environment 中注册不同的 profiler，并分别分析不同进程。

某些 profiler 事件是 IMMUTABLE（不可变）的，意味着一旦在 _ICorProfilerCallback::Initialize_ 回调中设置，就不能通过 ICorProfilerInfo::SetEventMask() 关闭。尝试修改不可变事件会导致 SetEventMask 返回失败的 HRESULT。

profiler 必须实现为 inproc COM server——一个 DLL，它会映射到被分析进程的同一地址空间。不支持其他类型的 COM server；例如，如果 profiler 想监控远程计算机上的应用，它必须在每台机器上实现“采集代理（collector agents）”，将结果批量汇总并发送到中心数据采集机。

Profiling API – 常见概念
========================

本节简要解释一些贯穿 Profiling API 的概念，而不是在每个方法说明中重复。

IDs
---

运行时通知会为被报告的类、线程、AppDomain 等提供一个 ID。这些 ID 可用于向运行时查询更多信息。这些 ID 实际上就是一块描述该项的内存块的地址；但对任何 profiler 来说，都应将它们视为不透明句柄。如果在任一 Profiling API 函数调用中使用了无效 ID，则结果未定义。通常很可能会导致访问违例（access violation）。用户必须确保所用 ID 绝对有效。Profiling API 不会做任何验证，因为验证会带来开销，并且会显著拖慢执行。

### 唯一性（Uniqueness）

ProcessID 在进程生命周期内在系统范围内唯一。其他所有 ID 在其生命周期内在进程范围内唯一。

### 层级与包含关系（Hierarchy & Containment）

ID 以层级组织，镜像进程中的层级：进程包含 AppDomain，AppDomain 包含程序集，程序集包含模块，模块包含类，类包含函数。线程包含于进程中，并可能从一个 AppDomain 移动到另一个 AppDomain。对象大多包含于 AppDomain 中（极少数对象可能同时属于多个 AppDomain）。上下文（contexts）包含于进程中。

### 生命周期与稳定性（Lifetime & Stability）

当某个 ID 死亡时，它所包含的所有 ID 也会死亡。

ProcessID – 从 Initialize 调用开始到 Shutdown 返回为止存活且稳定。

AppDomainID – 从 AppDomainCreationFinished 调用开始到 AppDomainShutdownStarted 返回为止存活且稳定。

AssemblyID、ModuleID、ClassID – 从该 ID 的 LoadFinished 调用开始到该 ID 的 UnloadStarted 返回为止存活且稳定。

FunctionID – 从 JITCompilationFinished 或 JITCachedFunctionSearchFinished 调用开始，到其包含的 ClassID 死亡为止存活且稳定。

ThreadID – 从 ThreadCreated 调用开始到 ThreadDestroyed 返回为止存活且稳定。

ObjectID – 从 ObjectAllocated 调用开始存活；会在每次 GC 中具备变化或死亡的可能。

GCHandleID – 从 HandleCreated 调用开始到 HandleDestroyed 返回为止存活且稳定。

此外，任何由 Profiling API 函数返回的 ID，在返回时必然是存活的。

### AppDomain 亲和性（App-Domain Affinity）

进程中每个用户创建的 app-domain 都对应一个 AppDomainID，另外还有“默认”域，以及一个用于承载域中立程序集（domain-neutral assemblies）的特殊伪域（pseudo-domain）。

Assembly、Module、Class、Function、GCHandleID 具有 app-domain 亲和性：如果某程序集被加载到多个 app domain，那么它（以及其中包含的所有模块、类、函数）在每个 app domain 中都会有不同的 ID，对各 ID 的操作只会在关联的 app domain 中生效。域中立程序集会出现在上述特殊伪域中。

### 特别说明（Special Notes）

除 ObjectID 外，所有 ID 都应视为不透明值。多数 ID 含义比较直观，但有少数值得更详细解释：

**ClassID** 表示类。对泛型类而言，它表示“完全实例化（fully-instantiated）”的类型。List<int>、List<char>、List<object>、List<string> 各自都有自己的 ClassID。List<T> 是未实例化类型，没有 ClassID。Dictionary<string,V> 是部分实例化类型，也没有 ClassID。

**FunctionID** 表示某个函数的本机代码。对泛型函数（或泛型类上的函数）而言，同一个函数可能有多个本机代码实例化，因此也会有多个 FunctionID。本机代码实例化可能在不同类型间共享——例如 List<string> 与 List<object> 共享所有代码——因此一个 FunctionID 可能“属于”多个 ClassID。

**ObjectID** 表示垃圾回收对象。ObjectID 是 profiler 接收到该 ObjectID 时对象的当前地址，并且会随每次 GC 变化。因此，一个 ObjectID 值只在“接收到它”到“下一次 GC 开始”之间有效。CLR 也提供通知，允许 profiler 更新其内部对象映射表，以便在 GC 期间维护跨 GC 的有效 ObjectID。

**GCHandleID** 表示 GC 句柄表中的条目。GCHandleID 与 ObjectID 不同，是不透明值。GC 句柄在某些情况下由运行时自身创建，也可以由用户代码使用 System.Runtime.InteropServices.GCHandle 结构创建。（注意：GCHandle 结构只是句柄的表现形式；句柄并不“存活在” GCHandle 结构内部。）

**ThreadID** 表示托管线程。如果宿主支持以 fiber 模式执行，那么一个托管线程在不同时间可能存在于不同 OS 线程上。（**注意：不支持对 fiber 模式应用进行 profiling。**）

回调返回值
-----------

对于 CLR 触发的每个通知，profiler 都会以 HRESULT 形式返回一个状态值，该值可能为 S_OK 或 E_FAIL。目前运行时会忽略所有回调的该状态值，唯独 ObjectReferences 回调例外。

由调用方分配的缓冲区（Caller-Allocated Buffers）
-------------------------------------------------

ICorProfilerInfo 中带调用方分配缓冲区的函数通常遵循如下签名：

		HRESULT GetBuffer( [in] /* Some query information */,
		   [in] ULONG32 cBuffer,
		   [out] ULONG32 *pcBuffer,
		   [out, size_is(cBuffer), length_is(*pcMap)] /* TYPE */ buffer[] );

这些函数总是按如下方式工作：

- cBuffer 是缓冲区中分配的元素数量。
- *pcBuffer 会被设置为可用元素的总数。
- buffer 会尽可能填充更多元素。

如果返回了任何元素，则返回值为 S_OK。调用方负责检查缓冲区是否足够大。

如果 buffer 为 NULL，则 cBuffer 必须为 0。函数会返回 S_OK，并将 *pcBuffer 设置为可用元素总数。

可选的 out 参数（Optional Out Parameters）
-------------------------------------------

除非某个函数只有一个 [out] 参数，否则 API 上的所有 [out] 参数都是可选的。profiler 可以对任何不感兴趣的 [out] 参数传入 NULL。profiler 还必须对相关的 [in] 参数传入一致值——例如如果某个 NULL 的 [out] 参数本应是一个填充数据的缓冲区，那么用于指定其大小的 [in] 参数就必须为 0。

通知线程（Notification Thread）
-------------------------------

多数情况下，通知由触发事件的同一线程执行。此类通知（例如 FunctionEnter 与 FunctionLeave_)_）不需要显式提供 ThreadID。profiler 也可以选择使用线程本地存储（TLS）来存储与更新分析数据块，而不是用全局存储并基于受影响线程的 ThreadID 索引。

每个通知都会说明由哪个线程发起调用——要么是生成事件的线程，要么是运行时内部的某个工具线程（例如垃圾回收器）。对于可能由不同线程调用的回调，用户可以调用 _ICorProfilerInfo::GetCurrentThreadID_ 来发现生成事件的线程。

注意：这些回调并不会被串行化。profiler 开发者必须编写防御性代码：使用线程安全的数据结构，并在必要时对 profiler 代码加锁以防止多线程并发访问。因此，在某些情况下可能会收到“不寻常”的回调序列。例如：假设一个托管应用创建了两个线程，它们执行相同代码。在这种情况下，可能会先从一个线程收到某函数的 JITCompilationStarted 事件，在尚未收到相应的 JITCompilationFinished 回调之前，另一个线程就已经发送了 FunctionEnter 回调。于是你会收到一个看起来“尚未完全 JIT 编译完成”的函数的 FunctionEnter 回调！

GC 安全的外调（GC-Safe Callouts）
----------------------------------

当 CLR 调用 _ICorProfilerCallback_ 中的某些函数时，在 profiler 从该调用返回之前，运行时无法执行垃圾回收。这是因为 profiling 服务无法总是将栈构造成对 GC 安全的状态；因此运行时会在该回调周围禁用 GC。对于这些情况，profiler 应当尽可能快地返回。适用的回调包括：

- FunctionEnter, FunctionLeave, FunctionTailCall
- ExceptionOSHandlerEnter, ExceptionOSHandlerLeave
- ExceptionUnwindFunctionEnter, ExceptionUnwindFunctionLeave
- ExceptionUnwindFinallyEnter, ExceptionUnwindFinallyLeave
- ExceptionCatcherEnter, ExceptionCatcherLeave
- ExceptionCLRCatcherFound, ExceptionCLRCatcherExecute
- COMClassicVTableCreated, COMClassicVTableDestroyed

此外，下列回调可能允许 profiler 阻塞，也可能不允许。是否安全可阻塞由每次调用的 fIsSafeToBlock 参数指示。该集合包括：

- JITCompilationStarted, JITCompilationFinished

注意：如果 profiler _确实_阻塞，会延迟 GC。只要 profiler 自身不尝试在托管堆上分配空间（这可能导致死锁），这种延迟是无害的。

使用 COM
--------

虽然 profiling API 接口被定义为 COM 接口，但运行时并不会为了使用它们而初始化 COM。这是为了避免在托管应用有机会指定其期望线程模型之前，就必须通过 CoInitialize 设置线程模型。类似地，profiler 本身也不应调用 CoInitialize，因为它可能选择了与被分析应用不兼容的线程模型，从而破坏应用。

回调与栈深度（Callbacks and Stack Depth）
----------------------------------------

profiler 回调可能在栈空间极其受限的情况下发出，回调内发生的栈溢出会导致进程立即退出。profiler 应当谨慎：在回调响应中尽量少用栈空间。如果 profiler 计划用于对栈溢出具有鲁棒性的进程，那么 profiler 自身也应避免触发栈溢出。

如何对 NT 服务进行 profiling
----------------------------

profiling 通过环境变量启用。由于 NT 服务会在操作系统启动时启动，因此在那个时间点这些环境变量必须已经存在并设置为所需值。要对 NT 服务进行 profiling，必须提前以系统范围设置相应环境变量：

MyComputer -> Properties -> Advanced -> EnvironmentVariables -> System Variables

必须设置 **CORECLR_ENABLE_PROFILING** 与 **CORECLR_PROFILER**，并确保 profiler DLL 已注册。然后应重启目标机器，使 NT 服务获取这些更改。注意：这会在系统范围启用 profiling。为避免随后运行的每个托管应用都被 profiling，用户应在重启后删除这些系统环境变量。

Profiling API – 高层描述
========================

加载器回调（Loader Callbacks）
-----------------------------

加载器回调是针对 app domain、程序集、模块与类加载发出的回调。

你可能期望 CLR 会先通知程序集加载，然后对该程序集通知一个或多个模块加载。但实际发生什么取决于 loader 实现中的许多因素。profiler 可以依赖以下事实：

- 对同一 ID，Started 回调一定先于 Finished 回调到达。
- Started 与 Finished 回调在同一线程上发出。

尽管加载器回调以 Started/Finished 成对出现，但它们不能用于准确地将时间归因到 loader 内部操作。

调用栈（Call stacks）
---------------------

Profiling API 提供两种获取调用栈的方法：一种是快照方法，适合稀疏采集调用栈；另一种是影子栈（shadow-stack）方法，适合在每个时刻跟踪调用栈。

### 栈快照（Stack Snapshot）

栈快照是在某一时刻对线程栈的跟踪。Profiling API 提供对栈上托管函数的跟踪支持，但对非托管函数的跟踪则交由 profiler 自己的栈回溯器完成。

### 影子栈（Shadow Stack）

过于频繁地使用上述快照方法会很快带来性能问题。当需要频繁获取栈跟踪时，profiler 应改为使用 FunctionEnter、FunctionLeave、FunctionTailCall 以及 Exception* 回调构建“影子栈”。影子栈始终是最新的，并且在需要栈快照时可以快速拷贝到存储。

影子栈可以获得函数参数、返回值以及泛型实例化信息。这些信息只能通过影子栈获取，因为它在函数进入时很容易得到，但在函数后续执行过程中可能已被优化掉。

垃圾回收（Garbage Collection）
------------------------------

当 profiler 指定 COR_PRF_MONITOR_GC flag 时，除 _ICorProfilerCallback::ObjectAllocated_ 事件外，所有 GC 事件都会触发。出于性能原因，ObjectAllocated 事件由另一个 flag 显式控制（见下一节）。注意：启用 COR_PRF_MONITOR_GC 时，会关闭并发 GC。

profiler 可以使用 GarbageCollectionStarted/Finished 回调来识别 GC 发生以及覆盖的代（generation）。

### 跟踪移动的对象（Tracking Moved Objects）

垃圾回收会回收“死亡”对象占用的内存并压缩空洞，从而导致存活对象在堆内移动。结果是：之前通知中发给 profiler 的 _ObjectID_ 会发生变化（对象自身内部状态不变，除了它对其他对象的引用；变化的是对象在内存中的位置，因此 _ObjectID_ 改变）。_MovedReferences_ 通知允许 profiler 更新其内部表，以便按 _ObjectID_ 跟踪信息。该通知名称有些误导，因为即使对象没有移动也会发出。

堆中的对象数量可能达到成千上万甚至数百万。如此规模下，不可能通过为每个对象提供一个 before/after ID 来通知其移动。但垃圾回收器倾向于把连续的一段存活对象作为一批一起移动——它们会移动到新的堆位置，但仍保持连续。因此该通知会报告这些连续对象段的“before”与“after” _ObjectID_。（见下面示例）

换言之，如果某个 _ObjectID_ 值落在范围：

	_oldObjectIDRangeStart[i] <= ObjectID < oldObjectIDRangeStart[i] + cObjectIDRangeLength[i]_
	
	对 _0 <= i < cMovedObjectIDRanges_ 成立，则该 _ObjectID_ 值已变为
	
	_ObjectID - oldObjectIDRangeStart[i] + newObjectIDRangeStart[i]_

所有这些回调都在运行时被挂起（suspended）期间发出，因此在运行时恢复并发生下一次 GC 之前，所有 _ObjectID_ 值都不会变化。

**示例：** 下图展示了 GC 之前的 10 个对象。它们的起始地址（等同于 _ObjectID_）分别为 08、09、10、12、13、15、16、17、18、19。其中 _ObjectID_ 09、13、19 为死亡对象（以阴影显示）；其空间会在 GC 时被回收。

![Garbage Collection](./asserts/profiling-gc.png)

“After” 图展示了死亡对象的空间如何被回收以容纳存活对象。存活对象在堆中移动到图中所示的新位置，因此其 _ObjectID_ 全部改变。用一张 before/after _ObjectID_ 表来描述这些变化的最简单方式如下：

|    | oldObjectIDRangeStart[] | newObjectIDRangeStart[] |
|:--:|:-----------------------:|:-----------------------:|
| 0  | 08 | 07 |
| 1  | 09 |    |
| 2  | 10 | 08 |
| 3  | 12 | 10 |
| 3  | 13 |    |
| 4  | 15 | 11 |
| 5  | 16 | 12 |
| 6  | 17 | 13 |
| 7  | 18 | 14 |
| 8  | 19 |    |

这种方式可行，但显然我们可以通过指定连续段的起点与大小来压缩信息，如下：

|    | oldObjectIDRangeStart[] | newObjectIDRangeStart[] | cObjectIDRangeLength[] |
|:--:|:-----------------------:|:-----------------------:|:----------------------:|
| 0  | 08 | 07 | 1 |
| 1  | 10 | 08 | 3 |
| 2  | 15 | 11 | 4 |

这正是 _MovedReferences_ 报告信息的方式。注意：_MovedReferencesCallback_ 在对象实际搬迁之前就报告了对象的新布局。因此旧的 _ObjectID_ 仍可用于调用 _ICorProfilerInfo_ 接口（而新的 _ObjectID_ 还不可以）。

#### 检测所有已删除对象（Detecting All Deleted Objects）

MovedReferences 会报告在一次压缩 GC 中存活的所有对象，无论它们是否移动；未被报告的对象都没有存活。但并非所有 GC 都是压缩的。

profiler 可以调用 ICorProfilerInfo2::GetGenerationBounds 获取 GC 堆段的边界。结果中 COR_PRF_GC_GENERATION_RANGE 结构的 rangeLength 字段可用于推导压缩后某一代中存活对象的范围。

GarbageCollectionStarted 回调会指示本次 GC 收集哪些代。处于未被收集代中的所有对象都会存活。

对于非压缩 GC（不会移动任何对象的 GC），会发出 SurvivingReferences 回调来指示哪些对象在该次 GC 中存活。

注意：一次 GC 可能对某一代是压缩的，而对另一代是非压缩的。对某次 GC 的某一代而言，要么会收到 SurvivingReferences 回调，要么会收到 MovedReferences 回调，不会两者都有。

#### 备注（Remarks）

GC 之后，应用会暂停，直到运行时向代码 profiler 传递完有关堆的信息。可以使用 _ICorProfilerInfo::GetClassFromObject_ 获取对象实例所属类的 _ClassID_；使用 _ICorProfilerInfo::GetTokenFromClass_ 获取该类的元数据 token 信息。

RootReferences2 允许 profiler 识别通过特殊句柄持有的对象。GetGenerationBounds 提供的代边界信息与 GarbageCollectionStarted 提供的收集代信息结合使用，可帮助 profiler 识别存活于未收集代中的对象。

对象检查（Object Inspection）
------------------------------

FunctionEnter2/Leave2 回调以内存区域的形式提供函数参数与返回值信息。参数按从左到右存放在给定内存区域中。profiler 可以使用函数的元数据签名来解释这些参数，如下：

| **ELEMENT_TYPE**                      | **表示形式（Representation）**         |
| -------------------------------------- | -------------------------- |
| 基元类型（ELEMENT_TYPE <= R8, I, U） | 基元值           |
| 值类型（VALUETYPE）                | 取决于具体类型            |
| 引用类型（CLASS, STRING, OBJECT, ARRAY, GENERICINST, SZARRAY） | ObjectID（指向 GC 堆的指针） |
| BYREF                                  | 托管指针（不是 ObjectID，但可能指向栈或 GC 堆） |
| PTR                                    | 非托管指针（不会被 GC 移动） |
| FNPTR                                  | 指针大小的不透明值 |
| TYPEDBYREF                             | 托管指针，后跟一个指针大小的不透明值 |

ObjectID 与托管指针之间的差异：

- ObjectID 只会指向 GC 堆或冻结对象堆（frozen object heap）。托管指针也可能指向栈。
- ObjectID 总是指向对象起始位置；托管指针可能指向对象的某个字段。
- 托管指针不能作为 ObjectID 传递给期望 ObjectID 的函数。

### 检查复杂类型（Inspecting Complex Types）

检查引用类型或非基元值类型需要一些高级技术。

对于除字符串或数组之外的值类型与引用类型，GetClassLayout 会提供每个字段的偏移。profiler 随后可以用元数据确定字段类型并递归求值。（注意：GetClassLayout 只返回该类自身定义的字段；父类定义的字段不包含在内。）

对于装箱（boxed）的值类型，GetBoxClassLayout 提供值类型在 box 内的偏移。值类型本身的布局不变，因此一旦 profiler 在 box 内找到该值类型，就可以使用 GetClassLayout 理解其布局。

对于字符串，GetStringClassLayout 提供字符串对象中相关数据片段的偏移。

数组较为特殊：理解数组需要对每个数组对象调用一个函数，而不是只对类型调用。（因为数组的格式太多，无法仅用偏移描述。）GetArrayObjectInfo 用于完成解释。

@TODO: Callbacks from which inspection is safe

@TODO: Functions that are legal to call when threads are hard-suspended

### 检查静态字段（Inspecting Static Fields）

GetThreadStaticAddress、GetAppDomainStaticAddress、GetContextStaticAddress、GetRVAStaticAddress 提供静态字段位置相关信息。查看该位置的内存时，应按如下方式解释：

- 引用类型：ObjectID
- 值类型：包含实际值的 box 的 ObjectID
- 基元类型：基元值

静态字段有四种类型。下表描述它们是什么以及如何在元数据中识别。

| **静态类型（Static Type）** | **定义（Definition）** | **在元数据中识别（Identifying in Metadata）** |
| --------------- | -------------- | --------------------------- |
| AppDomain       | 普通静态字段——在每个 app domain 中具有不同的值。 | 不带任何自定义特性的静态字段 |
| Thread          | 托管 TLS——在每个线程与每个 app domain 中具有唯一值的静态字段。 | 带有 System.ThreadStaticAttribute 的静态字段 |
| RVA             | 进程范围的静态字段，位于模块的数据段中 | hasRVA 标志为真的静态字段 |
| Context         | 在每个 COM+ Context 中具有不同值的静态字段 | 带有 System.ContextStaticAttribute 的静态字段 |

异常（Exceptions）
------------------

异常通知是所有通知里最难描述也最难理解的一类，这是因为异常处理本身极其复杂。下面描述的异常通知集合被设计为：为高级 profiler 提供所有所需信息——使得在任意时刻，它都能跟踪被分析进程中每个线程正在执行的异常处理阶段（第一遍或第二遍）、哪个栈帧、哪个 filter、以及哪个 finally 块。

注意：异常通知并不提供任何 _threadID_，但 profiler 可以随时调用 _ICorProfilerInfo::GetCurrentThreadID_ 来发现是哪一个托管线程抛出了异常。

![Exception callback sequence](./asserts/profiling-exception-callback-sequence.png)

上图展示了在监控异常事件时，代码 profiler 会收到的各种回调。每个线程初始处于“Normal Execution”。当线程处于大灰框内的状态时，异常系统控制该线程——在这些状态期间发生的任何非异常相关回调（例如 ObjectAllocated）都可以归因于异常系统本身。当线程处于大灰框之外状态时，它在运行任意托管代码。

### 嵌套异常（Nested Exceptions）

在线程处理异常的过程中，如果它又转换进入托管代码并抛出另一个异常，就会导致一轮全新的异常处理（图中的 “New EH Pass” 盒子）。如果这样的“嵌套”异常从原始异常的 filter/finally/catch 中逃逸，它会影响原始异常：

- 如果嵌套异常发生在 filter 内并从 filter 逃逸，则 filter 会被视为返回 “false”，第一遍继续。
- 如果嵌套异常发生在 finally 内并从 finally 逃逸，则原始异常的处理将永远不会恢复。
- 如果嵌套异常发生在 catch 内并从 catch 逃逸，则原始异常的处理将永远不会恢复。

### 非托管处理器（Unmanaged Handlers）

异常可能在非托管代码中被处理。在这种情况下，profiler 会看到 unwind 阶段，但不会看到任何 catch handler 的通知。执行会在非托管代码中正常继续。一个了解非托管的 profiler 能够检测到这一点，但一个仅关注托管的 profiler 可能会看到各种情况，包括但不限于：

- 当非托管代码调用或返回托管代码时的 UnmanagedToManagedTransition 回调。
- 线程终止（如果非托管代码位于线程根部）。
- 应用终止（如果非托管代码终止了应用）。

### CLR 处理器（CLR Handlers）

异常也可能由 CLR 自己处理。在这种情况下，profiler 会看到 unwind 阶段，但不会看到任何 catch handler 通知。它可能会看到执行在托管或非托管代码中正常继续。

### 未处理异常（Unhandled Exceptions）

默认情况下，未处理异常会导致进程终止。如果应用锁回了旧版异常策略，那么某些类型线程上的未处理异常可能只会导致线程终止。

代码生成（Code Generation）
---------------------------

### 从 IL 到本机代码（Getting from IL to Native Code）

.NET 程序集中的 IL 可以通过两种方式被编译为本机代码：要么在运行时由 JIT 编译，要么由名为 NGEN.exe 的工具（或 CoreCLR 的 CrossGen.exe）编译成“本机映像（native image）”。JIT 编译器与 NGEN 都提供了一组用于控制代码生成的 flag。

当程序集加载时，CLR 会首先查找该程序集的本机映像。如果找不到具有正确代码生成 flag 集的本机映像，CLR 就会在运行时按需 JIT 编译程序集中的函数。即使找到并加载了本机映像，CLR 也仍可能对该程序集中的部分函数进行 JIT 编译。

### profiler 对代码生成的控制（Profiler Control over Code-Generation）

profiler 对代码生成的控制如下表所示：

| **Flag**                       | **效果（Effect）** |
| ------------------------------ | --- |
| COR_PRF_USE_PROFILE_IMAGES | 使本机映像查找去寻找 profiler 增强的映像（ngen /profile）。对 JIT 代码无影响。 |
| COR_PRF_DISABLE_INLINING    | 对本机映像查找无影响；若进行 JIT，则禁用内联。其他优化仍然生效。 |
| COR_PRF_DISABLE_OPTIMIZATIONS | 对本机映像查找无影响；若进行 JIT，则禁用所有优化（包括内联）。 |
| COR_PRF_MONITOR_ENTERLEAVE  | 使本机映像查找去寻找 profiler 增强的映像（ngen /profile）。若进行 JIT，则在生成代码中插入 enter/leave 钩子。 |
| COR_PRF_MONITOR_CODE_TRANSITIONS | 使本机映像查找去寻找 profiler 增强的映像（ngen /profile）。若进行 JIT，则在托管/非托管转换点插入钩子。 |

### profiler 与本机映像（Profilers and Native Images）

当 NGEN.exe 创建本机映像时，它完成了许多 CLR 本应在运行时完成的工作——例如类加载与方法编译。因此，在某些工作已在 NGEN 时完成的情况下，运行时不会收到某些 profiler 回调：

- JITCompilation*
- ClassLoad*, ClassUnload*

为应对这种情况，不希望通过请求 profiler 增强本机映像而扰动进程的 profiler，应当准备在遇到 FunctionID 或 ClassID 时再延迟地收集所需数据。

### profiler 增强的本机映像（Profiler-Enhanced Native Images）

使用 NGEN /profile 创建本机映像会开启一组使映像更易于 profiling 的代码生成 flag：

- 在代码中插入 enter/leave 钩子。
- 插入托管/非托管转换钩子。
- 当本机映像中的每个函数首次被调用时，发出 JITCachedFunctionSearch 通知。
- 当本机映像中的每个类首次被使用时，发出 ClassLoad 通知。

由于 profiler 增强本机映像与普通映像差异显著，profiler 只应在额外扰动可接受时使用它们。

TODO: Instrumentation

TODO: Remoting

Profiling 中的安全问题
======================

profiler DLL 是一个非托管 DLL，它实际上作为 CLR 执行引擎的一部分运行。因此，profiler DLL 中的代码不受托管代码访问安全（code-access security）的限制，它唯一受限于操作系统对运行被分析应用的用户所施加的限制。

在代码 profiler 中混用托管与非托管代码
========================================

认真审视 CLR Profiling API 可能会给人一种印象：你可以写一个同时包含托管与非托管组件的 profiler，并通过 COM Interop 或 PInvoke 让二者互相调用。

尽管从设计角度这似乎可行，但 CLR Profiling API 并不支持这样做。CLR profiler 应当是纯非托管的。试图在 CLR profiler 中混用托管与非托管代码可能导致崩溃、卡死与死锁。危险很明显：profiler 的托管部分会“触发”事件回到其非托管组件，而非托管组件随后又会调用托管部分，如此往复。此时风险就非常清楚了。

CLR profiler 唯一可以安全调用托管代码的位置，是通过替换某个方法的 MSIL 体。在函数的 JIT 编译完成之前，profiler 在该方法的 MSIL 体内插入托管调用，然后让 JIT 编译它。这种技术可以成功用于对托管代码做选择性插桩，或者用于收集有关 JIT 的统计与耗时。

另一种做法是：在每个托管函数的 MSIL 体内插入本机“钩子”，让它调用进入非托管代码。这种技术可用于插桩与覆盖率。例如，代码 profiler 可以在每个 MSIL 基本块后插入插桩钩子，以确保该块被执行过。修改方法的 MSIL 体是一项非常精细的操作，需要考虑许多因素。

对非托管代码做 profiling
========================

运行时 profiling 接口对非托管代码的 profiling 支持很有限，提供的功能包括：

- 枚举栈链（stack chains），用于确定托管与非托管代码的边界。
- 判定某个栈链对应托管代码还是本机代码。

这些方法通过 CLR 调试 API 的进程内子集提供，定义在 CorDebug.IDL 中，并在 DebugRef.doc 中解释；请同时参考这两份文档以了解更多细节。

采样 profiler
=============

劫持（Hijacking）
-----------------

一些采样 profiler 通过在采样时“劫持”线程，强制线程去执行采样工作来实现。这是一种非常棘手的实践，我们不推荐。以下内容主要是为了劝退你不要走这条路。

### 劫持时机（Timing of Hijacks）

劫持式 profiler 必须跟踪运行时挂起事件（COR_PRF_MONITOR_SUSPENDS）。profiler 应假定：当从 RuntimeThreadSuspended 回调返回时，运行时会劫持该线程。profiler 必须避免自身劫持与运行时劫持发生冲突。为此，profiler 必须确保：

1. profiler 不会在 RuntimeThreadSuspended 与 RuntimeThreadResumed 之间尝试劫持线程。
1. 如果 profiler 已在 RuntimeThreadSuspended 回调发出之前开始劫持，则该回调在劫持完成之前不得返回。

这可以通过一些简单同步来实现。

#### 初始化运行时（Initializing the Runtime）

如果 profiler 有自己的线程，并会在该线程上调用 ICorProfilerInfo 函数，它需要确保在进行任何线程挂起之前先调用一次 ICorProfilerInfo 函数。这是因为运行时有每线程状态需要在所有其他线程运行时被初始化，以避免可能的死锁。

> 你可以把它想象成：**给 .NET 程序安装一个“全天候监控录像机”和“医生听诊器”。**
>
> ### 1. 它是干什么的？
> Profiling API 是一套接口。当你编写一个 .NET 程序时，如果你想知道：
> *   程序哪行代码跑得最慢？
> *   内存都被谁占用了？
> *   垃圾回收（GC）什么时候发生的？
> *   程序有没有报错（异常）？
>
> 你就需要写一个特殊的 **“监控程序”（Profiler）**。这个 API 就是让这个监控程序能和 .NET 程序“沟通”的桥梁。
>
> ### 2. 它是怎么工作的？（核心机制）
> 这就像是两家公司签合同：
> *   **回调接口 (ICorProfilerCallback)：** 这是 .NET 运行时（CLR）对监控程序说：“嘿，发生了一件事，我告诉你一声”。比如：“一个类加载了”、“一个函数开始了”、“垃圾回收开始了”。
> *   **查询接口 (ICorProfilerInfo)：** 这是监控程序反过来问 .NET 运行时：“刚才你说的那个函数叫什么名字？”、“这个内存对象是谁的？”。
>
> ### 3. 如何开启监控？
> 你不需要修改你的 .NET 代码。你只需要在启动程序前，设置两个“开关”（环境变量）：
> 1.  `CORECLR_ENABLE_PROFILING`：告诉程序，“我要开启监控功能”。
> 2.  `CORECLR_PROFILER`：告诉程序，“具体的监控员（DLL文件）是谁”。
>
> ### 4. 它能看到哪些数据？
> 文档列出了一大堆它能监控的东西，重点包括：
> *   **函数调用：** 进出每一个函数的时间（用于分析性能瓶颈）。
> *   **内存管理：** 追踪对象的创建、移动（GC 搬运对象）和销毁。
> *   **异常监控：** 只要程序报错，监控程序就能第一时间知道。
> *   **代码修改（重写）：** 甚至可以在代码运行前，偷偷改掉它的逻辑（比如加上日志代码）。
>
> ### 5. 有哪些限制和规则？
> *   **必须是 C++/非托管代码：** 监控程序不能用 C# 写。因为你不能用“被监控的语言”去监控它自己，这会导致死循环或死锁（就像医生不能在给自己做开胸手术的同时还要记录心率）。
> *   **同进程运行：** 监控程序必须和被监控的程序在同一个进程里，不支持远程监控。
> *   **性能影响：** 虽然设计得很高效，但监控开启后，程序肯定会变慢一点。
> *   **一次只能一个：** 一个程序同时只能挂载一个监控员。
>
> ### 总结一下：
> **对普通开发者的意义：**
> 你平时在 IDE 里点击“性能分析”或“内存诊断”时，后台其实就是通过这套 API 在工作的。它能让你在不改动源代码的情况下，把程序的五脏六腑看得清清楚楚。
>
> **如果你还是觉得抽象，可以记住这个比喻：**
> .NET 程序是一辆正在高速公路上跑的汽车，Profiling API 就是车上的 **“行车记录仪”和“传感器接口”**。这份文档就是教你如何制造一个能插进这个接口、读取车速和油耗的“诊断仪”。
