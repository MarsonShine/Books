// 请求和释放一把锁
#include "apue.h"
#include <fcntl.h>

// 请求或释放一把锁
// 参数：
//     fd：文件描述符
//     cmd：F_SETLK, F_SETLKW, F_GETLK
//     type：F_RDLCK, F_WRLCK, F_UNLCK
//     offset：加锁的起始位置
//     whence：SEEK_SET, SEEK_CUR, SEEK_END
//     len：加锁的长度
// 返回值：
//     成功：0
//     失败：-1
int lock_reg(int fd, int cmd, int type, off_t offset, int whence, off_t len)
{
    struct flock lock;

    lock.l_type = type; // F_RDLCK, F_WRLCK, F_UNLCK
    lock.l_start = offset; // 加锁的起始位置
    lock.l_whence = whence; // SEEK_SET, SEEK_CUR, SEEK_END
    lock.l_len = len; // 加锁的长度

    return (fcntl(fd, cmd, &lock));
}