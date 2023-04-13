#include "apue.h"
#include <errno.h>
#include <sys/time.h>

#if defined(MACOS)
#include <sys/syslimits.h>
#elif defined(SOLARIS)
#include <limits.h>
#elif defined(BSD)
#include <sys/param.h>
#endif
/*
 * 以下程序展示了进程调整 nice 值的效果。
 两个进程同时运行，各自增加一个计数器。父进程使用了默认的 nice 值。子进程通过调用 nice 调整了 nice 值。
 */
unsigned long long count;
struct timeval end;

void checktime(char *str)
{
    struct timeval tv;

    gettimeofday(&tv, NULL);
    if (tv.tv_sec >= end.tv_sec && tv.tv_usec >= end.tv_usec) {
        printf("%s count = %lld\n", str, count);
        exit(0);
    }
}

int main(int argc, char *argv[])
{
    pid_t pid;
    char *s;
    int nzero, ret;
    int adj = 0;

    setbuf(stdout, NULL); // 设置无缓冲
#if defined(NZERO)
    nzero = NZERO; // NZERO 是 nice 值的默认偏移量
#elif defined(_SC_NZERO)
    nzero = sysconf(_SC_NZERO); // 通过系统调用获取 nice 值的默认偏移量
#else