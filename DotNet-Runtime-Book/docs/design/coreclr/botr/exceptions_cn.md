运行时里每个开发者都需要了解的异常知识
========================================

日期：2005

在 CLR 中谈到“异常（exceptions）”时，有一个重要区别需要牢记：一类是托管异常（managed exceptions），应用程序通过诸如 C# 的 try/catch/finally 之类的机制接触到它们，运行时为实现这些机制提供了整套机器；另一类则是运行时自身内部使用异常的方式。大多数运行时开发者很少需要去思考如何构建并对外暴露托管异常模型；但每一个运行时开发者都必须理解异常在运行时实现中的用法。需要明确区分时，本文将把托管应用可能抛出或捕获的异常称为 _托管异常_（managed exceptions），把运行时用于自身错误处理的异常称为 _CLR 的内部异常_（internal exceptions）。不过在多数情况下，本文讨论的都是 CLR 的内部异常。

异常在哪些地方重要？
====================

异常几乎无处不在。它们在显式抛出或捕获异常的函数里最为重要，因为这些代码必须被明确编写为：要么抛出异常，要么捕获并正确处理异常。即使某个函数自身并不抛异常，它也可能调用一个会抛异常的函数，因此该函数必须被写成：当异常从它内部穿过时，它仍能表现正确。谨慎地使用 _holders_（持有者）可以极大降低正确编写此类代码的难度。

为什么 CLR 的内部异常不同？
===========================

CLR 的内部异常很像 C++ 异常，但并不完全相同。CoreCLR 可以在 Mac OSX、Linux、BSD 与 Windows 上构建。操作系统与编译器的差异决定了我们不能直接使用标准 C++ 的 try/catch。除此之外，CLR 的内部异常还提供了类似托管世界的 “finally” 与 “fault” 的特性。

借助一些宏，我们可以写出几乎与标准 C++ 一样易写、易读的异常处理代码。

捕获一个异常
============

EX_TRY
------

当然，最基础的宏是 EX_TRY / EX_CATCH / EX_END_CATCH，用起来大概像这样：

```
    EX_TRY
      // 调用某个函数。它可能会抛异常。
      Bar();
    EX_CATCH
      // 如果执行到这里，说明出错了。
      m_finalDisposition = terminallyHopeless;
      RethrowTransientExceptions();
    EX_END_CATCH
```

EX_TRY 宏只是引入 try 块，类似 C++ 的 “try”，但它也会包含一个左大括号 “{”。

EX_CATCH
--------

EX_CATCH 宏结束 try 块（包括右大括号 “}”），并开始 catch 块。与 EX_TRY 一样，它也会用一个左大括号开始 catch 块。

而这与 C++ 异常最大的差异在于：CLR 开发者不能指定要捕获什么。事实上，这组宏会捕获一切——包括 AV 这类非 C++ 异常，甚至托管异常。如果某段代码只需要捕获某一种异常或某个子集，那么它必须先捕获、检查异常，然后把不相关的异常重新抛出。

值得再次强调：EX_CATCH 宏会捕获所有异常。这种行为往往并不是函数真正需要的。接下来两个小节会讨论如何处理那些本不该被捕获的异常。

GET_EXCEPTION() & GET_THROWABLE()
---------------------------------

那么，CLR 开发者如何得知捕获到的到底是什么，并决定该怎么做呢？这取决于需求，有多种选项。

首先，无论捕获到的（C++）异常是什么，它都会以某个派生自全局 Exception 基类的实例形式被交付。其中一些派生类很直观，例如 OutOfMemoryException；一些比较领域特定，例如 EETypeLoadException；还有一些只是对另一套系统异常的包装，例如 CLRException（内部持有一个 OBJECTHANDLE，引用任意托管异常）或 HRException（包装一个 HRESULT）。如果原始异常并非派生自 Exception，这些宏会把它包装成一个派生自 Exception 的对象。（注意：这些异常类型都是系统提供且众所周知的。_除非与 Core Execution Engine Team 协作，否则不应添加新的异常类！_）

