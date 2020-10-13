# Linux 常用命令

查看IP连接信息

nestat -an

查看 TCP 端口连接数

netstat -nat|grep -i "端口数"|wc -l

```shell
# netstat | less // 查询本地所有的 socket
# netstat |wc -l // wc -l 是查询有多少个 socket
# netstat -t // 查看有哪些TCP连接
# netstat -t tcp // 查看有哪些tcp网络情况
# netstat -ntlp |grep 22	// 查看具体哪个端口的占用情况 -n：将特殊的端口号用数字显示，-t：tcp，-l：只显示连接中的连接，-p：显示程序名称
# netstat -antp //所有连接、数字显示主机、端口、TCP连接、监听的程序

# netstat -anup //所有连接、数字显示主机、端口、UDP连接、监听的程序

# netstat -s //统计所有（开机至今的）连接数据，包括tcp、udp等

# netstat -st //统计所有tcp连接数据

# netstat -su //统计所有udp连接数据

# netstat -su //粗略统计连接数据

# netstat -nlp|grep 9000 查指定端口号的信息

# sudo lsof -i tcp:6801 | wc -l 
```

```shell
# 删除文件
rm -f filepath
# 删除文件下所有的文件
rm -rf folderPath	// -r 向下递归，不管有多少级目录，全部删除 -f 强制删除

ps -ef|grep redis // 查看 redis 服务器是否都已经正常启动
```

ping ip 段的结果字段解析

- icmp_seq：一个网站需要使用 ICMP 协议，因此有这个字段
- time：往返一次的时间
- ttl：time to live，封包的生存时间，就是说一个封包开始倒计时，如果超过了返回显示的数字，就会丢包。

## vim 操作

```shell
vim filePath  // vim appsetting.json 进入命令模式
// 从其它地方复制内容到指定的文件
// 先按方向键找到要粘贴插入的位置
// 输入 shift + insert

// 删除多行
: 1,100d	// 删除从第1行到100行的内容

// 推出并保存
:wq

// 撤销最近一次的动作
:undo 或 :u
```

