数据访问组件（DAC）笔记
=======================

日期：2007

调试托管代码需要对托管对象和结构有专门的知识。例如，除了对象本身的数据之外，对象还包含各种头部信息。随着垃圾回收器（GC）的工作，对象可能会在内存中移动。获取类型信息可能需要加载器（loader）的帮助。要检索经历了“编辑并继续”（Edit-and-Continue, EnC）的函数的正确版本，或获取通过反射发出的函数的信息，调试器需要理解 EnC 版本号和元数据。调试器必须能够区分 AppDomain 与程序集。VM 目录中的代码体现了这些托管结构所需的知识。这本质上意味着：用于检索托管代码与数据的信息的 API，必须运行一些与执行引擎自身相同的算法。

调试器可以以 _进程内（in-process）_ 或 _进程外（out-of-process）_ 的方式工作。进程内调试器需要一个“活”的数据目标（被调试进程，debuggee）。在这种情况下，运行时已经被加载且目标正在运行。被调试进程中的一个辅助线程会运行执行引擎中的代码来计算调试器所需的信息。由于该辅助线程运行在目标进程中，它可以直接访问目标的地址空间和运行时代码。所有计算都发生在目标进程中。这是一种让调试器获得所需信息、从而以有意义的方式呈现托管结构的简单手段。尽管如此，进程内调试器也有一些限制。例如，如果被调试目标当前并未运行（如目标是转储文件 dump 的情况），则运行时并未加载（甚至可能在机器上也不可用）。此时，调试器无法执行运行时代码来获取所需信息。

从历史上看，CLR 调试器是进程内运行的。可以使用调试器扩展 SOS（Son of Strike）（或早期 CLR 时代的 Strike）来检查托管代码。从 .NET Framework 4 开始，调试器改为进程外运行。CLR 调试 API 提供了 SOS 的大部分功能，同时还提供了 SOS 不具备的其他功能。SOS 和 CLR 调试 API 都使用数据访问组件（Data Access Component, DAC）来实现进程外调试。概念上，DAC 是运行时执行引擎代码的一个子集，能够在进程外运行。这意味着它可以操作转储文件，即使机器上没有安装运行时。其实现主要由一组宏与模板构成，并通过对执行引擎代码进行条件编译来实现。构建运行时时，会同时生成 clr.dll 和 mscordacwks.dll。对于 CoreCLR 构建，二进制文件略有不同：coreclr.dll 与 msdaccore.dll。针对其他操作系统（如 OS X）构建时，文件名也会不同。为了检查目标，DAC 可以读取目标内存，为 mscordacwks 中的 VM 代码提供输入。然后它可以在宿主中运行相应函数来计算关于某个托管结构所需的信息，最后把结果返回给调试器。

注意：DAC 读取的是 _目标进程的内存_。务必要认识到，调试器与被调试进程是两个不同的进程，拥有不同的地址空间。因此，必须明确区分“目标内存”和“宿主内存”。在宿主进程中运行的代码如果使用目标地址，将得到完全不可预测且通常不正确的结果。使用 DAC 从目标检索内存时，一定要非常小心，确保使用的是正确地址空间的地址。此外，有时目标地址只是作为数据来使用；此时使用宿主地址同样是错误的。例如，要显示某个托管函数的信息，我们可能希望列出它的起始地址与大小。这里必须提供目标地址。在编写 VM 中将由 DAC 运行的代码时，需要正确选择何时使用宿主地址、何时使用目标地址。

DAC 基础设施（控制访问宿主/目标内存的宏与模板）提供了一些约定，用于区分哪些指针是宿主地址、哪些是目标地址。当某个函数被 _DAC 化（DACized）_（即使用 DAC 基础设施使其可以在进程外工作）时，类型为 `T` 的宿主指针声明为 `T *`。目标指针类型为 `PTR_T`。不过要记住，“宿主 vs 目标”的概念只对 DAC 有意义。在非 DAC 构建中，只有一个地址空间：宿主与目标相同，都是 CLR。如果我们在 VM 函数里声明一个局部变量，无论它是 `T *` _还是 `PTR_T`_，它都会是一个“宿主指针”。当我们在 clr.dll（coreclr.dll）中执行代码时，`T *` 与 `PTR_T` 的局部变量完全没有差别。若我们从同一份源代码编译出的 mscordacwks.dll（msdaccore.dll）里执行该函数，则声明为 `T *` 的变量将成为真正的宿主指针，宿主是调试器。这其实很显然，但当我们开始把这些指针传给其他 VM 函数时，往往会变得令人困惑。在 DAC 化函数（按需要将 `T *` 改为 `PTR_T`）时，我们有时需要追踪某个指针的起源，以确定它应当是宿主类型还是目标类型。

