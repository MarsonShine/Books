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