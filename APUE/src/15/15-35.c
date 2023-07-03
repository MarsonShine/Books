// 使用 POSIX 信号量的互斥
#include "slock.h"
#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <errno.h>

// 分配一个新的 slock 结构体
struct slock * s_alloc()
{
    struct slock *sp;
    static int cnt;

    // 分配内存空间
    if ((sp = malloc(sizeof(struct slock))) == NULL) {
        return NULL;
    }

    // 生成一个唯一的信号量名称
    do {
        snprintf(sp->name, sizeof(sp->name), "/%ld.%d", (long)getpid(), cnt++);
        sp->semp = sem_open(sp->name, O_CREAT | O_EXCL, S_IRWXU, 1);
    } while ((sp->semp == SEM_FAILED) && (errno == EEXIST));

    // 如果信号量创建失败，释放内存空间并返回 NULL
    if (sp->semp == SEM_FAILED) {
        free(sp);
        return NULL;
    }

    // 删除信号量的名称，使其在进程结束时自动删除
    sem_unlink(sp->name);

    return sp;
}

// 释放一个 slock 结构体
void s_free(struct slock *sp)
{
    sem_close(sp->semp);
    free(sp);
}

// 对一个 slock 结构体进行加锁
int s_lock(struct slock *sp)
{
    return (sem_wait(sp->semp));
}

// 尝试对一个 slock 结构体进行加锁
int s_trylock(struct slock *sp)
{
    return (sem_trywait(sp->semp));
}

// 对一个 slock 结构体进行解锁
int s_unlock(struct slock *sp)
{
    return (sem_post(sp->semp));
}