其次，每个 CLR 内部异常都会关联一个 HRESULT。有时（如 HRException）该值来自 COM；但内部错误与 Win32 API 失败也都会有 HRESULT。

最后，因为 CLR 内部的大多数异常都可能最终被传递回托管代码，所以存在一套从内部异常映射回对应托管异常的机制。托管异常对象不一定真的会被创建，但总是可以获取它。

因此，基于这些特性，CLR 开发者应当如何对异常分类？

很多时候，只需要用对应异常的 HRESULT 来分类，而获取它非常简单：

```
    HRESULT hr = GET_EXCEPTION()->GetHR();
```

更多信息往往更方便地存在于托管异常对象中。如果异常会被传回托管代码——无论是立即传回，还是缓存起来稍后传回——托管对象当然是必需的。获取异常对象同样很容易。当然，它是一个托管 objectref，因此需要遵守所有常规规则：

```
    OBJECTREF throwable = NULL;
    GCPROTECT_BEGIN(throwable);
    // . . .
    EX_TRY
        // . . . 做一些可能抛异常的事
    EX_CATCH
        throwable = GET_THROWABLE();
        RethrowTransientExceptions();
    EX_END_CATCH
    // . . . 对 throwable 做点什么
    GCPROTECT_END()
```

有时确实无法避免需要 C++ 异常对象，不过这大多发生在异常实现内部。如果必须精确知道 C++ 异常类型，CLR 提供了一组轻量的 RTTI-like 函数用于辅助分类。例如：

```
    Exception *pEx = GET_EXCEPTION();
    if (pEx->IsType(CLRException::GetType())) {/* ... */}
```

这可以判断异常是否为（或派生自）CLRException。

RethrowTransientExceptions
-------------------------

在上面的示例中，“RethrowTransientExceptions” 是 EX_CATCH 块里的一个宏，它是三种预定义宏之一，可视为“异常处置（exception disposition）”。这些宏及其含义如下：

- _RethrowTerminalExceptions_。更好的名字其实是 “RethrowThreadAbort”，因为它做的就是这件事。
- _RethrowTransientExceptions_。“瞬态（transient）异常”的最佳定义是：这类异常如果重试一次（可能换个上下文）就可能不再发生。瞬态异常包括：
  - COR_E_THREADABORTED
  - COR_E_THREADINTERRUPTED
  - COR_E_THREADSTOP
  - COR_E_APPDOMAINUNLOADED
  - E_OUTOFMEMORY
  - HRESULT_FROM_WIN32(ERROR_COMMITMENT_LIMIT)
  - HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY)
  - (HRESULT)STATUS_NO_MEMORY
  - COR_E_STACKOVERFLOW
  - MSEE_E_ASSEMBLYLOADINPROGRESS

如果 CLR 开发者对该用哪个宏心存疑虑，大概率应该选择 _RethrowTransientExceptions_。

不过无论如何，编写 EX_CATCH 块的开发者都需要认真思考：哪些异常应该被捕获，并且应当只捕获这些异常。而由于这些宏无论如何都会捕获全部异常，不捕获某个异常的唯一方式就是把它重新抛出。

## EX_CATCH_HRESULT

有时我们只需要异常对应的 HRESULT，尤其是在 COM 接口实现里。在这类场景中，EX_CATCH_HRESULT 比手写一个完整的 EX_CATCH 块更简单。典型用法如下：

```
    HRESULT hr;
    EX_TRY
      // code
    EX_CATCH_HRESULT (hr)

    return hr;
```

_然而，虽然它很诱人，但并不总是正确_。EX_CATCH_HRESULT 会捕获所有异常、保存 HRESULT，并吞掉（swallow）该异常。因此，除非函数确实需要吞掉异常，否则 EX_CATCH_HRESULT 并不合适。

EX_RETHROW
----------

