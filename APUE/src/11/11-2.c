#include "apue.h"
#include <pthread.h>

pthread_t ntid;

void printids(const char *s)
{
    pid_t pid;  // 进程 ID
    pthread_t tid; // 线程 ID

    pid = getpid(); // 获取进程 ID
    tid = pthread_self(); // 获取线程 ID
    printf("%s pid %lu tid %lu (0x%lx)\n", s, (unsigned long)pid,
        (unsigned long)tid, (unsigned long)tid);
}

void *thr_fn(void *arg)
{
    printids("new thread: ");
    return((void *)0);
}

int main(void)
{
    int err;
    err = pthread_create(&ntid, NULL, thr_fn, NULL); // 创建线程
    if (err!=0)
    {
        err_exit(err, "can't create thread");
    }
    printids("main thread: ");
    sleep(1); // 为什么要 sleep 1 秒？A: 为了让新线程有机会运行。
    exit(0);
}

// 在子简称访问共享变量时要注意，由于不知道子线程到底是先于主线程运行还是后于主线程运行
// 因此在子线程中访问共享变量时，要确保主线程已经对共享变量进行了初始化。