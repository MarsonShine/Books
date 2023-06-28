// 非阻塞I/O示例
#include "apue.h"
#include <errno.h>
#include <fcntl.h>

char buf[500000];

int main(void)
{
    int ntowrite, nwrite;
    char *ptr;

    ntowrite = read(STDIN_FILENO, buf, sizeof(buf)); // 从标准输入读取数据
    fprintf(stderr, "read %d bytes\n", ntowrite);

    set_fl(STDOUT_FILENO, O_NONBLOCK); // 设置标准输出为非阻塞模式

    ptr = buf;
    while (ntowrite > 0) {
        errno = 0;
        nwrite = write(STDOUT_FILENO, ptr, ntowrite); // 将数据写入标准输出
        fprintf(stderr, "nwrite = %d, errno = %d\n", nwrite, errno);

        if (nwrite > 0) {
            ptr += nwrite;
            ntowrite -= nwrite;
        }
    }

    clr_fl(STDOUT_FILENO, O_NONBLOCK); // 清除标准输出的非阻塞模式

    exit(0);
}