`System.Private.CoreLib` 与调用运行时
===

# 引言

`System.Private.CoreLib.dll` 是用于定义类型系统核心部分以及 .NET Framework 中相当一部分基础类库（Base Class Library, BCL）的程序集。在 .NET Core 中它最初名为 `mscorlib`，虽然代码和文档中的很多地方仍然将其称为 `mscorlib`。本文会尽量坚持使用 `System.Private.CoreLib` 或 CoreLib。基础数据类型位于该程序集中，并且它与 CLR 之间存在紧密耦合。在这里，你将了解 CoreLib 究竟为何/如何特殊，以及如何通过 QCall 和 FCall 方法从托管代码调用 CLR 的基础知识；文中也会讨论 CLR 内部如何反向调用托管代码。

## 依赖关系

由于 CoreLib 定义了诸如 `Object`、`Int32`、`String` 这类基础数据类型，CoreLib 不能依赖其他托管程序集。不过，CoreLib 与 CLR 之间存在很强的依赖关系。CoreLib 中的许多类型需要从本机代码访问，因此许多托管类型的布局既在托管代码中定义，也在 CLR 内部的本机代码中定义。此外，一些字段可能只在 Debug、Checked 或 Release 构建中存在，所以通常必须针对每种构建类型分别编译 CoreLib。

`System.Private.CoreLib.dll` 会分别为 64 位与 32 位构建，并且它暴露的一些公共常量会随位宽不同而不同。通过使用这些常量（例如 `IntPtr.Size`），CoreLib 之上的大多数库无需为了 32 位 vs. 64 位而分别构建。

## 是什么让 `System.Private.CoreLib` 如此特殊？

CoreLib 具有若干独特属性，其中很多都源于它与 CLR 的紧密耦合。

- CoreLib 定义了实现 CLR 虚拟对象系统（Virtual Object System）所必需的核心类型，例如基础数据类型（`Object`、`Int32`、`String` 等）。
- CLR 必须在启动时加载 CoreLib，以加载某些系统类型。
- 由于布局问题，一个进程内同一时间只能加载一个 CoreLib。加载多个 CoreLib 将需要形式化 CLR 与 CoreLib 之间关于行为、FCall 方法以及数据类型布局的契约，并且让该契约在版本之间保持相对稳定。
- CoreLib 的类型被大量用于本机互操作（native interop），并且托管异常应正确映射到本机错误码/格式。
- CLR 的多个 JIT 编译器可能会为 CoreLib 中一小组特定方法做特殊处理以提升性能：包括把某个方法优化掉（例如 `Math.Cos(double)`），或以特殊方式调用某个方法（例如 `Array.Length`，或 `StringBuilder` 中用于获取当前线程的一些实现细节）。
- CoreLib 需要在适当时候通过 P/Invoke 调用本机代码，主要是调用底层操作系统，或偶尔调用平台适配层（platform adaptation layer）。
- CoreLib 还需要调用 CLR 以暴露一些 CLR 特有的能力，例如触发一次垃圾回收、加载类，或以非平凡方式与类型系统交互。这需要在托管代码与 CLR 内部的本机“手动托管”代码之间搭建桥梁。
- CLR 需要调用托管代码：既要调用托管方法，也要访问仅在托管代码中实现的某些功能。

# 托管代码与 CLR 代码之间的接口

重申一下，CoreLib 中托管代码的需求包括：

- 能够在托管代码与 CLR 内部的“手动托管”代码中访问某些托管数据结构的字段。
- 托管代码必须能调用 CLR。
- CLR 必须能调用托管代码。

为实现这些需求，我们需要：一种让 CLR 能在本机代码中指定并可选地验证托管对象布局的方法、一种托管侧调用本机代码的机制、以及一种本机侧调用托管代码的机制。

托管侧调用本机代码的机制还必须支持 `String` 构造函数所使用的特殊托管调用约定：构造函数负责分配对象使用的内存（而不是典型情况下由 GC 先分配，再调用构造函数）。

