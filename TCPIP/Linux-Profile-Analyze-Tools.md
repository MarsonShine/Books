# Linux 系统监控工具

## tcpdump

tcpdump 是一个网络抓包工具，常用的选项如下：

- -n，使用IP地址表示主机，而不是主机名；使用数字表示端口号，而不是服务名称。
- -i，指定要监听的网卡接口。"-i any"表示抓取所有网卡接口上的数据包。
- -v，输出一个稍微详细的信息，例如，显示IP数据包中的TTL和TOS信息。
- -t，不打印时间戳。
- -e，显示以太网帧头部信息。
- -c，仅抓取指定数量的数据包。
- -x，以十六进制数显示数据包的内容，但不显示包中以太网帧的头部信息。
- -X，与-x选项类似，不过还打印每个十六进制字节对应的ASCII字符。
- -XX，与-X相同，不过还打印以太网帧的头部信息。
- -s，设置抓包时的抓取长度。
- -S，以绝对值来显示TCP报文段的序号，而不是相对值。
- -w，将tcpdump的输出以特殊的格式定向到某个文件。
- -r，从文件读取数据包信息并显示之。

tcpdump 还可以对数据包进行指定过滤：

```
tcpdump 'protocol field operator value'
```

下面是例子：

- 过滤源或目的IP：

  ```
  tcpdump 'src host 1.2.3.4'
  tcpdump 'dst host 2.3.4.5'
  ```

- 过滤源或目的端口：

  ```
  tcpdump 'src port 80'
  tcpdump 'dst port 443'
  ```

- 过滤协议类型：

  ```
  tcpdump 'ip proto tcp'
  ```

- 抓取TCP同步报文段

  ```
  tcpdump 'tcp[13]&2!=0' // TCP 头部的第 14 个字节的第2位就是同步标志
  ```

## lsof

列出当前系统打开的文件描述符。

```
$lsof -i[46][protocol][@hostname|ipaddr][:service|port]
```

- -i，显示 socket 文件描述符

- -u，显示指定用户启动的所有进程打开的所有文件描述符

- -c，显示指定的命令打开的所有文件描述符。

  ```
  lsof -c dotnet
  ```

- -p，显示指定进程打开的所有文件描述符

- -t，仅显示打开了目标文件描述符的进程的PID

## nc

主要被用来快速构建网络连接。

- -i，设置数据包传送的时间间隔。

- -l，以服务器方式运行，监听指定的端口。nc 命令默认以客户端方式运行。

- -k，重复接受并处理某个端口上的所有连接，必须与-l选项一起使用。

- -n，使用IP地址表示主机，而不是主机名；使用数字表示端口号，而不是服务名称。

- -p，当nc命令以客户端方式运行时，强制其使用指定的端口号。

- -s，设置本地主机发送出的数据包的IP地址。

- -C，将CR和LF两个字符作为行结束符。

- -U，使用UNIX本地域协议通信。

- -u，使用UDP协议。nc命令默认使用的传输层协议是TCP协议。

- -w，如果nc客户端在指定的时间内未检测到任何输入，则退出。

- -X，当nc客户端和代理服务器通信时，该选项指定它们之间使用的通信协议。

- -x，指定目标代理服务器的IP地址和端口号。

  ```
  $nc -x ernest-laptop:1080 -X connect www.baidu.com 80
  ```

  nc 先连接 ernest-laptop 的 socks5 代理端口1080。然后通过代理连接到 www.baidu.com 的 80 端口。最终构建出一个从本地通过ernest-laptop 这台 socks5 代理服务器访问 www.baidu.com:80 的通道。

- -z，扫描目标机器上的某个或某些服务是否开启（端口扫描）

  ```
  nc -z ernest-laptop 20-50
  ```

  扫描 ernest-laptop 上端口号在 20-50 之间的服务。

## trace

strace 是测试服务器性能的重要工具。它跟踪程序运行过程中执行的系统调用和接收到的信号，并将系统调用名、参数、返回值及信号名输出到标准输出或者指定的文件。

