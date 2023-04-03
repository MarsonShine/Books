#include "apue.h"

int globvar = 6;

int main(void)
{
    int var;
    pid_t pid;

    var = 88;
    printf("before vfork\n");
    if ((pid = vfork()) < 0) {
        err_sys("vfork error");
    } else if (pid == 0) {
        globvar++;
        var++;
        _exit(0);
    }
    // 父进程继续执行
    printf("pid = %ld, glob = %d, var = %d\n", (long)getpid(), globvar, var);
    exit(0);
}

// 是限制性 fork 还是执行主进程是无法控制的。这取决于内核所使用的调度算法。如果要求父进程和子进程之间互相同步，则要求某种形式的进程间通信。