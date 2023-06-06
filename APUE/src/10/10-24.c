/*
* 通过信号量来实现父子进程的协同工作
*/
#include "apue.h"

static volatile sig_atomic_t sigflag; /* 通过 sig handler 设置 nonzero */
static sigset_t newmask, oldmask, zeromask;

static void sig_usr(int signo) /* one signal handler for SIGUSR1 and SIGUSR2 */
{
    sigflag = 1;
}

void TELL_WAIT(void)
{
    if (signal(SIGUSR1, sig_usr) == SIG_ERR)
        err_sys("signal(SIGUSR1) error");
    if (signal(SIGUSR2, sig_usr) == SIG_ERR)
        err_sys("signal(SIGUSR2) error");
    sigemptyset(&zeromask); /* 初始化信号集 */
    sigemptyset(&newmask);
    sigaddset(&newmask, SIGUSR1); /* 将 SIGUSR1 信号添加到信号集中 */
    sigaddset(&newmask, SIGUSR2); /* 将 SIGUSR2 信号添加到信号集中 */

    /* 阻塞 SIGUSR1 和 SIGUSR2 信号，并保存当前信号屏蔽字 */
    if (sigprocmask(SIG_BLOCK, &newmask, &oldmask) < 0)
        err_sys("SIG_BLOCK error");
}

void TELL_PARENT(pid_t pid)
{
    kill(pid, SIGUSR2); /* 告诉父进程已经执行完毕 */
}

void WAIT_PARENT(void)
{
    while (sigflag == 0)
        sigsuspend(&zeromask); /* 等待父进程发送 SIGUSR2 信号 */
    sigflag = 0;

    /* 重置信号屏蔽字，解除对 SIGUSR1 和 SIGUSR2 的阻塞 */
    if (sigprocmask(SIG_SETMASK, &oldmask, NULL) < 0)
        err_sys("SIG_SETMASK error");
}

void TELL_CHILD(pid_t pid)
{
    kill(pid, SIGUSR1); /* 告诉子进程已经执行完毕 */
}

void WAIT_CHILD(void)
{
    while (sigflag == 0)
        sigsuspend(&zeromask); /* 等待子进程发送 SIGUSR1 信号 */
    sigflag = 0;

    /* 重置信号屏蔽字，解除对 SIGUSR1 和 SIGUSR2 的阻塞 */
    if (sigprocmask(SIG_SETMASK, &oldmask, NULL) < 0)
        err_sys("SIG_SETMASK error");
}