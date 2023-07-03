// 编写一个在父进程、子进程之间使用/dev/zero的存储映射I/O的IPC
#include "apue.h"
#include <fcntl.h>
#include <sys/mman.h>

#define NLOOPS 1000
#define SIZE sizeof(long) // 一个long的大小

static int update(long *ptr)
{
    return ((*ptr)++); // 返回加1之前的值
}

int main(void)
{
    int fd, i, counter;
    pid_t pid;
    void *area;

    if ((fd = open("/dev/zero", O_RDWR)) < 0) { // 打开/dev/zero
        err_sys("open error");
    }
    // 它将一个大小为 SIZE 字节的文件映射到进程的地址空间中，并返回一个指向映射区域的指针 area。
    if ((area = mmap(0, SIZE, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0)) == MAP_FAILED) { // 映射/dev/zero
        err_sys("mmap error");
    }
    // 一旦存储区映射成功，就要关闭此设备
    close(fd); // 关闭/dev/zero

    TELL_WAIT(); // 父进程、子进程之间的同步

    if ((pid = fork()) < 0) {
        err_sys("fork error");
    } else if (pid > 0) { // 父进程
        for (i = 0; i < NLOOPS; i += 2) {
            if ((counter = update((long *)area)) != i) { // 更新共享内存
                err_quit("parent: expected %d, got %d", i, counter);
            }
            TELL_CHILD(pid); // 通知子进程
            WAIT_CHILD(); // 等待子进程
        }
    } else { // 子进程
        for (i = 1; i < NLOOPS + 1; i += 2) {
            WAIT_PARENT(); // 等待父进程
            if ((counter = update((long *)area)) != i) { // 更新共享内存
                err_quit("child: expected %d, got %d", i, counter);
            }
            TELL_PARENT(getppid()); // 通知父进程
        }
    }

    exit(0);
}

