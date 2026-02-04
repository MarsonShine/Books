# 引言 #

在 Windows、Linux 以及其他非 Windows 平台上生成转储（dump）会遇到若干挑战。转储可能非常大，而且不同平台上的默认转储名称/位置并不一致。完整 core dump 的大小可以通过 "coredump_filter" 文件/标志在一定程度上控制，但即便在最小设置下仍可能过大，并且也可能缺少调试所需的全部托管（managed）状态。默认情况下，有些平台使用 _core_ 作为名称并将 core dump 放在启动程序的当前目录；其他平台会把 _pid_ 附加到名称中。要配置 core dump 的名称与位置通常需要超级用户权限。为了统一这些行为而要求使用超级用户权限并不是一个令人满意的选择。

我们的目标是在任何受支持的 Linux 平台上生成能够与 WER（Windows Error Reporting）崩溃转储相媲美的 core dump。至少我们希望实现以下能力：
- 自动生成体积尽可能小的 minidump。转储中包含的信息质量与数量，应当与传统的 Windows mini-dump 所包含的信息处于同一水平。
- 用户侧易于配置（而不是要求 _su_!）。

我们当前的方案是在运行时的 PAL 层拦截任何未处理异常，并让 coreclr 自身触发并生成一个“mini” core dump。

# 设计 #

我们调研了现有技术，例如 Breakpad 及其衍生方案（例如：SQL 团队内部的一个 MS 版本 _msbreakpad_ ....）。Breakpad 生成 Windows minidump，但它们与现有工具（如 Windbg 等）并不兼容；Msbreakpad 更是如此。虽然存在一个将 minidump 转换为 Linux core 的工具，但这似乎只是徒增一个额外步骤。_Breakpad_ 的确允许在进程内、在信号处理器中生成 minidump；但它会将可用 API 限制在“异步（async）”信号处理器（如 SIGSEGV）允许的范围内，并且 C++ 运行时也只能使用同样受限的一小部分功能。我们还需要把用于“托管”状态的一组内存区域加入转储，这要求加载并使用 _DAC_ 的（*）枚举内存接口。由于在 async 信号处理器中不允许加载模块，但允许 fork/execve，因此启动一个外部工具来加载 _DAC_、枚举内存区域列表并写出转储，是唯一合理的选择。这也可以顺便支持把转储上传到服务器。

