/*
* 条件变量和互斥量对线程进行同步的使用方法
*/
#include "apue.h"
#include <pthread.h>

struct msg {
    struct msg *m_next;
    /* ... 其它属性 ... */
};

struct msg *workg;

pthread_cond_t qready = PTHREAD_COND_INITIALIZER; // 条件变量
pthread_mutex_t qlock = PTHREAD_MUTEX_INITIALIZER; // 互斥量

void process_msg(void)
{
    struct msg *mp;
    for(;;) {
        pthread_mutex_lock(&qlock); // 获取互斥量
        while (workg == NULL) // 如果没有作业，则等待条件变量
            pthread_cond_wait(&qready, &qlock);
        mp = workg;
        workg = mp->m_next;
        pthread_mutex_unlock(&qlock); // 释放互斥量
        /* 现在可以处理消息 mp 了 */
    }
}

void enqueue_msg(struct msg *mp)
{
    pthread_mutex_lock(&qlock); // 获取互斥量
    mp->m_next = workg;
    workg = mp;
    pthread_mutex_unlock(&qlock); // 释放互斥量
    pthread_cond_signal(&qready); // 发送条件变量
}