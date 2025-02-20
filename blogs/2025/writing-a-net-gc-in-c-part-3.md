接下来，我们将开始研究如何遍历引用树以查找仍然可访问的对象。但在此之前，如果我们能有一种方法来获取堆中存储对象的更多信息以供调试使用，那就更好了。

为了说明这个问题，让我们看一下我们在前一部分中实现的 `GCHandleStore.DumpHandles` 方法：

```c#
public void DumpHandles()
{
    Write("GCHandleStore DumpHandles");

    for (int i = 0; i < _handleCount; i++)
    {
    	Write($"Handle {i} - {_store[i]}");
    }
}
```

提醒一下，这是我们 `ObjectHandle` 结构体及其 `ToString` 方法的实现：

```c#
[StructLayout(LayoutKind.Sequential)]
public struct ObjectHandle
{
    public nint Object;
    public nint ExtraInfo;
    public HandleType Type;

    public override string ToString() => $"{Type} - {Object:x2} - {ExtraInfo:x2}";
}
```

在测试应用中，打印的内容是：

```
[GC] GCHandleStore DumpHandles
[GC] Handle 0 - HNDTYPE_WEAK_SHORT - 00 - 00
[GC] Handle 1 - HNDTYPE_STRONG - 00 - 00
[GC] Handle 2 - HNDTYPE_WEAK_SHORT - 220881ae040 - 00
[GC] Handle 3 - HNDTYPE_STRONG - 220881ae040 - 00
[GC] Handle 4 - HNDTYPE_STRONG - 1dff1bc4d28 - 00
[GC] Handle 5 - HNDTYPE_STRONG - 1dff1bc6d20 - 00
[GC] Handle 6 - HNDTYPE_STRONG - 1dff1bc6d80 - 00
[GC] Handle 7 - HNDTYPE_STRONG - 1dff1bc6df8 - 00
[GC] Handle 8 - HNDTYPE_STRONG - 1dff1bc6e70 - 00
[GC] Handle 9 - HNDTYPE_PINNED - 1dff1bc6ee8 - 00
[GC] Handle 10 - HNDTYPE_WEAK_SHORT - 00 - 00
[GC] Handle 11 - HNDTYPE_STRONG - 00 - 00
[GC] Handle 12 - HNDTYPE_WEAK_SHORT - 1dff1bda7f8 - 00
[GC] Handle 13 - HNDTYPE_STRONG - 1dff1bda838 - 00
[GC] Handle 14 - HNDTYPE_WEAK_SHORT - 1dff1bda8a8 - 00
[GC] Handle 15 - HNDTYPE_STRONG - 1dff1bda8e8 - 00
[GC] Handle 16 - HNDTYPE_WEAK_SHORT - 1dff1bda588 - 00
[GC] Handle 17 - HNDTYPE_WEAK_SHORT - 220881b6218 - 00
[GC] Handle 18 - HNDTYPE_STRONG - 220881b6258 - 00
[GC] Handle 19 - HNDTYPE_WEAK_SHORT - 220881b62c8 - 00
[GC] Handle 20 - HNDTYPE_STRONG - 220881b6308 - 00
[GC] Handle 21 - HNDTYPE_WEAK_SHORT - 220881b5fc8 - 00
[GC] Handle 22 - HNDTYPE_WEAK_SHORT - 220881b63f0 - 00
[GC] Handle 23 - HNDTYPE_STRONG - 220881b6430 - 00
[GC] Handle 24 - HNDTYPE_WEAK_SHORT - 220881b65d8 - 00
[GC] Handle 25 - HNDTYPE_STRONG - 220881b6618 - 00
[GC] Handle 26 - HNDTYPE_WEAK_SHORT - 220881b7268 - 00
[GC] Handle 27 - HNDTYPE_STRONG - 220881b72a8 - 00
[GC] Handle 28 - HNDTYPE_WEAK_SHORT - 220881b7318 - 00
[GC] Handle 29 - HNDTYPE_STRONG - 220881b7358 - 00
[GC] Handle 30 - HNDTYPE_WEAK_SHORT - 220881b70f0 - 00
[GC] Handle 31 - HNDTYPE_DEPENDENT - 220881b7788 - 00
[GC] Handle 32 - HNDTYPE_WEAK_SHORT - 220881b6e90 - 00
```

