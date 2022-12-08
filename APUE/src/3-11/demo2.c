// 打开文件时修改文件标志属性值

#include "../h/apue.h"
#include <fcntl.h>

void set_fl(int fd, int flags)
{
    int val;
    if ((val == fcntl(fd, F_GETFL, 0)) < 0)
        err_sys("fcntl F_GETFL error");

    val |= flags;   // 更改 flags
    if (fcntl(fd, F_SETFL, val) < 0)
        err_sys("fcntl F_SETFL error");
}