如前所述，这些异常宏会捕获所有异常；想要捕获特定异常，唯一方式是先捕获全部，再将除目标异常外的其他异常重新抛出。因此，当异常被捕获、检查、可能记录等之后，如果它不应被捕获，就应当重新抛出。EX_RETHROW 会重新抛出同一个异常。

不捕获异常
==========

经常会遇到这样一种情况：某段代码并不需要捕获异常，但需要在退出时执行某种清理或补偿操作。Holders 往往非常适合这种场景，但并不总是够用。对于 holders 不足以处理的情况，CLR 提供了两种 “finally” 变体。

EX_TRY_FOR_FINALLY
------------------

当代码退出时需要执行某种补偿操作，try/finally 可能合适。CLR 提供了一组宏来实现 try/finally：

```
    EX_TRY_FOR_FINALLY
      // code
    EX_FINALLY
      // exit and/or backout code
    EX_END_FINALLY
```

**重要**：EX_TRY_FOR_FINALLY 宏基于 SEH 构建，而不是 C++ EH；C++ 编译器不允许在同一个函数里混用 SEH 与 C++ EH。带有自动析构的局部变量需要 C++ EH 才能在析构函数中运行。因此，任何使用 EX_TRY_FOR_FINALLY 的函数都不能再使用 EX_TRY，也不能包含任何带自动析构的局部变量。

EX_HOOK
-------

有时需要补偿代码，但只在异常逃逸时才执行。在这些场景里，EX_HOOK 类似 EX_FINALLY，但 “hook” 子句只在有异常时运行，并且会在 hook 子句结束时自动重新抛出异常。

```
    EX_TRY
      // code
    EX_HOOK
      // 当异常从 “code” 块逃逸时运行的代码。
    EX_END_HOOK
```

这种结构比简单地在 EX_CATCH 中 EX_RETHROW 更好，因为它会重新抛出非栈溢出异常；但对栈溢出异常，它会先捕获（并展开栈），再抛出一个新的栈溢出异常。

抛出异常
========

在 CLR 中抛异常通常就是调用

```
    COMPlusThrow ( < args > )
```

它有多个重载，核心思想是把异常的“种类（kind）”传给 COMPlusThrow。这个“种类”列表由一组对 [rexcep.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/rexcep.h) 的宏展开生成，对应的种类包括 kAmbiguousMatchException、kApplicationException 等。其他参数（重载所需）用于指定资源与替换文本。一般可通过查找报告相似错误的代码来决定使用哪个 kind。

有一些预定义的便捷变体：

COMPlusThrowOOM();
------------------

委托给 ThrowOutOfMemory()，后者会抛出 C++ 的 OOM 异常。它会抛出一个预分配的异常，以避免“因为内存不足而无法分配抛 OOM 异常所需的内存”这种尴尬问题！

当获取该异常对应的托管异常对象时，运行时会首先尝试分配一个新的托管对象 <sup>[1]</sup>；如果分配失败，则返回一个预分配、共享的全局 OOM 托管异常对象。

[1] 毕竟，如果失败的原因是申请一个 2GB 的数组，那么分配一个简单对象也许仍然可行。

COMPlusThrowHR(HRESULT theBadHR);
--------------------------------

它也有多个重载，例如当你有 IErrorInfo 等信息时。里面有一些出人意料地复杂的代码，用于推导某个 HRESULT 对应的是哪种异常。

COMPlusThrowWin32(); / COMPlusThrowWin32(hr);
---------------------------------------------

基本上就是抛出 HRESULT_FROM_WIN32(GetLastError())。

COMPlusThrowSO();
-----------------

抛出一个栈溢出（SO）异常。注意这不是一个硬 SO，而是当继续执行可能导致硬 SO 时我们抛出的异常。

与 OOM 一样，它会抛出一个预分配的 C++ SO 异常对象。与 OOM 不同的是：在获取对应托管对象时，运行时总是返回预分配、共享的全局栈溢出托管异常对象。

COMPlusThrowArgumentNull()
-------------------------

