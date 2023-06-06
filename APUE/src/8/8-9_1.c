#include "apue.h"

static void charatatime(char *);

int main(void)
{
    pid_t pid;
    if ((pid == fork()) < 0) {
        err_sys("fork error");
    } else if (pid == 0) { // 子进程
        charatatime("output from child\n");
    } else {// 父进程
        charatatime("output from parent\n");
    }
    exit(0);
}

static void charatatime(char *str)
{
    char *ptr;
    int c;
    setbuf(stdout, NULL); // 设置无缓冲
    for (ptr = str; (c = *ptr++) != 0;)
        putc(c, stdout);
}

// 可能的输出结果：
// 1. 
// ooutput form child
// utput from parent

// 2.
// oooutput from child
// utput from parent