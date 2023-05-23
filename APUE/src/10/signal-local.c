#include "apue.h"

/* 可靠版本的 signal 实现，使用的是 POSIX sigaction()*/
Sigfunc *signal(int signo, Sigfunc *func)
{
    struct sigaction act, oact;
    act.sa_handler = func; // 设置信号处理函数
    sigemptyset(&act.sa_mask); // 清空信号屏蔽字
    act.sa_flags = 0;
    if (signo == SIGALRM) { // 特殊处理 SIGALRM 信号
#ifdef SA_INTERRUPT
        act.sa_flags |= SA_INTERRUPT; // 系统调用被中断时，不重启动系统调用
#endif
    } else {
#ifdef SA_RESTART
        act.sa_flags |= SA_RESTART; // 系统调用被中断时，重启动系统调用
#endif
    }
    if (sigaction(signo, &act, &oact) < 0) // 设置信号处理函数
        return (SIG_ERR);
    return (oact.sa_handler);
}