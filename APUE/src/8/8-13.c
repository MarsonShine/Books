#include <sys/wait.h>
#include <errno.h>
#include <unistd.h>
#include "apue.h"

int system(const char *cmdString) // version without signal handling
{
    pid_t pid;
    int status;

    if (cmdString == NULL)
        return 1; // always a command processor with UNIX

    if ((pid = fork()) < 0) {
        status = -1; // probably out of processes
    } else if (pid == 0) { // child
        execl("/bin/sh", "sh", "-c", cmdString, (char *)0);
        _exit(127); // execl error
    } else { // parent
        while (waitpid(pid, &status, 0) < 0) { // wait for child,system 产生的子进程终止才退出循环
            if (errno != EINTR) {
                status = -1; // error other than EINTR from waitpid()
                break;
            }
        }
    }
    return status;
}

int main(void)
{
    int status;
    if ((status = system("date")) < 0)
        err_sys("system() error");
    
    pr_exit(status);

    if ((status = system("nosuchcommand")) < 0)
        err_sys("system() error");

    pr_exit(status);

    if ((status = system("who; exit 44")) < 0)
        err_sys("system() error");

    pr_exit(status);

    exit(0);
}