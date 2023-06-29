// 通过管道的形式实现让父进程和子进程同步的例程
// 15-7.c
#include "apue.h"

static int pfd1[2], pfd2[2];

// 创建一个从父进程到子进程的管道，并且父进程经由该管道向子进程传送数据
void TELL_WAIT(void)
{
    if (pipe(pfd1) < 0 || pipe(pfd2) < 0) { // 创建管道
        err_sys("pipe error");
    }
}

// 通知子进程
void TELL_CHILD(pid_t pid)
{
    if (write(pfd2[1], "c", 1) != 1) { // 向子进程写入数据
        err_sys("write error");
    }
}

// 等待子进程通知
void WAIT_CHILD(void)
{
    char c;

    if (read(pfd1[0], &c, 1) != 1) { // 从子进程读取数据
        err_sys("read error");
    }
    if (c != 'p') {
        err_quit("WAIT_CHILD: incorrect data");
    }
}

// 通知父进程
void TELL_PARENT(pid_t pid)
{
    if (write(pfd1[1], "p", 1) != 1) { // 向父进程写入数据
        err_sys("write error");
    }
}

// 等待父进程通知
void WAIT_PARENT(void)
{
    char c;

    if (read(pfd2[0], &c, 1) != 1) { // 从父进程读取数据
        err_sys("read error");
    }
    if (c != 'c') {
        err_quit("WAIT_PARENT: incorrect data");
    }
}

// 创建一个从父进程到子进程的管道，并且父进程经由该管道向子进程传送数据
int main(void)
{
    pid_t pid;
    char line[MAXLINE];
    int n;

    TELL_WAIT(); // 创建一个文件锁

    // 创建一个子进程
    if ((pid = fork()) < 0) {
        err_sys("fork error");
    } else if (pid > 0) { // 父进程
        close(pfd1[0]); // 关闭读端
        close(pfd2[1]); // 关闭写端
        waitpid(pid, NULL, 0); // 等待子进程终止
        n = read(pfd2[0], line, MAXLINE); // 从子进程读取数据
        write(STDOUT_FILENO, line, n); // 输出数据
    } else { // 子进程
        close(pfd1[1]); // 关闭写端
        close(pfd2[0]); // 关闭读端
        TELL_PARENT(getppid()); // 通知父进程
        WAIT_PARENT(); // 等待父进程通知
        n = read(pfd1[0], line, MAXLINE); // 从父进程读取数据
        write(pfd2[1], line, n); // 向父进程写入数据
    }

    exit(0);
}