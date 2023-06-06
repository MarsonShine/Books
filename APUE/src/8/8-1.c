#include "apue.h"

int globvar = 6;
charbuf[] = "a write to stdout\n";

int main(void)
{
    int var;
    pid_t pid;

    var = 88;
    if (write(STDOUT_FILENO, buf, sizeof(buf)-1) != sizeof(buf)-1)
        err_sys("write error");
    printf("before fork\n");

    if ((pid = fork()) < 0) {
        err_sys("fork error");
    } else if (pid == 0) {
        globvar++;
        var++;
    } else {
        sleep(2);
    }

    printf("pid = %ld, glob = %d, var = %d\n", (long)getpid(), globvar, var);
    exit(0);
}

// 是限制性 fork 还是执行主进程是无法控制的。这取决于内核所使用的调度算法。如果要求父进程和子进程之间互相同步，则要求某种形式的进程间通信。