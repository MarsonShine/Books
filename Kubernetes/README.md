# Kubernetes查漏补缺

## Pause容器

kubernetes每次启动容器时，都会先启动"puase"容器，原因是什么呢？

[官方文档](https://kubernetes.io/docs/concepts/windows/intro/#pause-container)有特意提到：在 Kubernetes Pod 中，**首先要创建一个基础设施或“Pause”容器来承载容器**。在 Linux 中，组成 pod 的 cgroup 和 namespace 需要一个进程来维持它们的持续存在；而 pause 容器就提供了这一点。属于同一个 pod 容器，包括基础设施和工作者容器，它们共享同一个网络（相同的IPv4和/或IPv6地址，相同的网络端口空间）。Kubernetes 使用 pause 容器来允许工作容器崩溃或重启而不丢失任何网络配置。

## DaemonSet如何保证每个Node上有且只有一个被管理的Pod？

DaemonSet 首先会从 Etcd 中获取所有的 Node 列表，然后遍历。这个时候就能可以检查，当前遍历的 Node 是否迭代 `name=fluented-elasticsearch` 标签的 pod 运行。

检查的结果：

1. 没有该标签的 Pod，那么就在该 Node 上创建这样的 Pod
2. 找到匹配的 Pod，如果数量多余1，那就要把其它多余的 Pod 从该 Node 移除；
3. 只有一个就什么都不用做了

## Job的并行调度

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: pi
spec:
  # 控制并行作业
  parallelism: 2 # 一个job在任意时间内最多同时运行几个（即同时启动多少个pod）
  completions: 4 # 一个job至少要完成的几个任务（即完成的pod数）
  template:
  	...
  backoffLimit: 4 # 该job发生失败重启的最大次数
  activeDeadlineSeconds: 100 # job最长运行的时间 s
```

上述的 `parallelism` 和 `completions` 的效果就是，Job一启动就会创建两个pod并进行调度，完成了之后这两个pod会释放。但是由于没有满足 `completions:4` 的完成数，所以控制器又会立即启动两个 pod，成功了之后整个Job任务才算完成。

所以 JobController 操作的对象直接就是 Pods，内部很简单就是根据设置的这些参数（正在运行的 pod 数，已经成功退出的 pod 数目，以及 `parallelism` 和 `completions` 继而调用 Kubernetes API 来完成的）

**而 CronJob 操作的对象是 Job 控制器。**

关于Job的使用模式，详见：[Job patterns](https://kubernetes.io/docs/concepts/workloads/controllers/job/#job-patterns)

## Operator工作原理

实际上就是利用 Kubernetes 的自定义 API 资源（CRD），来描述我们想要部署的“有状态应用”；然后在自定义控制器里，根据自定义 API 对象的变化，来完成具体的部署和运维工作。

也就是说编写自定义 Operator 其实跟写一个自定义的控制器差不多。

## Volume 如何实现持久化的

什么是 Volume？**其实就是将一个宿主机上的目录，跟一个容器里的目录绑定挂载在一起**。

而持久化 Volume 就是将这个**宿主机上的目录的内容进行持久化**。而通常在 volume 节点配置的 `hostPath` 和 `emptyDir` 是不具有持久性的。所以要想持久化就需要依赖一个远程存储服务，比如：远程文件存储（NFS、FlusterFS）、远程块存储（例如 AWS 提供的存储服务等）。

Kubernetes 就是使用这些远程服务来为容器准备一个具有持久化能力的宿主机目录。整个过程分为**两个阶段**

1. Attach 阶段：挂载远程存储服务（远程磁盘）操作

   > 如当一个 Pod 调度到一个节点上时，kubelet 就要负责为这个 Pod 创建它的 Volume 目录，这个时候 kubelet 就会知道此时你申明的 Volume 类型了，随机做对应的 attach 操作。

2. Mount 阶段：有了存储的地方（attach）那么就要使用这个远程磁盘了；这个阶段就会格式化磁盘设备，然后将它挂载到宿主机上指定的目录上。

在 Kubernetes 中实际上是有专门的控制器来处理持久化存储的，叫 VolumeController。在源码中是以 [startPersistentVolumeBinderController](https://github.com/kubernetes/kubernetes/blob/master/cmd/kube-controller-manager/app/core.go#L244) 体现的。是有 kube-manager-controller 负责维护。而具体的第二部则是依赖于各个节点的具体目录，所以必须是在 Pod 对应的宿主机上，这个控制循环的名字，叫作：[volume.attachdetach.reconciler](https://github.com/kubernetes/kubernetes/blob/master/pkg/controller/volume/attachdetach/reconciler/reconciler.go#L68)，它运行起来之后，是一个独立于 kubelet 主循环的 Goroutine。

> 注意，我们不应该将一个宿主机上的目录当作 PV 使用，因为这种本地存储行为是可控的，当发生本地磁盘被写满后就会造成应用不可用甚至宕机。并且不同的本地目录之间也缺乏 I/O 隔离机制。所以最佳实践是，一块额外挂载在宿主机上的磁盘或块设备（简称一个 PV 一个盘）

## CSI 存储插件

CSI 存储插件包含一个外部组件和一个插件服务；其中外部组件包含三大组件：

1. **Driver Registrar**：负责将插件注册到 kubelet 里面，是通过请求 CSI Identity 服务来获取具体插件信息的。
2. **External Provisional**：负责 [Provision](https://kubernetes.io/docs/concepts/storage/dynamic-provisioning/) 阶段，是通过监听了 APIServer 中的 PVC 对象的变化来触发对应的 [CSI Controller](https://github.com/container-storage-interface/spec/blob/master/csi.proto#L62) 中的 CreateVolume 方法。
3. **External Attacher**：负责 Attach 阶段，是通过监听 APIServer 中的 VolumeAttachment 对象的变化来触发调用 [CSI Controller](https://github.com/container-storage-interface/spec/blob/master/csi.proto#L62) 中的 ControllerPublishVolume 方法。

CSI 插件服务包含三大服务：

1. **CSI Identity：**负责对外暴露插件的信息，代码详见：https://github.com/container-storage-interface/spec/blob/master/csi.proto#L51
2. **CSI Controller：**对 CSI Volume（对应 Kubernetes 里的 PV）的管理接口，如 CSI Volume 添加与删除、CSI Volume 的 Attach/Dettach（源码中是以 Publish/UnPublish 体现的）以及 Snapshot 相关的操作。
3. **CSI Node：**：定义了 CSI Volume 需要在宿主机上执行的操作

以上的组件和服务都可以在 [CSI 规范](https://github.com/container-storage-interface/)中查阅。

更多设计细节详情详见：

https://github.com/container-storage-interface/spec/blob/master/spec.md

https://github.com/kubernetes/design-proposals-archive/blob/main/storage/container-storage-interface.md

## 如何在 Kubernetes 中让 Pod 尽可能地分布在不同的节点？

