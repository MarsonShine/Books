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

查看 linux 后台执行了哪些程序：`ps -aux`

关闭linux正确操作：1. sync(同步数据) 2.shutdown(关机) 3.reboot,halt,poweroff(重新开机，关机)

## ls 指令

```shell
ls # 查看当前目录下的所有文件
ls -al # 查看当前目录下的所有文件的详细信息（权限，时间，所属者等等）
ls -l --full-time	# 显示完整时间
```

设置命令别名，比如一般情况下都要看某个目录下的文件名称，以及文件属性等信息，所以我每次都要输入如此命令 `ls -al`，设置别名可以让我们自定义命令：`alias lm='ls -al'`。这样以后只需要输入 `lm` 即可。

## 权限

关于文件权限描述，总共有10个占位符：drwxr-xr-x

- 第一位代表的是“目录、文件或链接文件”：
  - [d]：目录
  - [-]：文件
  - [l]：链接文件（link file）
  - [b]：设备文件里面的可供储存的周边设备（可随机存取设备）
  - [c]：设备文件里面的序列埠设备，例如键盘、鼠标（一次性读取设
    备）
- 剩下的位是以三个为一组，都是”rwx“三个参数中组合。r：可读，w：可写，x：执行；要注意，这三个字符位置不会改变，如果没有权限，就会以”-“代替
  - 第一组为”文件拥有者可具备的权限“，以上面的例子举例：该文件拥有者可以读，写，执行
  - 第二组为”加入此群组之帐号的权限“
  - 第三组为“非本人且没有加入本群组之其他帐号的权限”

举个完整的例子：\[-][rwx]\[r-x][r--] > 1 234 567 890

1：表示文件目录或链接文件，这里是（-）

234：拥有者的权限，可读可写可执行

567：同群组的使用者权限，对应到 r-x 可读可执行不可写

890：其他使用者权限，可读

### 修改文件权限命令

主要有三个：

- chgrp：改变文件所属群组（change group），**被改变的群组名必须要在 /etc/group 中**
- chown：改变文件拥有者，**在 /etc/passwd 这个文件中有纪录的使用者名称才能改变**
- chmod：改变文件的权限，SUID, SGID, SBIT等等的特性

```shell
# 如果想把某个文件夹并且连同这个文件下的所有次目录或文件的拥有者改变，可以加可选参数 -R
chown [-R] 账号名称 文件或目录
chown [-R] 账号名称:群组名称 文件或目录
```

Linux 使用数字来表示各个权限，各权限对照表如下：

r:4 > w:2 > x:1

每组身份各自的三个权限都是累加的，例如当权限为: \[-rwxrwx---] 分数则是：onwer = rwx = 4 + 2 + 1 = 7 > group = rwx = 4+2+1 = 7 > others = --- = 0 + 0 + 0 = 0；那么该文件的权限数字就是 770，那这样命令就成了 `chmod [-R] 770 文件或目录`

## 文件操作类的指令

Linux 中以小数点 `.` 开头的都是隐藏文件

