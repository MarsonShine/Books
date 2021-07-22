# 守护进程和 inetd 超级服务器

守护进程(daemon process)是在后台运行的进程，与控制终端无关。Unix 系统通常有许多守护进程(大约 20 到 50 个)，它们在后台运行，执行不同的管理任务。

缺少控制终端通常有一个副作用，是要由系统初始化脚本启动的(例如，在启动时)。但是守护进程如果得通过用户指定输入运行脚本开启，那么就要把守护进程从控制终端分离出来避免不要的交互，通过作业控制、终端会话管理、或是简单的在守护进程在后台运行的时候避免不必要的输出到终端。

这里有一些启动守护进程的方法：

1. **通过运行脚本**。在系统启动期间，许多守护进程是通过系统初始化脚本启动的。这些脚本通常位于 /etc 目录或名称以 /etc/rc 开头的目录中，但它们的位置和内容依赖于实现。由这些脚本启动的守护进程以超级用户权限开始。

   一些网络服务器也经常从这些脚本启动：inetd 超级服务器(后面将讨论)、Web 服务器和邮件服务器(通常是 sendmail)。我们将在后续中描述的 syslogd 守护进程也是由这些脚本之一启动的。

2. **通过 inetd 超级服务启动**。许多网络服务器是由 inetd 超级服务启动的。inetd 本身是从步骤 1 中的一个脚本启动的。inetd 会监听这些网络请求（Telnet、FTP 等），并且当一个请求到达时会调用那些实际的服务（Telnet Server、FTP Server 等）。

3. **通过 cron 守护进程启动**。cron 守护进程会定期执行，它调用的程序作为守护进程运行。在系统启动过程中，在步骤 1 中启动 cron守护进程本身。

4. **通过 corn 守护进程在未来某个时间点启动**。由 at 命令指定的。cron 守护进程通常在这些程序到达时间时启动它们，因此这些程序作为守护进程运行。

5. **通过用户终端、前台、后台启动（一般用作测试）**。守护进程可以从用户终端启动，既可以在前台也可以在后台。这通常在测试守护进程或重新启动因某些原因终止的守护进程时进行

## syslogd 守护进程

在 Unix 系统中，syslog 进程一般是通过初始化脚本伴随系统启动而启动的。它主要执行下面几步：

1. 读取配置文件（一般在 /etc/syslog.conf）指定了守护进程如何处理可以接收的每种类型的日志消息。这些信息是追加文件的形式（一个特殊的情况是文件 /dev/console，它将消息写入控制台）写入到特定用户(如果该用户已登录)，或转发到另一台主机上的 syslogd 守护进程。
2. 创建一个 Unix 域 Socket 并绑定到路径名 /var/run/log(在某些系统上是 /dev/log)。
3. 创建 UDP Socket 并绑定端口 514（即 syslog 服务）。
4. 路径 /dev/klog 是开放的。任何来自 kernel 内部的错误信息都作为该设备的输入显示。

syslogd 守护进程在一个无限循环中运行，该循环调用 select，等待它的三个描述符(步骤 2、3、4)中的任何一个可读；它读取日志消息，并执行配置文件对该消息执行的操作。如果守护进程接收到SIGHUP 信号，它将重新读取其配置文件。

通过创建 Unix 域数据报套接字并将消息发送到守护进程绑定的路径名，我们可以将日志消息从守护进程发送到 syslogd 守护进程，但更简单的接口是 syslog 函数，我们将在下一节中描述它。或者，我们可以创建一个 UDP 套接字并将我们的日志消息发送到环回地址和端口 514。

> 最近的更新实现中禁用 UDP 套接字的创建，除非管理员指定，否则允许任何人向这个端口发送 UDP 数据报会使系统受到拒绝服务攻击，有人可能会把文件系统填满(例如,通过填充日志文件)或导致了日志消息被丢弃(例如,通过溢出 syslog 的 socket 接收缓冲区)。
>
> syslogd 的各种实现之间存在差异。例如，Unix 域套接字被伯克利(berkeley)派生的实现使用，但是 System V 是使用一个 STREAMS日志驱动程序实现的。不同的 berkeley 派生实现对 Unix 域套接字使用不同的路径名。如果使用 syslog 函数，我们可以忽略所有这些细节。

## syslog 函数

由于守护进程没有控制终端，所以它不能直接将 fprintf 输出到 stderr。记录守护进程的消息的常用技术就是调用 syslog 函数。

```c
#include <syslog.h>
void syslog(int priority, const char *message, ... );
```