用于抛出“参数 foo 不能为空”的异常的辅助函数。

COMPlusThrowArgumentOutOfRange()
--------------------------------

顾名思义。

COMPlusThrowArgumentException()
-------------------------------

又一种参数无效异常。

COMPlusThrowInvalidCastException(thFrom, thTo)
----------------------------------------------

给定 attempted cast 的 from/to 类型句柄，该辅助函数会生成一个格式良好的异常消息。

EX_THROW
--------

这是一个底层 throw 构造，一般的业务代码并不需要直接使用。许多 COMPlusThrowXXX 函数内部会使用 EX_THROW，其他一些专门的 ThrowXXX 函数也是如此。最好尽量减少直接使用 EX_THROW，以便尽可能把异常机制的细枝末节封装起来。不过当更高层的 Throw 函数都不适用时，直接使用 EX_THROW 也是可以的。

该宏接受两个参数：要抛出的异常类型（某个 C++ Exception 类的子类型），以及一个括号包裹的、用于该异常类型构造函数的参数列表。

直接使用 SEH
============

有少数情况下直接使用 SEH 是合适的。尤其是：如果需要在第一遍（first pass）进行一些处理，也就是在栈尚未展开之前，就必须用 SEH。SEH 的 __try/__except 中的 filter 代码除了决定是否处理异常之外，还可以做任何事情。调试器通知有时就需要在第一遍处理。

filter 代码必须非常小心地编写。一般来说，filter 必须能够应对任意随机且可能不一致的状态。因为 filter 运行于第一遍，而析构函数（dtors）运行于第二遍，holders 尚未运行，因此它们也尚未恢复其状态。

PAL_TRY / PAL_EXCEPT, PAL_EXCEPT_FILTER, PAL_FINALLY / PAL_ENDTRY
-----------------------------------------------------------------

当需要 filter 时，PAL_TRY 家族是 CLR 中可移植的写法。因为 filter 直接使用 SEH，所以它与同一函数中的 C++ EH 不兼容，因此函数中不能有任何 holders。

再次强调：这些都应当很少见。

__try / __except, __finally
---------------------------

在 CLR 中没有充分理由直接使用它们。