```shell
cp source destination # 拷贝文件从 source 到 destination
cp [-adfilprsu] source detination
# 其中可选参数
-a: 相当于 --presever-all 的意思（常用）
-d: 若来源文件为链接文件的属性（link file），则复制链接文件属性而非文件本身
-f: 为强制（force）的意思，若目标文件已经存在且无法打开，则移除后再尝试一次
-i: 若目标文件（destination）已经存在时，在覆盖时会先询问动作的进行（常用）
-l: 进行硬式链接（hard link）的链接文件创建，而非复制文件本身
-p: 连同文件的属性（权限、用户、时间）一起复制过去，而非使用默认属性（备份常用）
-r: 递回持续复制，用于目录的复制行为（常用）
-s: 复制成为符号链接文件 （symbolic link），亦即“捷径”文件
-u: destination 比 source 旧才更新 destination，或 destination 不存在的情况下才复制
--preserve=all: 除了 -p 的权限相关参数外，还加入 SELinux 的属性, links, xattr 等也复制了。最后需要注意的，如果来源文件有两个以上，则最后一个目的文件一定要是“目录”才行！
# 如果是密码档等一些特殊文件，需要用 cp -a/-p 才行
# 当复制链接属性时，如果没有添加可选参数则默认行为就是复制链接文件的原始文件，而 -d 是复制链接文件的属性。
cp bashrc_slink bashhr_slink1 # 复制的链接文件的原始链接
cp -d bashrc_slink bashrc_slink2 # 复制的是链接文件的属性
# 将多个文件一次复制到同一个目录
cp ~/.bashrc ~/.bash_history /tmp	# 最后一个参数一定是目录

# 删除操作
rm [-fir] 文件或目录
-f: 强制删除，忽略不存在的文件，不会弹出警告提示
-i: 互动模式，在删除前会询问你是否删除
-r: 递归删除，最常用在目录的删除了，这是非常危险的选项
# 删除一个带有 '-' 等这种有意义的符号开头的文件会报错，"invalid option -- 'a'"
# 需要加转义符
rm .\-aaa-

# 移动文件，文件重命名
mv [-fiu] source destination
mv [options] source1 source2 source3 ... directory
-f: 强制覆盖
-i: 操作前询问用户
-u: 若文件已经存在，如果 source 较新，则会更新（常用）
# 当指令用来重命名时，还有一个指令也可以 rename，这个命令是用与多个文件的重命名

```

### 目录处理指令

```shell
cd # 变换目录 change directory
###################################################
pwd # 显示目前的目录
pwd -P # 显示出确实的目录，而非使用链接（link）路径 如：
cd /var/mail
pwd # 会显示 /var/mail
pwd -P # 会显示 /var/spool/mail
ls -ld /var/mail # 就会发现输入 pwd 显示的 `/var/mail` 是一个链接文件，链接到 `/var/spool/mail`
####################################################
mkdir # 创建一个新的目录
mkdir -m # 设置文件的权限
mkdir -p # 帮助你直接将所需要的目录（包含上层目录）递回创建起来，比如我要递进创建如下目录：mkdir test1/test2/test3 在没有 -p 的参数下是无法新建成功的
####################################################
rmdir # 删除一个空的目录
rmdir -p # 递归删除目录，空目录也删掉
# 例如我要删除 test 开头的文件目录,先找出来
ls -ld test* # 前缀模糊搜索 test 开头的文件目录
rmdir test # 删除test，如果 test 目录里面是空的，则删除成功，如果含有子目录则要执行以下命令
rmdir -p test1/test2/test3/
####################################################
```

### 目录环境变量

```shell
echo $PATH # 输出所有的环境变量
PATH="${PATH}:/root" # 修改环境变量，将 /root 加入到环境变量中，这样就可以在执行 root 目录下的可执行的命令
# 需要注意的是，由于有高速缓存的存在，可能修改完环境变量后还没无法识别命令，只需要刷新下 执行如下命令
exit # 退出
su - # 登录
```

### 文件与目录的显示

```shell
ls [-aAdfFhilnrRst] 文件名或目录名称
ls [--color={never,auto,always}] 文件名或目录名称
ls [--full-time] 文件名或目录名称
# 其中选项与参数的意思：
-a: 全部的文件，连同隐藏文件（ 开头为 . 的文件） 一起列出来（常用）
-A: 全部的文件，连同隐藏文件，但不包括 . 与 .. 这两个目录
-d: 仅列出目录本身，而不是列出目录内的文件数据
-f: 直接列出结果，而不进行排序(ls 会默认文件名排序)
-F: 根据文件、目录等信息，给予附加数据结构，
    *:代表可可执行文件； /:代表目录； =:代表 socket 文件；等
-h: 将文件大小以人类较易读的方式（例如 GB, KB 等等）列出来
-i: 列出 inode 号码
-l: 长数据串行出，包含文件的属性与权限等等数据；（常用）
-n: 列出 UID 与 GID 而非使用者与群组的名称
-r: 将排序结果反向输出
-R: 连同子目录内容一起列出来，等于该目录下的所有文件都会显示出来
-S: 以文件大小大小排序，而不是用文件名排序；
-t: 依时间排序，而不是用文件名。
--color=never: 不要依据文件特性给予颜色显示
--color=always: 显示颜色
--color=auto: 系统自行判断
--full-time: 以完整时间模式 （包含年、月、日、时、分） 输出
--time={atime,ctime}: 输出 access 时间或改变权限属性时间 （ctime）
					而非内容变更时间 （modification time）
```

