#include "apue.h"
#include <pthread.h>

int makethread(void *(*fn)(void *), void *arg)
{
    int err;
    pthread_t tid;
    pthread_attr_t attr;

    err = pthread_attr_init(&attr); // 初始化线程属性
    if (err != 0)
        return (err);
    err = pthread_attr_getdetachstate(&attr, PTHREAD_CREATE_DETACHED); // 设置线程属性,以分离状态启动线程
    if (err == 0)
        err = pthread_create(&tid, &attr, fn, arg); // 创建线程
    pthread_attr_destroy(&attr);                    // 销毁线程属性，如果这一步发生错误，则会导致内存泄漏
    return (err);
}