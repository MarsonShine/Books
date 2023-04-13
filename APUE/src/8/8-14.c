#include "apue.h"

int main(void)
{
    pid_t pid;
    if ((pid = fork()) < 0)
        err_sys("fork error");
    else if (pid != 0) { // 父进程
        sleep(2);
        exit(2);    // 退出状态为 2
    }

    if ((pid = fork()) < 0)
        err_sys("fork error");
    else if (pid != 0) { // 第一个子进程
        sleep(4);
        abort();    // 退出状态为 6
    }

    if ((pid = fork()) < 0)
        err_sys("fork error");
    else if (pid != 0) { // 第二个子进程
        execl("/bin/dd", "dd", "if=/etc/passwd", "of=/dev/null", NULL);
        exit(7);    // 退出状态为 7; 正常情况下不应该执行到这里
    }

    if ((pid = fork()) < 0)
        err_sys("fork error");
    else if (pid != 0) { // 第三个子进程
        sleep(8);
        exit(0);    // 退出状态为 0,正常退出
    }

    sleep(6); // 第四个子进程
    kill(getpid(), SIGKILL); // 强制终止
    exit(6);    // 退出状态为 6; 正常情况下不应该执行到这里
}