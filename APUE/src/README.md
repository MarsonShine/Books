# 如何运行代码

1. 下载源码：http://www.apuebook.com/code3e.html

2. 在 CentOS-7 下解压源代码文件（可以是任意位置）

   ```
   tar -zxv -f src.3e.tar.gz -C /usr/include/
   ```

3. 安装 bsd 兼容包

   ```
   yum install https://dl.fedoraproject.org/pub/epel/epel-release-latest-7.noarch.rpm
   
   yum install libbsd libbsd-devel
   ```

4. make

5. 移动头文件和库文件

   ```
   cp ./include/apue.h /usr/include/
   cp ./lib/libapue.a /usr/local/lib/
   ```

6. 编译文件

   ```sh
   gcc hello.c -l apue # 编译
   ./a.out # 运行
   ```

   