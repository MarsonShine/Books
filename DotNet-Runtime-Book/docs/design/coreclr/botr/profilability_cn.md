实现可分析性（Profilability）
============================

本文档描述了为 CLR 的某个特性增加“可分析性（profilability）”所涉及的技术细节。目标读者是正在修改 Profiling API、并希望让其特性可被 profiler 分析的开发者。

理念
====

契约（Contracts）
-----------------

在深入讨论 profiling API 应该使用哪些契约之前，先理解整体理念会很有帮助。

CLR（除 profiling API 之外）的默认契约推进运动背后有一个理念：鼓励 CLR 的大多数代码能够应对诸如“更激进的行为（aggressive behavior）”，例如抛出异常（throwing）或触发 GC（triggering）。你会看到，这与我们对回调（ICorProfilerCallback）契约的建议相辅相成：通常更偏好更宽松（“更激进”）的契约选项。这样能让 profiler 在回调期间拥有最大的灵活性（尤其是在通过 ICorProfilerInfo 发起 CLR 调用时）。

然而，下面这些 Info 函数（ICorProfilerInfo）恰好相反：我们更倾向于让它们更严格而不是更宽松。为什么？因为我们希望它们能让 profiler 在尽可能多的位置都能安全调用——甚至是在那些比我们期望更严格的回调中也能调用（例如由于某些原因必须是 GC_NOTRIGGER 的回调）。

同时，对 ICorProfilerInfo 更严格契约的偏好并不与 CLR 的总体默认契约理念矛盾，因为我们预期 CLR 中会有一小部分函数需要更加严格；ICorProfilerInfo 正是这类调用路径的根。由于 profiler 可能在非常微妙的时机调用回 CLR，我们希望这些调用尽可能“无侵入”。这些并不是 CLR 中的主流函数，而是少数需要非常谨慎的特殊调用路径。

因此，一般指导原则是：在 CLR 中尽可能使用默认契约。但当你需要开辟一条从 profiler 发起（即从 ICorProfilerInfo 出发）的调用链时，这条链需要显式指定契约，并且要比默认契约更严格。

性能还是易用性？
----------------

两者都想要当然最好。但如果必须权衡，请偏向性能。Profiling API 的目标是作为 CLR 与 profiler DLL 之间一个轻量、薄层、进程内的桥梁。Profiler 编写者非常少，且多为相当资深的开发者。CLR 对输入做一些简单校验是预期的，但我们只做到一定程度。

例如，考虑所有 profiler 的 ID：它们只是 C++ EE 对象实例指针的强转（AppDomain*、MethodTable* 等），并会被直接调用。Profiler 如果传了伪造 ID？CLR 会 AV（访问违例）！这是预期行为。CLR 不会为了验证查找而对 ID 做哈希。默认 profiler 知道自己在做什么。

不过再强调一次：CLR 仍应做简单的输入校验，例如检查 NULL 指针、确认用于检查的类已初始化、确保“并行参数”一致（例如数组指针参数非空时其 size 参数才可非零）等。

ICorProfilerCallback
===================

该接口包含 CLR 调用 profiler、用于通知有趣事件的回调。每个回调都会在 EE 中被一个薄包装方法包裹，该包装方法负责定位 profiler 对 ICorProfilerCallback(2) 的实现，并调用其对应方法。

Profiler 通过调用 ICorProfilerInfo::SetEventMask()/ICorProfilerInfo::SetEventMask2() 并设置相应 flag 来订阅事件。Profiling API 会保存这些选择，并通过一些专门的内联函数（CORProfiler*）将其暴露给 CLR，这些函数会与对应 flag 的 bit 做掩码判断。于是你会在 CLR 代码中到处看到类似如下代码：在事件发生时调用 ICorProfilerCallback 的包装器，但该调用会受 flag 是否设置的影响（通过调用专门的内联函数判断）：

```
{
    // check if profiler set flag
    BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());

    // call the ProfControlBlock wrapper around the profiler's callback implementation
    // which pins the profiler in DoOneProfilerIteration via EvacuationCounterHolder
    (&g_profControlBlock)->ModuleLoadStarted((ModuleID) this);
    // unpins the profiler after completing the callback

    END_PROFILER_CALLBACK();
}
```

