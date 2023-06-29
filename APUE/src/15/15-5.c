#include "apue.h"

// 创建一个从父进程到子进程的管道，并且父进程经由该管道向子进程传送数据
int main(void)
{
    int fd[2];
    pid_t pid;
    char line[MAXLINE];
    int n;

    if (pipe(fd) < 0) { // 创建管道
        err_sys("pipe error");
    }
    if ((pid = fork()) < 0) { // 创建子进程
        err_sys("fork error");
    } else if (pid > 0) { // 父进程
        close(fd[0]); // 关闭读端
        write(fd[1], "hello world\n", 12); // 向子进程写入数据
    } else { // 子进程
        close(fd[1]); // 关闭写端
        n = read(fd[0], line, MAXLINE); // 从父进程读取数据
        write(STDOUT_FILENO, line, n); // 输出数据
    }

    exit(0);
}