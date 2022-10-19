# 容器时代的名词解释

你应该知道的围绕容器的主要标准（尽管你不需要知道所有的细节）是：

- Open Container Initiative(OCI)：开放容器倡议，发布了关于容器和镜像的规范
- Kubernetes Container Runtime Interface(CRI)：容器运行时接口，它定义了 Kubernetes 和下面容器运行时之间的 API

下面这张图准确地展示了 Docker、Kubernetes、CRI、OCI、containd 和 runc 在这个生态系统中是如何结合在一起的：

![](https://www.tutorialworks.com/assets/images/container-ecosystem.drawio.png?ezimgfmt=rs:704x1183/rscb6/ng:webp/ngcb6)

## Docker

在我们对容器术语的梳理中，我们必须从 Docker 开始，因为它是使用容器的最流行的开发工具。而且，对很多人来说，"Docker" 这个名字本身就是 "容器" 这个词的同义词。

Docker 启动了这整个革命。Docker 创造了一个非常符合人机工程学的（好用的）容器工作工具 -- 它也被称为 docker。

很多人都喜欢它，它变得非常流行。

![](https://www.tutorialworks.com/assets/images/container-ecosystem-docker.drawio.png?ezimgfmt=rs:704x305/rscb6/ng:webp/ngcb6)

`docker` 被设计成安装在工作站或服务器上，并提供了一堆工具，使其作为开发者或 DevOps 人员能够轻松构建和运行容器。

`docker` 命令行工具可以构建容器映像，从注册表中提取它们，创建、启动和管理容器。

为了实现这一切，现在你输入 docker 时得到的体验是由图中这些项目组成的（还有其他项目，但这些是主要的）：

1. [docker-cli](https://github.com/docker/cli)：这是一个命令行工具，你使用 `docker ...` 命令进行交互。
2. [containerd](https://containerd.io/)：这是一个管理和运行容器的守护进程。它推送和拉取镜像，管理存储和网络，并监督容器的运行。
3. [runc](https://github.com/opencontainers/runc)：**这是低级别的容器运行时，或者说是实际创建和运行容器的东西**。）它包括 `libcontainer`，一个基于 Go 的本地实现，用于创建容器。

实际上，当你用 docker 运行一个容器时，你实际上是通过 Docker 守护进程运行它，它调用 containerd，然后使用 runc。

### Dockershim:Docker in Kubernetes

Docker 与 Kubernetes 是什么关系？

Kubernetes 包括一个叫做 dockershim 的组件，它允许它用 Docker 运行容器。

但实际上，Kubernetes 更倾向于通过任何支持 `Container Runtime Interafce(CRI)`的容器运行时来运行容器。

Docker 比 Kubernetes 更早，并没有实现 CRI。所以这就是 dockershim 存在的原因，基本上是把 Docker 栓在 Kubernetes 上。或者 Kubernetes 到 Docker 上，无论你喜欢哪种方式。

> Dockershim 是什么？
>
> 在现实世界中，shim 是：“垫片:用于对齐零件、使其贴合或减少磨损的垫圈或细条材料”
>
> 在技术术语中，垫片是软件系统中的一个组件，它作为不同 API 之间的桥梁，或作为一个兼容层。当你想使用一个第三方组件时，有时会添加一个垫片，但你需要一点胶水代码（glue code）来使其工作。

后续，Kubernetes 将直接取消对 Docker 的支持，而倾向于[只使用实现 CRI 的容器运行时](https://kubernetes.io/docs/setup/production-environment/container-runtimes/)。这可能意味着使用 containerd 或 CRI-O。

但这并不意味着 Kubernetes 将不能运行 Docker 格式的容器。containerd 和 CRI-O 都可以运行 Docker 格式（实际上是 OCI 格式）的镜像；它们只是无需使用 docker 命令或 Docker 守护程序。

### 什么是 Docker 镜像

许多人所说的 Docker 镜像实际上是以开放容器倡议(OCI)格式打包的镜像。

因此，如果你从 Docker Hub 或其他仓储中心拉出一个镜像，你应该能够用 docker 命令，或在 Kubernetes 集群上，或用 podman 工具，或任何其他支持 OCI 镜像格式规范的工具使用它。

这就是开放标准的好处--任何人都可以编写支持该标准的软件

## Container Runtime Interface(OCI)

CRI 是 Kubernetes 用来控制创建和管理容器的不同运行时的协议。

CRI 是你想使用的任何种类的容器运行时的抽象。因此，CRI 使 Kubernetes 能更容易地使用不同的容器运行时。

Kubernetes 项目不需要手动添加对每个运行时的支持，CRI API 描述了 Kubernetes 如何与每个运行时进行交互。因此，这就需要运行时来实际管理容器。只要它遵守 CRI API，它就可以做任何它喜欢的事情。

![](https://www.tutorialworks.com/assets/images/container-ecosystem-cri.drawio.png?ezimgfmt=rs:704x353/rscb6/ng:webp/ngcb6)

因此，如果您更喜欢使用 container 来运行您的容器，您是可以这样做的。或者，如果您更喜欢使用 CRI-O，那么您也可以这样做。这是因为这两个运行时都实现了 CRI 规范。

> 如何在 Kubernetes 检查运行时
>
> kubelet(在每个节点上运行的代理)负责向容器运行时发送启动和运行容器的指令。
>
> 您可以通过查看每个节点上的 kubelet 参数来检查正在使用哪个容器运行时。有一个选项——`container-runtime` 和——`container-runtime-endpoint` 用于配置要使用的运行时。

### containerd

containerd 是一个来自 Docker 的高级容器运行时，实现了 CRI 规范。它从注册表中提取映像，管理它们，然后移交给低级运行时，后者实际创建并运行容器进程。

containerd 从 Docker 项目中分离出来，使 Docker 更加模块化。

Docker 自己在内部使用 containerd。当你安装 Docker 时，它也会安装 containerd。

containerd 通过其 cri 插件实现了 Kubernetes CRI。

### CRI-O

CRI-O 是实现容器运行时接口(CRI)的另一种高级容器运行时。它是容器的另一种选择。它从注册中心提取容器映像，在磁盘上管理它们，并启动较低级的运行时来运行容器进程。

CRI-O 是另一个容器运行时。它诞生于 Red Hat、IBM、Intel、SUSE 等公司。

它是专门从头开始创建的，作为 Kubernetes 的一个容器运行时。它提供了启动、停止和重启容器的能力，就像 containerd 一样。

## Open Container Initiative(OCI)

OCI 是一个由科技公司组成的团体，他们维护容器镜像格式的规范，以及容器应该如何运行。

OCI 背后的想法是，你可以选择符合规范的不同运行时。这些运行时都有不同的底层实现。

例如，你可能有一个符合 OCI 的运行时用于你的 Linux 主机，一个用于你的 Windows 主机。

这就是拥有一个可以由许多不同项目实施的标准的好处。这种同样的 "一个标准，多种实现" 的方式到处都在使用，从蓝牙设备到 Java APIs。

### runc

[runc](https://github.com/opencontainers/runc) 是一个兼容 OCI 的容器运行时。它实现了 OCI 规范并运行容器进程。

runc 被称为 OCI 的参考实现。

> 什么是参考实现（reference implementation）？
>
> 参考实现是一个实现了规范或标准的所有要求的软件。它通常是根据规范开发的第一件软件。
>
> 在 OCI 的案例中，runc 提供了符合 OCI 要求的运行时的所有功能，尽管任何人都可以实现自己的 OCI 运行时。

runc 为容器提供了所有的低级功能，与现有的低级 Linux 特性，如namespace 和 controller group 进行交互。它使用这些功能来创建和运行容器进程。

下面几个是 runc 的替代品：

- [crun](https://github.com/containers/crun)用 C 编写的容器运行时(相比之下，runc 是用 Go 编写的)。
- [kata-runtime](https://github.com/kata-containers/runtime)来源于 [Katacontainers](https://katacontainers.io/) 项目，它将 OCI 规范实现为单个轻量级虚拟机(硬件虚拟化)
- [gVisor](https://gvisor.dev/) 来自谷歌，它创建具有自己内核的容器。它在名为 `runsc` 的运行时中实现了 OCI

> Windows上的 runc 等价于什么？
>
> runc 是一个在 Linux 上运行容器的工具。这意味着它可以在Linux、裸机或 VM 中运行。
>
> 在 Windows 上，情况略有不同。与 runc 相当的是微软的主机计算服务（Host Compute Service，HCS）。[它包括一个叫runhcs 的工具，它本身是 runc 的一个分叉](https://docs.microsoft.com/en-us/virtualization/windowscontainers/deploy-containers/containerd)，也实现了开放容器倡议的规范。

原文地址：

https://www.tutorialworks.com/difference-docker-containerd-runc-crio-oci/#understanding-docker