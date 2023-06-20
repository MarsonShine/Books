#include <string.h>
#include <errno.h>
#include <pthread.h>
#include <stdlib.h>

extern char **environ;

pthread_mutex_t env_mutex;

static pthread_once_t init_done = PTHREAD_ONCE_INIT;

static void thread_init(void)
{
    pthread_mutexattr_t attr;

    pthread_mutexattr_init(&attr); // 初始化互斥量属性
    pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE); // 设置互斥量属性为递归锁
    pthread_mutex_init(&env_mutex, &attr); // 初始化互斥量
    pthread_mutexattr_destroy(&attr); // 销毁互斥量属性
}

// 非线程安全的
char *getenv(const char *name)
{
    int i, len;
    len = strlen(name);
    for (i = 0; environ[i] != NULL; i++) {
        if ((strncmp(name, environ[i], len) == 0) &&
            (environ[i][len] == '=')) {
                strcpy(buf, &environ[i][len+1])
                return (&environ[i][len+1]);
        }
    }
    return (NULL);
}

// 线程安全的
int getenv_r(const char *name, char *buf, int buflen)
{
    int i, len, olen;

    pthread_once(&init_done, thread_init); // 确保线程只初始化一次
    len = strlen(name);
    pthread_mutex_lock(&env_mutex); // 获取互斥量
    for (i = 0; environ[i] != NULL; i++) {
        if ((strncmp(name, environ[i], len) == 0) &&
            (environ[i][len] == '=')) {
            olen = strlen(&environ[i][len+1]);
            if (olen >= buflen) {
                pthread_mutex_unlock(&env_mutex); // 释放互斥量
                return (ENOSPC);
            }
            strcpy(buf, &environ[i][len+1]);
            pthread_mutex_unlock(&env_mutex); // 释放互斥量
            return (0);
        }
    }
    pthread_mutex_unlock(&env_mutex); // 释放互斥量
    return (ENOENT);
}