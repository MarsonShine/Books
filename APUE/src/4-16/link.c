#include "apue.h"
#include <fcntl.h>

int main(int argc, char const *argv[])
{
    if (open("tempfile", O_RDWR) < 0)
        err_sys("open error");
    if (unlink("tempfile") < 0)
        err_sys("unlink error");
    printf("file unlinked\n");
    sleep(15);
    printf("done\n");    
    exit(0);
}

/*
进程用 open/create 创建一个文件，然后立即用 unlink，因为该文件仍然是打开的，所以不会将其内容删除。
只有当进程关闭该文件或终止时（在这种情况下，内核关闭该进程所打开的全部文件），该文件才能被删除
*/