\* _DAC_ 是 coreclr 运行时部分组件的一种特殊构建版本，允许在脱离现场的情况下检查运行时的托管状态（栈、变量、GC 状态堆等）。它提供的众多接口之一是 [ICLRDataEnumMemoryRegions](https://github.com/dotnet/runtime/blob/main/src/coreclr/debug/daccess/dacimpl.h)，该接口会枚举 minidump 为了提供良好调试体验所需的全部托管状态。

即便在生成工具中，_Breakpad_ 仍可用于“脱离现场”的方式生成转储，但当最终仍需要转换为原生 Linux core 格式、并且多数场景必须使用平台工具（如 _lldb_）时，Windows 风格的 minidump 格式似乎并无价值。它还会让 coreclr 增加对 Google 的 _Breakpad_ 或 SQL 的 _msbreakpad_ 源码仓库的构建依赖。唯一的优势是：breakpad minidump 可能会更小一些，因为 minidump 的内存区域可以按字节粒度划分，而 Linux core 的内存区域需要按页粒度划分。

# 实现细节 #

### Linux ###

当 coreclr 即将中止（通过 [PROCAbort()](https://github.com/dotnet/runtime/blob/main/src/coreclr/pal/src/include/pal/process.h)）进程时（原因可能是未处理的托管异常，或 SIGSEGV、SIGILL、SIGFPE 等异步信号），就会触发 core dump 生成。_createdump_ 工具与 libcoreclr.so 位于同一目录，通过 fork/execve 启动。子进程 _createdump_ 会被授予 ptrace 权限，并可访问崩溃进程的各种特殊 /proc 文件；而崩溃进程会等待直到 _createdump_ 结束。

_createdump_ 工具首先使用 ptrace 枚举并挂起目标进程的所有线程，收集进程与线程信息（状态、寄存器等），枚举 auxv 条目与 _DSO_ 信息。_DSO_ 是内存中的数据结构，用于描述目标进程已加载的共享模块；gdb 与 lldb 需要这些内存才能枚举已加载的共享模块并访问其符号信息。模块的内存映射来自 /proc/$pid/maps。程序本体或共享模块的内存区域不会被显式加入转储的内存区域列表。随后加载 _DAC_，并使用枚举内存区域接口构建内存区域列表（与 Windows 上的做法一致）。再将线程栈、以及 IP 周围一页代码加入列表。按字节粒度的区域会向上取整到页，并合并为连续区域。

来自 /proc/$pid/maps 的所有内存映射都会出现在 PT_LOAD 段中，即使这些内存并未实际写入转储。它们的 file offset/size 为 0。

在收集完所有崩溃信息后，就开始写入 ELF core dump：先创建并写出主 ELF 头；然后写 PT_LOAD note 段（每个内存区域一条记录）；写入进程信息、auxv 数据与 NT_FILE 条目。NT_FILE 条目由 /proc/$pid/maps 的模块映射构建。接着写入线程状态与寄存器。最后，将上面通过 _DAC_ 等收集到的所有内存区域从目标进程读出并写入 core dump。随后恢复目标进程的所有线程，_createdump_ 退出。

**严重内存损坏**

只要控制流能进入 signal/abort 处理器，并且 fork/execve 启动工具成功，那么 _DAC_ 的内存枚举接口在一定程度上能处理损坏；只是生成的转储可能缺少足够的托管状态而不够有用。我们可以考虑检测这种情况并改为写出完整 core dump。

**栈溢出异常**

与严重内存损坏类似，只要信号处理器（`SIGSEGV`）获得控制权，它就能检测大多数栈溢出场景并触发生成 core dump。但仍有很多情况下不会发生，OS 会直接终止进程。在更早版本（2.1.x 或更低）的运行时中存在一个 bug：对任何栈溢出都不会调用 _createdump_。

### FreeBSD/OpenBSD/NetBSD ###

在收集崩溃信息方面会有一些差异，但这些平台仍使用 ELF 格式的 core dump，因此工具中写 core 的那部分不应有太大差别。Linux 上用于授予 _createdump_ 使用 ptrace 以及访问 /proc 的机制，在这些平台上并不存在。

### macOS ###

在 .NET 5.0 中，createdump 支持在 macOS 上生成转储，但它生成的是 ELF core dump，而不是 MachO 转储格式。这是因为当时在生成端开发 MachO 写入器、以及在诊断工具端（dotnet-dump 与 CLRMD）开发 MachO 读取器的时间不够。这意味着在 5.0 运行时上获得的转储无法被原生调试器（如 gdb 与 lldb）使用，但 dotnet-dump 工具仍可以分析其中的托管状态。由于这一行为，需要额外设置一个环境变量（COMPlus_DbgEnableElfDumpOnMacOS=1），并与下面 Configuration/Policy 小节的变量一起设置。

从 .NET 6.0 开始，会生成原生 Mach-O core 文件，并且变量 COMPlus_DbgEnableElfDumpOnMacOS 已被弃用。

### Windows ###

从 .NET 5.0 起，Windows 也支持 createdump 以及下面的配置环境变量。其实现基于 Windows 的 MiniDumpWriteDump API，从而在我们支持的所有平台上提供一致的崩溃/未处理异常转储。

# 配置/策略 #

注意：在 docker 容器中生成 core dump 需要 ptrace 能力（--cap-add=SYS_PTRACE 或 --privileged 的 run/exec 选项）。

所有配置/策略通过环境变量设置，并作为选项传递给 _createdump_ 工具。

支持的环境变量：

- `DOTNET_DbgEnableMiniDump`：若设置为 "1"，启用 core dump 生成。默认是不生成转储。
- `DOTNET_DbgMiniDumpType`：见下文。默认值为 "2"（MiniDumpWithPrivateReadWriteMemory）。
- `DOTNET_DbgMiniDumpName`：若设置，则作为模板用于生成转储路径与文件名。格式规则见 “Dump name formatting”。默认是 _/tmp/coredump.%p_。
- `DOTNET_DbgCreateDumpToolPath`：**（仅 NativeAOT）** 若设置，指定 createdump 工具所在的目录路径。运行时会在该目录中查找 createdump 二进制文件。这在运行时未随附 createdump、需要“自带”转储生成工具的场景中很有用。该环境变量仅在 NativeAOT 应用中受支持，否则会被忽略。
- `DOTNET_CreateDumpDiagnostics`：若设置为 "1"，启用 _createdump_ 的诊断消息（TRACE 宏）。
- `DOTNET_CreateDumpVerboseDiagnostics`：若设置为 "1"，启用 _createdump_ 的详细诊断消息（TRACE_VERBOSE 宏）。
- `DOTNET_CreateDumpLogToFile`：若设置，则为写入 _createdump_ 诊断消息的文件路径。
- `DOTNET_EnableCrashReport`：在 .NET 6.0 或更高版本中，若设置为 "1"，createdump 还会生成 json 格式的崩溃报告，其中包含崩溃应用的线程与栈帧信息。崩溃报告名为转储路径/名称后追加 _.crashreport.json_。
- `DOTNET_EnableCrashReportOnly`：在 .NET 7.0 或更高版本中，与 DOTNET_EnableCrashReport 相同，但不生成 core dump。

DOTNET_DbgMiniDumpType 的取值：


|Value|Minidump Enum|Description|
|-|:----------:|----------|
|1| MiniDumpNormal                               | 仅包含捕获进程中所有现存线程的堆栈跟踪所需的信息。包含有限的 GC 堆内存与信息。 |
|2| MiniDumpWithPrivateReadWriteMemory (default) | 包含 GC 堆以及捕获进程中所有现存线程的堆栈跟踪所需的信息。 |
|3| MiniDumpFilterTriage                         | 仅包含捕获进程中所有现存线程的堆栈跟踪所需的信息。包含有限的 GC 堆内存与信息。 |
|4| MiniDumpWithFullMemory                       | 包含进程中所有可访问内存。原始内存数据会被放在末尾，使得初始结构可以在没有原始内存信息的情况下直接映射。该选项可能导致文件非常大。 |

（请参考 MSDN 中对上面所列 [minidump enum values](https://msdn.microsoft.com/en-us/library/windows/desktop/ms680519(v=vs.85).aspx) 的说明）

**命令行用法**

createdump 工具也可以在命令行中对任意 .NET Core 进程运行。转储类型可通过下面的命令行开关控制。默认是一个包含大多数调试所需内存与托管状态的 “minidump”。除非具备 ptrace（CAP_SYS_PTRACE）管理权限，否则你需要使用 sudo 或 su 运行，这与使用 lldb 或其他原生调试器进行附加调试的要求相同。

```
createdump [options] pid
-f, --name - dump path and file name. The default is '/tmp/coredump.%p'. These specifiers are substituted with following values:
   %p  PID of dumped process.
   %e  The process executable filename.
   %h  Hostname return by gethostname().
   %t  Time of dump, expressed as seconds since the Epoch, 1970-01-01 00:00:00 +0000 (UTC).
-n, --normal - create minidump.
-h, --withheap - create minidump with heap (default).
-t, --triage - create triage minidump.
-u, --full - create full core dump.
-d, --diag - enable diagnostic messages.
-v, --verbose - enable verbose diagnostic messages.
-l, --logtofile - file path and name to log diagnostic messages.
--crashreport - write crash report file (dump file path + .crashreport.json).
--crashreportonly - write crash report file only (no dump).
--crashthread <id> - the thread id of the crashing thread.
--signal <code> - the signal code of the crash.
--singlefile - enable single-file app check.
```

**Dump name formatting**

从 .NET 5.0 开始，支持如下 core pattern（见 [core](https://man7.org/linux/man-pages/man5/core.5.html)）格式化规则的一个子集：

    %%  单个 % 字符。
    %d  被转储进程的 PID（为保持 createdump 的向后兼容）。
    %p  被转储进程的 PID。
    %e  进程可执行文件名。
    %h  gethostname() 返回的主机名。
    %t  转储时间，以自 Epoch 起的秒数表示：1970-01-01 00:00:00 +0000 (UTC)。

**使用自定义 createdump 工具（仅 NativeAOT）**

在 NativeAOT 运行时未随附 createdump 工具的场景中，你可以通过 `DOTNET_DbgCreateDumpToolPath` 环境变量指定自定义目录路径：

```bash
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgCreateDumpToolPath=/path/to/directory
./myapp
```

运行时会在该目录中查找 `createdump` 二进制文件，从而允许你“自带”转储生成工具。注意：该环境变量仅在 NativeAOT 应用中受支持，否则会被忽略。

# 测试 #

测试计划是修改（仍然）私有的 debuggertests 仓库中的 SOS 测试，使其触发并使用生成的 core minidump。在我们拥有 ELF core dump 读取器之前，Linux 上的托管 core dump 还无法被 _mdbg_ 调试，因此只会修改 SOS 测试（Linux 上该测试使用 _lldb_）。