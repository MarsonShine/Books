# Docker/Kubernetes 中的网络

## 网络栈

包括网卡（Network Interface）、回环设备（Loopback Device）、路由表（Routing Table）以及 IPtables 规则。

单机环境下，两个容器是如何互相通信的呢？

首先 Docker 默认会在宿主机上创建一个名为 docker0 的网桥，凡是连接在 docker0 网桥上的容器，就可以通过它进行通信。

那么如何将这些容器连接到 docker0 网桥呢？

是通过 **Veth Pair** 的虚拟设备。

### Veth Pair

Veth Pair 是成对出现的，它像一个“网线”连接到容器端和宿主机。即一端插在容器中，以 eth0 的网卡体现。而在宿主机端同样有一个，这样就完成了网络互通。如下图所示：

![](/asserts/network-in-docker.png)

**不同节点的容器进行网络访问，是如何实现的呢？**

假设 Node A 的应用 A 发出请求到 NodeB 的应用 B，如发出 ping 192.168.3.67。那么该请求包首先会经过宿主机的网桥 docker0，然后宿主机将该网络转出（通过 NAT）给应用 B，并将源地址改成宿主机 IP。返回的过程也是如此。



