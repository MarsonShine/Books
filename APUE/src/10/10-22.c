#include "apue.h"

static void sig_int(int);

int main(void)
{
    sigset_t newmask, oldmask, waitmask;

    pr_mask("program start: "); // 打印进程的当前信号屏蔽字

    if (signal(SIGINT, sig_int) == SIG_ERR)
        err_sys("signal(SIGINT) error");
    sigemptyset(&waitmask); // 清空信号集
    sigaddset(&waitmask, SIGUSR1); // 将 SIGUSR1 信号添加到信号集中
    sigemptyset(&newmask); // 清空信号集
    sigaddset(&newmask, SIGINT); // 将 SIGINT 信号添加到信号集中

    /*
     * Block SIGINT and save current signal mask.
     * 阻塞 SIGINT 信号，并保存当前的信号屏蔽字
     */
    if (sigprocmask(SIG_BLOCK, &newmask, &oldmask) < 0)
        err_sys("SIG_BLOCK error");
    
    pr_mask("in critical region: "); // 打印进程的当前信号屏蔽字

    /*
     * Pause, allowing all signals except SIGUSR1.
     * 暂停，允许除 SIGUSR1 之外的所有信号
     */
    if (sigsuspend(&waitmask) != -1) // 暂停进程，直到捕捉到一个信号
        err_sys("sigsuspend error");

    pr_mask("after return from sigsuspend: "); // 打印进程的当前信号屏蔽字

    /*
     * Reset signal mask which unblocks SIGINT.
     * 重置信号屏蔽字，解除对 SIGINT 的阻塞
     */
    if (sigprocmask(SIG_SETMASK, &oldmask, NULL) < 0)
        err_sys("SIG_SETMASK error");

    pr_mask("program exit: "); // 打印进程的当前信号屏蔽字

    exit(0);
}

static void sig_int(int signo)
{
    pr_mask("\nin sig_int: "); // 打印进程的当前信号屏蔽字
}

/*
*
上述代码中的两次 sigprocmask 调用分别是：
sigprocmask(SIG_BLOCK, &newmask, &oldmask)：该调用将 newmask 中的信号添加到进程的信号屏蔽字中，同时将原来的信号屏蔽字保存到 oldmask 中。因此，该调用会阻塞 SIGINT 信号，并保存当前的信号屏蔽字。
sigprocmask(SIG_SETMASK, &oldmask, NULL)：该调用将进程的信号屏蔽字设置为 oldmask 中保存的信号屏蔽字，即解除对 SIGINT 信号的阻塞。因此，该调用会重置信号屏蔽字，解除对 SIGINT 的阻塞。
总的来说，第一次调用 sigprocmask 阻塞了 SIGINT 信号，并保存了当前的信号屏蔽字，第二次调用 sigprocmask 解除了对 SIGINT 信号的阻塞，恢复了之前的信号屏蔽字。
*
*/

