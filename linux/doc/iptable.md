# IPtables 简介

## 概述

在现代世界中，大量的数据在机器之间交换。在大多数情况下，这种交换发生在两台不受信任的机器之间。例如，任何通过 HTTP 流动的数据都与应用程序运行的机器无关。

出于对隐私和数据保护的特别关注，机器必须将其网络限制在受信任的客户列表中。因此，考虑到这一点，我们通常在防火墙后面保护一个网络。

在本教程中，我们将讨论 iptables，**它是 Linux 机器的一个用户空间防火墙**。它根据用户定义的规则来过滤连接。在下面的章节中，我们将详细了解这些规则和它们的行为。

## 安装 iptables

通常 iptables 是预先安装在 Linux 发行版上的。

但是，它也可以通过各种包安装程序(如 apt 和 yum)进行安装。这样做将安装并启动 iptables 作为 Linux 中的一个服务。

我们可以验证否成功安装：

```
iptables -L -v
```

请注意，非 root 用户必须在执行上述命令时使用 sudo。它所做的就是输出它的默认规则：

```
Chain INPUT (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination

Chain FORWARD (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination

Chain OUTPUT (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
```

现在我们已经安装了 iptables，在实现包过滤（packet filtering）的自定义规则之前，我们将讨论一些基本概念。

## 表

顾名思义，iptables 维护着一个表，每一行都指定了一个过滤输入数据包的规则。主要有三种类型的表：

- **filter** - Linux 内核将为每个输入数据包在此表中搜索规则。根据该规则，数据包要么被接受，要么被丢弃。
- **nat** - 内核使用这个表来制定 NAT 规则。网络地址转换（NAT）允许我们改变数据包中的源地址或目的 IP 地址，iptables 可以对传入和传出的数据包这样做。
- **mangle** - 该表允许我们改变 IP 头。例如，我们可以改变输入数据包中的 TTL 值。

一般来说，过滤器表是使用最广泛的，因此它也是默认表。如果我们想选择其他表，例如，改变或添加一个 NAT 规则，我们可以使用 iptables 命令中的 -t 选项：

```
iptables -t nat -L -v
```

该命令将显示 nat 表的默认规则：

```
Chain PREROUTING (policy ACCEPT 0 packets, 0 bytes)
 pkts bytes target     prot opt in     out     source               destination

Chain INPUT (policy ACCEPT 0 packets, 0 bytes)
 pkts bytes target     prot opt in     out     source               destination

Chain OUTPUT (policy ACCEPT 0 packets, 0 bytes)
 pkts bytes target     prot opt in     out     source               destination

Chain POSTROUTING (policy ACCEPT 0 packets, 0 bytes)
 pkts bytes target     prot opt in     out     source               destination
```

## 链（Chains）

正如我们在上一节中提到的，表中的每一行都是一组规则，内核必须应用于输入数据包。所有这些规则被连锁成一组，称为链。

在过滤器表中有三种类型的链。

- **INPUT** - 该链包含应用于传入连接（incoming connection）的规则
- **FORWARD** - 这包含了必须只被转发而不被本地消耗的数据包的规则。例如，一个只转发数据到其他机器的路由器
- **OUTPUT** - 这条链包含对出站连接（outgoing connection）的规则

在每个链中，我们可以创建一些规则。每个规则由以下部分组成：

- 匹配表达式（Matching Expression） - 内核应用于过滤数据包的一个标准
- 目标（Target） - 内核对数据包执行的操作

**对于每一个连接，内核都会遍历该链，并在数据包上应用匹配的表达式。如果它找到一个匹配，它就在数据包上应用给定的目标。**

尽管有各种类型的目标，我们只讨论最常见的：

- **ACCEPT** - 允许数据包到达目标套接字
- **DROP** - 丢弃数据包，但不向客户端发送任何错误信息
- **REJECT** - 丢弃数据包并向客户端发送错误信息

## iptables 命令

iptables 暴露了一个同名的用户空间命令 - `iptables`。我们可以使用这个命令来添加或删除链中的规则。我们可以为影响所有连接的默认链添加规则，也可以根据匹配表达式创建新规则。`iptables` 命令为我们提供了一个广泛的数据包或连接特征列表来应用过滤器。

### 更改默认链

