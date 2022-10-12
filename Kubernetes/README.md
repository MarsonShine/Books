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

在 Kubernetes 中实际上是有专门的控制器来处理持久化存储的，叫 VolumeController