#include <signal.h>
#include <errno.h>

/*
* <signal.h> usually defines NSIG to include signal number 0. 头文件 <signal.h> 通常会定义 NSIG，其中包括信号编号 0。
* 这段代码的作用是在已有的代码中添加一个宏定义，用于检查信号编号是否有效。
*/
#define SIGBAD(signo) ((signo) <= 0 || (signo) >= NSIG)

int sigaddset(sigset_t *set, int signo)
{
    if (SIGBAD(signo)) { // 检查信号编号是否有效
        errno = EINVAL;
        return -1;
    }
    *set |= 1 << (signo - 1); // 将信号编号对应的位设置为 1
    return 0;
}

int sigdelset(sigset_t *set, int signo)
{
    if (SIGBAD(signo)) { // 检查信号编号是否有效
        errno = EINVAL;
        return -1;
    }
    *set &= ~(1 << (signo - 1)); // 将信号编号对应的位设置为 0
    return 0;
}

int sigismember(const sigset_t *set, int signo)
{
    if (SIGBAD(signo)) { // 检查信号编号是否有效
        errno = EINVAL;
        return -1;
    }
    return ((*set & (1 << (signo - 1))) != 0); // 检查信号编号对应的位是否为 1
}