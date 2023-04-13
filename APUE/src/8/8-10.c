#include "apue.h"
#include <sys/wait.h>

char *env_init[] = { "USER=unknown", "PATH=/tmp", NULL };

int main(void)
{
    pid_t pid;
    if ((pid == fork()) < 0) {
        err_sys("fork error");
    } else if (pid == 0) { // child, 指定 pathname，环境变量
        // execle 要求传递一个路径名和一个特定的环境变量
        if (execle("/home/sar/bin/echoall", "echoall", "myarg1", "MY ARG2", (char *)0, env_init) < 0)
            err_sys("execle error");
    }
    if (waitpid(pid, NULL, 0) < 0)
        err_sys("wait error");
    if ((pid = fork()) < 0) {
        err_sys("fork error");
    } else if (pid == 0) { // child, 指定 filename，继承环境变量
        // 第一个要执行的文件参数之所以只需要 echoall，是因为它就当前路径下（/home/sar/bin），所以可以不用指定路径
        if (execlp("echoall", "echoall", "only 1 arg", (char *)0) < 0)
            err_sys("execlp error");
    }
    exit(0);
}