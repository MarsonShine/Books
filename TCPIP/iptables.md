## 链接追踪(Connection Tracking)

iptables 是 linux 2.4.xx 内核版本新增的功能。就是使用连接追踪状态来检查和限制连接的。connection tracking 存储了连接的信息。可以根据以下连接状态来允许或拒绝访问。

- `NEW` -- 新连接的包请求，比如一个 HTTP 请求。
- `ESTABLISHED`-- 作为现有连接的一部分包。
- `RELATED` -- 一个请求新连接的数据包，但却是现有连接的一部分。例如，FTP使用21端口号建立连接，但数据是在不同的端口传输（通常是20端口传输）。
- `INVALID` -- 不属于连接跟踪表中任何连接的一个包。

你可以对任何网络协议使用iptables连接跟踪状态功能，即使该协议本身是无状态的（如 UDP）。下面的例子显示了一个使用连接追踪的规则，它只转发了与已建立的连接有关的数据包。

```shell
$ iptables -A FORWARD -m state --state ESTABLISHED,RELATED -j ACCEPT
```

### 案例

接着用 FTP 的案例来理解上述四种状态

```
Client										FTP Server
202.54.1.10								64.67.33.76
client.me.com							ftp.me.com
```

1. 连接 FTP 服务器：`ftp ftp.me.com`;

   这时它会在 ftp 服务器打开新连接-- `NEW`

   ```
   client          NEW        FTP Server
   202.54.1.10     --->       64.67.33.76
   client.me.com              ftp.me.com
   ```

2. 下载文件：`get bigfile.tar.gz`

   当客户端从 ftp 服务器上下载文件时，我们称之为已建立连接（`ESTABLISHED`）

   ```
   client          ESTABLISHED   FTP Server
   202.54.1.10              64.67.33.76
   client.me.com                 ftp.me.com
   ```

   请注意，当您看到用户名/密码提示时，您的连接已经建立，对 ftp 服务器的访问在认证成功后被授予。

3. 被动 ftp 连接

   在一个被动的 ftp 连接中，客户端的连接端口是20，但传输端口可以是任何未使用的1024或更高的端口。要启用被动模式，客户端可以发送 pass 命令：`ftp>pass`。Passive 模式就开启了

如果希望允许被动的 ftp 访问，则需要在防火墙级别使用 `RELATED` 状态。下面是 SSH 服务器的示例，仅允许 SSH 服务器IP为 64.67.33.76 的用户建立新的连接。

```shell
iptables -A INPUT -p tcp -s 0/0 –sport 513:65535 -d 64.67.33.76 –dport 22 -m state –state NEW,ESTABLISHED -j ACCEPT
```

```shell
iptables -A OUTPUT -p tcp -s 64.67.33.76 –sport 22 -d 0/0 –dport 513:65535 -m state –state ESTABLISHED -j ACCEPT
```

它也适用于无状态协议，如 UDP。下面的例子允许连接跟踪只转发与已建立的连接有关的数据包。

```shell
iptables -A FORWARD -m state –state ESTABLISHED,RELATED -j ALLOW
```

