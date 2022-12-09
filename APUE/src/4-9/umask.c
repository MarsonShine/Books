#include "apue.h"
#include <fcntl.h>

#define RWRWRW (S_IRUSR|S_IWUSR|S_IRGRP|S_IWGRP|S_IROTH|S_IWOTH)

int main(int argc, char const *argv[])
{
    umask(0);
    if (creat("foo", RWRWRW) < 0) // 创建 foo 文件，umask 为 0
        err_sys("create error for foo");
    umask(S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH);
    if (creat("bar", RWRWRW) < 0)
        err_sys("create error for bar");
    exit(0);
}
