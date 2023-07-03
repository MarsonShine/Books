#include "apue.h"
#include <sys/wait.h>

// 为了避免将所有数据写道一个临时文件中显示，可以通过管道将输出直接送到分页程序

#define DEF_PAGER "/bin/more" // 默认分页程序

// 写一个带有注释的例子，说明如何使用管道将输出直接送到分页程序
int main(int argc, char *argv[1])
{
    int n;
    int fd[2];
    pid_t pid;
    char *pager, *argv0;
    char line[MAXLINE];
    FILE *fp;

    if (argc != 2) {
        err_quit("usage: %s <pathname>", argv[0]);
    }

    if ((fp = fopen(argv[1], "r")) == NULL) {
        err_sys("can't open %s", argv[1]);
    }
    if (pipe(fd) < 0) {
        err_sys("pipe error");
    }

    if ((pid = fork()) < 0) {
        err_sys("fork error");
    } else if (pid > 0) { // 父进程
        close(fd[0]); // 关闭读端

        // 父进程将文件内容写入管道
        while (fgets(line, MAXLINE, fp) != NULL) {
            n = strlen(line);
            if (write(fd[1], line, n) != n) {
                err_sys("write error to pipe");
            }
        }
        if (ferror(fp)) {
            err_sys("fgets error");
        }

        close(fd[1]); // 关闭写端
        // 如果父进程不使用 waitpid 等待子进程结束，那么子进程将成为僵尸进程。在这种情况下，子进程已经终止，但是它的状态信息仍然保存在系统中，直到父进程调用 waitpid 或者 wait 函数来获取子进程的状态信息。如果父进程不调用这些函数，那么子进程的状态信息将一直保存在系统中，占用系统资源，可能会导致系统资源的浪费。
        // 另外，如果子进程的输出比较多，而父进程没有使用 waitpid 等待子进程结束，那么父进程可能会在子进程还没有结束时就退出，导致子进程成为孤儿进程，从而被 init 进程接管。这种情况下，子进程的输出可能会被 init 进程接管，而不是传递给分页程序，导致输出结果不正确。
        // 因此在使用管道将输出直接送到分页程序时，父进程应该使用 waitpid 或者 wait 函数等待子进程结束，以避免子进程成为僵尸进程或者孤儿进程。
        if (waitpid(pid, NULL, 0) < 0) {
            err_sys("waitpid error");
        }
        exit(0);
    } else { // 子进程
        close(fd[1]); // 关闭写端
        if (fd[0] != STDIN_FILENO) {
            if (dup2(fd[0], STDIN_FILENO) != STDIN_FILENO) { // 将管道的读端复制到标准输入
                err_sys("dup2 error to stdin");
            }
            close(fd[0]); // 关闭原来的文件描述符
        }

        // 获取分页程序的名称
        if ((pager = getenv("PAGER")) == NULL) {
            pager = DEF_PAGER;
        }
        if ((argv0 = strrchr(pager, '/')) != NULL) {
            argv0++; // 找到最后一个/字符
        } else {
            argv0 = pager; // 没有找到/字符
        }

        // 将分页程序作为子进程执行
        if (execl(pager, argv0, (char *)0) < 0) {
            err_sys("execl error for %s", pager);
        }
    }
}