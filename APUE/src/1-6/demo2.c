#include "../h/apue.h"
#include "sys/wait.h"

int main(void)
{
    char buf[MAXLINE];
    pid_t pid;
    int status;

    printf("%% "); // 打印，%% 才能打印 % 符号
    while (fgets(buf, MAXLINE, stdin) != NULL) // 从标准输入一次读一行
    {
        if (buf[strlen(buf) - 1] == '\n')
            buf[strlen(buf) - 1] = 0; /* 替换所有的换行符 */
        if ((pid = fork()) < 0) 
            err_sys("fork error");
        else if (pid == 0) {
            execlp(buf, buf, (char *)0);
            err_ret("couldn't execute: %s", buf)
            exit(127)
        }

        if ((pid = waitpid(pid, &status, 0)) < 0)   // 父进程等待子进程终止
        {
            err_sys("waitpid error");
        }
        printf("%% ");
        
    }
    exit(0);
}