如果不了解 DAC，很容易觉得这些 DAC 基础设施让人厌烦。`TADDR`、`PTR_this`、`dac_cast` 等看起来让代码“很乱”、更难理解。但只要稍微花点功夫，你会发现这些并不难学。把宿主地址与目标地址明确地区分开，本质上是一种强类型约束；我们越严谨，就越容易保证代码正确。

由于 DAC 可能运行在转储文件上，我们编进 mscordacwks.dll（msdaccore.dll）的那部分 VM 源码必须是非侵入式（non-invasive）的。具体来说，我们通常不希望做任何会向目标地址空间写入的事情，也不能执行任何可能立即触发垃圾回收（GC）的代码。（如果能延后 GC，可能还可以分配内存。）注意，_宿主_ 状态总是会被修改（临时变量、栈或本地堆的值等）；真正有问题的是修改 _目标_ 地址空间。为强制这一点，我们采用两种手段：代码拆分（factoring）与条件编译。在理想情况下，我们会重构 VM 代码，将侵入式行为严格隔离到与非侵入式函数分开的函数中。

不幸的是，我们拥有庞大的代码库，其中大部分在编写时从未考虑过 DAC。我们有大量“查找或创建”（find-or-create）语义的函数，以及许多函数一部分只做检查、另一部分会写入目标。有时我们用传入的标志位来控制这种行为，比如在加载器代码中就很常见。为了避免在使用 DAC 前必须完成对全部 VM 代码的巨大重构工作，我们采用第二种方法来防止进程外执行侵入式代码：定义预处理常量 `DACCESS_COMPILE` 来控制编进 DAC 的代码部分。我们希望尽可能少用 `DACCESS_COMPILE`；因此当 DAC 化一条新的代码路径时，我们会尽量在可行时进行重构。于是，一个具备“查找或创建”语义的函数应当拆分为两个函数：一个只尝试查找信息，另一个包装函数调用“查找”并在失败时创建。这样 DAC 路径就能直接调用“查找”函数，从而避免“创建”。

DAC 如何工作？
==============

如前所述，DAC 通过编组（marshaling）其所需数据，并在 mscordacwks.dll（msdaccore.dll）模块中运行代码来工作。它通过从目标地址空间读取来获取目标值，然后把这些值存入宿主地址空间，使 mscordacwks 中的函数可以对其进行操作，从而完成编组。这个过程仅在按需发生；如果 mscordacwks 的函数从不需要某个目标值，DAC 就不会去编组它。

编组原则
--------

DAC 维护一个已读取数据的缓存，以避免重复读取同一值的开销。当然，如果目标是“活”的，值可能会变化。我们只能在被调试进程保持停止（stopped）的期间假设缓存值有效。一旦允许目标继续执行，就必须清空 DAC 缓存。之后当调试器再次停止目标进行检查时，DAC 会重新获取这些值。DAC 缓存项的类型为 `DAC_INSTANCE`，其中包含（除其他数据外）目标地址、数据大小，以及用于存放已编组数据本身的空间。当 DAC 编组数据时，它会返回该缓存项中“已编组数据”部分的地址，作为宿主地址。

当 DAC 从目标读取一个值时，它会把该值按其类型所决定的大小，作为一段字节块进行编组。通过把目标地址作为缓存项的一个字段保存下来，它维持了从目标地址到宿主地址（缓存中的地址）的映射。在一次“停止—继续”之间，只要后续访问使用相同类型，DAC 对每个被请求的值最多编组一次。（如果我们用两种不同类型引用同一个目标地址，大小可能不同，DAC 会为新类型创建一个新的缓存项。）如果某个值已在缓存中，DAC 就能用其目标地址进行查找。也就是说，只要我们用相同类型访问了两个指针，就可以正确地比较两个宿主指针是否（不）相等。但这种“指针同一性”在类型转换后不再成立。此外，我们无法保证分别编组出来的值在缓存中的空间布局关系与它们在目标中的关系一致，因此用小于/大于来比较两个宿主指针是错误的。由于宿主与目标中的对象布局必须一致，我们可以用与目标相同的偏移量来访问缓存对象的字段。记住：编组对象中的任何指针字段都将是目标地址（通常声明为 `PTR` 类型的数据成员）。如果需要这些地址处的值，DAC 必须在解引用前先将其编组到宿主。