这已经是一个不错的开始，但如果我们知道 `220881ae040` 或 `1dff1bc4d28` 指向的对象类型，那就更有帮助了。

通常，获取一个对象的类型在 .NET 中是非常简单的：只需调用 `GetType()` 方法。然而，在我们的句柄存储中，我们只知道对象的地址，但我们可以使用一些不安全的代码来构建对它的引用。让我们尝试一下：

```c#
    public void DumpHandles()
    {
        Write("GCHandleStore DumpHandles");

        for (int i = 0; i < _handleCount; i++)
        {
            var handle = _store[i];
            var output = $"Handle {i} - {_store[i]}";

            if (handle.Object != 0)
            {
                // Take the address of the pointer,
                // reinterpret it as a pointer to an object reference,
                // and dereference it to get the object reference.
                var obj = *(object*)&handle.Object;
                output += $" - Object type: {obj.GetType()}";
            }

            Write(output);
        }
    }
```

但是，如果我们测试它，它会在第一个非空对象处立即崩溃：

```
[GC] GCHandleStore DumpHandles
[GC] Handle 0 - HNDTYPE_WEAK_SHORT - 00 - 00
[GC] Handle 1 - HNDTYPE_STRONG - 00 - 00
Fatal error. Internal CLR error. (0x80131506)
   at System.GC.Collect()
   at Program.<Main>$(System.String[])
```

问题出在我们的垃圾回收（GC）是用 NativeAOT 编译的，后者有自己的运行时。这个运行时与测试应用使用的 .NET 运行时是分开的。类型系统是独立的，且不兼容，因此我们不能直接在 NativeAOT 运行时中操作 .NET 类型。

那么……我们该如何获得该类型信息呢？GC 没有提供获取该信息的 API，因为这通常不是它需要的东西。理论上，我们可以手动解析方法表和模块元数据，然后使用这些信息来查找类型的名称。虽然这会是一个有趣的文章话题，但我们现在还是先寻找更简单的解决方案。

### 协作方式

一种获取类型信息的方法是简单地要求测试应用提供它。我们的思路是在测试应用中添加一个方法，根据给定的对象地址来获取对象的类型，然后在需要时由 GC 调用它。这将强烈耦合应用程序与 GC，但可能是可以接受的，因为我们只需要这个信息用于调试目的。

这个方法将从 NativeAOT 调用，所以我们为它加上 `[UnmanagedCallersOnly]` 特性。它接收一个缓冲区来写入类型名称，并返回字符串的长度。

```c#
    [UnmanagedCallersOnly]
    public static unsafe int GetType(IntPtr address, char* buffer, int capacity)
    {
        var destination = new Span<char>(buffer, capacity);

        var obj = *(object*)&address;
        var type = obj.GetType().ToString();
        var length = Math.Min(type.Length, capacity);
        type[..length].CopyTo(destination);

        return length;
    }
```

我们还需要告诉 GC 该方法的地址，所以我们添加了一个 P/Invoke 并在启动时调用它：

```c#
    public static void Initialize()
    {
        SetGetTypeCallback(&GetType);
    }

    [DllImport("ManagedDotnetGC.dll")]
    private static extern void SetGetTypeCallback(delegate* unmanaged<IntPtr, char*, int, int> callback);
```

在 GC 端，我们导出 `SetGetTypeCallback` 方法，并将参数存储在一个字段中：

```c#
    [UnmanagedCallersOnly(EntryPoint = "SetGetTypeCallback")]
    public static unsafe void SetGetTypeCallback(IntPtr callback)
    {
        GetTypeCallback = (delegate* unmanaged<IntPtr, char*, int, int>)callback;
    }

    internal static unsafe delegate* unmanaged<IntPtr, char*, int, int> GetTypeCallback;
```

