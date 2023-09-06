# Linux 性能调优、调试与测试

系统资源的限制一般分为两种：

- 软限制：是一个建议性的，最好不要超越的限制，如果要超越的话，系统可能会向进程发送终止信号以结束运行。
- 硬限制：一般指软限制的上限。普通程序可以减少这个限制值，只有 root 身份运行的程序才能增加该限制值。

可以通过命令 `ulimit` 来修改限制值，也可以通过直接修改 `/etc/security/limits.conf`：

```
*hard nofile max-file-number  -- 硬限制
*soft nofile max-file-number  -- 软限制
```

## 调整内核参数

在 `/proc/sys` 文件系统下提供了某些配置文件以供用户调整模块的属性和行为。通常一个配置文件对应一个内核参数，文件名就是参数的名字，文件的内容是参数的值。我们可以通过命令 `sysctl -a` 查看所有这些内核参数。

下面介绍一些可能会常用的参数：

### 文件系统相关

- `/proc/sys/fs/file-max`，系统级文件描述符限制。一般修改了这个值后，应用程序要把 `/proc/sys/fs/inode-max` 设置为 `/proc/sys/fs/file-max` 的 3~4 倍，否则可能会导致 i 节点不够。
- `/proc/sys/fs/epoll/max_user_watches`，一个用户能够往 epoll 内核时间表中注册的最大事件的总量。它是指该用户打开的所有 epoll 示例总共能监听的事件数量。

### 网络相关

内核中网络模块的相关参数都位于 `/proc/sys/net` 目录下，其中和 TCP/IP 协议相关的参数主要位于如下三个子目录中：core、ipv4 和
 ipv6。

- `/proc/sys/net/core/somaxconn`，指定 listen 监听队列里，能够建立完整连接从而进入 ESTABLISHED 状态的 socket 的最大数目。
- `/proc/sys/net/ipv4/tcp_max_syn_backlog`，它包含3个值，分别指定一个 socket 的TCP写缓冲区的最小值、默认值和最大值
- `/proc/sys/net/ipv4/tcp_rmem`，它包含3个值，分别指定一个 socket 的TCP读缓冲区的最小值、默认值和最大值。
- `/proc/sys/net/ipv4/tcp_syncookies`，指定是否打开TCP同步标签（syncookie）。同步标签通过启动 cookie 来防止一个监听 socket 因不停地重复接收来自同一个地址的连接请求（同步报文段），而导致 listen 监听队列溢出（所谓的SYN风暴）。

除了通过直接修改文件的方式来修改这些系统参数外，我们也可以使用 sysctl 命令来修改它们。这两种修改方式都是临时的。永久的修改方法是在 `/etc/sysctl.conf` 文件中加入相应网络参数及其数值，并执行 `sysctl -p` 使之生效，就像修改系统最大允许打开的文件描述符数那样。