> 这里的 **SEH** 指的是 **Structured Exception Handling（结构化异常处理）**。
>
> 它是 Windows 操作系统层面提供的一种异常处理机制，由 MSVC 编译器通过 `__try`、`__except`、`__finally` 等关键字进行扩展支持。在 .NET (CLR) 的底层 C++ 源码中，SEH 被广泛用于处理硬件异常（如内存访问违规 Access Violation）和实现复杂的异常转发逻辑。
>
> 为了让你更清晰地理解这段内容，我将其核心要点拆解如下：
>
> ### 1. 核心概念：什么是“两阶段”处理？
>
> SEH 与普通 C++ `try/catch` 的最大区别在于它将异常处理分为两个阶段：
>
> - 第一遍 (First Pass / Search Phase)：
>   - 当异常发生时，系统会从栈顶向下寻找匹配的处理程序。
>   - 此时**栈还没有展开（Stack Unwind）**。也就是说，发生异常现场的局部变量、调用堆栈都还是原封不动的。
>   - **Filter（过滤器）代码就在这一步运行。** `__except(FilterFunc())` 里的函数会被调用，用来决定是处理这个异常、继续寻找还是忽略。
> - 第二遍 (Second Pass / Unwind Phase)：
>   - 一旦确定了由谁处理，系统会再次遍历栈，执行中间所有的析构函数（Destructors）和 `__finally` 块，最后跳转到处理异常的代码块。
>
> ### 2. 为什么有时必须“直接使用 SEH”？
>
> 文档中提到“直接使用 SEH”通常是为了利用 **第一遍（First Pass）** 的特性：
>
> - **获取最真实的现场：** 此时栈未展开，调试器或异常分析工具可以抓取到发生崩溃瞬间的精确状态（寄存器、内存）。
> - **在“清理”之前做决策：** C++ 的 `catch` 块运行在第二遍，此时局部对象可能已经被析构了。如果你需要在对象被销毁前检查它们的状态，就必须用 SEH 的 Filter。
> - **特殊通知：** 比如 CLR 内部需要通知调试器“异常即将发生”，这就必须在第一遍完成。
>
> ### 3. Filter 代码的风险（为什么必须“非常小心”）
>
> 文档强调 Filter 必须能应对“不一致的状态”。
>
> - **场景：** 假设线程在修改一个复杂的链表时发生了崩溃。
> - **问题：** 因为此时还在第一遍，析构函数和状态恢复逻辑（CLR 里的 Holders）都还没运行。链表现在可能处于一个“断开”的坏状态。
> - **要求：** 你的 Filter 代码如果去读取这个链表，必须非常健壮，不能因为读取了坏数据导致二次崩溃。
>
> ### 4. 关于 PAL_TRY / PAL_EXCEPT
>
> 由于 SEH 是 Windows 特有的，而 .NET 是跨平台的，CLR 引入了 **PAL (Platform Adaptation Layer，平台适配层)**。
>
> - 在 Windows 上，这些宏会翻译成原生的 `__try/__except`。
> - 在 Linux/macOS 上，PAL 通过 `setjmp/longjmp` 和信号处理（Signals）来模拟出类似的“两阶段”异常处理行为。
>
> ### 5. 为什么不能与 Holders / C++ EH 混用？
>
> - **Holders：** 这是 CLR 源码中常用的一种类似 RAII 的智能指针或锁管理工具（利用 C++ 析构函数自动释放资源）。
> - **冲突：** 编译器通常不允许在同一个函数内混合使用 C++ 异常处理（`try/catch`）和原生 SEH（`__try/__except`）。因为它们的栈展开机制不同，混合使用会导致编译器无法确定如何正确生成清理代码。
> - **结论：** 如果一个函数里用了 `PAL_TRY`，这个函数里就不能有任何带析构函数的 C++ 局部对象。
>
> ### 总结
>
> **“虽然我们通常推荐使用更高级的包装，但如果你需要在异常清理发生前（即栈展开前）拦截异常或观察状态，你就得直接用 SEH。但由于 SEH 的运行环境非常原始（状态可能乱七八糟），且与 C++ 自动清理机制冲突，所以你写代码时要像走钢丝一样小心，并且不要在这个函数里用任何依赖析构函数的 C++ 对象。”**

异常与 GC 模式
==============

通过 COMPlusThrowXXX() 抛出异常不会影响 GC 模式，并且在任何模式下都是安全的。异常向外展开回到 EX_CATCH 的过程中，堆栈上的 holders 会被展开，释放资源并恢复状态。到执行恢复至 EX_CATCH 时，holder 保护的状态已经恢复到进入 EX_TRY 时的状态。

跨边界转换（Transitions）
=========================

从托管代码、CLR、COM 服务器以及其他本地代码的角度看，调用约定、内存管理以及异常处理机制之间存在很多可能的转换。关于异常，幸运的是，大多数转换要么完全发生在运行时之外，要么会被自动处理。

对 CLR 开发者来说，日常真正关心的转换只有三类。其他内容属于高级主题，需要了解的人自然知道自己需要了解！

托管代码进入运行时
------------------

这包括 “fcall”、“jit helper” 等。运行时向托管代码报告错误的典型方式是抛出托管异常。因此，如果某个 fcall 函数（直接或间接）引发了托管异常，这是完全没问题的。正常的 CLR 托管异常实现会做“正确的事情”，寻找合适的托管处理器。

另一方面，如果某个 fcall 函数可能做出任何会抛 CLR 内部异常（即某个 C++ 异常）的事情，那么该异常绝不能泄漏回托管代码。为处理这种情况，CLR 提供了 UnwindAndContinueHandler（UACH），这是一套捕获 C++ EH 异常并将其重新作为托管异常抛出的代码。