最后，我们更新 `DumpHandles` 方法来调用这个方法：

```c#
    public void DumpHandles()
    {
        Write("GCHandleStore DumpHandles");

        var buffer = new char[1000];

        fixed (char* p = buffer)
        {
            for (int i = 0; i < _handleCount; i++)
            {
                var handle = _store[i];
                var output = $"Handle {i} - {_store[i]}";

                if (handle.Object != 0)
                {
                    if (GetTypeCallback != null)
                    {
                        var size = GetTypeCallback(handle.Object, p, buffer.Length);
                        output += $" - Object type: {new string(buffer[..size])}";
                    }
                }

                Write(output);
            }
        }
    }
```

不幸的是，当运行时它崩溃了：

```
[GC] GCHandleStore DumpHandles
[GC] Handle 0 - HNDTYPE_WEAK_SHORT - 00 - 00
[GC] Handle 1 - HNDTYPE_STRONG - 00 - 00
Fatal error. Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code.
   at System.GC.Collect()
   at Program.<Main>$(System.String[])
```

正如消息所示，我们不允许从托管代码调用 `[UnmanagedCallersOnly]` 方法（同样适用于 `Marshal.GetDelegateForFunctionPointer`）。然而，我们是从 GC 调用它的，GC 是非托管代码，那么到底是怎么回事呢？

