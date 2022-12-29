#include "apue.h"
#include <errno.h>

void make_temp(char *template);

int main()
{
    char good_template[] = "/tmp/dirXXXXXX"; // 正确
    char *bad_template = "/tmp/dirXXXXXX"; // 错误

    printf("trying to create first temp file...\n"); 
    make_temp(good_template); // 第一个是通过数组申明的，名字是在栈上分配的。
    printf("trying to create second temp file...\n");
    make_temp(bad_template); // 此方式使用了指针，编译器把字符串存放在可执行文件的只读段，当 mkstemp 函数试图修改字符串时，就会出现了段错误（segment fault）
    exit(0);
}

void make_temp(char *template)
{
    int fd;
    struct stat sbuf;

    if ((fd = mkstemp(template)) < 0)
        err_sys("can't create temp file");
    printf("temp name = %s\n", template);
    close(fd);
    if (stat(template, &sbuf) < 0) {
        if (errno == ENOENT)
            printf("file doesn't exist\n");
        else
            err_sys("stat failed");
    }
    else {
        printf("file exists\n");
        unlink(template);
    }
}

