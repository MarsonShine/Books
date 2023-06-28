#include "apue.h"
#include <fcntl.h>

static void lockabyte(const char *name, int fd, off_t offset)
{
    if (writew_lock(fd, offset, SEEK_SET, 1) < 0) {
        err_sys("%s: writew_lock error", name);
    }
    printf("%s: got the lock, byte %lld\n", name, (long long)offset);
}

// 死锁
int main(void)
{
    int fd;
    pid_t pid;

    // 创建一个文件并写入数据
    if ((fd = creat("templock", FILE_MODE)) < 0) {
        err_sys("creat error");
    }
    if (write(fd, "ab", 2) != 2) {
        err_sys("write error");
    }

    TELL_WAIT(); // 创建一个文件锁

    // 创建一个子进程
    if ((pid = fork()) < 0) {
        err_sys("fork error");
    } else if (pid == 0) { // 子进程
        lockabyte("child", fd, 0); // 加锁
        TELL_PARENT(getppid()); // 通知父进程
        WAIT_PARENT(); // 等待父进程通知
        lockabyte("child", fd, 1); // 加锁
    } else { // 父进程
        lockabyte("parent", fd, 1); // 加锁
        TELL_CHILD(pid); // 通知子进程
        WAIT_CHILD(); // 等待子进程通知
        lockabyte("parent", fd, 0); // 加锁
    }

    exit(0);
}