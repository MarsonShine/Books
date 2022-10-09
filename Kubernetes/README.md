# Kubernetes查漏补缺

## Pause容器

kubernetes每次启动容器时，都会先启动"puase"容器，原因是什么呢？

[官方文档](https://kubernetes.io/docs/concepts/windows/intro/#pause-container)有特意提到：在 Kubernetes Pod 中，**首先要创建一个基础设施或“Pause”容器来承载容器**。在 Linux 中，组成 pod 的 cgroup 和 namespace 需要一个进程来维持它们的持续存在；而 pause 容器就提供了这一点。属于同一个 pod 容器，包括基础设施和工作者容器，它们共享同一个网络（相同的IPv4和/或IPv6地址，相同的网络端口空间）。Kubernetes 使用 pause 容器来允许工作容器崩溃或重启而不丢失任何网络配置。