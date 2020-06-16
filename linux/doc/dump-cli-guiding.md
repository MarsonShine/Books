# 安装 dotnet-dump

```shell
dotnet tool install -g dotnet-dump
```

# 收集指定程序进程 dump 信息

```powershell
dotnet-dump collect -p 17327 -o /data/temp/dump --diag
```

# 分析 dump 文件并进入 SOS 命令

```powershell
dotnet-dump analyze dump-path
```

# SOS 命令

```shell
// 在托管堆查看指定内存大小的对象
!dumpheap -stat -min 102400 // 查找内存大于 100M 的对象
// 查看线程信息
!threads
// 查看每个托管线程做的工作
~*e !clrstack
// 进入指定线程
~1s // 进入 0 号线程
// dump 线程种的对象信息
!dso
// 查看具体对象大小信息
!objsize 000001d3fa581518
```