需要明确的是：上面这段代码就是你会在代码库里看到的调用点。它所调用的函数（此处是 ModuleLoadStarted()）是我们对 profiler 回调实现（此处是 ICorProfilerCallback::ModuleLoadStarted()）的包装器。我们所有的包装器都集中在一个文件中（vm\EEToProfInterfaceImpl.cpp），而下面各节给出的指导针对的是这些包装器；不是针对上面这个调用包装器的示例代码。

BEGIN_PROFILER_CALLBACK 宏会求值其参数表达式：如果结果为 TRUE，则执行 BEGIN_PROFILER_CALLBACK 与 END_PROFILER_CALLBACK 宏之间的代码，并通过 ProfControlBlock 包装器将 profiler “钉住（pinned）”在内存中（意味着 profiler 将无法从进程中 detach）。如果表达式为 FALSE，则跳过两宏之间的全部代码。关于 BEGIN_PROFILER_CALLBACK 与 END_PROFILER_CALLBACK 宏的更多信息，请在代码库中找到其定义并阅读其中注释。

契约
----

每一个回调包装器在开头都必须有一些共同的“样板”。例如：

```
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;
    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                            LL_INFO10,
                            "**PROF: useful logging text here.\n"));
```

重要要点：

- 你必须为 throws、triggers、mode、take_lock 以及 ASSERT_NO_EE_LOCKS_HELD()（后者仅回调需要）显式指定一个值。这能帮助我们保持面向 profiler 编写者的文档准确。
- 每个契约都必须有自己的注释（下面会对契约细节给出更具体的要求）。

对每种契约类型都有一个“首选（preferred）”值。若可能，请使用它，并用 “Yay!” 做注释，这样其他人复制/粘贴你的代码到别处时会知道什么是最佳实践；若无法使用首选值，请注释原因。

下面是回调的首选值：

| Preferred | Why | Details |
| --------- | --- | ------- |
| NOTHROW   | 允许在任意 CLR 上下文中发出回调。既然 Infos 也应当是 NOTHROW，这对 profiler 来说不应是负担。 | 注意：如果 profiler 从这里调用了一个标记为 THROWS 的 Info 函数，即使 profiler 用 try/catch 包住该调用，你仍会收到 throws 违规（因为我们的契约系统看不到 profiler 的 try/catch）。因此在调用进入 profiler 之前，你需要插入一个以 CONTRACT_VIOLATION(ThrowsViolation) 为作用域的块。 |
| GC_TRIGGERS | 给予 profiler 在 Infos 上最大的灵活性。 | 如果回调发生在敏感时刻，保护所有 object refs 既容易出错又会显著降低性能，则使用 GC_NOTRIGGER（当然也要写注释！）。 |
| MODE_PREEMPTIVE 若可行，否则 MODE_COOPERATIVE | MODE_PREEMPTIVE 给予 profiler 在 Infos 上最大的灵活性（除非由于 ObjectID 必须 coop）。此外，MODE_PREEMPTIVE 也是 EE 中一个更偏好的“默认”契约值，让回调强制处于 preemptive 有助于促进 EE 其它地方也使用 preemptive。 | 如果你要向 profiler 传递 ObjectID 参数，则 MODE_COOPERATIVE 是合理的。否则，请指定 MODE_PREEMPTIVE。回调的调用方最好已经处于 preemptive 模式；若不是，请重新思考原因，并可能将调用方改为 preemptive。否则，你需要在调用回调前使用 GCX_PREEMP() 宏。 |
| CAN_TAKE_LOCK | 给予 profiler 在 Infos 上最大的灵活性 | 这里不再赘述。 |
| ASSERT_NO_EE_LOCKS_HELD() | 让 profiler 在 Infos 上拥有更多灵活性：它确保没有任何 Info 会尝试重入同一把锁或以错误顺序获取锁（因为根本没有锁可“重入”或破坏锁顺序）。 | 这其实不是一个契约，但契约块是放置它的方便位置，避免遗忘。与契约类似，若无法指定，请注释原因。 |

注意：回调不需要指定 EE_THREAD_NOT_REQUIRED / EE_THREAD_REQUIRED。GC 回调本来也不能指定 “REQUIRED”（可能没有 EE Thread）。这些只在 Info 函数（profiler → CLR）上才值得考虑。

入口点宏
---------

如上例所示，在契约之后应当有一个入口点宏。它负责日志记录、在 EE Thread 对象上标记我们处于回调中、移除 stack guard，以及做一些断言。宏有一些变体可选：

```
    CLR_TO_PROFILER_ENTRYPOINT
```

这是首选、也是最常用的宏。

