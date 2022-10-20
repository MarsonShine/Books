# Service，DNS/服务发现

通过 Kubernetes 创建 `replic:3` 的 Service：

```yaml
apiVersion: v1
kind: Service
metadata:
  name: hostnames
spec:
  selector:
    app: hostnames
  ports:
  - name: default
    protocol: TCP
    port: 80
    targetPort: 9376
```
Pod:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hostnames
spec:
  selector:
    matchLabels:
      app: hostnames
  replicas: 3
  template:
    metadata:
      labels:
        app: hostnames
    spec:
      containers:
      - name: hostnames
        image: k8s.gcr.io/serve_hostname
        ports:
        - containerPort: 9376
          protocol: TCP
```

这个时候我们通过下面的命令：

```bash
kubectl get svc hostnames

NAME        TYPE        CLUSTER-IP   EXTERNAL-IP   PORT(S)   AGE
hostnames   ClusterIP   10.0.1.175   <none>        80/TCP    5s

curl 10.0.1.175:80
hostnames-0uton

curl 10.0.1.175:80
hostnames-yp2kp

curl 10.0.1.175:80
hostnames-bvc05
```

通过调用同一个 IP 地址，得到的响应是来自不同的 pod。这是通过 ClusterIP 的 Service 解决的

这是如何做到的呢？

**实际上这是通过 kube-proxy 和 iptables 共同实现的。**

一旦 kubectl 创建 Service 提交到 Kubernetes；kube-proxy 就会通过 Service 的 Infomer 循环监控得知有一个 Service 被添加进来，就会触发对应的响应事件，它就会在宿主机上创建这样一条 iptables 规则，通过规则匹配讲到符合的 iptables 链（iptables chains）来定位到具体是哪个 pod 地址。

> kubectl 下的 kube-controller-manager 的执行过程详见：[Kubernetes cli 的执行过程](kubernetes-cli-how-implement-in-sourcecode.md)

这种方式的主要限制是当 Kubernetes 的节点 pod 非常多时，频繁的 pod 变化会导致每个 pod 的 iptables 规则频繁变化，会大量占据系统的 CPU 资源，严重会导致卡死在这个过程。

所以采用这种模式的 Kubernetes 的节点规模不能太大。

另一种是 **IPVS 模式**；

在创建 Service 之后，kube-proxy 监控到了 pod 的变化，会在宿主机上创建一个虚拟的网卡（kebe-ipvs0），并为它分配 Service VIP 作为它的 IP 地址。然后 kube-proxy 会通过 Linux 的内核 IPVS 模块为这个 IP 地址设置三个 VPS 虚拟机，然后在这三个虚拟机之间设置负载均衡模式。

**IPVS 在内核中的实现也是基于 Netfilter 的 NAT 模式，所以在转发这一层上，理论上 IPVS 没有显著的性能提升。但是它有一个优势就是不需要像第一个模式为每个 pod 设置 iptables 规则，而是把这些规则的处理放到内核态，从而降低了维护这些规则的代价。**

> 关于 Netfilter 和 iptables 之间的区别详见：[深入研究 Iptables 和 Netfilter 架构](../linux/doc/a-deep-dive-into-iptables-and-netfilter-architecture.md)



