#!/bin/bash
# 如果是用的 bash 语法，那么第一行的注释就必须要，这是告诉系统需要用 bash 语法执行
# 定义变量 PATH
PATH=/bin:/sbin:/usr/bin:/usr/sbin:/usr/local/bin:/usr/local/sbin:~/bin
# 导出变量
export PATH

echo -e "Hello World! \a \n"
exit 0