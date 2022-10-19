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

### 跨主通信

**不同节点的容器进行网络访问，是如何实现的呢？**

假设 Node A 的应用 A 发出请求到 NodeB 的应用 B，如发出 ping 192.168.3.67。那么该请求包首先会经过宿主机的网桥 docker0，然后宿主机将该网络转出（通过 NAT）给应用 B，并将源地址改成宿主机 IP。返回的过程也是如此。

跨主通信主要通过三种方式实现得：

- UDP
- VXLAN
- host-gw

#### UDP

利用TUN设备（Tunnel）将 IP 数据包在用户态和内核态进行信息换地，所以性能很低，现在已经基本不使用了。其主要的通信过程如下：

1. IP 数据包进入内核网桥（docker0），发生了进程上下文切换（用户态到内核态）
2. IP 根据路由表进入 TUN 设备，回到 flannel0 设备，此时由内核态返回用户态
3. TUN 对 IP 数据包进行封装，让后将其进入宿主机网桥转发出去。此时又发生用户态到内核态、

网络结构流程如下

![](/asserts/flannel-udp.png)

#### VXLAN

Virtual Extensible LAN 虚拟可拓展局域网，XLAN 本身是在 Linux 系统内核所支持的一种网络虚拟化技术，所以它本身支持在内核态进行封包和解包。为了将两端（包括不同节点网段）通信，VXLAN 会在宿主机上设置一个特殊的网络设备将其连接起来。这个设备叫做 VTEP（VXLAN Tunnel End Point，虚拟隧道端点）

网络结构流程如下

![](/asserts/flannel-VXLAN.png)

VXLAN 最大的优势就是避免了内核态与用户态的转换，以及上下文赋值等操作，性能很好。



Kubernetes 的网络模型与 Docker 网络模型 不一致，Docker 是基于 [CNM](https://github.com/moby/libnetwork/blob/master/docs/design.md#the-container-network-model)，而 Kubernetes 是基于 [CNI](https://github.com/containernetworking/cni) 的。Kubernetes 内部实现了对 Docker CNM 的支持（具体的实现源码在 [dockershim](https://github.com/MarsonShine/kubernetes/tree/release-1.22/pkg/kubelet/dockershim)）。Kubernetes 在启动时会生成一个 cni0 来代替原来的 docker0。

为什么要自己重新实现一个网络模型呢？

这是由 Kubernetes 设计思想决定的，在 Kubertenes 启动基础容器之后，就要立即调用 CNI 的网络插件，为这个基础容器的 Network Namespance 配置符合预期的网络栈。

> 在 Kubernetes 最新的实现中已经移出了 dockershim，具体说明详见：[Why was the dockershim removed from Kubernetes? ](https://kubernetes.io/blog/2022/02/17/dockershim-faq/#why-was-the-dockershim-removed-from-kubernetes)
>
> tips: 而关于 docker 的重度使用者来说，这里的回答显然也是需要关注的：[Dockershim not needed: Docker Desktop with Kubernetes 1.24+](https://www.docker.com/blog/dockershim-not-needed-docker-desktop-with-kubernetes-1-24/)

