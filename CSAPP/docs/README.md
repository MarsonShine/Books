# 如何查看汇编代码（C/C++）

```
linux> gcc -Og -S mstore.c
```

上述命令会生成一个 mstore.s 的汇编文件。

```
linux> gcc -Og -c mstore.c
```

上述命令会编译并汇编该代码，进而生成目标代码文件 mstore.o，它是二进制格式文件，无法直接查看。

如果我们要查看 mstore.o 的二进制代码文件该如何查看呢？我们可以使用下面指令

```
linux> gdb x/14xb multstore
```

上述命令告诉 GDB 显示从函数 multstore 处地址开始的 14 个十六进制格式（简写 x）表示的字节（简写 b）。

也可以使用反汇编器：

```
linux> objdump -d mstore.o
```

**生成实际可执行的代码需要对一组目标代码文件运行连接器，而这一组目标代码文件中必须含有一个 main 函数。**有了 main 函数我们就可以生成可执行文件：

```
linux> gcc -Og -o prog main.c mstore.c
```

当然我们也可以直接对可执行文件 prog 进行反汇编，的出来的结果与 `objdump -d mstore.o` 几乎一样。

```
linux> objdump -d prog
```

> 其实也是有些微差别的，如代码存储的地址不一样，因为生成可执行文件的时候 linker（链接器）将代码转移到另外的地址空间存放。第二不同的地方是链接器在调用函数 mult2 会插入 callq 指令调用函数需要的地址。这也是链接器的作用，链接各个代码片段（找到调用函数匹配的位置）合在一起最终生成可执行文件。最后一个区别是末尾多了两个 nop 指令，这是对程序没有任何影响的指令，其目的是内存对齐，为了更好的放置下一个代码块。

汇编代码会因为操作系统的不同格式也有区别，我们可以指定汇编代码的格式，如 Intel 汇编格式：

```
linux> gcc -Og -S -masm=intel mstore.c
```

# 一个程序的编译过程

有两个代码，sum.h、main.cpp，分别如下：

```c++
// main.c
#include "sum.h"

int sum(int *a, int n);
int array[2] = {1, 2};

int main()
{
    int val = sum(array, 2);
    return val;
}

// sum.h
int sum(int *a, int n)
{
    int i, s = 0;
    for (i = 0; i < n; i++)
    {
        s += a[i];
    }
    return s;  
}
```

如何把它编译成可执行文件（Linux下的ELF，windows下的PE文件格式）呢

首先执行如下命令

```
gcc -c .\main.cpp
```

会直接生成`.o`文件格式，该文件是**可重定位的目标文件（relocatable object file）**，后续是需要连接器将这些目标文件链接合并起来生成可执行文件的。

在生成目标文件的时候，其实编译器还进行两个步骤，只是上述进行合并了。

首先是编译器驱动程序会将目标代码文件生成文件格式为`.i`的中间代码文件，继而编译成汇编代码文件格式`.s`:

```
gcc -S .\main.cpp
```

再由汇编器将汇编代码文件生成目标文件`.o`:

```
gcc -c .\main.s
```

最后再由链接器将目标文件合并成最终的可执行文件：

```
gcc -o main main.o
```