文件名称和目录名称的显示：

```shell
basename /etc/sysconfig/network	# 显示文件的名称：输出 network
dirname /etc/sysconfig/network  # 显示目录的名称：输出 /etc/sysconfig
```

## 文件内容查看相关操作指令

最常用的文件内容操作指令就是：`cat`，`more`，`less`。如果要查看的文件很大，并且只用看尾部几行，那么用前面的指令会很慢，甚至卡死，我们可以用 `tail`、`tac`。

- cat：由第一行开始显示文件内容
- tac：从最后一行开始显示（cat 与 tac 是倒着的拼写）
- nl：显示时输出行号
- more：一页一页的显示文件内容
- less：与 more 类似，但是可以往前翻页
- head：只看头几行
- tail：只看尾几行
- od：以二进制的方式读取文件内容

```shell
# cat 可选参数
-A  ：相当于 -vET 的整合选项，可列出一些特殊字符而不是空白而已；
-b  ：列出行号，仅针对非空白行做行号显示，空白行不标行号！
-E  ：将结尾的断行字符 $ 显示出来；
-n  ：打印出行号，连同空白行也会有行号，与 -b 的选项不同；
-T  ：将 [tab] 按键以 Î 显示出来；
-v  ：列出一些看不出来的特殊字符
```

当文件内容很大时，用 `cat` 查找内容时很不方便，这个时候可以用 `more` 来一页页查看，但是如果要在如此大量的内容找到具体的内容还是很复杂，得一遍遍往下翻，其实有一个查找功能：

**输入 / 之后，光标就会自动到最后一行等待用户继续输入要查找的内容**：`/MANPATH`，然后就会往下搜寻指定的字符串，按 n 继续搜索下一个匹配的字符串，q 退出。

less 指令详情：

```shell
# less 可选参数
空白键 ：向下翻动一页；
[pagedown]：向下翻动一页；
[pageup] ：向上翻动一页；
/字串 ：向下搜寻“字串”的功能；
?字串 ：向上搜寻“字串”的功能；
n ：重复前一个搜寻 （与 / 或 ? 有关！）
N ：反向的重复前一个搜寻 （与 / 或 ? 有关！）
g ：前进到这个数据的第一行去；
G ：前进到这个数据的最后一行去 （注意大小写）；
q ：离开 less 这个程序；
```

特定取 n 行：

```shell
head -n n # 取头部 n 行
head -n +n # 取 n 行之后的数据
head -n 20 /etc/yum.conf | tail -10 # 取21-最后10行
tail -n n # 取尾部 n 行
```

查看二进制文件

```shell
od [-t TYPE] 文件
-t 后面可以接如下类型：
	  a       ：利用默认的字符来输出；
      c       ：使用 ASCII 字符来输出
      d[size] ：利用十进制（decimal）来输出数据，每个整数占用 size Bytes ；
      f[size] ：利用浮点数值（floating）来输出数据，每个数占用 size Bytes ；
      o[size] ：利用八进位（octal）来输出数据，每个整数占用 size Bytes ；
      x[size] ：利用十六进制（hexadecimal）来输出数据，每个整数占用 size Bytes ；
# 举个例子：请将/etc/issue这个文件的内容以8进位列出储存值与ASCII的对照表
od -t oCc /etc/issue
# 找出任意字符的 ASCII 对照吗
echo password | od -t oCc echo
```

