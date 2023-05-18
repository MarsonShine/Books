#include "apue.h"
#include <pwd.h>

static void my_alarm(int signo)
{
    struct passwd* rootptr;
    printf("in signal handler\n");
    if ((rootptr = getpwnam("root")) == NULL)
        err_sys("getpwnam(root) error");
    alarm(1);
}

int main(void)
{
    struct passwd* ptr;

    signal(SIGALRM, my_alarm);
    alarm(1);
    for (;;) {
        if ((ptr = getpwnam("root")) == NULL)
            err_sys("getpwnam error");
        if (strcmp(ptr->pw_name, "root") != 0)
            printf("return value corrupted!, pw_name = %s\n", ptr->pw_name);
    }
}

// 上述函数都调用了不可重入函数 getpwnam
// 检查 core 文件，可以从中看出 main 已经调用了 getpwnam 函数，但当 getpwnam 调用 free 时，信号处理程序中断它的运行，并调用 getpwnam 函数，这时 getpwnam 函数又调用了 free。
// 在信号处理程序调用 free 而主程序也在调用 free 时，malloc 和 free 维护的数据结构就会遭到破坏。
// 程序运行结果也是随机的：正常运行数秒，然后因为产生 SIGSEGV 信号而终止。在捕捉到信号后，若 main 函数仍在正确运行，其返回值却有时错误，有时正确。