任何从托管代码被调用、并且可能抛出 C++ EH 异常的运行时函数，都必须用 INSTALL_UNWIND_AND_CONTINUE_HANDLER / UNINSTALL_UNWIND_AND_CONTINUE_HANDLER 将可能抛异常的代码包裹起来。安装 UACH 有不小的开销，因此不应到处都用。一种在性能关键代码中常用的技术是：默认不装 UACH，只在即将抛异常之前再安装。

当抛出 C++ 异常而缺少 UACH 时，典型失败会是在 CPFH_RealFirstPassHandler 中出现 “GC_NOTRIGGER 区域内调用了 GC_TRIGGERS” 的契约违规。要修复这类问题，请查找托管到运行时的转换点，并检查是否安装了 INSTALL_UNWIND_AND_CONTINUE_HANDLER。

运行时代码进入托管代码
----------------------

从运行时进入托管代码的转换有非常强的平台依赖。在 32-bit Windows 平台上，CLR 的托管异常代码要求在进入托管代码之前安装 “COMPlusFrameHandler”。这类转换由高度专门化的 helper 函数处理，它们会负责安装合适的异常处理器。几乎可以肯定，任何典型的新“进入托管代码”的调用都不会走其他路径。

如果缺少 COMPlusFrameHandler，最可能的效果是：目标托管代码里的异常处理逻辑不会执行——没有 finally 块，也没有 catch 块。

运行时代码进入外部本地代码
----------------------------

运行时调用其他本地代码（OS、CRT 或其他 DLL）时可能需要特别关注。关键场景是：外部代码可能引发异常。问题来源于 EX_TRY 宏的实现，尤其是它们如何把非 Exception 类型转换/包装成 Exception。

在 C++ EH 中，可以通过 “catch(...)” 捕获所有异常，但代价是丢失被捕获异常的全部信息。而当捕获 Exception* 时，这些宏有异常对象可供检查；但当捕获到的是其他类型时，就没有对象可检查，宏只能猜测实际的异常类型。而当异常来自运行时之外时，这些宏总会猜错。

当前解决方案是：用一个“callout filter”包裹对外部代码的调用。filter 会捕获外部异常，并把它转换为 SEHException（运行时内部异常之一）。该 filter 已预定义，使用也很简单。但使用 filter 意味着使用 SEH，这当然也就禁止在同一函数里使用 C++ EH。因此，要在一个使用 C++ EH 的函数里添加 callout filter，通常需要把函数拆成两个。

要使用 callout filter，不要写成这样：

```
    length = SysStringLen(pBSTR);
```

而应写成这样：

```
    BOOL OneShot = TRUE;
    struct Param {
        BSTR*  pBSTR;
        int length;
    };
    struct Param param;
    param.pBSTR = pBSTR;

    PAL_TRY(Param*, pParam, &param)
    {
      pParam->length = SysStringLen(pParam->pBSTR);
    }
    PAL_EXCEPT_FILTER(CallOutFilter, &OneShot)
    {
      _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
    }
    PAL_ENDTRY;
```

在一个可能抛异常的外部调用上缺少 callout filter，将总是导致运行时报告错误的异常类型。而且这个被错误报告的类型甚至不总是确定的：如果当前已经有某个托管异常“在途（in flight）”，那么报告的会是那个托管异常；如果当前没有异常，则会报告 OOM。在 checked build 上，缺少 callout filter 往往会触发断言，这些断言信息通常包含文本 “The runtime may have lost track of the type of an exception”。

杂项
====

EX_TRY 实际涉及的宏非常多，其中大多数永远、永远不应该在宏实现之外使用。

