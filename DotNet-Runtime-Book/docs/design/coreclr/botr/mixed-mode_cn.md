# 混合模式程序集
## 简介
托管代码与本机代码之间的大多数互操作都通过 P/Invoke、COM 或 WinRT 实现。由于 P/Invoke 在运行时才与本机代码绑定，它很容易受到各种错误的影响：从名称写错，到签名里一些很细微但会导致栈损坏的错误。COM 也可用于从本机代码调用托管代码，但它往往需要注册，并且会带来一定的性能开销。WinRT 避免了这些问题，但并非在所有场景下都可用。

C++/CLI 提供了另一种不同的、由编译器验证的互操作方式，称为混合模式程序集（mixed-mode assemblies，也有时被称为 It-Just-Works 或 IJW）。它不要求开发者像写 P/Invoke 那样进行特殊声明，而是由 C++ 编译器自动生成在托管与本机代码之间切换所需的一切。此外，C++ 编译器会决定某个 C++ 方法应当是托管的还是本机的，因此即使在同一个程序集内部，也会频繁发生切换，而且不需要开发者显式介入。

## 调用本机代码
C++/CLI 代码可以调用同一程序集内的本机代码，也可以调用其他库中的本机代码。对不同库的调用会生成类似于在 C# 中手写的 P/Invoke（但由于 C++ 编译器会读取该库的头文件，这种 P/Invoke 不会受到开发者手误的影响）。不过，对同一程序集的调用方式不同：对不同库的 P/Invoke 会指定要调用的库名和导出函数名；而对同一库的 P/Invoke 则具有空的入口点（entry point），并设置一个 RVA——即库内要调用的地址。在元数据中看起来像这样：
```
MethodName: delete (060000EE)
Flags     : [Assem] [Static] [ReuseSlot] [PinvokeImpl] [HasSecurity]  (00006013)
RVA       : 0x0001332a
Pinvoke Map Data:
Entry point:
```
调用这些 P/Invoke 的方式与使用具名入口点的 P/Invoke 基本相同，唯一的区别是：需要基于模块地址与 RVA 手动计算目标地址，而不是去查找某个导出符号。

## 调用托管代码
虽然 native->native 调用以及 managed->native 调用都可以基于本机函数地址来完成，但 native->managed 调用却不行，因为托管代码是不可执行的 IL。为了解决这个问题，编译器会生成一个查找表，它会出现在 CIL 元数据头中的 ```.vtfixup``` 表里。磁盘上的库文件中，```Vtfixups``` 会把一个 RVA 映射到一个托管方法的 token。程序集加载时，CLR 会为 ```.vtfixup``` 表中的每个方法生成一个可供本机调用的封送（marshaling）stub，用于调用对应的托管方法；然后它会把这些 token 替换为 stub 方法的地址。当本机代码要调用某个托管方法时，会通过 ```.vtfixup``` 表中更新后的地址间接调用。

例如，如果 IjwLib.dll 中的某个本机方法想调用 token 为 06000002 的托管 Bar 方法，它会发出：
```
call    IjwLib!Bar (1000112b)
```
在该地址处，会放置一个跳转间接：
```
jmp     dword ptr [IjwLib!_mep?Bar$$FYAXXZ (10010008)]
```
其中 10010008 对应一个 ```.vtfixup``` 条目，形如：
```
.vtfixup [1] int32 retainappdomain at D_00010008 // 06000002 (Bar's token)
```
根据 ECMA 335，```vtfixups``` 可以包含多个条目。不过，Microsoft Visual C++ 编译器（MSVC）似乎不会生成这种情况。Vtfixups 还包含一些标志，用于指示调用是否应当转到当前线程的 appdomain，以及调用方是否为非托管代码。MSVC 似乎总是会设置这些标志。

## 启动运行时
混合模式程序集可以被加载到一个已经运行的 CLR 中，但并非总是如此。混合模式可执行文件也可能用于启动进程；或者一个正在运行的本机进程也可能加载混合模式库并调用其中代码。在 .NET Framework（目前唯一具备该功能的实现）上，本机代码的 ```Main``` 或 ```DllMain``` 会调用 mscoree.dll 的 ```_CorDllMain``` 函数（该函数从一个众所周知的位置解析得到）。当发生这种调用时，```_CorDllMain``` 既负责启动运行时，也负责按前述方式填充 vtfixups。
