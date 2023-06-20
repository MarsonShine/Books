#include "apue.h"
#include <pthread.h>

int quitflag;   // 由线程设置，主程序读取
sigset_t mask;  // 信号屏蔽字

pthread_mutex_t lock = PTHREAD_MUTEX_INITIALIZER; // 互斥量
pthread_cond_t waitloc = PTHREAD_COND_INITIALIZER; // 条件变量

void *
thr_fn(void *arg)
{
    int err, signo;
    for (;;) {
        err = sigwait(&mask, &signo); // 等待信号
        if (err != 0)
            err_exit(err, "sigwait failed");
        switch (signo) {
        case SIGINT:
            printf("\ninterrupt\n");
            break;
        case SIGQUIT:
            pthread_mutex_lock(&lock); // 获取互斥量
            quitflag = 1;
            pthread_mutex_unlock(&lock); // 释放互斥量
            pthread_cond_signal(&waitloc); // 发送条件变量
            return (0);
        default:
            printf("unexpected signal %d\n", signo);
            exit(1);
        }
    }
}

int main(void)
{
    int err;
    sigset_t oldmask;
    pthread_t tid;

    sigemptyset(&mask); // 清空信号集
    sigaddset(&mask, SIGINT); // 添加信号
    sigaddset(&mask, SIGQUIT); // 添加信号
    if ((err = pthread_sigmask(SIG_BLOCK, &mask, &oldmask)) != 0) // 设置信号屏蔽字
        err_exit(err, "SIG_BLOCK error");

    err = pthread_create(&tid, NULL, thr_fn, 0); // 创建线程
    if (err != 0)
        err_exit(err, "can't create thread");

    pthread_mutex_lock(&lock); // 获取互斥量
    while (quitflag == 0)
        pthread_cond_wait(&waitloc, &lock); // 等待条件变量
    pthread_mutex_unlock(&lock); // 释放互斥量

    /* SIGQUIT has been caught and is now blocked; do whatever */
    quitflag = 0;

    /* reset signal mask which unblocks SIGQUIT */
    if (sigprocmask(SIG_SETMASK, &oldmask, NULL) < 0) // 设置信号屏蔽字
        err_sys("SIG_SETMASK error");

    exit(0);
}