> 既然你是高级程序员，我们直接切入本质。
>
> 这段文档主要是在讲 **CLR（公共语言运行时）作为一个混合型引擎，在不同的代码世界（Managed vs Internal C++ vs External Native）之间切换时，如何处理异常模型的“阻抗匹配”问题。**
>
> CLR 内部维护了一套自己的异常体系（基于 C++ 异常），但它必须时刻面对三种不同的异常模型：
> 1.  **Managed Exceptions:** .NET层面的异常（`System.Exception`）。
> 2.  **CLR Internal C++ Exceptions:** 运行时内部定义的 C++ 类（继承自 `Exception` 类）。
> 3.  **External/SEH Exceptions:** 操作系统或其他 DLL 抛出的原生 SEH 或标准 C++ 异常。
>
> 这段文档主要讲这三者在“边界穿越”时如果不做特殊处理，会导致状态损坏或难以调试的 Bug。
>
> ### 1. 托管代码 -> 运行时 (Managed calls FCall/Helper)
>
> **场景：** C# 代码调用了一个底层方法（比如 `String.Length` 或者是某个 JIT Helper），执行流从 Managed 进入了 Native C++。
>
> **核心矛盾：**
> *   C# 只能捕获 `System.Exception`。
> *   CLR 内部的 C++ 代码可能会抛出其内部的 C++ 异常（`Exception*` 类型）。
> *   **如果不处理：** C++ 异常一旦“漏”回托管栈帧，托管代码的异常处理机制（catch/finally）根本看不懂这个 C++ 对象，会导致进程崩溃或行为未定义。
>
> **解决方案 (UACH - UnwindAndContinueHandler)：**
> 这是一个**翻译层**。
> *   它的作用是捕获内部的 C++ 异常，将其转换（Wrap）成一个托管的异常对象，然后重新抛出。
> *   **性能权衡：** 安装这个 Handler 有开销（设计到 `setjmp/longjmp` 或复杂的栈注册），所以不能给所有 FCall 都默认加上。
> *   **GC 契约问题：** 文档提到的 `GC_NOTRIGGER` 违规，是因为如果 C++ 异常直接穿过，会导致 CLR 维护的 "GC 状态机"（当前线程是否允许触发 GC）没有被正确重置（因为析构逻辑被跳过或混乱）。UACH 保证了异常发生时，状态能正确回滚。
>
> ### 2. 运行时 -> 托管代码 (Runtime calls Managed)
>
> **场景：** CLR 内部需要执行一段托管代码（例如调用静态构造函数 `.cctor`，或者反向调用委托）。
>
> **核心矛盾：**
> *   JIT 编译后的代码需要特定的栈帧布局才能让异常机制（两阶段搜索）正常工作。
> *   如果直接从 C++ `call` 指令跳进去，并没有建立“异常处理链表”或“栈帧元数据”。
>
> **解决方案 (COMPlusFrameHandler)：**
> 这是**桩（Stub）**。在进入托管代码前，必须由 Helper 函数在栈上安插一个 `COMPlusFrameHandler`。这相当于告诉异常系统：“嘿，从这儿往上是托管代码的栈帧，如果上面抛异常了，请按照托管代码的规则来搜索 handler。”
> *   **后果：** 如果没装这个，上层托管代码里的 `try/catch` 就像不存在一样，异常会直接穿透。
>
> ### 3. 运行时 -> 外部本地代码 (Runtime calls External/OS)
>
> 这是文档中最晦涩也是最“坑”的部分。
>
> **场景：** CLR 内部 C++ 代码调用 `kernel32.dll` (如 `SysStringLen`) 或 CRT 函数。
>
> **核心矛盾：**
> CLR 内部有一套很“独断”的异常宏（`EX_TRY` / `EX_CATCH`）。这些宏被设计为**只捕获并处理 CLR 自己的 `Exception*` 指针**。
>
> *   **问题所在：** CLR 的宏底层可能用了 `catch(...)` 来兜底。
>     *   如果外部代码抛出了一个 SEH（比如空指针 AV）或者一个未知的 C++ 异常（比如 `std::string`）。
>     *   CLR 的宏捕获了它，但**不知道它是什么**（类型信息丢失）。
> *   **错误的猜测：** 宏一旦丢失类型，就会开始“瞎猜”：
>     *   如果有其他的托管异常正在处理中（In-flight），它就误报是那个异常。
>     *   如果没有，它通常就直接报 `OutOfMemoryException`。这就是为什么有时候你明明是访问违规，日志里却全是 OOM 的原因。
>
> **解决方案 (CallOutFilter)：**
> 必须在调用外部代码时，用一个**消毒层**包裹起来。
> *   **机制：** 使用 `PAL_TRY` (SEH) + `CallOutFilter`。
> *   **流程：**
>     1.  执行外部调用。
>     2.  如果外部抛出 SEH/未知异常，`CallOutFilter` 此时作为 SEH Filter 介入。
>     3.  Filter 识别出这是一个“异类”，立马把它封装成一个 CLR 认识的 `SEHException`（CLR 内部定义的一种异常类型）。
>     4.  然后外层的 `EX_TRY` 就能捕获到一个它认识的 `Exception*` (即那个 `SEHException`)，从而保留了正确的错误信息。
>
> **代码写法的讲究：**
> 文档中那个繁琐的结构体写法：
> ```cpp
> struct Param { ... };
> PAL_TRY(Param*, pParam, &param) { ... }
> ```
> 这是因为 `PAL_TRY` 是基于 SEH 的（在 Windows 上就是 `__try/__except`）。
> **C++ 编译器限制：** 你不能在一个函数里同时使用 C++ 对象析构（C++ EH）和 SEH (`__try`)。
> *   如果你的函数里有 `std::vector` 或智能指针，你就不能直接写 `PAL_TRY`。
> *   所以通常的做法是：**把不安全的外部调用逻辑提取到一个单独的 helper 函数中**，或者像示例那样，用结构体传参，在一个没有 C++ 局部对象的纯 C 风格的代码块里执行调用。
>
> ### 总结
> **CLR 不信任边界。**
>
> *   **Managed 进此时：** 必须把 C++ 异常转译成 Managed 异常 (UACH)。
> *   **进入 Managed 时：** 必须注册栈帧让托管异常机制生效 (COMPlusFrameHandler)。
> *   **出去调 OS 时：** 必须防止 OS 的随机异常搞乱 CLR 的内部异常类型系统，所以要用 SEH Filter 先把异常“消毒”成 CLR 认识的类型 (CallOutFilter)。
>
> #### 为什么有时候程序崩了却报 OOM
>
> 文档里提到了一句话：
>
> > *"如果缺少 callout filter... 如果当前没有异常，则会报告 OOM (OutOfMemoryException)。"*
>
> **现实场景：** 你写了一个 C# 程序，调用了一个第三方的 C++ DLL。那个 C++ DLL 内部写得烂，发生了一个空指针引用（Access Violation）。 按理说，你的 C# 程序应该崩掉，或者捕获到一个 `AccessViolationException`。 **结果：** 你的 C# 程序捕获到了一个 `OutOfMemoryException`（内存不足）。 **你的困惑：** 于是你疯狂查内存泄漏，查了三天三夜发现内存很充足，差点怀疑人生。 **真相：** 正如文档所说，是因为 CLR 内部在转换那个 C++ 错误时，“猜错”了异常类型，把一个未知的崩溃胡乱包装成了 OOM。
>
> #### 为什么 .NET 4.0 以后 `try-catch` 抓不住某些错误了
>
> 你可能遇到过这种情况：C# 代码里写了 `try { ... } catch (Exception ex) { ... }`，但当程序出现“破坏性状态”（如访问非法内存）时，`catch` 根本不执行，程序直接闪退。
>
> 这是因为文档里提到的“两阶段异常”和“状态破坏”。CLR 认为如果发生了 SEH 异常（硬件级错误），栈可能已经坏了，再让你用 C# 的 `catch` 去处理是不安全的。 除非你在方法上加 `[HandleProcessCorruptedStateExceptions]` 属性，这其实就是在告诉 CLR：“我知道这很危险，但请把那些底层的 SEH 异常转换给我，不要直接崩溃。”
