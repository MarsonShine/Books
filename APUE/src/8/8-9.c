#include "apue.h"
TELL_WAIT(); // 为 TELL_XXX 和 WAIT_XXX 设置事情。
if ((pid = fork()) < 0)
    err_sys("fork error");
else if (pid == 0) { // 子进程
    // 子进程继续执行要做的事情 ...
    TELL_PARENT(getppid()); // 通知父进程
    WAIT_PARENT(); // 等待父进程
    // 子进程继续执行
    exit(0);
}
// 父进程继续执行要做的事情 ...
TELL_CHILD(pid);    // 通知子进程
WAIT_CHILD();// 等待子进程
// 父进程继续执行
exit(0);