也可以使用其他宏 **但你必须注释** 为什么无法使用上述（首选）宏。

```
    *_FOR_THREAD_*
```

这些宏用于 ICorProfilerCallback 中带 ThreadID 参数的方法，且该 ThreadID 的值并不总是“当前” ThreadID 的情况。你必须将 ThreadID 作为这些宏的第一个参数传入。随后宏会使用你提供的 ThreadID（而不是 GetThread()）来断言当前对该 ThreadID 发出回调是合法的（即我们尚未对该 ThreadID 发出 ThreadDestroyed()）。

ICorProfilerInfo
===============

该接口包含 profiler 用来调用 CLR 的入口点。

同步 / 异步
------------

每个 Info 调用都被归类为同步或异步。同步函数必须在回调内调用，而异步函数可在任意时间安全调用。

### 同步（Synchronous）

绝大多数 Info 调用都是同步的：profiler 只能在执行某个 Callback（回调）期间调用它们。换句话说，调用同步 Info 函数时，堆栈上必须存在一个 ICorProfilerCallback。我们通过 EE Thread 对象上的一个 bit 来跟踪这一点：进入回调时置位，回调返回时清零。同步 Info 函数被调用时会检查该 bit；若未置位，则不允许调用。

#### 没有 EE Thread 的线程

因为上述 bit 是通过 EE Thread 对象跟踪的，所以只有在带有 EE Thread 对象的线程上进行的 Info 调用才会被强制执行“同步性”检查。在非 EE Thread 线程上进行的 Info 调用会被立即认为是合法的。这通常没问题，因为主要是 EE Thread 线程会构建复杂上下文，使得重新进入会有风险；此外，最终仍由 profiler 自己保证正确性。正如上面所说，出于性能考虑，Profiling API 历来只做最低限度的正确性检查，以免增加开销。通常，profiler 在非 EE Thread 上发起的 Info 调用会属于以下类别：

- 在 server GC 回调期间发起的 Info 调用。
- 在 profiler 自己创建的线程上发起的 Info 调用，例如采样线程（因此其堆栈上没有 CLR 代码）。

#### Enter / Leave 钩子

如果 profiler 请求 enter/leave 钩子并使用 fast path（即 jitted 代码直接调用 profiler，中间不经过任何 profiling API 代码），那么它在 enter/leave 钩子内部对 Info 函数的任何调用都会被视为异步调用。同样，这是出于工程可行性：如果 profiling API 代码没有机会运行（为了性能），我们就没有机会设置 EE Thread 的 bit 来表明当前正在回调中。这意味着 profiler 在 enter/leave 钩子内部只能调用“异步安全”的 Info 函数。这通常可以接受，因为一个对性能极度敏感、以至于要求 enter/leave 直接调用的 profiler，往往也不会在 enter/leave 钩子里调用任何 Info。

另一种做法是：profiler 设置一个 flag，声明它希望获得参数或返回值信息，这会强制在 enter/leave 钩子前插入一段 profiling API 的 C 函数，用于为 profiler 的 Enter/Leave 钩子准备参数/返回值信息。当设置了该 flag 时，profiling API 会在这段准备函数中设置 EE Thread 的 bit，从而使 profiler 可以在 Enter/Leave 钩子内部调用同步 Info 函数。

### 异步（Asynchronous）

异步 Info 函数是那些可以在任何时候调用（在回调内或回调外都可）的函数。异步 Info 函数相对很少，它们通常是劫持式采样 profiler（例如 Visual Studio profiler）希望在采样点调用的那类函数。一个标注为异步的 Info 函数必须能够在任何可能的调用栈上执行，这是至关重要的：线程可能在持有任意数量的锁（自旋锁、thread store lock、OS heap lock 等）时被打断，然后又被 profiler 强制通过异步 Info 函数重新进入运行时，这很容易导致死锁或数据破坏。

异步 Info 函数保证自身安全一般有两种方式：

- 尽可能非常、非常简单：不加锁、不触发 GC、不访问可能不一致的数据等。或者
- 如果必须更复杂，则在开头进行充分的检查，确保锁、数据结构等处于安全状态后再继续。
    - 通常这包括询问当前线程是否处于 forbid suspend thread region 内，若是则返回错误并退出；但这在所有情况下都不一定足够。
    - DoStackSnapshot 就是一个复杂异步函数的例子。它结合了多种检查（包括检查当前线程是否在 forbid suspend thread region 内）来决定继续还是退出。

契约
-----