虽然这个功能最初是为 BSD 系统开发的，但是现在几乎所有的 Unix 供应商都提供了这个函数。POSIX 规范中对 syslog 的描述与我们在这里描述的一致。[RFC 3164](https://datatracker.ietf.org/doc/html/rfc3164) 提供了 BSD syslog 协议的文档。

priority 参数是 level 和 facility 的结合体，如图13.1和13.2所示。关于优先级的更多细节可以在 [RFC 3164](https://datatracker.ietf.org/doc/html/rfc3164) 中找到。message 类似于printf 的格式字符串，附加了 %m 规范，它被对应于 errno 的当前值的错误消息所取代。换行符可以出现在消息的结尾，但不是强制性的。

日志消息的级别介于 0 到 7 之间，如图13.1所示。这些是有序值。如果发送方未指定级别，则默认为 LOG_NOTICE。

| 级别        | 值   | 描述                     |
| ----------- | ---- | ------------------------ |
| LOG_EMERG   | 0    | 系统不可用（最高优先级） |
| LOG_ALERT   | 1    | 必须立即行动             |
| LOG_CRIT    | 2    | 临界条件                 |
| LOG_ERR     | 3    | 错误                     |
| LOG_WARNING | 4    | 警告                     |
| LOG_NOTICE  | 5    | 正常（默认状态）         |
| LOG_INFO    | 6    | 信息                     |
| LOG_DEBUG   | 7    | 调试（最低级别）         |

​																			图13.1 消息日志级别

日志消息还包含一个 facility 标识发送消息的进程类型。我们在图 13.2 中显示了不同的值。如果没有指定 facility，LOG_USER 是默认值。

| facility               | 描述                   |
| ---------------------- | ---------------------- |
| LOG_AUTH               | 安全/授权消息          |
| LOG_AUTHPRIV           | 安全/授权消息（私有）  |
| LOG_CRON               | cron 守护进程          |
| LOG_DAEMON             | 系统守护进程           |
| LOG_FTP                | FTP 守护进程           |
| LOG_KERN               | 内核消息               |
| LOG_LOCAL0～LOG_LOCAL7 | 本地使用               |
| LOG_LPR                | 行打印机系统           |
| LOG_MAIL               | 邮箱系统               |
| LOG_NEWS               | 网络新闻系统           |
| LOG_SYSLOG             | 通过 syslog 生成的消息 |
| LOG_USER               | 随机用户级消息         |
| LOG_UUCP               | LOG_UUCP 系统          |

例如守护进程对象下面的 syslog 函数进行调用 rename 函数时会发生未知的错误：

```c
syslog(LOG_INFO|LOG_LOCAL2, "rename(%s, %s): %m", file1, file2);
```

facility 和 level 的目的是允许给定一个 facility 将来自 /etc/syslog.conf 文件中所有消息被相同地处理，或者允许给定 level 的所有消息被相同地处理。例如，配置文件可以包含以下行：

```
kern.* /dev/console 
local7.debug /var/log/cisco.log
```

指定将所有内核消息记录到控制台，并将来自 facility 为 local7 的所有调试消息追加到 /var/log/cisco.log 文件中。

当应用程序第一次调用 syslog 时，它创建一个 Unix 域数据报套接字，然后调用 connect 到由 syslogd 守护进程创建的套接字的路径名(如 /var/run/log)。这个套接字保持打开状态，直到进程终止。或者进程可以调用 openlog 和 closelog。

```c
#include <syslog.h>
void openlog(const char *ident, int options, int facility); 
void closelog(void);
```

openlog 可以在第一次调用 syslog 之前调用以及 closellog 可以在应用程序发送完日志消息后调用。

ident 是一个字符串，将通过 syslog 添加到每个日志消息的前面。通常这是程序名。

options 参数是由图13.3 中一个或多个常量的逻辑或构成。

| options    | 描述                                            |
| ---------- | ----------------------------------------------- |
| LOG_CONS   | 如果不能发送到 syslogd 守护进程，日志到控制台。 |
| LOG_NDELAY | 立即打开创建套接字。                            |
| LOG_PERROR | 记录到标准错误以及发送到 syslogd 守护进程。     |
| LOG_PID    | 为每个消息记录进程 ID。                         |

​													图13.3 openlog options 参数

在调用 openlog 时，通常不创建 Unix 域套接字。相反，它在第一次调用 syslog 时打开。LOG_NDELAY 选项导致在调用 openlog 时创建套接字。

openlog 的 facility 参数为那些未指定 facility 的后续 syslog 调用指定了一个默认设施。一些守护进程调用 openlog 并指定该 facility(对于给定的守护进程，facility 通常不会更改)。然后它们在每次对 syslog 的调用中只指定级别(因为级别可以根据错误而改变)。

日志消息也可以由 logger 命令生成。这可以在 shell 脚本中使用，例如，将消息发送到 syslogd。