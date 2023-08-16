在 Linux 中 socket 就是一个文件，它是一个可读、可写、可控制、可关闭的文件描述符。

## 通用 socket 地址结构

socket 网络编程接口中表示的 socket 地址是结构体 `sockaddr`：

```c
#include <bits/socket.h>
struct sockaddr
{
	sa_family_t sa_family; // 地址族类型,通常与协议族类型对应
	char sa_data[14]; // 存放 socket 地址值
}
```

协议族和地址族的关系

| 协议族   | 地址族   | 描述              |
| -------- | -------- | ----------------- |
| PF_UNIX  | AF_UNIX  | UNIX 本地域协议族 |
| PF_INET  | AF_INET  | TCP/IPv4 协议族   |
| PF_INET6 | AF_INET6 | TCP/IPv6 协议族   |

`sa_data` 存放的 socket 地址值的含义和长度也会随协议族的不同而不同：

| 协议族   | 地址值含义和长度                                             |
| -------- | ------------------------------------------------------------ |
| PF_UNIX  | 文件的路径名，长度可达 108 字节                              |
| PF_INET  | 16 位端口号和 32 位 IPv4 地址，共6字节                       |
| PF_INET6 | 16 位端口号，32 位流标识，128 位 IPv6 地址，32 位范围 ID，共26字节 |

由上表可见，14 字节的 `sa_data` 是不足以容纳多协议族的地址值，所以 Linux 定义了一个新的通用 socket 地址结构体：

```c
#include <bits/socket.h>
struct sockaddr_storage
{
	sa_family_t sa_family; // 地址族类型,通常与协议族类型对应
	unsigned long int __ss_align; // 内存对齐
	char __ss_padding[128-sizeof(__ss_align)]; // 存放 socket 地址值
}
```

这个结构体不仅提供了足够大的空间用于存放地址值，而且是内存对齐的（这是__ss_align成员的作用）。

## 创建 socket

```c
#include <sys/types.h>
#include <sys/socket.h>
int socket(int domain,int type,int protocal);
```

参数 `domain` 对应的就是协议族。

参数 `type` 指服务类型。对于 domain 为 TCP/IP 协议族，服务类型为 `流服务（SOCK_STREAM）`，UDP 协议取的是 `数据报服务（SOCK_UGRAM）`。

返回参数为一个 socket 文件描述符，创建失败则返回 -1。

## 命名 socket

创建 socket，并没有指定使用该地址族中的哪个具体的 socket 地址。**将一个 socket与另一个 socket 地址绑定的过程称为命令 socket**。

```c
#include＜sys/types.h＞
#include＜sys/socket.h＞
int bind(int sockfd,const struct sockaddr *my_addr,socklen_t addrlen);
```

`bind` 将 my_addr 所指的 socket 地址分配给未命名的 sockfd 文件描述符，addrlen 参数指出该 socket 地址的长度。

## 监听 socket

socket 被命名之后，还不能马上接受客户连接，我们需要使用如下系统调用来创建一个监听队列以存放待处理的客户连接：

```c
#include＜sys/socket.h＞
int listen(int sockfd,int backlog);
```

`sockfd` 参数指定被监听的 socket。

`backlog` 参数提示内核监听队列的最大长度。监听队列的长度如果超过 backlog，服务器将不受理新的客户连接。

listen 成功时返回 0，失败则返回 -1 并设置 errno。

## 连接 socket

从监听队列中取出一个连接：

```c
#include＜sys/types.h＞
#include＜sys/socket.h＞
int accept(int sockfd,struct sockaddr* addr,socklen_t* addrlen);
```

`sockfd` 参数是执行过 listen 系统调用的监听 socket。

`addr` 参数用来获取被接受连接的远端 socket 地址，该 socket 地址的长度由 `addrlen` 参数指出。

`accept` 返回成功时会返回一个新的 socket 连接，唯一标识了被接受的这个连接，服务器可以通过读写该 socket 来与被接受连接对应的客户端通信。

> 注意：
>
> 服务器扫描监听队列并取出一个连接，这个时候服务器是不知道客户端的连接状态的，也不关心网络的变化。

## 发起连接

客户端需要通过如下系统调用来与服务器建立连接：

```c
#include＜sys/types.h＞
#include＜sys/socket.h＞
int connect(int sockfd,const struct sockaddr* serv_addr,socklen_t addrlen);
```

`sockfd` 参数由 socket 系统调用返回一个 socket。

`serv_addr` 参数是服务器监听的 socket 地址。

`addrlen` 参数则指定这个地址的长度。

返回 0 表示连接建立成功。`sockfd` 就唯一地标识了这个连接，客户端就可以通过读写 `sockfd` 来与服务器通信。

## 关闭连接

关闭对应的 socket 文件描述符：

```c
#include＜unistd.h＞
int close(int fd);
```

`fd` 参数是待关闭的 socket。**不过，`close` 系统调用并非总是立即关闭一个连接，而是将 fd 的引用计数减 1。只有当 fd 的引用计数为 0 时，才真正关闭连接。**多进程程序中，一次 fork 系统调用默认将使父进程中打开的 socket 的引用计数加 1，因此我们必须在父进程和子进程中都对该 socket 执行 close 调用才能将连接关闭。

## 读写数据

```c
#include＜sys/types.h＞
#include＜sys/socket.h＞
ssize_t recv(int sockfd,void* buf,size_t len,int flags);
ssize_t send(int sockfd,const void* buf,size_t len,int flags);
```

`recv` 读取的是 `sockfd` 上的数据，`buf` 和 `len` 参数分贝是指读缓冲区的位置和大小。**recv 成功时返回实际读取到的数据的长度，它可能小于我们期望的长度len。因此我们能要多次调用 recv，才能读取到完整的数据。recv 可能返回0，这意味着通信对方已经关闭连接了。**

`send` 是往 `sockfd` 写入数据，`buf` 和 `len` 参数分别指定写缓冲区的位置和大小。`send` 成功时返回实际写入的数据的长度。

## 如何重用 TIME_WAIT 占用的地址

在一般情况下，TCP 连接的 TIME_WAIT 状态是不允许其它进程重用的。

我们可以通过设置 socket 的 `SO_REUSEADDR` 选项强制使用被处于 TIME_WAIT 状态的连接占用的 socket 地址。

具体实现代码如下：

```c
int sock = socket(PF_INET, SOCK_STREAM, 0);
assert(sock >= 0);
int reuse = 1;
setsockopt(sock,SOL_SOCKET,SO_REUSEADDR,&reuse,sizeof(reuse));
struct sockaddr_in address;
bzero(&address,sizeof(address));
address.sin_family=AF_INET;
inet_pton(AF_INET,ip,&address.sin_addr);
address.sin_port=htons(port);
int ret=bind(sock,(struct sockaddr*)&address,sizeof(address));
```

这样，即使 socket 处于 TIME_WAIT 状态，与之绑定的 socket 地址可以立即重用。而且我们可以直接修改 Linux 内核参数 `/proc/sys/net/ipv4/tcp_tw_recycle` 来快速回收被关闭的 socket，从而使得 TCP 连接根本不用进入 TIME_WAIT 状态，进而就允许应用程序立即绑定该 socket 地址。