iptables 中的某些默认链不声明任何匹配表达式。因此，如果数据包不匹配任何自定义规则，内核将使用这些链:

```
$ iptables -L -v
 
Chain INPUT (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
 
Chain FORWARD (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
 
Chain OUTPUT (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
```

**所以在默认情况下，iptables允许所有输入和输出包通过。然而，我们可以改变这种行为，并为这些链中的任何一个添加一个新策略:**

```
iptables --policy FORWARD DROP
```

因此，iptables 将丢弃所有不在内核本地使用的数据包：

```
$ iptables -L -v
 
Chain INPUT (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
 
Chain FORWARD (policy DROP 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
 
Chain OUTPUT (policy ACCEPT 0 packets, 0 bytes)
pkts bytes target     prot opt in out   source destination
```

### 通过表达式匹配过滤报文

一般来说，机器不会公平的对待来自所有来源的连接或数据包。因此，适用于没有任何区别对待所有连接的默认链，有时是不够的。

iptables 允许我们根据很多特性来过滤连接，如源 I P地址、源端口和协议：

- 丢弃指定 IP 的所有数据包：

  ```
  iptables -A INPUT -s 10.1.2.3 -j DROP
  ```

  这将丢弃 IP 为 10.1.2.3 的机器的所有数据包。

- 根据指定端口丢弃所有数据包：

  ```
  iptables -A INPUT -p tcp --dport 8080 -s 10.1.2.3 -j DROP
  ```

  这个命令将阻止所有从 IP 为 10.1.2.3 的机器发送到 8080 端口的数据包：

  ```
  Chain INPUT (policy ACCEPT 0 packets, 0 bytes)
   pkts bytes target     prot opt in     out     source               destination
      0     0 DROP       tcp  --  any    any     10.1.2.3             anywhere             tcp dpt:8080
  
  Chain FORWARD (policy ACCEPT 0 packets, 0 bytes)
   pkts bytes target     prot opt in     out     source               destination
  
  Chain OUTPUT (policy ACCEPT 0 packets, 0 bytes)
   pkts bytes target     prot opt in     out     source               destination
  ```

- 通过指定协议丢弃所有数据包：

  ```
  iptables -A INPUT -p tcp --dport 22 -j DROP
  ```

  这个命令会阻止所有从来自 TCP 的 22 端口的数据包。因此这就不允许 SSH 连接。

### iptables Append 和 Delete 规则

正如我们前面所讨论的，规则在 iptables 中被串联起来。当一个数据包到达时，内核会识别该链并浏览它，直到找到一个匹配的表达式。然后，它将在数据包上应用定义的目标，从而决定放弃（DROP）、接受（ACCEP）或拒绝（REJECT）该数据包。

`iptables` 命令允许我们从这些链上添加或删除规则。例如，我们在上一节讨论的命令在 INPUT 链中添加了一条规则：

```
iptables -A INPUT -p tcp --dport 22 -j DROP
```

通过提供 `-A` 作为参数，我们向链中追加了一个新规则。当数据包到来时，内核将在接受所有连接的默认规则之前查找此规则。

于此类似，我们可以从链中删除某个规则：

```
iptables -D INPUT -p tcp --dport 22 -j DROP
```

任何时候，我们都可以列出链中所有的规则：

```
iptables -L -v
```

这将按照规则在链中添加的顺序输出规则列表。

## 保存 IP 表规则

到目前为止，我们添加的规则只是临时的，如果 iptables 或机器重新启动，内核将删除这些规则。

为了避免这种情况，我们必须在重启之前保存这些规则：

```
/sbin/iptables-save
```

这个命令一般工作在 Ubuntu 或基于 Debian 的发行版上。

针对 Red Hat 或 Centos 我们可以这样做：

```
/sbin/service iptables save
```

这将永久地将规则保存在存储中，内核将在启动时将规则重新加载到 iptables 中

## 总结

在这篇文章中，我们讨论了如何将 iptables 作为 Linux 机器中的防火墙使用。我们首先在 Linux 机器上安装了 iptables，并解释了它是如何作为内核的查询表来决定是否接受或放弃数据包的。

然后，我们使用 iptables 命令在防火墙中添加规则，根据 IP、端口和协议阻止或允许连接。

原文链接：https://www.baeldung.com/linux/iptables-intro