# 链接器

链接是将各种代码和数据片段收集并组合成为单个可执行的文件的过程，这个文件可以被加载（复制）到内存并执行。链接可以执行于编译时，也就是在源代码被翻译成机器代码阶段；也可以执行于加载时，即程序被加载器（loader）加载到内存并执行；甚至是运行时，也就是应用程序来执行。

**编译驱动程序的最终目的就是将 ASCII 源代码翻译成可执行的目标文件。**

运行过程就是 

1. 源代码 `main.c`
2. 将源代码转成中间语言（C 预处理器将 `main.c` 翻译成 `main.i`）
3. 将中间语言转为 ASCII 汇编语言文件（C 编译器将 `main.i` 转成 `main.s`）
4. 将汇编语言文件转成可重定向的目标文件（汇编器将 `main.s` 转成 `main.o`）

链接器的职责就是将上述步骤生成的可重定向的目标文件(`main.o`)链接在一起生成一个可执行的目标文件。

而加载器就是将可执行目标文件加载（复制）到内存中执行。

## 静态链接器

以一组可重定位的目标文件和命令参数作为输入，生成一个完全可链接的可以加载和运行的可执行文件作为输出。

链接器的一个基本事实就是：**目标文件纯粹就是字节的集合，其中包含程序代码和数据程序以及用来引导链接器和加载器的数据结构**。将这些块连接起来就能确定运行的位置。

## 目标文件

目标文件三种形式

1. 可重定向目标文件：**包含二进制代码和数据**，可以在编译时重定向目标文件，将结合起来生成一个可执行文件
2. 可执行目标文件：包含**二进制代码和数据**，可以加载（复制）到内存中并执行
3. 共享目标文件：是一种特殊的可执行的目标文件，可以加载或运行时动态的加载（复制）到内存中执行。

编译器和汇编器生成可重定向的目标文件

链接器生成可执行的目标文件

## 符号表

每个可重定向目标文件中都有一个符号表：包括符号定义 m 以及符号引用的信息

符号表有三种表现形式：

1. 模块 m 定义的以及被其它模块引用的**全局符号**，全局链接器符号对应于非静态的 C 函数和全局变量。
2. 被其它模块定义以及被模块 m 引用的全局符号，也叫”外部符号“。对应于其它模块定义的非静态的 C 函数和全局变量。
3. 模块 m 定义的以及被自己引用的局部符号，在 m 模块中任意可见，但是无法被其它模块引用。

符号表的基本数据结构：

```c
typedef struct {
	int name;	// 字符串表中的字节偏移
	char type : 4,
		binding : 4;	// 表示符号是本地还是全局的
	char reserved;
	short section;	// 到头部表的索引
	long value;	// 符号地址，对于可重定位的模块来说就是举例目标的节起始位置的偏移
	long size;	// 目标的大小（以字节为单位）
} Elf64_Symbol;
```

符号表中节点的描述

| 节                                                           | 描述                             |
| ------------------------------------------------------------ | -------------------------------- |
| .text                                                        | 已编译的机器代码                 |
| .data                                                        | 已初始化的全局变量和全局静态变量 |
| .bss                                                         | 未初始化的全局变量和全局静态变量 |
| COMMON                                                       | 未初始化的全局变量               |
| .rodata、.symtab、.rel.text、<br />.rel.data、.debug、.line、<br />.strtab |                                  |

## 符号解析

将所有模块打包成一个单独的文件，这个文件被称为静态库。它可以作为链接器的输入，当链接器构造出可执行文件时，只用复制静态库中被应用程序引用的目标模块。

完成符号解析就能把代码中的每个符号引用和之前的符号表中的符号定义关联上，此时链接器就能直到它的输入目标模块的**代码节和数据节**的确切大小了。然后链接器利用**重定位节和符号定义**以及**重定位节的符号引用**来进行重定位（合并输入模块，将相同符号类型的合并在一起）**给每个符号分配正确的运行地址**。

**而重定位节的符号引用就是用来修改代码节和数据节中的符号引用的，让其指向正确的运行地址（修改节又需要依赖重定位条目）。链接器会根据重定位条目结构（引用节偏移，符号标识，符号类型，偏移值）以及内部的计算算法来确认重定向地址。**

## 动态链接库

表示共享的库，可以加载到任意内存，可以在内存中的程序链接起来的。与静态链接库不同，它需要将数据节和代码节复制到内存中执行，而动态链接则不需要。

创建可执行文件时，静态执行一些链接，然后再程序加载时动态完成链接过程。其表现形式如 C# 中的 DLL 尾缀文件， Linux 中的 wo 尾缀文件。

## 位置无关的代码

由于共享库被不同的进程（应用程序）访问，如果我选择给每个共享库都添加一个专有的地址空间的话，这个会带来严重的空间浪费，因为在众多程序中也许只有其中一个库被引用到了，其它没有引用的共享库依然分配了空间。并且如果其中的共享库发生了变化，重新编译还要确定原来的空间大小是否依然合适。这样很难管理。

而现在用一种新的编辑技术，使得把共享库加载到内存的任何位置，而不需要修改链接器，这样**无限进程都可以共享这个共享模块的代码端的一个副本**。

像这种**可以加载而无需重定向的代码就被称为位置无关的代码**（是靠 PCI 数据引用以及 PCI 函数调用实现的）

# 总结

链接器的主要任务就是**符号解析和重定位**

- 符号解析：**将目标文件的每个全局符号都绑定到一个唯一的定义**
- 重定位：**确定每个符号的最终内存地址，并修改对那些目标的引用**

# 问题

还有很多问题没有彻底弄明白，比如**重定位的内存地址计算**，这块实在是难啃，等以后看吸收其它更多的知识再回过头来看看吧。