每一个 Info 函数开头也必须有一些共同的“样板”。例如：

```
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;
    PROFILER_TO_CLR_ENTRYPOINT_SYNC((LF_CORPROF,
                                     LL_INFO1000,
                                     "**PROF: EnumModuleFrozenObjects 0x%p.\n",
                                     moduleID));
```

下面是各类契约的“首选（preferred）”值。注意它们与回调的首选值大多不同！如果这让你困惑，请回到第 2 节再读一遍。

| Preferred | Why | Details |
| --------- | --- | ------- |
| NOTHROW | 让 profiler 更容易调用；profiler 不需要自己 try/catch。 | 如果被调函数都是 NOTHROW，就使用 NOTHROW；否则，把自己标为 THROWS 往往比自己搭 try/catch 更好，因为 profiler 可以更高效地把多个 Info 调用放在同一个 try 块中。 |
| GC_NOTRIGGER | 让 profiler 能在更多场景安全调用 | 要尽量避免触发。如果某个 Info 函数“可能”触发（例如在类型尚未加载时触发加载），请尽量提供一种方式让 profiler 指定“不走触发路径”（例如 fAllowLoad 参数可设为 FALSE），并据此做条件化契约标注。 |
| MODE_ANY | 让 profiler 能在更多场景安全调用 | 如果参数或返回值包含 ObjectID，则 MODE_COOPERATIVE 是合理的；否则强烈建议 MODE_ANY。 |
| CANNOT_TAKE_LOCK | 让 profiler 能在更多场景安全调用 | 确保你的被调函数不加锁；如果必须加锁，请准确注释会获取哪些锁。 |
| 可选：EE_THREAD_NOT_REQUIRED | 允许 profiler 在 GC 回调以及 profiler 自建线程（如采样线程）中使用该 Info 函数。 | 这些契约目前尚未被强制执行，因此留空也没问题。如果你很确定该 Info 函数不需要（或不会调用需要）当前 EE Thread，可将 EE_THREAD_NOT_REQUIRED 作为提示写上，供未来线程契约被强制执行时参考。 |

下面是一个“没那么 yay”但带有注释的契约示例：

```
    CONTRACTL
    {
        // ModuleILHeap::CreateNew throws
        THROWS;

        // AppDomainIterator::Next calls AppDomain::Release which can destroy AppDomain, and
        // ~AppDomain triggers, according to its contract.
        GC_TRIGGERS;

        // Need cooperative mode, otherwise objectId can become invalid
        if (GetThreadNULLOk() != NULL) { MODE_COOPERATIVE;  }

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Generics::GetExactInstantiationsFromCallInformation eventually
        // reads metadata which causes us to take a reader lock.
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;
```

入口点宏
--------

契约之后应当有一个入口点宏。它负责日志记录；对同步函数而言，还会检查回调状态标志来强制其必须真的在回调内调用。根据 Info 函数是同步、异步，还是只能在 Initialize 回调中调用，选择如下宏之一：

- PROFILER_TO_CLR_ENTRYPOINT_**SYNC** （典型选择）
- PROFILER_TO_CLR_ENTRYPOINT_**ASYNC**
- PROFILER_TO_CLR_ENTRYPOINT_CALLABLE_ON_INIT_ONLY

如上所述，异步 Info 方法很少，并且要求更高。对异步方法而言，上面给出的首选契约值更加“必须”，其中这两项是硬性要求：GC_NOTRIGGER 与 MODE_ANY。CANNOT_TAKE_LOCK 虽然对异步方法更强烈推荐，但并非总能做到；具体应对方式见上面的 _Asynchronous_ 小节。

你需要修改的文件
=================

要添加或修改方法，去哪里改其实相当直接，做代码查阅就足以弄明白。下面是你需要去的地方：

corprof.idl
-----------

所有 Profiling API 接口与类型都定义在 [src\inc\corprof.idl](https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/corprof.idl)。应首先在这里定义你的类型与方法。

EEToProfInterfaceImpl.*
-----------------------

对 profiler 的 ICorProfilerCallback 实现的包装器位于 [src\vm\EEToProfInterfaceImpl.*](https://github.com/dotnet/runtime/tree/main/src/coreclr/vm)。

ProfToEEInterfaceImpl.*
-----------------------

ICorProfilerInfo 的实现位于 [src\vm\ProfToEEInterfaceImpl.*](https://github.com/dotnet/runtime/tree/main/src/coreclr/vm)。
