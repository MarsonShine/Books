## 用 C# 写一个 .NET 垃圾回收器（二）

在[第一部分](writing-a-net-gc-in-c-part-1.md)中，我们准备了项目，并修复了由 NativeAOT 工具链引起的初始化问题。在本部分，我们将开始实现自己的 GC（垃圾回收器）。目前的目标是构建一个尽可能简单的 GC，使其能够运行基本的 .NET 应用程序。这个 GC 只会进行内存分配，而不会回收内存，类似于 [Konrad Kokosa 提出的 **bump-pointer GC**](https://github.com/kkokosa/UpsilonGC/tree/master/src/ZeroGC.BumpPointer)（碰撞指针 GC）。

第一步是编写 GC 需要的本地接口。目前有以下四个接口：

- **IGCToCLR**：提供可供 GC 调用的执行引擎 API（例如挂起线程）。
- **IGCHeap**：主要的 GC API。
- **IGCHandleManager**：提供创建或销毁句柄的 API。
- **IGCHandleStore**：句柄管理器使用的底层存储。

其中，**IGCToCLR** 由运行时提供，而其他三个接口需要由 GC 自行实现。

为了处理与本地代码的互操作，我们将使用与托管分析器（managed profiler）相同的 [**NativeObjects** 库](https://github.com/kevingosse/NativeObjects)。我们需要做的就是在 C# 中定义接口，并使用 `[NativeObject]` 特性进行标注：

```c#
[NativeObject]
public unsafe interface IGCHeap
{
    /// <summary>
    /// Returns whether or not the given size is a valid segment size.
    /// </summary>
    bool IsValidSegmentSize(nint size);

    /// <summary>
    /// Returns whether or not the given size is a valid gen 0 max size.
    /// </summary>
    bool IsValidGen0MaxSize(nint size);

    /// <summary>
    /// Gets a valid segment size.
    /// </summary>
    nint GetValidSegmentSize(bool large_seg = false);

    [...]
}
```

接下来，我们可以通过两种方式获取接口的实现：

1. 获取托管实现的本地指针（native pointer）。

   ```c#
   var gcHeap = new GCHeap();
   IntPtr gcHeapPtr = NativeObjects.IGCHeap.Wrap(gcHeap);
   // Give the pointer to native code
   // ...
   ```

2. 获取本地实现的托管指针（managed pointer）。

   ```c#
   // Receive the pointer from native code
   IntPtr ptr = ...
   var gcHeap = NativeObjects.IGCHeap.Wrap(ptr);
   // Use gcHeap like a normal managed object
   ```

这些托管接口本质上是 [.NET 源代码中 C++ 接口的 C# 转换版本](https://github.com/dotnet/runtime/blob/main/src/coreclr/gc/gcinterface.h)，因此这里不再详细介绍。

## 句柄存储（Handle Store）

GC 句柄（GC Handles）在 .NET 运行时中是一个非常基础的概念，即便是我们这个简单的 GC 也需要提供一定程度的支持。幸运的是，由于我们的 GC **不会释放或移动内存**，我们目前可以采用相对简单的实现方式。

一个 GC 句柄包含以下三部分信息：

1. **句柄类型**（弱引用、强引用、固定引用（pinned）等）。
2. **对象地址**（句柄所指向的对象的内存地址）。
3. **一个指针大小的额外信息字段**（用于存储额外的元数据）。

因此，我们可以用一个结构体来表示 GC 句柄：

```c#
[StructLayout(LayoutKind.Sequential)]
public struct ObjectHandle
{
    public nint Object;
    public nint ExtraInfo;
    public HandleType Type;

    public override string ToString() => $"{Type} - {Object:x2} - {ExtraInfo:x2}";
}

public enum HandleType
{
    HNDTYPE_WEAK_SHORT = 0,
    HNDTYPE_WEAK_LONG = 1,
    HNDTYPE_WEAK_DEFAULT = 1,
    HNDTYPE_STRONG = 2,
    HNDTYPE_DEFAULT = 2,
    HNDTYPE_PINNED = 3,
    HNDTYPE_VARIABLE = 4,
    HNDTYPE_REFCOUNTED = 5,
    HNDTYPE_DEPENDENT = 6,
    HNDTYPE_ASYNCPINNED = 7,
    HNDTYPE_SIZEDREF = 8,
    HNDTYPE_WEAK_NATIVE_COM = 9
}
```

目前，并非所有句柄类型都需要额外信息，因此理论上，我们可以为不同的句柄类型设计专门的结构体来节省空间。但对于我们这个简单的 GC 来说，这并不重要，因此不会做这种优化。

目前，我们可以使用一个**固定大小的数组**来存储句柄。为了简化实现，我们将句柄的最大数量硬编码为 **10,000**，这个数量足以支持测试应用程序的运行。然而，由于我们的 GC **不会回收内存**，这意味着对于运行时间较长的应用程序，句柄总会被耗尽。因此，未来我们可能需要重新设计这部分逻辑。

```c#
public unsafe class GCHandleStore : IGCHandleStore
{
    private const int MaxHandles = 10_000;

    private readonly NativeObjects.IGCHandleStore _nativeObject;
    private readonly ObjectHandle* _store;
    private int _handleCount;

    public GCHandleStore()
    {
        _nativeObject = NativeObjects.IGCHandleStore.Wrap(this);
        _store = (ObjectHandle*)NativeMemory.AllocZeroed(MaxHandles, (nuint)sizeof(ObjectHandle));
    }

    public IntPtr IGCHandleStoreObject => _nativeObject;

    // TODO: Implement IGCHandleStore methods
}
```

`IGCHandleStoreObject` 属性用于暴露**本地指针**（native pointer），以便将其传递给 .NET 运行时。

> ### 关于句柄存储的设计选择
>
> 在实现过程中，我曾考虑过使用**固定的 `ObjectHandle[]` 数组**（并通过 `pinned` 关键字固定其地址）来替代直接使用本地内存。然而，最终我认为这种做法有些“投机取巧”（cheating），因此决定仅依赖 NativeAOT GC **来管理元数据结构**，而所有暴露给 .NET 运行时的内容都应当由我们手动管理并存储在本地内存中。

现在我们可以实现句柄生成。`IGCHandleStore` 接口的多个方法都运行相同的逻辑：

```c#
public unsafe ref ObjectHandle CreateHandleOfType(GCObject* obj, HandleType type)
{
	return ref CreateHandleWithExtraInfo(obj, type, null);
}

public unsafe ref ObjectHandle CreateHandleOfType2(GCObject* obj, HandleType type, int heapToAffinitizeTo)
{
	return ref CreateHandleWithExtraInfo(obj, type, null);
}

public unsafe ref ObjectHandle CreateDependentHandle(GCObject* primary, GCObject* secondary)
{
	return ref CreateHandleWithExtraInfo(primary, HandleType.HNDTYPE_DEPENDENT, secondary);
}

public unsafe ref ObjectHandle CreateHandleWithExtraInfo(GCObject* obj, HandleType type, void* pExtraInfo)
{
    var index = Interlocked.Increment(ref _handleCount) - 1;

    if (index >= MaxHandles)
    {
    Environment.FailFast("Too many handles");
    }

    ref var handle = ref _store[index];

    handle.Object = (nint)obj;
    handle.Type = type;
    handle.ExtraInfo = (nint)pExtraInfo;

    return ref handle;
}
```

`GCObject*` 代表一个指向**托管对象**（managed object）的指针。虽然目前我们不会对其进行解引用（dereference），但 `GCObject` 结构的布局模仿了 .NET 运行时中的托管对象格式。

```c#
[StructLayout(LayoutKind.Sequential)]
public readonly struct GCObject
{
    public readonly IntPtr MethodTable;
    public readonly int Length;
}
```

**IGCHandleStore 接口** 还暴露了一个 `ContainsHandle` 方法，不过在 .NET 运行时中似乎没有地方实际使用它。不过，由于实现起来相对简单，我们还是会提供该方法。此外，我们还添加了一个 `DumpHandles` 方法，以便在调试 GC 时查看当前句柄的状态。

```c#
public void DumpHandles()
{
    Write("GCHandleStore DumpHandles");

    for (int i = 0; i < _handleCount; i++)
    {
        Write($"Handle {i} - {_store[i]}");
    }
}

public bool ContainsHandle(ref ObjectHandle handle)
{
    var ptr = Unsafe.AsPointer(ref handle);
    return ptr >= _store && ptr < _store + _handleCount;
}
```

目前来说，这些实现已经足够满足我们的需求。不过，将来我们可能会引入更复杂的**句柄存储机制**，比如：

- **基于段（segment-based）的存储结构**，可以根据需要动态扩展。
- **空闲列表（free-list）**，用于重用已释放的句柄。

## **句柄管理器（Handle Manager）**

在 .NET 运行时中，句柄的管理涉及两个接口：`IGCHandleManager` 和 `IGCHandleStore`。但为什么需要这两个不同的接口？老实说，我也不太确定。理论上，这两个接口**完全可以合并**。

从历史角度来看，这可能源自 .NET 早期的设计，当时运行时可能存在多个句柄存储（handle store），比如与 **AppDomains** 相关的机制。不过，现在这种情况已经不复存在了。

`IGCHandleManager` 主要提供以下功能：

- 访问底层的 `IGCHandleStore`。
- 读取或修改句柄的元信息。

这样，**执行引擎（execution engine）** 无需关心 GC 在内存中如何存储和管理句柄。

```c#
internal unsafe class GCHandleManager : IGCHandleManager
{
    private readonly NativeObjects.IGCHandleManager _nativeObject;
    private readonly GCHandleStore _gcHandleStore;

    public GCHandleManager()
    {
        _gcHandleStore = new GCHandleStore();
        _nativeObject = NativeObjects.IGCHandleManager.Wrap(this);
    }

    public IntPtr IGCHandleManagerObject => _nativeObject;

    public GCHandleStore Store => _gcHandleStore;

    public bool Initialize()
    {
        return true;
    }

    public IntPtr GetGlobalHandleStore()
    {
        return _gcHandleStore.IGCHandleStoreObject;
    }

    public unsafe ref ObjectHandle CreateGlobalHandleOfType(GCObject* obj, HandleType type)
    {
        return ref _gcHandleStore.CreateHandleOfType(obj, type);
    }

    public ref ObjectHandle CreateDuplicateHandle(ref ObjectHandle handle)
    {
        ref var newHandle = ref _gcHandleStore.CreateHandleOfType((GCObject*)handle.Object, handle.Type);
        newHandle.ExtraInfo = handle.ExtraInfo;
        return ref newHandle;
    }

    public unsafe void SetExtraInfoForHandle(ref ObjectHandle handle, HandleType type, nint extraInfo)
    {
        handle.ExtraInfo = extraInfo;
    }

    public unsafe nint GetExtraInfoFromHandle(ref ObjectHandle handle)
    {
        return handle.ExtraInfo;
    }

    public unsafe void StoreObjectInHandle(ref ObjectHandle handle, GCObject* obj)
    {
        handle.Object = (nint)obj;
    }

    public unsafe bool StoreObjectInHandleIfNull(ref ObjectHandle handle, GCObject* obj)
    {
        var result = InterlockedCompareExchangeObjectInHandle(ref handle, obj, null);        
        return result == null;
    }

    public unsafe void SetDependentHandleSecondary(ref ObjectHandle handle, GCObject* obj)
    {
        handle.ExtraInfo = (nint)obj;
    }

    public unsafe GCObject* GetDependentHandleSecondary(ref ObjectHandle handle)
    {
        return (GCObject*)handle.ExtraInfo;
    }

    public unsafe GCObject* InterlockedCompareExchangeObjectInHandle(ref ObjectHandle handle, GCObject* obj, GCObject* comparandObject)
    {
        return (GCObject*)Interlocked.CompareExchange(ref handle.Object, (nint)obj, (nint)comparandObject);
    }

    public HandleType HandleFetchType(ref ObjectHandle handle)
    {
        return handle.Type;
    }
}
```

**注意**：这里仅展示了我们目前实现的部分方法。其他与**句柄释放**相关的方法暂时不需要，因此未作实现。

## **IGCHeap**

`IGCHeap` 是 GC 的核心接口，通常提到 GC API 时，我们想到的就是它。这个接口非常庞大，包含**多达 88 个方法**，但对于我们的**简化版 GC**，我们只需要实现其中的一部分关键方法。

`GCHeap` 类的构造函数与 `IGCHandleStore` 和 `IGCHandleManager` 的实现类似，主要用于**初始化本地互操作的封装（native interop wrappers）**。

```c#
internal unsafe class GCHeap : Interfaces.IGCHeap
{
    private readonly IGCToCLRInvoker _gcToClr;
    private readonly GCHandleManager _gcHandleManager;
    private readonly IGCHeap _nativeObject;

    public GCHeap(IGCToCLRInvoker gcToClr)
    {
        _gcToClr = gcToClr;
        _gcHandleManager = new GCHandleManager();

        _nativeObject = IGCHeap.Wrap(this);
    }

    public IntPtr IGCHeapObject => _nativeObject;
    public IntPtr IGCHandleManagerObject => _gcHandleManager.IGCHandleManagerObject;
}
```

`Initialize` 方法是 GC 需要实现的第一个关键方法。这个方法会在**运行时初始化的早期**被调用，让 GC 预先准备好托管代码运行所需的一切。在 .NET 的正式 GC 中，`Initialize` 主要用于：

- **计算内存段（segment）或区域（region）的大小**。
- **预分配堆（heap）**。
- **进行必要的内存管理设置**。

不过，由于我们这个 GC 仅仅是**最基础的实现**，所以不需要做这些复杂的初始化。但是，我们仍然需要**设置写屏障（write barrier）**。

> 这个例子很好地展示了**独立 GC API**（Standalone GC API）实际上只是**围绕标准 .NET GC 的一层封装**，而不是一个真正面向自定义 GC 设计的合理 API。即便我们不打算使用**卡表（card table）**，API 依然没有提供关闭或修改**写屏障（write barrier）**的方式，因此我们仍然需要正确地进行初始化。

幸运的是，Konrad Kokosa 在他的 GC 实现中使用了一个**技巧**。在 **Workstation GC** 模式下，写屏障在写入卡表之前会检查目标地址是否**位于 GC 管理的范围内**。我们可以利用这一点，将 GC 的**范围**设置为一个特殊值，使得所有地址都**超出这个范围**，从而**间接禁用**写屏障。

```c#
public HResult Initialize()
{
    Write("Initialize GCHeap");

    var parameters = new WriteBarrierParameters
    {
        operation = WriteBarrierOp.Initialize,
        is_runtime_suspended = true,
        ephemeral_low = -1 // nuint.MaxValue
    };

    _gcToClr.StompWriteBarrier(&parameters);

    return HResult.S_OK;
}
```

我们可以**设置 GC 地址范围的最低值（`ephemeral_low`）为最大可能的地址**，这样所有对象的地址都会超出 GC 监控的范围。这使得写屏障不会执行卡表更新，从而达到**“屏蔽”写屏障的效果**。

如果不使用这个技巧，我们就需要：

- **为卡表分配内存**，并将其赋值给 `WriteBarrierParameters` 结构体的 `card_table` 字段。
- **确保卡表足够大**，可以覆盖整个堆（card table 的每个字节映射 2KB 内存）。

然而，这会带来**两个问题**：

1. **如何管理堆的边界（bookkeeping）？**
   我们使用 `NativeMemory.Alloc` 进行内存分配，而它返回的地址范围是**不可预测**的，因此我们很难计算堆的精确范围。
2. **如何预分配足够大的卡表？**
   如果无法动态调整大小，我们可能需要分配一个**超大**的卡表，以适应可能的堆大小，这会造成内存浪费。

遗憾的是，这个技巧**仅适用于 Workstation GC**。在 **Server GC** 模式下，写屏障不会检查目标地址是否在 GC 管理的范围内，因此**必须提供一个有效的卡表**。目前，我们的方案不会支持 **Server GC**（毕竟对于我们的 GC 来说，**Server GC 这个概念本身就没有意义**，这也是 .NET GC 设计细节“泄漏”到独立 GC API 的又一例证）。

**`Alloc` 是 GC 最核心的方法**，每当线程需要内存来分配新对象时，都会调用它。因此，我们必须引入**“分配上下文”（Allocation Context）**的概念。

如果每次分配对象时，线程都必须向 GC 申请内存，那会导致**严重的性能问题**。为了解决这个问题，GC 采用了**分配上下文（allocation context）**：

- GC 会给每个线程分配一块内存，称为 **“分配上下文”**。
- 线程可以在这个分配上下文中**自行分配对象**，无需每次都请求 GC。
- **只有当分配上下文耗尽**或在**特殊情况下**（如分配带有终结器的对象）时，线程才会请求 GC 进行新的内存分配。

下面是分配上下文的基本结构（即 GC 分配给每个线程的内存块）：

```c#
[StructLayout(LayoutKind.Sequential)]
public unsafe struct gc_alloc_context
{
    public nint alloc_ptr;
    public nint alloc_limit;
    public long alloc_bytes;
    public long alloc_bytes_uoh;

    public void* gc_reserved_1;
    public void* gc_reserved_2;
    public int alloc_count;
}
```

`alloc_ptr` 指向当前分配上下文（allocation context）中的可用位置，而 `alloc_limit` 则标记了分配上下文的结束位置。此外，分配上下文中还包含多个字段，用于跟踪该线程的分配统计信息，以及两个指针大小的字段，这些字段由 GC 自行决定如何使用。在标准 GC 中，这些字段用于标记线程关联的堆（heap affinity），我在[之前的文章](https://minidump.net/dumping-the-managed-heap-in-csharp/)中也曾利用过这一点。

现在来说，我们的分配策略保持简单：

1. 当调用 `Alloc` 时，首先检查当前分配上下文是否还有足够的空间：
   - 如果有空间，我们只需递增 `alloc_ptr`，即可完成对象分配。
   - 这种方式避免了频繁向 GC 申请内存，提高了分配效率。
2. 如果分配上下文不够大：
   - 我们分配32KB 的新内存块，作为新的分配上下文。
   - 特殊情况：如果要分配的对象大于 **32KB**，则直接**分配精确大小的内存块**，而不是使用标准的 32KB 块。

```c#
public GCObject* Alloc(ref gc_alloc_context acontext, nint size, GC_ALLOC_FLAGS flags)
{
    var result = acontext.alloc_ptr;
    var advance = result + size;

    if (advance <= acontext.alloc_limit)
    {
    // The allocation context is big enough for this allocation
    acontext.alloc_ptr = advance;
    return (GCObject*)result;
    }

    // The allocation context is too small, we need to allocate a new one
    var growthSize = Math.Max(size, 32 * 1024) + IntPtr.Size;
    var newPages = (IntPtr)NativeMemory.AllocZeroed((nuint)growthSize);

    var allocationStart = newPages + IntPtr.Size;
    acontext.alloc_ptr = allocationStart + size;
    acontext.alloc_limit = newPages + growthSize;

    return (GCObject*)allocationStart;
}
```

你可能会注意到，我们对**分配上下文的起始地址进行了偏移**（即 `alloc_ptr` 向后移动 `IntPtr.Size` 字节）。其原因如下：

- `Alloc` 需要返回一个 `GCObject*`，即指向**托管对象**的指针。
- 在 .NET 中，**托管对象的引用并不会直接指向对象的起始位置**，而是指向**对象的 Method Table 指针**。
- 对象的起始位置前面有一个指针大小的字段，用于存储**对象头（Object Header）**。
- 因此，我们在分配对象时，需要偏移 `IntPtr.Size`，确保返回的指针正确指向 Method Table。

```
+-----------------+ 
| Object header   | 
+-----------------+
| MethodTable*    |   <----- GCObject*
+-----------------+
|                 |
| Data            |
|                 |
+-----------------+
```

这个设计确保了 `.NET` 运行时在访问对象时，能够正确地找到该对象的类型信息和元数据。

`IGCHeap` 接口中，我们还需要实现最后一个方法：**`GarbageCollect`**。
目前，我们不会真的执行垃圾回收（GC），但可以**用它来输出调试信息**，例如：

- **转储（dump）当前句柄存储的信息**。
- **打印内存分配情况**，方便后续优化 GC 实现。

```c#
public HResult GarbageCollect(int generation, bool low_memory_p, int mode)
{
    Write("GarbageCollect");
    _gcHandleManager.Store.DumpHandles();
    return HResult.S_OK;
}
```

## 基本功能已完成

我们已经实现了所有必要的 GC 接口，现在可以在 `GC_Initialize` 方法中将它们**连接起来**。

```c#
[UnmanagedCallersOnly(EntryPoint = "_GC_Initialize")]
    public static unsafe HResult GC_Initialize(IntPtr clrToGC, IntPtr* gcHeap, IntPtr* gcHandleManager, GcDacVars* gcDacVars)
    {
    Write("GC_Initialize");

var clrToGc = NativeObjects.IGCToCLR.Wrap(clrToGC);
var gc = new GCHeap(clrToGc);

*gcHeap = gc.IGCHeapObject;
*gcHandleManager = gc.IGCHandleManagerObject;

return HResult.S_OK;
}
```

由于我们的 GC **不支持 Server GC 模式**，我们需要额外的代码来：

1. 通过 `IClrToGC` 接口**获取运行时的 GC 配置**。
2. **如果 Server GC 处于启用状态，则直接终止初始化**。

在本地代码 `IGCToCLR.GetBooleanConfigValue` 方法中，参数**必须是单字节编码的字符串**，而 .NET 的 `string` 采用 **UTF-16**（两个字节存储一个字符）。为了避免字符串转换，我们使用 `u8` 后缀来直接获取 **UTF-8** 字符串。

```c#
[UnmanagedCallersOnly(EntryPoint = "_GC_Initialize")]
public static unsafe HResult GC_Initialize(IntPtr clrToGC, IntPtr* gcHeap, IntPtr* gcHandleManager, GcDacVars* gcDacVars)
{
    Write("GC_Initialize");

    var clrToGc = NativeObjects.IGCToCLR.Wrap(clrToGC);

    fixed (byte* privateKey = "gcServer"u8, publicKey = "System.GC.Server"u8)
    {
        clrToGc.GetBooleanConfigValue(privateKey, publicKey, out var gcServerEnabled);

        if (gcServerEnabled)
        {
            Write("This GC isn't compatible with server GC. Set DOTNET_gcServer=0 to disable it.");
            return HResult.E_FAIL;
        }
    }

    var gc = new GCHeap(clrToGc);

    *gcHeap = gc.IGCHeapObject;
    *gcHandleManager = gc.IGCHandleManagerObject;

    return HResult.S_OK;
}
```

为了测试这个 GC，我们编写了一个**简单的控制台应用**，它会：

1. 分配几个对象（包括一个大对象）。
2. 操作一个 `DependentHandle`（用于验证 `IHandleStore` 代码）。
3. 调用 `GC.Collect()`，触发 `DumpHandles` 代码，输出当前句柄信息。

虽然测试用例并不复杂，但它可以完整运行且不会崩溃！ 在控制台中，大部分日志信息是运行时调用的 GC 方法，而这些方法我们尚未完全实现。

为了更严谨地验证 GC，我们尝试运行了 [OrchardCore.Samples](https://github.com/OrchardCMS/OrchardCore.Samples) 仪表盘应用（在升级到 .NET 9 并禁用 Server GC 之后），未发现明显的错误。

当然，目前的 GC 仍然存在以下问题：

- **所有已分配的内存都无法回收**，导致**严重的内存泄漏**。
- **句柄的最大数量是固定的**，达到上限后，应用程序一定会崩溃。

要让这个 GC **真正运行在生产环境中**，仍有大量工作需要完成。

下一步，我们将增加诊断代码，用于显示托管对象的详细信息，以便后续调试。这可能听起来很简单，但**由于 GC 运行在自己的运行时中，它无法使用反射（Reflection）或 .NET 标准 API 来检查托管对象**，这给调试带来了一定挑战。

本文示例代码已上传到 [GitHub](https://github.com/kevingosse/ManagedDotnetGC/tree/Part2)，感兴趣的读者可以自行下载研究。