- -c，统计每个系统调用执行时间、执行次数和出错次数。
- -f，跟踪由fork调用生成的子进程。
- -t，在输出的每一行信息前加上时间信息。
- -e，指定一个表达式，用来控制如何跟踪系统调用（或接收到的信号，下同）。
  - `-e trace=set`，只跟踪指定的系统调用。例如，`-e trace=open，close，read，write` 表示只跟踪 open、close、read 和 write 这四种系统调用。
  - `-e trace=file`，只跟踪与文件操作相关的系统调用。
  - `-e trace=process`，只跟踪与进程控制相关的系统调用。
  - `-e trace=network`，只跟踪与网络相关的系统调用。
  - `-e trace=signal`，只跟踪与信号相关的系统调用。
  - `-e trace=ipc`，只跟踪与进程间通信相关的系统调用。
  - `-e signal=set`，只跟踪指定的信号。比如，`-e signal=!SIGIO` 表示跟踪除SIGIO之外的所有信号。
  - `-e read=set`，输出从指定文件中读入的数据。例如，-e read=3，5 表示输出所有从文件描述符3和5读入的数据。
- -o，将 strace 的输出写入指定的文件。

## netstat

netstat 是一个功能很强大的网络信息统计工具。它可以打印本地网卡接口上的全部连接、路由表信息、网卡接口信息等。

- -n，使用IP地址表示主机，而不是主机名；使用数字表示端口号，而不是服务名称。
- -a，显示结果中也包含监听socket。
- -t，仅显示TCP连接。
- -r，显示路由信息。
- -i，显示网卡接口的数据流量。
- -c，每隔1s输出一次。
- -o，显示socket定时器（比如保活定时器）的信息。
- -p，显示socket所属的进程的PID和名字。

## vmstat

它能实时输出系统的各种资源的使用情况，比如进程信息、内存使用、CPU使用率以及I/O使用情况。

- -f，显示系统自启动以来执行的fork次数。

- -s，显示内存相关的统计信息以及多种系统活动的数量（比如CPU上下文切换次数）。

- -d，显示磁盘相关的统计信息。

- -p，显示指定磁盘分区的统计信息。

- -S，使用指定的单位来显示。参数k、K、m、M分别代表1000、1024、1,000,000 和 1,048,576字节。

- delay，采样间隔（单位是s），即每隔 delay 的时间输出一次统计信息。

- count，采样次数，即共输出count次统计信息。

  ```
  $vmstat 5 3 #每隔5秒输出一次结果，共输出三次
  ```

## ifstat

ifstat 是 interface statistics 的缩写，它是一个简单的网络流量监测工具。

- -a，监测系统上的所有网卡接口。
- -i，指定要监测的网卡接口。
- -t，在每行输出信息前加上时间戳。
- -b，以Kbit/s为单位显示数据，而不是默认的KB/s。
- delay，采样间隔（单位是s），即每隔delay的时间输出一次统计信息。
- count，采样次数，即共输出count次统计信息。

```
$ifstat -a 2 5 #每隔2秒输出一次结果，共输出5次
```

> Kbit/s 和 KB/s 表示的数据单位不同:
>
> Kbit/s 表示每秒千比特(kilobits per second)，通信领域常用的速率单位。
> KB/s 表示每秒千字节(kilobytes per second)，计算机存储领域常用的大小单位。
>
> 两者的换算关系是:
>
> 1 Byte = 8 bit
>
> 因此:
>
> 1 KB/s = 1000 Bytes/s
>
> 1 Kbit/s = 1000/8 = 125 Bytes/s
>
> 举例:
>
> 一个100 Kbit/s 的连接速率,相当于 100 * 1000 / 8 = 12500 Bytes/s,即 12.5 KB/s。
>
> 一个10 KB/s 的传输速率,相当于10 * 1000 = 10000 Bytes/s,即 80 Kbit/s。
>
> 所以 Kbit/s 和 KB/s 表示的数据单位不同，数值上也不相同，两者不能混为一谈，对网络速率有不同的表示意义。
>

## mpstat

mpstat 能实时监测多处理器系统上每个CPU的使用情况。mpstat 命令和 iostat 命令通常都集成在包 sysstat 中，安装 sysstat 即可获得这两个命令。

使用方法如下：

```
mpstat[-P{|ALL}][interval[count]]
```

例子：

```
$mpstat -P ALL 5 2#每隔5秒输出一次结果，共输出2次
```