*要注意，执行多个命令，可以用分号隔开，如：`date; ls /etc/yum.config`*

## 搜索文件操作指令

which：能找出使用的命令（环境变量PATH）所在的目录位置

```shell
which [-a] command
# ifconfig 所在的位置
which -a ifconfig # 输出 /usr/sbin/ifconfig
```

文件的搜索，一般情况下 `find` 指令不常用，因为它非常耗时，消耗磁盘。一般都是用 `whereis` 和 `locate` 指令，这两个指令找不到才会用 `find`。

whereis：只招特定目录下的文件

locate：是利用数据库来搜寻文件名

```shell
whereis -l # 列出 whereis 会向哪些特定的目录查询文件
whereis [-bmsu] 文件和目录名
选项与参数：
-l    :可以列出 whereis 会去查询的几个主要目录而已
-b    :只找 binary 格式的文件
-m    :只找在说明文档 manual 路径下的文件
-s    :只找 source 来源文件
-u    :搜寻不在上述三个项目当中的其他特殊文件

locate [-ir] keyword
选项与参数：
-i  ：忽略大小写的差异；
-c  ：不输出文件名，仅计算找到的文件数量
-l  ：仅输出几行的意思，例如输出五行则是 -l 5
-S  ：输出 locate 所使用的数据库文件的相关信息，包括该数据库纪录的文件/目录数量等
-r  ：后面可接正则表达式的显示方式
# 注意，如果在镜像拉去的 centos 可能没有这个命令，则需要下载安装即可
yum -y install mlocate
# locate 读取速度很快，那是因为是完全走数据库，并且这是有限制的，centos 每天更新数据库的频率是一天一次，所以有些新建的文件你是查不到（因为没有及时更新数据库）
# 手动更新数据库命令
updatedb	# updatedb 是根据 /etc/updatedb.conf 的配置来搜索文件名，并更新 /var/lib/mlocate 内的数据库文件，所以 locate 执行也就是 /var/lib/mlocate 中的数据库文件
```

## 文件/文件夹解压缩

```shell
gzip [-cdtv#] 文件名 # 压缩文件
zcat 文件名.gz	# 查看压缩中的文件，并把内容显示出来
选项与参数：
-c  ：将压缩的数据输出到屏幕上，可通过数据流重导向来处理；
-d  ：解压缩的参数；
-t  ：可以用来检验一个压缩文件的一致性～看看文件有无错误；
-v  ：可以显示出原文件/压缩文件的压缩比等信息；
-#  ：# 为数字的意思，代表压缩等级，-1 最快，但是压缩比最差、-9 最慢，但是压缩比最好！默认是 -6

# 将 services 按最佳压缩率压缩成 services.gz，并保留源文件
gzip -c -9 services > services.gz
# 找出压缩文件中 http 这个关键字在哪几行？
gzrep -n 'http' service.gz
```

### 打包指令