如果我们查找 .NET 运行时源代码中的错误消息，我们会发现它是从方法 [ReversePInvokeBadTransition](https://github.com/dotnet/runtime/blob/826d9313afff3c406df6f8c13a8d70bcbe4e34e8/src/coreclr/vm/dllimportcallback.cpp#L184-L196) 抛出的（“反向 P/Invoke”是指本地代码调用托管代码）。该方法是从[反向 P/Invoke 的入口点调用的](https://github.com/dotnet/runtime/blob/826d9313afff3c406df6f8c13a8d70bcbe4e34e8/src/coreclr/vm/dllimportcallback.cpp#L210-L212)，当当前线程禁用了“抢占模式”时：

```
    // Verify the current thread isn't in COOP mode.
    if (pThread->PreemptiveGCDisabled())
        ReversePInvokeBadTransition();
```

我已经在我的文章[SuppressGCTransition](https://minidump.net/suppressgctransition-b9a8a774edbd/)中解释了抢占模式是什么，但这里简单提醒一下：在 .NET 中，线程有两种模式：抢占模式和协作模式。当 GC 触发垃圾回收时，它需要确保没有托管代码在运行。为此，它会挂起所有处于协作模式的线程（或者更准确地说，它“与它们合作”将它们挂起在一个安全的点）。处于抢占模式的线程不会被挂起，它们被信任不会运行任何托管代码。所以总结来说：

- 托管代码总是运行在协作模式下。
- 本地代码通常运行在抢占模式下，但有时也可以在协作模式下运行（当执行需要访问托管对象的 CLR 函数时，或者当一个 P/Invoke 被标注为 `[SuppressGCTransition]` 时）。

错误消息告诉我们，调用反向 P/Invoke 时线程处于协作模式下，这是不被允许的。

> 老实说，我并不完全理解为什么在协作模式下调用反向 P/Invoke 不被允许。这种情况通常不会发生，所以我猜测他们使用这种方式来确保托管代码不会错误地调用 `[UnmanagedCallersOnly]` 方法。但这只是猜测。

那么，为什么我们的线程在协作模式下呢？托管代码正在调用 `GC.Collect`，而 `GC.Collect` 又通过 QCall 调用 CLR 中的 `GCInterface_Collect` 函数：

```c#
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_Collect")]
        private static partial void _Collect(int generation, int mode);
```

QCall 是一种特殊类型的 P/Invoke，用于从托管代码调用 CLR。它们的确切行为有[详细文档说明](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/corelib.md#calling-from-managed-to-native-code)，文档明确指出：

QCall 也像普通的 P/Invoke 一样切换到抢占模式。

> 所以我们的线程应该处于抢占模式，除非……

答案就在 `GCInterface_Collect` 方法本身：

`GCX_COOP` 宏用于在调用 `IGCHeap::GarbageCollect` 之前将线程切换到协作模式。由于我们是在 `GarbageCollect` 方法中调用 `DumpHandles`，这就是为什么我们的反向 P/Invoke 失败的原因。

是死胡同吗？不完全是。

`GC_Initialize` 方法（如果你忘记了前面几部分）提供了控制线程模式的专用方法。我们可以使用这些方法，在调用反向 P/Invoke 之前将线程切换到抢占模式，之后再切换回协作模式。

```c#
    public void DumpHandles()
    {
        Write("GCHandleStore DumpHandles");

        bool isPreeptiveGCDisabled = _gcToClr.IsPreemptiveGCDisabled();

        if (isPreeptiveGCDisabled)
        {
            _gcToClr.EnablePreemptiveGC();
        }

        var buffer = new char[1000];

        fixed (char* p = buffer)
        {
            for (int i = 0; i < _handleCount; i++)
            {
                var handle = _store[i];
                var output = $"Handle {i} - {_store[i]}";

                if (handle.Object != 0)
                {
                    if (GetTypeCallback != null)
                    {
                        var size = GetTypeCallback(handle.Object, p, buffer.Length);
                        output += $" - Object type: {new string(buffer[..size])}";
                    }
                }

                Write(output);
            }
        }

        if (isPreeptiveGCDisabled)
        {
            _gcToClr.DisablePreemptiveGC();
        }
    }
```

现在如果我们再次运行测试应用，我们就可以看到对象的类型了：

```
[GC] GCHandleStore DumpHandles
[GC] Handle 0 - HNDTYPE_WEAK_SHORT - 00 - 00
[GC] Handle 1 - HNDTYPE_STRONG - 00 - 00
[GC] Handle 2 - HNDTYPE_WEAK_SHORT - 21235ae1fa0 - 00 - Object type: System.Threading.Thread
[GC] Handle 3 - HNDTYPE_STRONG - 21235ae1fa0 - 00 - Object type: System.Threading.Thread
[GC] Handle 4 - HNDTYPE_STRONG - 1d19f5bbb48 - 00 - Object type: System.Object[]
[GC] Handle 5 - HNDTYPE_STRONG - 1d19f5bdb40 - 00 - Object type: System.Int32[]
[GC] Handle 6 - HNDTYPE_STRONG - 1d19f5bdba0 - 00 - Object type: System.OutOfMemoryException
[GC] Handle 7 - HNDTYPE_STRONG - 1d19f5bdc18 - 00 - Object type: System.StackOverflowException
[GC] Handle 8 - HNDTYPE_STRONG - 1d19f5bdc90 - 00 - Object type: System.ExecutionEngineException
[GC] Handle 9 - HNDTYPE_PINNED - 1d19f5bdd08 - 00 - Object type: System.Object
[GC] Handle 10 - HNDTYPE_WEAK_SHORT - 00 - 00
[GC] Handle 11 - HNDTYPE_STRONG - 00 - 00
[GC] Handle 12 - HNDTYPE_WEAK_SHORT - 1d19f5cb988 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 13 - HNDTYPE_STRONG - 1d19f5cb9c8 - 00 - Object type: System.Diagnostics.Tracing.EtwEventProvider
[GC] Handle 14 - HNDTYPE_WEAK_SHORT - 1d19f5cba38 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 15 - HNDTYPE_STRONG - 1d19f5cba78 - 00 - Object type: System.Diagnostics.Tracing.EventPipeEventProvider
[GC] Handle 16 - HNDTYPE_WEAK_SHORT - 1d19f5cb718 - 00 - Object type: System.Diagnostics.Tracing.NativeRuntimeEventSource
[GC] Handle 17 - HNDTYPE_WEAK_SHORT - 21235aea178 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 18 - HNDTYPE_STRONG - 21235aea1b8 - 00 - Object type: System.Diagnostics.Tracing.EtwEventProvider
[GC] Handle 19 - HNDTYPE_WEAK_SHORT - 21235aea228 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 20 - HNDTYPE_STRONG - 21235aea268 - 00 - Object type: System.Diagnostics.Tracing.EventPipeEventProvider
[GC] Handle 21 - HNDTYPE_WEAK_SHORT - 21235ae9f28 - 00 - Object type: System.Diagnostics.Tracing.RuntimeEventSource
[GC] Handle 22 - HNDTYPE_WEAK_SHORT - 21235aea350 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 23 - HNDTYPE_STRONG - 21235aea390 - 00 - Object type: System.Diagnostics.Tracing.EtwEventProvider
[GC] Handle 24 - HNDTYPE_WEAK_SHORT - 21235aea538 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 25 - HNDTYPE_STRONG - 21235aea578 - 00 - Object type: System.Diagnostics.Tracing.EventPipeEventProvider
[GC] Handle 26 - HNDTYPE_DEPENDENT - 21235aeae18 - 21235aeae48 - Object type: System.Object
[GC] Handle 27 - HNDTYPE_WEAK_SHORT - 21235aeb238 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 28 - HNDTYPE_STRONG - 21235aeb278 - 00 - Object type: System.Diagnostics.Tracing.EtwEventProvider
[GC] Handle 29 - HNDTYPE_WEAK_SHORT - 21235aeb2e8 - 00 - Object type: System.Diagnostics.Tracing.EventSource+OverrideEventProvider
[GC] Handle 30 - HNDTYPE_STRONG - 21235aeb328 - 00 - Object type: System.Diagnostics.Tracing.EventPipeEventProvider
[GC] Handle 31 - HNDTYPE_WEAK_SHORT - 21235aeb0c0 - 00 - Object type: System.Buffers.ArrayPoolEventSource
[GC] Handle 32 - HNDTYPE_DEPENDENT - 21235aec4b8 - 00 - Object type: System.Buffers.SharedArrayPoolThreadLocalArray[]
[GC] Handle 33 - HNDTYPE_WEAK_SHORT - 21235aeae60 - 00 - Object type: System.Buffers.SharedArrayPool`1[System.Char]
[GC] Handle 34 - HNDTYPE_WEAK_LONG - 21235b98528 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 35 - HNDTYPE_WEAK_LONG - 21235b98668 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 36 - HNDTYPE_WEAK_LONG - 21235b98740 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 37 - HNDTYPE_WEAK_LONG - 21235b98818 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 38 - HNDTYPE_WEAK_LONG - 21235b98908 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 39 - HNDTYPE_WEAK_LONG - 21235b989f8 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 40 - HNDTYPE_WEAK_LONG - 21235b98af0 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 41 - HNDTYPE_WEAK_LONG - 21235b98bc0 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 42 - HNDTYPE_WEAK_LONG - 21235b98cf0 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 43 - HNDTYPE_WEAK_LONG - 21235b98e00 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 44 - HNDTYPE_WEAK_LONG - 21235b98f18 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 45 - HNDTYPE_WEAK_LONG - 21235b99038 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 46 - HNDTYPE_WEAK_LONG - 21235b99148 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 47 - HNDTYPE_WEAK_LONG - 21235b99248 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 48 - HNDTYPE_WEAK_LONG - 21235b99360 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
[GC] Handle 49 - HNDTYPE_WEAK_LONG - 21235b99470 - 00 - Object type: System.RuntimeType+RuntimeTypeCache
```

因此，我们可以从测试应用中暴露一个方法，并从 GC 调用它。然而，它有一个重大缺点：当我们最终实现一个实际的垃圾回收时，我们将希望在堆处于不一致状态时检查对象，这可能会导致一些不可预测的行为。这是一次有趣的探索，但我们需要找到更好的方法。

### 更好的方法

总结来说，我们需要一种在不运行任何托管代码的情况下检查托管对象的方法。嗯，这正是调试器所做的，所以也许我们可以使用它们使用的相同 API？

这些 API 在与运行时捆绑的独立组件中暴露，称为 DAC。它封装了与运行时数据结构交互所需的所有逻辑。

DAC 旨在与各种目标一起使用：实时进程、远程进程、崩溃转储……为了使这成为可能，它将基本操作（如读取和写入内存）抽象为一个 `ICLRDataTarget` 接口，调试器必须实现该接口。像往常一样，我将原始的 C++ 接口转换为 C#，并使用我的 [NativeObjects 库](https://github.com/kevingosse/NativeObjects)进行包装。对于我们的简单用例，我们只需要实现接口中的一些方法：

- `ReadVirtual`：从目标读取内存
- `GetMachineType`：获取目标的架构
- `GetPointerSize`：获取目标上指针的大小
- `GetImageBase`：获取给定模块的基地址

```c#
public unsafe class ClrDataTarget : ICLRDataTarget, IDisposable
{
   private readonly NativeObjects.ICLRDataTarget _clrDataTarget;

   public ClrDataTarget()
   {
       _clrDataTarget = NativeObjects.ICLRDataTarget.Wrap(this);
   }

   public IntPtr ICLRDataTargetObject => _clrDataTarget;

   public HResult GetMachineType(out uint machine)
   {
       var architecture = RuntimeInformation.ProcessArchitecture;

       // https://learn.microsoft.com/en-us/windows/win32/sysinfo/image-file-machine-constants
       if (architecture == Architecture.X86)
       {
           machine = 0x14c; // IMAGE_FILE_MACHINE_I386
       }
       else if (architecture == Architecture.X64)
       {
           machine = 0x8664; // IMAGE_FILE_MACHINE_AMD64
       }
       else if (architecture == Architecture.Arm64)
       {
           machine = 0xaa64; // IMAGE_FILE_MACHINE_ARM64
       }
       else
       {
           machine = 0;
           return HResult.E_FAIL;
       }        

       return HResult.S_OK;
   }

   public HResult GetPointerSize(out uint size)
   {
       size = (uint)IntPtr.Size;
       return HResult.S_OK;
   }

   public HResult GetImageBase(char* moduleName, out CLRDATA_ADDRESS baseAddress)
   {
       var name = new string(moduleName);

       foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
       {
           if (module.ModuleName == name)
           {
               baseAddress = new CLRDATA_ADDRESS(module.BaseAddress.ToInt64());
               return HResult.S_OK;
           }
       }

       baseAddress = default;
       return HResult.E_FAIL;
   }

   public HResult ReadVirtual(CLRDATA_ADDRESS address, byte* buffer, uint size, out uint done)
   {
       Unsafe.CopyBlock(buffer, (void*)(IntPtr)address.Value, size);
       done = size;
       return HResult.S_OK;
   }
}
```

对于所有其他方法，我们简单地返回 `E_NOTIMPL`。我们还需要实现 `IUnknown`，但没有什么特别的地方，因此这里不展示。

`ReadVirtual` 和 `GetImageBase` 方法使用 `CLRDATA_ADDRESS` 结构表示地址。显然，这是一个带符号类型，[在访问 32 位进程的前 2GB 时会导致转换问题](https://github.com/dotnet/runtime/blob/main/src/coreclr/debug/daccess/dacimpl.h#L31-L36)。这非常让人困惑，所以我决定直接窃取 [Lee Culver 为 ClrMD 编写的 C# 实现](https://github.com/microsoft/clrmd/blob/fb8c39b99ed792d650640823ee022f9f16996fe2/src/Microsoft.Diagnostics.Runtime/DacInterface/ClrDataAddress.cs)。

接下来的步骤是将 DAC 加载到进程中。DAC 存储在一个共享库中，存储在与运行时相同的目录下。为了定位运行时，我们查找当前进程中的 `coreclr.dll` 模块并从其路径中提取目录。因为共享库的名称取决于平台，我添加了一个简短的辅助方法来进行转换。

```c#
public class DacManager : IDisposable
{
    public static unsafe HResult TryLoad(out DacManager? dacManager)
    {
        var coreclr = GetLibraryName("coreclr");

        var module = Process.GetCurrentProcess().Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(m => m.ModuleName == coreclr);

        if (module == null)
        {
            Log.Write($"{coreclr} not found");
            dacManager = null;
            return HResult.E_FAIL;
        }

        var dacPath = Path.Combine(
            Path.GetDirectoryName(module.FileName)!,
            GetLibraryName("mscordaccore"));

        if (!File.Exists(dacPath))
        {
            Log.Write($"The DAC wasn't found at the expected path ({dacPath})");
            dacManager = null;
            return HResult.E_FAIL;
        }

        var library = NativeLibrary.Load(dacPath);

        // TODO
    }

    private static string GetLibraryName(string name)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{name}.dll";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"lib{name}.so";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"lib{name}.dylib";
        }
        
        throw new PlatformNotSupportedException();
    }
}
```

一旦 DAC 加载到进程中，我们需要调用 `CLRDataCreateInstance` 函数，并将我们的 `ICLRDataTarget` 对象传递给它。作为回报，它会给我们一个 `IUnknown` 实例，我们可以在上面调用 `QueryInterface` 来检索 `ISOSDacInterface`，它暴露了 DAC 的功能。

```c#
        var library = NativeLibrary.Load(dacPath);

        try
        {
            var export = NativeLibrary.GetExport(library, "CLRDataCreateInstance");
            var createInstance = (delegate* unmanaged[Stdcall]<in Guid, IntPtr, out IntPtr, HResult>)export;

            var dataTarget = new ClrDataTarget();
            var result = createInstance(IClrDataProcessGuid, dataTarget.ICLRDataTargetObject, out var pUnk);

            var unknown = NativeObjects.IUnknown.Wrap(pUnk);
            result = unknown.QueryInterface(ISOSDacInterface.Guid, out var sosDacInterfacePtr);

            dacManager = result ? new DacManager(library, sosDacInterfacePtr) : null;
            return result;
        }
        catch
        {
            NativeLibrary.Free(library);
            throw;
        }
```

我们的 `DacManager` 类将 `ISOSDacInterface` 对象的引用存储在 `Dac` 属性中。我们可以使用它实现一个方法，给定一个托管对象的地址，提取它的类型：

```c#
    public unsafe string? GetObjectName(CLRDATA_ADDRESS address)
    {
        var result = Dac.GetObjectClassName(address, 0, null, out var needed);

        if (!result)
        {
            return null;
        }

        char* str = stackalloc char[(int)needed];
        result = Dac.GetObjectClassName(address, needed, str, out _);

        if (!result)
        {
            return null;
        }

        return new string(str);
    }
```

这是一种经典的 Win32 模式：我们无法提前知道名称的大小，因此我们首先用一个空缓冲区调用该方法以获取大小，然后分配一个正确大小的缓冲区并再次调用该方法。

我们最终可以在 `DumpHandle` 方法中使用 `DacManager`，并进行适当的检查，以使调试 API 成为可选的：

```c#
    public void DumpHandles(DacManager? dacManager)
    {
        Write("GCHandleStore DumpHandles");

        for (int i = 0; i < _handleCount; i++)
        {
            ref var handle = ref _store[i];
            var output = $"Handle {i} - {handle}";

            if (dacManager != null && handle.Object != 0)
            {
                output += $" - {dacManager.GetObjectName(new(handle.Object))}";
            }

            Write(output);
        }
    }
```

如果我们再次运行测试应用程序，就可以看到存储在句柄存储中的对象类型。请注意，先前解决方案中的所有 `System.RuntimeType+RuntimeTypeCache` 对象都消失了，我猜它们是在 P/Invoke 或反向 P/Invoke 调用期间分配的。

### 结论

我们在实现 GC 时绕了一个小路，但这也是一个很好的借口，来探索 GC 模式并学习如何使用 DAC。下次，我们将最终开始研究如何实现我们自定义 GC 的标记阶段。

本文的代码可以在 [GitHub](https://github.com/kevingosse/ManagedDotnetGC/tree/Part3) 上找到。