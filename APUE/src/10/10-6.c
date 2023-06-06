#include "apue.h"

#ifdef __linux__
#include <sys/wait.h>
#endif

static void sig_cld(int);

#include "apue.h"

#ifdef __linux__
#include <sys/wait.h>
#endif

static void sig_cld(int);

int main()
{
    pid_t pid;
    if (signal(SIGCLD, sig_cld) == SIG_ERR)
        perror("signal error");
    if ((pid = fork()) < 0) {
        perror("fork error");
    } else if (pid == 0) { // 子进程
        sleep(2);
        _exit(0);
    }

    pause();
    exit(0);
}

static void sig_cld(int signo) // 中断暂停
{
    pid_t pid;
    int status;

    printf("SIGCLD received\n");

    if (signal(SIGCLD, sig_cld) == SIG_ERR) // 重新注册信号处理函数
        perror("signal error");

    while ((pid = waitpid(-1, &status, WNOHANG)) > 0) { // 等待指定的子进程终止
        printf("pid = %d\n", pid);
    }
}
