#include "apue.h"
#include <fcntl.h>
#include <sys/mman.h>


#define COPYINCR (1024 * 1024 * 1024) // 1GB
// 写一个带注释的用存储映射I/O复制文件例子
int main(int argc, char *argv[])
{
    int fdin, fdout;
    void *src, *dst;
    size_t copysz;
    struct stat sbuf;
    off_t fsz = 0;

    if (argc != 3) {
        err_quit("usage: %s <fromfile> <tofile>", argv[0]);
    }

    // 打开输入文件
    if ((fdin = open(argv[1], O_RDONLY)) < 0) {
        err_sys("can't open %s for reading", argv[1]);
    }

    // 打开输出文件
    if ((fdout = open(argv[2], O_RDWR | O_CREAT | O_TRUNC, FILE_MODE)) < 0) {
        err_sys("can't creat %s for writing", argv[2]);
    }

    // 获取输入文件的状态
    if (fstat(fdin, &sbuf) < 0) {
        err_sys("fstat error");
    }

    // 设置输出文件的大小
    if (ftruncate(fdout, sbuf.st_size) < 0) {
        err_sys("ftruncate error");
    }

    // 循环复制文件
    while (fsz < sbuf.st_size) {
        // 计算本次复制的字节数
        if ((sbuf.st_size - fsz) > COPYINCR) {
            copysz = COPYINCR;
        } else {
            copysz = sbuf.st_size - fsz;
        }

        // 将输入文件的一部分映射到内存
        if ((src = mmap(0, copysz, PROT_READ, MAP_SHARED, fdin, fsz)) == MAP_FAILED) {
            err_sys("mmap error for input");
        }

        // 将输出文件的一部分映射到内存
        if ((dst = mmap(0, copysz, PROT_READ | PROT_WRITE, MAP_SHARED, fdout, fsz)) == MAP_FAILED) {
            err_sys("mmap error for output");
        }

        // 将输入文件的一部分复制到输出文件
        memcpy(dst, src, copysz);

        // 解除映射
        munmap(src, copysz);
        munmap(dst, copysz);

        // 更新已复制的字节数
        fsz += copysz;
    }

    exit(0);
}