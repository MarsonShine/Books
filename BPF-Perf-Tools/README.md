# BPF之巅

## BPF、eBPF

BPF 的全称是伯克利数据包过滤器（Berkeley Package Filter，BPF）。

BPF 提供了一种在各种内核事件和应用程序事件发生时运行一小段程序的机制，在这种机制下就催生了很多新的编程技术。该技术将**内核变得可编程**，允许用户（包括非专业内核开发人员）定制和控制他们的系统，以解决现实问题。

BPF 是一个非常灵活而高校的技术，由指令集、存储对象和辅助函数等几部分组成。由于它采用了虚拟指令集规范，因此也可以视作一种虚拟机实现。这些指令由 Linux 内核的 **BPF 运行时模块**执行；该运行时提供两种运行机制：一个**解释器**和将 BPF 指令动态转换为本地化指令的**即时（JIT）编译器**。

BPF 指令必须先经过验证器（verifer）的安全性检查，以确保 BPF 程序本身不会崩溃和损坏内核。

BPF 目前主要的应用领域是：网络、可观测性和安全。

而 eBPF 是在 BPF 的拓展（extend Berkeley Package Filter）。

现在我们普遍提到的 BPF 其实就是指的 eBPF。

BPF 宣称“一次编译，到处运行”（Compile Once-Run Everywhere）的项目，即旨在支持将 BPF 程序**一次性编译为字节码**，保存后分发至其它机器执行。

## 辅助函数

### bpf_probe_read()

`bpf_probe_read()` 是一个特别重要的辅助函数。BPF 中的内存访问仅限于 BPF 寄存器和栈空间（BPF 映射表）。如果需要访问其它内存（如 BPF 之外的其它内核地址），就必须经过 `bpf_probe_read()` 函数来读取。这个函数会进行安全性校验，内部会检查函数即将访问的内存页是否已经映射在内存中，如果不在则直接返回错误，这样就保证了在 probe 上下文中不会发生缺页中断。这个函数是实现 tracing、monitoring、package processing 等功能的基础。

> 检查缺页中断的原因是提高安全性。因为如果发生缺页中断处理程序，则会暴露以下风险：
>
> 1. 缺页中断会陷入内核态，这样会打断 BPF 程序的执行。BPF 运行在内核态下的沙盒环境中，由自己独立的寄存器状态和栈空间。触发了缺页中断会保存并切换掉当前 BPF 的上下文转而去处理中断服务程序。这种上下文切换一是有开销，二是该操作是不可见的，容易位置的错误（如寄存器值、栈空间内容被修改等）。
> 2. 缺页中断的页面加载可能会被攻击者利用，加载恶意代码。

## kprobes

**kprobes**是 Linux 内核中得一种动态跟踪机制，运行在运行时安全地对内核的各种函数进行 hook，**不需要重启内核**。

它执行过程如下：

1. 在将要 hook 的目标函数地址中的字节内容复制并保存（目的是单步断点指令，腾出一个位置来”插播“运行应用程序）。也就是注册 kprobes。
2. 当内核执行到目标地址断点时，该断点处理函数会检查这个断点是否有 kprobes 注册的，如果有就会执行 kprobes 函数注册的自定义处理程序。
3. 在处理程序执行期间，可以访问内核状态，也可以调用原内核函数。
4. 原始指令会继续执行，指令流继续。
5. 当执行结束后或不需要 kprobes 时，原始的字节内容会被复制回目标地址上，这样指令就回到了它们最初始的状态。

bcc 和 bpftrace 就用到了这个接口。

## uprobes

**uprobes** 与 kprobes 类似，只不过 uprobes 是用户级动态追踪机制，可以用于跟踪和分析用户空间程序的执行。

uprobes 可以在用户态程序的以下位置插入自定义函数（hook）：函数入口、特定偏移处和函数返回处。

它的执行过程与 kprobes 是一致的。

## BCC

**BCC**由三个部分组成。

- 一个 C++ 前端 API，用于内核态的 BPF 程序的编制，包括：
  - 一个预处理宏，负责将内存引用转换为 `bpf_probe_read()` 函数调用（在未来的内核中，还包括使用 bpf_probe_read() 的变体）。
- 一个 C++ 后端驱动：
  - 使用 Clang/LLVM 编译 BPF 程序。
  - 将 BPF 程序装载到内核中。
  - 将 BPF 程序挂载到事件上。
  - 对 BPF 映射表进行读/写。
- 用于编写 BCC 工具的语言前端：Python、C++ 和 Lua。

下面是 BCC 的内部架构图：

![](./asserts/1.png)

![](./asserts/2.png)

### BCC 装载 BPF 程序的执行过程

1. 创建 Python BPF 对象，将 BPF C 程序传递给该 BPF 对象。
2. 使用 **BCC 改写器**对 BPC C 程序进行预处理，将内存访问替换为 `bpf_probe_read()`调用。
3. 使用 Clang 将 BPF C 程序编译为 **LLVM IR**。
4. 使用 BCC codegen 根据需要增加额外的 LLVM IR。
5. **LLVM 将 IR 编译为 BPF 字节码**。
6. 如果用到了映射表，就创建这些映射表。
7. 字节码被传送到内核，并经过 BPF 验证器的检查。
8. 事件被启用，BPF 程序被挂载到事件上。
9. BCC 程序通过映射表或 `perf_event` 缓冲区读取数据。

上述内容其实我们可以简化一下：

- 开发者用 Python 编写的 BPF 程序，BCC 前端进行检查和分析，最后输出 C 语言代码。
- LLVM 将 C 语言代码编译生成 **BPF 字节码**。
- 通过调用 BPF 的系统调用函数（辅助函数），将 BPF 字节码加载到内核的 BPF 虚拟机。
- 在虚拟机触发事件调用时（加载 BPF 字节码），就会触发编写的处理函数。

## bpftrace

**bpftrace**已经演变成为了一种编程语言。这些 bpftrace 特性会按照事件源、动作和一般特性。

下面是一个检查 IO 负载情况的 bpftrace 代码：

```bash
#!/usr/bin/bpftrace

// 在磁盘IO开始时记录时间戳 
BEGIN
{
    printf("Tracing disk I/O... Hit Ctrl-C to end.\n");
}

// 追踪bio请求结构
tracepoint:block:block_rq_issue
{
  @start[tid] = nsecs; 
}

// 追踪bio请求完成
tracepoint:block:block_rq_complete
{
  @ns[tid] = nsecs - @start[tid];
  @bytes[tid] = args->bytes; 
  delete(@start[tid]);
}

// 聚合结果
END
{
  clear(@start);
  printf("Duration (ns)  Bytes \n");
  print(@ns, @bytes);
  clear(@ns); 
  clear(@bytes);
}
```

这个程序使用了 `tracepoint` 追踪块设备 IO 的发出和完成时刻。记录每次 IO 的字节数和持续时间，最终聚合打印出总体结果，反映 IO 负载情况。







