# Linux 高级 I/O

除了 Linux 提供的基础 I/O 函数（`open`、`read`）之外，还提供了一些高级 API。

这些函数大致分为三类：

- 用户创建文件描述符的函数，包括 `pipe`、`dup/dup2` 函数。
- 用户读写数据的函数，包括 `readv/writev`、`sendfile`、`mmap/munmap`、`splice`、`tee` 函数。
- 用户控制 I/O 行为和属性的函数，包括 `fcntl` 函数。

## pipe

创建一个管道（单向和双向）实现进程间通信。

```c
int pipe(int fd[2]);
```

管道传输的是字节流，这与 TCP 字节流的概念相同。但两者有一点区别。

应用程序往一个 TCP 连接中写入数据的多少是受对方设置的**窗口大小**和本端的**拥塞窗口**的大小决定的。而管道本身设置了一个限制值，它规定如果应用程序数据不走管道，该管道最多能被写入多少个字节数据。Linux 2.6.11 设置的默认值是 65536。我们也可以通过 `fcntl` 函数修改默认值。

## dup/dup2

把标准输入重定向到一个文件，或者把标注输出重定向到一个网络连接（比如 CGI 编程）。

```c
#include＜unistd.h＞
int dup(int file_descriptor);
int dup2(int file_descriptor_one,int file_descriptor_two);
```

dup 函数创建一个新的文件描述符，该新文件描述符和原有文件描述符 file_descriptor 指向相同的文件、管道或者网络连接。返回值是当前系统可用的最小整数值。

而 dup2 跟 dup 一样，只是返回值是返回第一个不小于 file_descriptor_two 的整数值。

## readv/writev

readv 是将数据从文件描述符读到分散的数据块中。

writev 是将分散的数据块中合并写入文件描述符中。

```c
#include＜sys/uio.h＞
ssize_t readv(int fd,const struct iovec* vector,int count)；
ssize_t writev(int fd,const struct iovec* vector,int count);
```

## sendfile

**sendfile** 函数在两个文件描述符之间直接传递数据（都是在内核中操作的），从而避免了数据从内核态到用户态的数据拷贝，效率很高，这被称为零拷贝。

```c
#include＜sys/sendfile.h＞
ssize_t sendfile(int out_fd,int in_fd,off_t* offset,size_t count);
```

## mmap、munmap

mmap 函数用于申请一段内存空间。**我们可以将这段内存作为进程间通信的共享内存，也可以将文件直接映射到内存中。**

munmap 函数则是释放 mmap 创建的这段内存空间。

```c
#include＜sys/mman.h＞
void*mmap(void* start,size_t length,int prot,int flags,int fd,off_t offset);
int munmap(void* start,size_t length);
```