CLR 内部提供了一个 [`mscorlib` binder](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/binder.cpp)，用于建立非托管类型/字段到托管类型/字段的映射。binder 会查找并加载类，并允许调用托管方法。它还会对托管与本机代码中指定的布局信息做简单校验，以保证其正确性。binder 会确保尝试加载的托管类存在于 mscorlib 中、已被加载，并且字段偏移是正确的；它还需要能够区分不同签名的重载方法。

# 从托管代码调用本机代码

从托管代码调用 CLR 有两种技术。FCall 允许你直接调用 CLR 代码，并且在操作对象方面更灵活，但如果不正确跟踪对象引用，很容易产生 GC 洞（GC hole）。QCall 也允许通过 P/Invoke 调用 CLR，但更难被误用。FCall 在托管代码里表现为 extern 方法，并且设置了 [`MethodImplOptions.InternalCall`](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.methodimploptions) 标志位。QCall 以 `static extern` 方法形式标记，类似常规 P/Invoke，但它指向一个名为 `"QCall"` 的库。

### 在 FCall、QCall、P/Invoke 与“写成托管代码”之间如何选择

首先，记住尽可能多地写托管代码。这样能避免大量潜在的 GC 洞问题，调试体验更好，代码也往往更简单。

过去编写 FCall 的理由大体分三类：语言特性缺失、更高性能、或实现与运行时的独特交互。如今 C# 已拥有你可能从 C++ 获得的几乎所有有用语言特性，包括 unsafe 代码与栈分配缓冲区，这消除了前两类理由。我们过去已经把 CLR 中某些高度依赖 FCall 的部分移植为托管实现（例如 Reflection、部分 Encoding 与 String 操作），并且我们打算继续保持这一势头。

如果你定义一个 FCall 的唯一理由是调用某个本机方法，那么你应该直接使用 P/Invoke 来调用该本机方法。[P/Invoke](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute) 是公开的本机方法接口，应当能以正确方式完成你需要的一切。

如果你仍然需要在运行时内部实现某个特性，考虑是否有办法降低进入本机代码的频率：能否把常见路径用托管实现，仅在一些罕见的边界情况才进入本机？通常来说，让更多逻辑留在托管端是更优的选择。

面向未来，我们更推荐 QCall。只有在“被迫”的情况下才应使用 FCall。所谓“被迫”的情况是：存在一个非常重要的常见短路径（short path）需要优化。这个短路径不应超过几百条指令，不能分配 GC 内存、不能加锁、不能抛出异常（`GC_NOTRIGGER`、`NOTHROWS`）。除此之外，你都应该使用 QCall。

FCall 的设计目标是服务于必须被优化的短路径，它允许显式控制何时建立帧（erecting a frame）。但对很多 API 来说，这种复杂度既容易出错也不值得。QCall 本质上是调用 CLR 的 P/Invoke。如果确实需要 FCall 的性能，考虑创建一个 QCall 并标注 [`SuppressGCTransitionAttribute`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.suppressgctransitionattribute)。

因此，QCall 会自动为 `SafeHandle` 提供一些有利的封送（marshaling）：你的本机方法只需要接收一个 `HANDLE` 类型，就不用担心有人在该方法体执行期间释放这个 handle。若用 FCall 实现则需要 `SafeHandleHolder` 并可能需要保护 `SafeHandle` 等。利用 P/Invoke 封送器可以避免这些额外的样板代码。

## QCall 的功能行为

QCall 很像从 CoreLib 到 CLR 的普通 P/Invoke。不同于 FCall，QCall 会像普通 P/Invoke 那样把所有参数封送为非托管类型。QCall 也会像普通 P/Invoke 一样切换到 GC 抢占模式。这两个特性让 QCall 相比 FCall 更容易可靠地编写。QCall 不容易出现 FCall 常见的 GC 洞与 GC 饥饿（GC starvation）问题。