```shell
tar [-z|-j|-J] [cv] [-f 待创建的新文件名] filename... <==打包
选项与参数：
-c  ：创建打包文件，可搭配 -v 来察看过程中被打包的文件名（filename）
-t  ：察看打包文件的内容含有哪些文件名，重点在察看“文件名”就是了；
-x  ：解打包或解压缩的功能，可以搭配 -C （大写） 在特定目录解开
      特别留意的是， -c, -t, -x 不可同时出现在一串命令行中。
-z  ：通过 gzip  的支持进行压缩/解压缩：此时文件名最好为 *.tar.gz
-j  ：通过 bzip2 的支持进行压缩/解压缩：此时文件名最好为 *.tar.bz2
-J  ：通过 xz    的支持进行压缩/解压缩：此时文件名最好为 *.tar.xz
      特别留意， -z, -j, -J 不可以同时出现在一串命令行中
-v  ：在压缩/解压缩的过程中，将正在处理的文件名显示出来！
-f filename：-f 后面要立刻接要被处理的文件名！建议 -f 单独写一个选项啰！（比较不会忘记）
-C 目录    ：这个选项用在解压缩，若要在特定目录解压缩，可以使用这个选项。
其他后续练习会使用到的选项介绍：
-p（小写） ：保留备份数据的原本权限与属性，常用于备份（-c）重要的配置文件
-P（大写） ：保留绝对路径，亦即允许备份数据中含有根目录存在之意；
--exclude=FILE：在压缩的过程中，不要将 FILE 打包！

# 举例
su - # 切换超级管理员 root
# 备份 /etc/ 这个目录
time tar -zpcv -f /root/etc.tar.gz /etc	# 执行完会有一个警告 “tar: Removing leading /' from member names（移除了文件名开头的 /' ，所以你查看打包文件里面的文件，都是去掉了根目录的
tar -ztv -f /root/etc.tar.gz
# 解包
tar -zxv -f /root/etc.tar.gz
# 解压到 tmp 指定目录
tar -zxv -f /root/etc.tar.gz -C /tmp
# 查看 etc 目录所占内存
du -sm /etc/
```

## 变量

变量的取用指令：echo；比如输出环境变量：`echo $PATH`，又比如输出 `echo $variable`；读取变量都要在前面加上美元符号 `$`。还有一种变量读取方式：`echo ${PATH};echo ${variable}`。

`myname=marsonshine` 这是直接定义变量 myname 并赋值为 marsonshine，注意赋值的时候中间不能有任何空格字符。

### 变量值的空格问题

不能在一个变量值的内容不存在空格字符，那么如何赋值带有空格字符的内容？——可以使用双引号或单引号将内容结合起来：`myname='marson shine'`。也可以直接使用反斜杠 `\` 来转义：`myname=marson\ shine`。

如果要在变量中引用其他命令的值可以使用反单引号 **`指令`**或者 `$(指令)` 例如：version=\`uname -r\` 或者 `version=$(uname -r)`。 获取内核版本信息

变量值的追加内容：`PATH="PATH":/home/bin` 或 `PATH=${PATH}:/home/bin`。

取消变量：`unset myname`。 

列出环境变量指令：`env`

列出所有定义的变量信息：`set`

## Linux 环境语言相关指令

`locale -a`：查询当前系统支持的语言。关于语言相关的文件存储在 `/usr/lib/locale` 目录中。整体系统默认的语言定义在 `/etc/locale.conf`。

在终端与用户键盘交互：`read`；

```shell
read [-pt] variable	# 读取变量 variable（用户键盘输入的值）
选项与参数：
-p  ：后面可以接提示字符！
-t  ：后面可以接等待的“秒数”，到达描述自动结束
# 例子
read atest	# 此时光标会等待你输入值  当你输入 this is read value
echo ${atest} # 就会输出用户输入的值 "this is read value"
read -p "please keyin your name: " -t 30 named # 提示用户 "please keyin your name: " 30 秒有效
marsonshine summerzhu # 用户输入
echo ${named} # 输出 "marsonshine summerzhu"
```

变量内容的删除与替换

```shell
path=${PATH}
echo ${path}
/usr/local/bin:/usr/bin:/usr/local/sbin:/usr/sbin:/home/marsonshine/.local/bin

# 要将 local/bin 删掉
echo ${path#/*local/bin:}
/usr/bin:/usr/local/sbin:/usr/sbin:/home/marsonshine/.local/bin

${variable#/*local/bin:}
# #代表要被删除的部分，由于 # 代表由前面开始删除，所以这里便由开始的 / 写起。
# 需要注意的是，我们还可以通过万用字符 * 来取代 0 到无穷多个任意字符
```



# 参考资料：

- [《鸟哥的Linux私房菜-基础篇》](http://linux.vbird.org/linux_basic/)