## TCP

TCP 协议是面向传输的协议，是属于传输层。

客户端之间的 TCP 连接首先从建立连接（Connection）开始，成功建立连接之后，两个端在应用层上就属于两个会话（Session）。

### TCP的握手与挥手

首先先说明下 TCP 几个核心操作：

- SYN；请求同步，即客户端A与客户端B建立连接，A会发起一个连接请求到B，这个过程称为 SYN；
- PSH；发送数据，一个端向另一个端发送数据包；
- FIN；一个端主动断开请求，称为 FIN，此时请求完成；
- ACK；反馈确认，对上面的三个操作，所达端都要发送一个ACK回去响应。

### 三次握手

端到端的 TCP 连接是通过三次握手建立的，以客户端A与客户端B建立TCP连接为例：

1. 一次握手

   - 客户端A向B发出建立连接请求，即SYN；

2. 二次握手

   - 客户端B针对SYN返回一个ACK作为响应，表示准备好建立连接；

   - **同时发送一个SYN给客户端A**

3. 第一次握手后，双方已经做好建立连接的准备了。三次握手

   - 客户端A针对B的SYN作出响应，即向B发送一次ACK。

4. 至此A，B成功建立连接

### 四次挥手

断开连接由一方主动发起断开连接请求

1. 一次挥手
   - 客户端A向B发出断开连接请求，即FIN；
2. 二次挥手
   - 客户端B针对A的SYN请求返回ACK响应，表示准备断开连接
   - 此时B不能向A马上发送FIN请求，因为网络有延迟，客户端B可能还有没有ACK的包以及一些清理操作要处理，即不能像建立连接一样合并
3. 三次挥手
   - 客户端B向A发出断开连接请求，即FIN
4. 四次挥手
   - 客户端A针对B的SYN请求返回ACK响应，表示准备断开连接，并进行一些清理操作。

### 为什么TCP连接需要三次握手和四次挥手？

首先要知道的是 TCP 是面向连接（connection-oriented）的协议；并且 TCP 连接提供的是全双工服务（full-duplex service）：如果客户端A与另一个端B存在一条TCP连接，那么应用层数据就可以在A进程流向B进程的同时，也可以从进程B流向进程A。TCP连接 也是点对点（point-to-point）的，即存在单个发送方与单个接收方之间的连接。

所以当一个进程向另一个进程发送数据之前，这两个进程必须要建立连接，“互相握手”。