QCall 参数的首选类型是 P/Invoke 封送器能高效处理的原始类型（`INT32`、`LPCWSTR`、`BOOL`）。注意：`BOOL` 才是 QCall 参数正确的布尔类型口味（flavor）。另一方面，`CLR_BOOL` 才是 FCall 参数正确的布尔类型。

指向常见非托管 EE 结构的指针应当包装为 handle 类型。这样能让托管侧实现具备类型安全，避免到处使用 unsafe C#。示例请参考 [vm\qcall.h][qcall] 中的 AssemblyHandle。

[qcall]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/qcall.h

QCall 中传入/传出对象引用，需要把指向局部变量的指针包装到一个 handle 中。这种方式刻意设计得比较繁琐，能避免则应尽量避免。参见下面示例中的 `StringHandleOnStack`。从 QCall 返回对象（特别是字符串）是唯一一种常见模式：此时“直接传递原始对象”被广泛接受。（关于为什么这种限制能让 QCall 不那么容易产生 GC 洞，请阅读下面的 ["GC Holes, FCall, and QCall"](#gcholes) 一节。）

QCall 应当以 C 风格的方法签名来实现，这会让未来的 AOT 工具更容易把托管侧的 QCall 与本机侧实现连接起来。

### QCall 示例 - 托管侧

不要把注释复制到你实际的 QCall 实现中。这里只用于说明。

```CSharp
class Foo
{
    // All QCalls should have the following DllImport attribute
    [DllImport(RuntimeHelpers.QCall, EntryPoint = "Foo_BarInternal", CharSet = CharSet.Unicode)]

    // QCalls should always be static extern.
    private static extern bool BarInternal(int flags, string inString, StringHandleOnStack retString);

    // Many QCalls have a thin managed wrapper around them to perform
    // as much work prior to the transition as possible. An example would be
    // argument validation which is easier in managed than native code.
    public string Bar(int flags)
    {
        if (flags != 0)
            throw new ArgumentException("Invalid flags");

        string retString = null;
        // The strings are returned from QCalls by taking address
        // of a local variable using StringHandleOnStack
        if (!BarInternal(flags, this.Id, new StringHandleOnStack(ref retString)))
            FatalError();

        return retString;
    }
}
```

### QCall 示例 - 非托管侧

不要把注释复制到你实际的 QCall 实现中。

QCall 入口点需要在 [vm\qcallentrypoints.cpp][qcall-entrypoints] 的表里通过 `DllImportEntry` 宏注册。参见下方 ["Registering your QCall or FCall Method"](#register)。

[qcall-entrypoints]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/qcallentrypoints.cpp

```C++
// All QCalls should be free functions and tagged with QCALLTYPE and extern "C"
extern "C" BOOL QCALLTYPE Foo_BarInternal(int flags, LPCWSTR wszString, QCall::StringHandleOnStack retString)
{
    // All QCalls should have QCALL_CONTRACT.
    // It is alias for THROWS; GC_TRIGGERS; MODE_PREEMPTIVE.
    QCALL_CONTRACT;

    // Optionally, use QCALL_CHECK instead and the expanded form of the contract
    // if you want to specify preconditions:
    // CONTRACTL {
    //     QCALL_CHECK;
    //     PRECONDITION(wszString != NULL);
    // } CONTRACTL_END;

    // The only line between QCALL_CONTRACT and BEGIN_QCALL
    // should be the return value declaration if there is one.
    BOOL retVal = FALSE;

    // The body has to be enclosed in BEGIN_QCALL/END_QCALL macro.
    // It is necessary for exception handling.
    BEGIN_QCALL;

    // Argument validation would ideally be in managed, but in some cases
    // needs to be done in native. If argument validation is done in
    // managed asserting in native is warranted.
    _ASSERTE(flags != 0);

    // No need to worry about GC moving strings passed into QCall.
    // Marshalling pins them for us.
    printf("%S\n", wszString);

    // This is the most efficient way to return strings back
    // to managed code. No need to use StringBuilder.
    retString.Set(L"Hello");

    // You can not return from inside of BEGIN_QCALL/END_QCALL.
    // The return value has to be passed out in helper variable.
    retVal = TRUE;

    END_QCALL;

    return retVal;
}
```

## FCall 的功能行为

FCall 在传递对象引用方面更灵活，但代码复杂度更高，也更容易出错。此外，对于任何非平凡长度（non-trivial length）的 FCall，都需要显式轮询是否必须触发一次垃圾回收。如果做不到这一点，那么当托管代码在紧密循环中反复调用该 FCall 方法时，会导致饥饿问题：因为 FCall 在执行期间线程只以协作方式（cooperative）允许 GC 运行。

FCall 需要大量样板代码，这里难以尽述。细节请参阅 [fcall.h][fcall]。

[fcall]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/fcall.h

### <a name="gcholes"></a> GC 洞、FCall 与 QCall

关于 GC 洞的更完整讨论可见 [CLR Code Guide](../../../coding-guidelines/clr-code-guide.md)。请查看 ["Is your code GC-safe?"](../../../coding-guidelines/clr-code-guide.md#2.1)。这一小节的讨论会进一步解释：为什么 FCall 与 QCall 会有一些看起来很奇怪的约定。

作为参数传给 FCall 的对象引用并不会受到 GC 保护，这意味着一旦发生 GC，这些引用仍会指向对象在旧地址处的内存，而不是新地址。因此，FCall 通常遵循这样的纪律：以类似 `StringObject*` 作为参数类型，然后在可能触发 GC 的操作之前，显式把它转换为 `STRINGREF`。如果你期望稍后再次使用某个对象引用，那么在触发 GC 前必须对对象引用进行 GC 保护。

没有正确报告 `OBJECTREF`，或者没有更新内部指针（interior pointer），通常被称为“GC 洞（GC hole）”。原因是：`OBJECTREF` 类在 Debug 和 Checked 构建中，每次解引用时都会做一些校验以确保它指向一个有效对象。当一个指向无效对象的 `OBJECTREF` 被解引用时，会触发断言，提示类似“Detected an invalid object reference. Possible GC hole?”。在编写“手动托管”代码时，这个断言不幸地很容易踩到。

请注意：QCall 的编程模型比较受限，目的就是通过强制你传入“栈上对象引用的地址”来避开 GC 洞。这保证了对象引用会被 JIT 的上报逻辑当作 GC 保护对象，并且对象引用本身不会移动，因为它并不分配在 GC 堆上。QCall 是我们推荐的方案，正是因为它让 GC 洞更难被写出来。

### x86 的 FCall 尾声（epilog）遍历器

托管堆栈遍历器需要能够从 FCall 返回并继续找到调用方。在较新的平台上这相对容易，因为 ABI 已经把堆栈展开（unwinding）约定定义为 ABI 的一部分。但 x86 的 ABI 并未定义堆栈展开约定。运行时通过实现一个尾声遍历器（epilog walker）来绕过这一点。尾声遍历器通过模拟 FCall 的执行，计算 FCall 返回地址以及被调用方保存寄存器（callee-save registers）。这会限制 FCall 实现中允许使用的构造。

在 FCall 实现中使用带析构函数的栈分配对象或异常处理等复杂构造，可能会让尾声遍历器困惑，导致 GC 洞或在堆栈遍历期间崩溃。并不存在一份全面的“必须避免哪些构造”清单来彻底防止这类 bug。某个 FCall 实现今天还没问题，可能在下一次 C++ 编译器更新后就出问题了。我们依赖压力测试与代码覆盖来发现该领域的 bug。

### FCall 示例 – 托管侧

下面是来自 `String` 类的一个真实示例：

```CSharp
public partial sealed class String
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    private extern string? IsInterned();

    public static string? IsInterned(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        return str.IsInterned();
    }
}
```

### FCall 示例 – 非托管侧

FCall 入口点需要在 [vm\ecalllist.h][ecalllist] 中通过 `FCFuncEntry` 宏在表里注册。参见 ["Registering your QCall or FCall Method"](#register)。

[ecalllist]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/ecalllist.h

这个示例展示了一个接收托管对象（`Object*`）作为原始指针的 FCall 方法。这些原始输入被视为“不安全”，如果在 GC 敏感上下文中使用，就必须进行校验或转换。

```C++
FCIMPL1(FC_BOOL_RET, ExceptionNative::IsImmutableAgileException, Object* pExceptionUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pExceptionUNSAFE != NULL);

    OBJECTREF pException = (OBJECTREF) pExceptionUNSAFE;

    FC_RETURN_BOOL(CLRException::IsPreallocatedExceptionObject(pException));
}
FCIMPLEND
```

## <a name="register"></a> 注册你的 QCall 或 FCall 方法

CLR 必须知道你的 QCall 和 FCall 方法的名字：既包括托管侧的类名与方法名，也包括要调用的本机方法。对 FCall 而言，注册在 [ecalllist.h][ecalllist] 中完成，涉及两个数组。第一个数组把命名空间与类名映射到一个“函数元素数组”。该函数元素数组再把具体的方法名与签名映射到函数指针。

假设我们定义了上面示例中的 `String.IsInterned()` 的 FCall。首先，我们需要确保针对 String 类存在一个函数元素数组。

``` C++
// Note these have to remain sorted by name:namespace pair
    ...
    FCClassElement("String", "System", gStringFuncs)
    ...
```

其次，我们必须确保 `gStringFuncs` 中包含 `IsInterned` 的正确条目。注意：如果某个方法名有多个重载，我们可以指定签名：

```C++
FCFuncStart(gStringFuncs)
    ...
    FCFuncElement("IsInterned", AppDomainNative::IsStringInterned)
    ...
FCFuncEnd()
```

QCall 则在 [qcallentrypoints.cpp][qcall-entrypoints] 的 `s_QCall` 数组中通过 `DllImportEntry` 宏注册，如下所示：

```C++
static const Entry s_QCall[] =
{
    ...
    DllImportEntry(MyQCall),
    ...
};
```

## 命名约定

FCall 与 QCall 不应公开暴露。相反，应包装实际的 FCall/QCall，并提供一个经过 API 审批的名称。

内部 FCall 或 QCall 应使用 `Internal` 后缀，以便把 FCall/QCall 的名字与公共入口点区分开（例如公共入口点做错误检查，然后调用具有完全相同签名的共享 worker 函数）。这与在纯托管 BCL 中处理同类情况没有区别。

# 具有托管/非托管二重性的类型

某些托管类型必须在托管与本机代码中都提供一种表示。你可以问：一个类型的权威定义究竟在托管代码里，还是在 CLR 的本机代码里——但答案并不重要。关键在于：两者必须完全一致。这将使 CLR 本机代码能够快速、高效地访问托管对象中的字段。

还有一种更复杂的方式：使用与反射类似的机制，在 `MethodTable` 与 `FieldDesc` 之上读取字段值。但这种方式性能不够理想，也不太好用。对常用类型而言，在本机代码中声明一个数据结构并保持两边同步是更合理的做法。

CLR 为此提供了一个 binder。在你定义了托管与本机类之后，你应给 binder 提供一些线索，以帮助确保字段偏移保持一致，从而能快速发现有人不小心只在其中一个定义里添加了字段。

在 [corelib.h][corelib.h] 中，使用以 `"_U"` 结尾的宏来描述某个类型、托管代码中的字段名，以及对应本机数据结构中的字段名。此外，你还可以指定一个方法列表，并在之后尝试调用这些方法时按名称引用它们。

[corelib.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/corelib.h

``` C++
DEFINE_CLASS_U(SAFE_HANDLE,         Interop,                SafeHandle,         SafeHandle)
DEFINE_FIELD(SAFE_HANDLE,           HANDLE,                 handle)
DEFINE_FIELD_U(SAFE_HANDLE,         STATE,                  _state,                     SafeHandle,            m_state)
DEFINE_FIELD_U(SAFE_HANDLE,         OWNS_HANDLE,            _ownsHandle,                SafeHandle,            m_ownsHandle)
DEFINE_FIELD_U(SAFE_HANDLE,         INITIALIZED,            _fullyInitialized,          SafeHandle,            m_fullyInitialized)
DEFINE_METHOD(SAFE_HANDLE,          GET_IS_INVALID,         get_IsInvalid,              IM_RetBool)
DEFINE_METHOD(SAFE_HANDLE,          RELEASE_HANDLE,         ReleaseHandle,              IM_RetBool)
DEFINE_METHOD(SAFE_HANDLE,          DISPOSE,                Dispose,                    IM_RetVoid)
DEFINE_METHOD(SAFE_HANDLE,          DISPOSE_BOOL,           Dispose,                    IM_Bool_RetVoid)
```

然后，你可以使用 `REF<T>` 模板创建诸如 `SAFEHANDLEREF` 这样的类型名。`OBJECTREF` 的所有错误检查都内建在 `REF<T>` 模板中，你可以自由地解引用 `SAFEHANDLEREF` 并使用其字段。但你仍然必须对这些引用进行 GC 保护。

# 从非托管代码调用托管代码

显然，CLR 本机侧有时需要调用托管代码。为此，我们添加了 `MethodDescCallSite` 类来处理许多管线化细节。概念上，你需要做的就是：找到你要调用方法的 `MethodDesc*`、找到一个托管对象作为 `this` 指针（如果你要调用实例方法）、传入一个参数数组，并处理返回值。内部实现还需要可能切换线程状态以允许 GC 在抢占模式下运行等。

下面给出一个简化示例。注意：这个例子使用了上一节描述的 binder，通过它来调用 `SafeHandle` 的虚方法 `ReleaseHandle`。

```C++
void SafeHandle::RunReleaseMethod(SafeHandle* psh)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    SAFEHANDLEREF sh(psh);

    GCPROTECT_BEGIN(sh);

    MethodDescCallSite releaseHandle(s_pReleaseHandleMethod, METHOD__SAFE_HANDLE__RELEASE_HANDLE, (OBJECTREF*)&sh, TypeHandle(), TRUE);

    ARG_SLOT releaseArgs[] = { ObjToArgSlot(sh) };
    if (!(BOOL)releaseHandle.Call_RetBool(releaseArgs)) {
        MDA_TRIGGER_ASSISTANT(ReleaseHandleFailed, ReportViolation)(sh->GetTypeHandle(), sh->m_handle);
    }

    GCPROTECT_END();
}
```

# 与其他子系统的交互

## 调试器

如今 FCall 的一个限制是：你无法在 Visual Studio 的 Interop（或混合模式）调试中轻松同时调试托管代码与 FCall。在 FCall 内设置断点并用 Interop 调试基本不可用。这个问题很可能不会被修复。

# 物理架构

当 CLR 启动时，CoreLib 会由一个名为 `SystemDomain::LoadBaseSystemClasses()` 的方法加载。在这里，会加载基础数据类型与其他类似的类（例如 `Exception`），并设置合适的全局指针来引用 CoreLib 的类型。

对于 FCall，请查看 [fcall.h][fcall] 中的基础设施，以及 [ecalllist.h][ecalllist] 以正确告知运行时你的 FCall 方法。

对于 QCall，请查看 [qcall.h][qcall] 中的相关基础设施，以及 [qcallentrypoints.cpp][qcall-entrypoints] 以正确告知运行时你的 QCall 方法。

更通用的基础设施以及一些本机类型定义可以在 [object.h][object.h] 中找到。binder 使用 `mscorlib.h` 来关联托管与本机类。

[object.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/object.h