由于我们从与构建 mscorwks.dll（coreclr.dll）相同的源代码构建该 dll，调试器使用的 mscordacwks.dll（msdaccore.dll）必须与 mscorwks 的构建完全匹配。这一点很容易理解：不同构建之间我们可能为某个类型添加或移除字段，那么 mscorwks 中对象大小就会与 mscordacwks 中不同，DAC 将无法正确编组该对象。这带来一个看似显然但容易忽略的影响：对象中不能存在“仅在 DAC 构建中存在”或“仅在非 DAC 构建中存在”的字段。因此，如下声明会导致错误行为：

	class Foo
	{
		...
		int nCount;
	
		// 不要这样做！！DAC 构建中对象布局必须匹配
		#ifndef DACCESS_COMPILE
	
			DWORD dwFlags;
	
		#endif
	
		PTR_Bar pBar;
		...
	};

编组细节
--------

DAC 编组通过一组 typedef、宏和模板化类型来工作，它们在 DAC 构建中通常有一种含义，在非 DAC 构建中则有另一种含义。你可以在 [src\inc\daccess.h][daccess.h] 中找到这些声明。你也会在该文件开头看到一段很长的注释，解释了编写使用 DAC 的代码所需的细节。

[daccess.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/daccess.h

一个例子有助于理解编组的工作方式。常见的调试场景可以用下图表示：

![DAC 概览](./asserts/dac-overview.png)

图中的调试器可以是 Visual Studio、MDbg、WinDbg 等。调试器通过 CLR 调试接口（DBI）API 获取所需信息。必须来自目标的信息会经过 DAC。调试器实现“数据目标”（data target），其职责是实现 `ReadVirtual` 函数以读取目标内存。图中的虚线表示进程边界。

假设调试器需要显示托管应用中某个通过 ngen 生成的方法的起始地址，而该信息来自托管栈。我们假设调试器已经通过 DBI 得到一个 `ICorDebugFunction` 实例。接下来它会调用 DBI API `ICorDebugFunction::GetNativeCode`。这会通过 DAC/DBI 接口函数 `GetNativeCodeInfo` 进入 DAC，并传入该函数的 domain file 和元数据 token。下面的代码片段是对实际函数的简化，但它展示了编组的关键点而不引入无关细节。

	void DacDbiInterfaceImpl::GetNativeCodeInfo(TADDR taddrDomainFile,
	            mdToken functionToken,
	            NativeCodeFunctionData * pCodeInfo)
	{
		...
	
		DomainFile * pDomainFile = dac_cast<PTR_DomainFile>(taddrDomainFile);
		Module * pModule = pDomainFile->GetCurrentModule();
	
		MethodDesc* pMethodDesc = pModule->LookupMethodDef (functionToken);
		pCodeInfo->pNativeCodeMethodDescToken = pMethodDesc;
	
		// if we are loading a module and trying to bind a previously set breakpoint, we may not have
		// a method desc yet, so check for that situation
		if(pMethodDesc != NULL)
		{
			pCodeInfo->startAddress = pMethodDesc->GetNativeCode();
			...
		}
	}

第一步是获取托管函数所在的模块。我们传入的 `taddrDomainFile` 参数代表一个目标地址，但这里需要对它解引用，这意味着需要 DAC 编组该值。`dac_cast` 运算符会构造一个新的 `PTR_DomainFile` 实例，其目标地址等于 `domainFileTaddr` 的值。当我们把它赋给 `pDomainFile` 时，会发生到宿主指针类型的隐式转换。这个转换运算符是 `PTR` 类型的成员，编组就在这里发生。DAC 首先在缓存中查找该目标地址；如果没找到，它就从目标读取要编组的 `DomainFile` 实例的数据并拷贝进缓存；最后返回该已编组值的宿主地址。

现在我们可以在这个宿主端的 `DomainFile` 实例上调用 `GetCurrentModule`。该函数是一个简单的访问器，返回 `DomainFile::m_pModule`。注意它返回 `Module *`，这将是一个宿主地址。字段 `m_pModule` 的值是目标地址（DAC 会把 `DomainFile` 实例作为原始字节拷贝到缓存中）。不过该字段类型是 `PTR_Module`，因此当函数返回它时，DAC 会在转换为 `Module *` 的过程中自动编组它。这意味着返回值是宿主地址。现在我们有了正确的模块与方法 token，就具备获取 `MethodDesc` 所需的信息了。

	Module * DomainFile::GetCurrentModule()
	{
		LEAF_CONTRACT;
		SUPPORTS_DAC;
		return m_pModule;
	}

在该简化版本中，我们假设方法 token 是一个方法定义。因此下一步是对 `Module` 实例调用 `LookupMethodDef`。

	inline MethodDesc *Module::LookupMethodDef(mdMethodDef token)
	{
		WRAPPER_CONTRACT;
		SUPPORTS_DAC;
		...
		return dac_cast<PTR_MethodDesc>(GetFromRidMap(&m_MethodDefToDescMap, RidFromToken(token)));
	}

这会使用 `RidMap` 来查找 `MethodDesc`。如果你查看该函数的定义，会看到它返回一个 `TADDR`：

	TADDR GetFromRidMap(LookupMap *pMap, DWORD rid)
	{
		...
	
		TADDR result = pMap->pTable[rid];
		...
		return result;
	}

这代表一个目标地址，但它并不是一个“真正的指针”；它只是一个数值（尽管它代表一个地址）。问题在于 `LookupMethodDef` 需要返回一个我们可以解引用的 `MethodDesc` 的地址。为此，函数使用 `dac_cast` 到 `PTR_MethodDesc` 来把 `TADDR` 转成 `PTR_MethodDesc`。你可以把它看成是在目标地址空间里把 `void *` 转成 `MethodDesc *` 的对应物。实际上，如果 `GetFromRidMap` 返回的是 `PTR_VOID`（具备指针语义）而不是 `TADDR`（整数语义），代码会更“干净”一些。再次强调，return 语句中的隐式类型转换保证 DAC 会在必要时编组对象，并返回该 `MethodDesc` 在 DAC 缓存中的宿主地址。

`GetFromRidMap` 中的赋值语句通过索引数组来取得特定值。参数 `pMap` 是 `MethodDesc` 的某个结构字段的地址。因此，当 DAC 编组 `MethodDesc` 实例时，会把整个字段复制进缓存；也就是说 `pMap`（该结构的地址）是宿主指针，对其解引用不会涉及 DAC。另一方面，`pTable` 字段是 `PTR_TADDR`。这告诉我们 `pTable` 是一个“目标地址数组”，但它的类型表明它是可编组类型，因此 `pTable` 本身也是一个目标地址。我们通过 `PTR` 类型重载的索引运算符来解引用它。该运算符会取出数组的目标地址，并计算所需元素的目标地址。索引的最后一步会把该数组元素编组回 DAC 缓存中的宿主实例并返回其值。我们把元素值（一个 `TADDR`）赋给局部变量 result 并返回。

最后，为了得到代码地址，DAC/DBI 接口函数会调用 `MethodDesc::GetNativeCode`。该函数返回 `PCODE` 类型的值。该类型是一个目标地址，但我们不能对其解引用（它只是 `TADDR` 的别名），并且我们专门用它来表示代码地址。我们把该值存到 `ICorDebugFunction` 实例中并返回给调试器。

### PTR 类型

由于 DAC 会把目标地址空间中的值编组到宿主地址空间，因此理解 DAC 如何处理目标指针是基础。我们把用于编组的这些基础类型统称为“PTR 类型”。你会看到 [daccess.h][daccess.h] 定义了两个类：`__TPtrBase`（具有多个派生类型）和 `__GlobalPtr`。我们不直接使用这些类型；我们只通过一些宏间接使用它们。它们都只包含一个数据成员，用于给出该值的目标地址。对于 `__TPtrBase`，这是一个完整地址；对于 `__GlobalPtr`，这是一个相对地址，相对于某个 DAC 全局基址位置。“__TPtrBase” 中的 “T” 表示 “target（目标）”。顾名思义，我们用从 `__TPtrBase` 派生的类型来表示作为数据成员或局部变量的指针，用 `__GlobalPtr` 来表示全局变量与静态变量。

在实践中，我们只通过宏使用这些类型。[daccess.h][daccess.h] 的开头注释包含了所有这些宏的使用示例。有趣的是：这些宏在 DAC 构建中会展开为声明这些编组模板的具体实例类型，而在非 DAC 构建中则是空操作（no-op）。例如，下面的定义声明 `PTR_MethodTable` 作为表示 method table 指针的类型（约定是用 `PTR_` 前缀命名这类类型）：

	typedef DPTR(class MethodTable) PTR_MethodTable;

在 DAC 构建中，`DPTR` 宏会展开为声明一个名为 `PTR_MethodTable` 的 `__DPtr<MethodTable>` 类型。在非 DAC 构建中，该宏只是把 `PTR_MethodTable` 声明为 `MethodTable *`。这意味着 DAC 功能不会在非 DAC 构建中引入行为变化或性能退化。

更进一步，在 DAC 构建中，DAC 会自动编组声明为 `PTR_MethodTable` 类型的变量、数据成员或返回值，正如上一节示例所示。编组是完全透明的。`__DPtr` 类型重载了运算符函数以重新定义指针解引用与数组索引，并提供转换运算符以转换为宿主指针类型。这些操作会判断请求的值是否已在缓存中：若在，立即返回；若不在，则必须从目标读取并把值加载进缓存，然后再返回。如果你对细节感兴趣，负责这些缓存操作的函数是 `DacInstantiateTypeByAddressHelper`。

用 `DPTR` 定义的 `PTR` 类型在运行时中最常见；但我们也有用于全局/静态指针、受限用途数组、指向可变大小对象的指针，以及指向带虚函数的类的指针（这些类的虚函数可能需要从 mscordacwks.dll（msdaccore.dll）调用）。大多数这类都比较少见；如果需要，你可以参考 [daccess.h][daccess.h] 了解更多。

`GPTR` 与 `VPTR` 宏很常见，值得在此特别说明。它们的使用方式与外部行为都与 `DPTR` 十分类似：编组同样是自动且透明的。`VPTR` 宏用于为带虚函数的类声明可编组指针类型。之所以需要这个特殊宏，是因为虚函数表本质上是一个隐式的额外字段。DAC 必须单独编组它，因为其中的函数地址都是目标地址，DAC 必须把它们转换为宿主地址。这样处理这些类意味着 DAC 会自动实例化正确的实现类，使得在基类/派生类间进行转换变得不必要。声明一个 `VPTR` 类型时，还必须在 [vptr_list.h][vptr_list.h] 中列出它。`__GlobalPtr` 类型提供了通过 `GPTR`、`GVAL`、`SPTR`、`SVAL` 宏编组全局变量与静态数据成员的基础功能。全局变量的实现与静态字段几乎相同（都使用 `__GlobalPtr` 类），并且都需要在 [dacvars.h][dacvars.h] 中添加条目。DAC 中使用的全局函数在实现处不需要宏，但必须在 [gfunc_list.h][gfunc_list.h] 头文件中声明，以便其地址能自动提供给 DAC。[daccess.h][daccess.h] 与 [dacvars.h][dacvars.h] 的注释提供了更多关于声明这些类型的细节。

[dacvars.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/dacvars.h
[vptr_list.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/vptr_list.h
[gfunc_list.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gfunc_list.h

全局与静态的“值/指针”很有意思，因为它们构成了进入目标地址空间的入口点（DAC 的其他使用都要求你已经有一个目标地址）。运行时中的许多全局变量已经被 DAC 化。有时需要让一个先前未 DAC 化的（或新引入的）全局变量对 DAC 可用。通过使用合适的宏并在 [dacvars.h][dacvars.h] 中添加条目，你可以启用 dac table 机制（在 [dactable.cpp] 中实现）把该全局变量的地址保存到一个从 coreclr.dll 导出的表中。DAC 在运行时使用该表来确定：当代码访问某个全局变量时，应该在目标地址空间的哪里去查找。

[dactable.cpp]: https://github.com/dotnet/runtime/blob/main/src/coreclr/debug/ee/dactable.cpp

### VAL 类型

除了指针类型外，DAC 还必须编组静态与全局的“值”（即不是通过静态/全局指针引用的值）。为此我们提供了一组 `?VAL_*` 宏：全局值用 `GVAL_*`，静态值用 `SVAL_*`。[daccess.h][daccess.h] 的注释里有一张表，展示了各种形式的用法，并包含声明 DAC 化代码将使用的全局/静态值（以及全局/静态指针）的说明。

### 纯地址（Pure Addresses）

我们在 DAC 工作示例中引入的 `TADDR` 与 `PCODE` 类型是“纯目标地址”。它们实际上是整数类型，而不是指针类型，这能防止宿主代码错误地对其解引用。DAC 也不会把它们当成指针处理。具体来说，因为没有类型或大小信息，无法进行解引用或编组。我们主要在两种场景使用它们：把目标地址当作纯数据；以及需要对目标地址做指针算术（当然也可以用 `PTR` 类型做指针算术）。由于 `TADDR` 没有类型信息，当做地址算术时需要显式地把大小因素考虑进去。

我们还有一类不涉及编组的特殊 `PTR`：`PTR_VOID` 与 `PTR_CVOID`，它们分别是目标端的 `void *` 与 `const void *`。因为 `TADDR` 只是数值，不具备指针语义，所以如果我们在 DAC 化代码时把 `void *` 变成 `TADDR`（过去常见），往往需要额外的转换和修改，甚至在不为 DAC 编译的代码里也是如此。使用 `PTR_VOID` 更容易、更干净，因为它保留了 `void *` 期望的语义。如果我们 DAC 化的函数使用 `PTR_VOID` 或 `PTR_CVOID`，我们无法直接从这些地址编组数据，因为不知道应读取多少字节；这意味着我们不能解引用它们（甚至不能做指针算术），但这与 `void *` 的语义一致。与 `void *` 一样，我们通常在需要使用时把它们转换为更具体的 `PTR` 类型。我们还有 `PTR_BYTE` 类型，它是一个标准可编组的目标指针（支持指针算术等）。通常在 DAC 化代码时，`void *` 变为 `PTR_VOID`，`BYTE *` 变为 `PTR_BYTE`，这正符合直觉。[daccess.h][daccess.h] 中有注释进一步解释了 `PTR_VOID` 的用法与语义。

有时遗留代码会把目标地址存到类似 `void *` 这样的宿主指针类型里。这总是一个 bug，会极大地增加代码推理难度；并且在我们支持跨平台时会出问题（不同平台指针大小不同）。在 DAC 构建中，`void *` 是宿主指针，永远不应该包含目标地址。改用 `PTR_VOID` 可以表明该 void 指针实际上是目标地址。我们正在尝试消除所有这类用法，但有些在代码中相当普遍，需要一段时间才能彻底清理。

### 转换（Conversions）

在早期 CLR 版本中，我们使用 C 风格类型转换、宏与构造函数在类型间进行转换。例如，在 `MethodIterator::Next` 中，我们有如下代码：

	if (methodCold)
	{
		PTR_CORCOMPILE_METHOD_COLD_HEADER methodColdHeader
		            = PTR_CORCOMPILE_METHOD_COLD_HEADER((TADDR)methodCold);
	
		if (((TADDR)methodCode) == PTR_TO_TADDR(methodColdHeader->hotHeader))
		{
			// Matched the cold code
			m_pCMH = PTR_CORCOMPILE_METHOD_COLD_HEADER((TADDR)methodCold);
			...

`methodCold` 与 `methodCode` 都声明为 `BYTE *`，但实际上保存的是目标地址。第 4 行把 `methodCold` 转换为 `TADDR` 并作为 `PTR_CORCOMPILE_METHOD_COLD_HEADER` 构造函数参数；此时 `methodColdHeader` 明确是目标地址。第 6 行对 `methodCode` 又做了一次 C 风格转换。`methodColdHeader` 的 `hotHeader` 字段类型为 `PTR_CORCOMPILE_METHOD_HEADER`；宏 `PTR_TO_TADDR` 会从该 `PTR` 类型中提取原始目标地址并赋给 `methodCode`。最后第 9 行又构造了一个 `PTR_CORCOMPILE_METHOD_COLD_HEADER`；同样把 `methodCold` 转成 `TADDR` 传给构造函数。

如果你觉得这段代码过于复杂、令人困惑，那就对了：它确实如此。更糟糕的是，它没有对宿主/目标地址分离提供任何保护。从 `methodCold` 与 `methodCode` 的声明看，并没有理由把它们解释为目标地址。在 DAC 构建中如果把这些指针当作宿主指针去解引用，进程很可能会访问冲突（AV）。这段代码还展示了：任意指针类型（而非 `PTR` 类型）都可以被转换成 `TADDR`。既然这两个变量总是保存目标地址，它们应当是 `PTR_BYTE` 类型，而不是 `BYTE *`。

我们也提供了一种更规范的方式在不同 `PTR` 类型之间转换：`dac_cast`。`dac_cast` 是 DAC 感知版本的 C++ `static_cast`（CLR 编码规范要求用它代替 C 风格指针转换）。`dac_cast` 可以完成以下任意操作：

1. 从 `TADDR` 创建一个 `PTR` 类型
2. 把一个 `PTR` 类型转换为另一个 `PTR` 类型
3. 从先前已编组到 DAC 缓存中的宿主实例创建一个 `PTR`
4. 从 `PTR` 类型中提取 `TADDR`
5. 从先前已编组到 DAC 缓存中的宿主实例获取 `TADDR`

现在，假设 `methodCold` 与 `methodCode` 都声明为 `PTR_BYTE`，上述代码可改写为：

	if (methodCold)
	{
		PTR_CORCOMPILE_METHOD_COLD_HEADER methodColdHeader
		            = dac_cast<PTR_CORCOMPILE_METHOD_COLD_HEADER>(methodCold);
	
		if (methodCode == methodColdHeader->hotHeader)
		{
			// Matched the cold code
			m_pCMH = methodColdHeader;

你可能仍会认为这段代码复杂且令人困惑，但至少我们大幅减少了转换与构造的数量。我们也使用了能保持宿主/目标指针分离的结构，使代码更安全。尤其是：如果我们尝试做错误的事情，`dac_cast` 往往会生成编译期或运行时错误。一般而言，进行转换应当使用 `dac_cast`。

> ### 1. 什么是 DAC？它的作用是什么？
>
> **DAC (Data Access Component)** 的全称是**数据访问组件**。
>
> - **它的本质：** 它是 .NET 运行时（`coreclr.dll`）的一套“双胞胎”代码。开发人员把运行时里关于“如何读取内存数据”的代码拿出来，专门编译成了一个独立的动态库（在 Windows 上叫 `msdaccore.dll` 或 `mscordacwks.dll`）。
> - **它的作用：** 让**调试器**（外部工具，如 Visual Studio, WinDbg）能够“读懂”**目标程序**内部的复杂结构。
>
> **为什么要它？** 想象你在调试一个已经崩溃的程序（或者是查看一个 Dump 转储文件）。此时程序已经不运行了，你不能在程序内部运行任何函数。你手里只有一堆乱七八糟的内存二进制数据（0101...）。 你如何知道这块内存是一个 `String` 还是一个 `Array`？你如何知道它的长度？ **DAC 就是那本“解码字典”**。它告诉调试器：“在地址 0x123 开始的 4 个字节代表对象的类型，接下来的 4 个字节代表长度。”
>
> ### 2. 宿主指针 vs 目标指针：地址空间的“平行世界”
>
> 理解这个概念的关键在于：**调试器和被调试的 APP 是两个独立的进程。**
>
> 我们可以用**“两个房间”**来做比喻：
>
> - **房间 A (宿主/Host)：** 这是调试器（比如 Visual Studio）所在的房间。
> - **房间 B (目标/Target)：** 这是你的 APP 所在的房间。
>
> #### 什么是“目标指针” (Target Pointer)?
>
> 在房间 B 里，有一张桌子上放着一本书。书的**位置**（地址）是 `0x001`。这个 `0x001` 就是**目标指针**。 注意：这个地址只在房间 B 里有效。
>
> #### 什么是“宿主指针” (Host Pointer)?
>
> 调试器在房间 A 里运行。如果调试器尝试在自己的房间 A 里寻找 `0x001` 位置，它可能在相同位置找到一张椅子，或者根本什么都没有。 **如果你直接在调试器里使用目标地址，就会报错或读到错误的数据。**
>
> #### 什么是“解引用” (Dereferencing)?
>
> - **普通解引用：** 你手里有一个地址（指针），你顺着这个地址去“看”那里的内容。
> - **DAC 的挑战：** 调试器（房间 A）想看房间 B 里 `0x001` 地址的内容。它不能直接走过去看，因为它过不去。
> - DAC 的解法（编组/Marshaling）：
>   1. 调试器告诉 DAC：“我想看房间 B 里 `0x001` 的数据。”
>   2. DAC 调用系统接口（像伸出一个长手臂），把房间 B 里 `0x001` 的那块数据**复制一份**。
>   3. 把复制的数据拿到房间 A 的一张新桌子上（假设新地址是 `0x999`）。
>   4. 调试器现在去读 `0x999`，就看到了原本在房间 B 里 `0x001` 的内容。
>
> 在这个过程中：
>
> - `0x001` 是**目标指针**。
> - `0x999` 是**宿主指针**。
> - 把数据从 B 搬到 A 的过程叫**编组 (Marshaling)**。
>
> ### 3. DAC 的作用是什么？
>
> 可以利用第三方工具来访问目标APP的应用数据。**这正是它的核心用途。**
>
> 当你使用调试工具查看变量、堆栈、GC（垃圾回收）状态时，背后都是 DAC 在工作：
>
> 1. **SOS (Son of Strike) 扩展：** 这是 WinDbg 里著名的调试插件。你输入 `!dumpobj` 命令时，SOS 就会调用 DAC 来解析目标 APP 的内存，告诉你这个对象叫什么名字，里面有哪些字段。
> 2. **Visual Studio：** 当你在调试 .NET 程序，把鼠标悬停在一个变量上看到它的值时，VS 内部也在通过类似的机制（或直接使用 DAC）来跨进程读取数据。
>
> ### 总结
>
> - **DAC** 是运行时的“离线翻译机”。
> - **目标指针** 是被调试程序内存里的原始地址（在调试器里不能直接点开）。
> - **宿主指针** 是 DAC 把数据抓取过来后，存放在调试器内存里的临时地址。
> - **解引用** 就是“去地址指向的地方拿东西”。在 DAC 环境下，解引用意味着一次跨进程的内存拷贝。
>
> 如果没有 DAC，调试器看到的只是一堆无意义的字节流；有了 DAC，调试器才能把这些字节还原成我们能看懂的代码对象。

DAC 化（DACizing）
==================

何时需要 DAC 化？
----------------

每当你添加一个新特性，都需要考虑它的可调试性需求，并对代码进行 DAC 化以支持该特性。你还必须确保其他任何改动（例如修 bug 或清理代码）在必要时符合 DAC 规则；否则这些改动会破坏调试器或 SOS。如果你只是修改现有代码（而不是实现新特性），通常可以通过判断你修改的函数是否包含 `SUPPORTS_DAC` contract 来确定是否需要关心 DAC。该 contract 还有一些变体，例如 `SUPPORTS_DAC_WRAPPER` 与 `LEAF_DAC_CONTRACT`。你可以在 [contract.h][contract.h] 中找到注释解释它们的差异。如果你在函数中看到许多 DAC 特有类型，应当假设该代码会在 DAC 构建中运行。

[contract.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/contract.h

DAC 化确保引擎中的代码能与 DAC 正确协作。正确使用 DAC 将值从目标编组到宿主非常重要。在宿主中错误使用目标地址（或反之）可能会引用到未映射地址；即便地址被映射，值也会与期望的完全无关。因此，DAC 化主要涉及确保：DAC 需要编组的所有值都使用 `PTR` 类型。另一项主要任务是确保在 DAC 构建中不允许执行侵入式代码。在实践中，这意味着我们有时必须重构代码或添加 `DACCESS_COMPILE` 预处理指令。我们还希望确保添加适当的 `SUPPORTS_DAC` contract。该 contract 的使用向开发者表明该函数可在 DAC 下工作，这很明确，原因有二：

1. 如果之后我们从其他 `SUPPORTS_DAC` 函数调用它，我们知道它是 DAC 安全的，无需担心再次 DAC 化。
2. 如果我们修改该函数，需要确保改动对 DAC 安全；若从该函数新增对另一个函数的调用，也必须确保被调用函数对 DAC 安全，或仅在非 DAC 构建中进行调用。

> ### 1. 为什么要“DAC 化”？
>
> 原本 .NET 运行时的代码（比如查找某个类的方法）是为“房间 B”（APP 内部）设计的。在房间 B 里，所有的指针都是**直接可用**的。
>
> 现在，你想让“房间 A”（调试器）也运行这段代码，来帮你在调试时查找信息。但是，房间 A 里的代码无法直接读取房间 B 的内存。
>
> **“DAC 化”就是把这段代码改造一下，让它既能在房间 B 运行，也能在房间 A 运行。**
>
> ### 2. “DAC 化”具体要做哪几件事？
>
> 如果你是一名 .NET 运行时的开发者，你需要做以下三步改造：
>
> #### 第一步：换“指针”类型（核心工作���
>
> - **普通代码：** 使用 `MethodTable* pMT;`（这是普通指针，只能看自己房间的东西）。
> - **DAC 化后的代码：** 使用 `PTR_MethodTable pMT;`。
> - 效果：这个 PTR_开头的特殊指针就像安装了“自动感应器”。
>   - 当它在 APP 内部运行时，它表现得和普通指针一样。
>   - 当它在调试器里运行时，它会自动触发“长手臂”动作（编组），跨进程去抓取数据。
>
> #### 第二步：遵守“只许看，不许动”原则（非侵入性）
>
> 调试器应该只是观察者。如果你在调试一个程序时，调试工具竟然修改了程序的数据，或者触发了垃圾回收（GC），那程序可能直接就崩了。
>
> - 改造方法：
>   - 如果一个函数原来是“没有就创建一个”（Find or Create），在 DAC 化时，要把它拆开。调试器只能调用那个“查找”（Find）的部分，不准调用“创建”的部分。
>   - 使用 `#ifdef DACCESS_COMPILE`：这段代码就像一个开关，告诉编译器：“如果现在是编译给调试器用的版本，请把这段‘写入数据’的代码删掉。”
>
> #### 第三步：贴上“安全标签” (Contract)
>
> 你会看到代码里有 `SUPPORTS_DAC` 这样的标记。
>
> - **这就好比一个“质检合格证”：** 开发者在代码里写下这个标记，意思是：“我保证这个函数已经改造完成了，它的指针都换成了 `PTR_` 类型，而且它不会乱动目标程序的内存，调试器可以放心调用。”
>
> ### 3. `SUPPORTS_DAC`、`LEAF_DAC_CONTRACT` 等标签
>
> 这其实是给开发者看的**警告标志**：
>
> - 如果你正在写一个 DAC 化的函数，你只能调用其他也带有 `SUPPORTS_DAC` 标签的函数。
> - 如果你不小心调用了一个没带标签的普通函数，编译器或检查工具就会报错，因为它知道那个普通函数可能会导致调试器去直接读取错误的地址，从而引发崩溃。
