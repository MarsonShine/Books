// 测试一把锁
#include "apue.h"
#include <fcntl.h>


/**
 * 测试文件锁
 * @param fd 文件描述符
 * @param type 锁类型：F_RDLCK, F_WRLCK, F_UNLCK
 * @param offset 加锁的起始位置
 * @param whence SEEK_SET, SEEK_CUR, SEEK_END
 * @param len 加锁的长度
 * @return 如果该锁未被加锁，返回0；否则返回加锁的进程ID
 */
pid_t lock_test(int fd, int type, off_t offset, int whence, off_t len)
{
    struct flock lock;

    lock.l_type = type; // F_RDLCK, F_WRLCK, F_UNLCK
    lock.l_start = offset; // 加锁的起始位置
    lock.l_whence = whence; // SEEK_SET, SEEK_CUR, SEEK_END
    lock.l_len = len; // 加锁的长度

    if (fcntl(fd, F_GETLK, &lock) < 0) {
        err_sys("fcntl error");
    }

    if (lock.l_type == F_UNLCK) {
        return (0); // 该锁未被加锁
    }

    return (lock.l_pid); // 返回加锁的进程ID
}
