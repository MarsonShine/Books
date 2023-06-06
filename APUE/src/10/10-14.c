/*
* sigprocmask 函数的使用
*/
#include "apue.h"
#include <errno.h>

void pr_mask(const char *str)
{
    sigset_t sigset;
    int errno_save;

    errno_save = errno; // 保存 errno
    if (sigprocmask(0, NULL, &sigset) < 0) {
        err_ret("sigprocmask error");
    } else {
        printf("%s", str);
        if (sigismember(&sigset, SIGINT)) // 检查 SIGINT 信号是否在信号屏蔽字中
            printf(" SIGINT");
        if (sigismember(&sigset, SIGQUIT)) // 检查 SIGQUIT 信号是否在信号屏蔽字中
            printf(" SIGQUIT");
        if (sigismember(&sigset, SIGUSR1)) // 检查 SIGUSR1 信号是否在信号屏蔽字中
            printf(" SIGUSR1");
        if (sigismember(&sigset, SIGALRM)) // 检查 SIGALRM 信号是否在信号屏蔽字中
            printf(" SIGALRM");

        /* remaining signals can go here  其他信号可以在这里添加 */

        printf("\n");
    }

    errno = errno_save; // 恢复 errno
}