// readn,writen
#include "apue.h"

ssize_t readn(int fd, void *ptr, size_t n)
{
    size_t nleft;
    ssize_t nread;

    nleft = n; // 剩余字节数
    while (nleft > 0) {
        if ((nread = read(fd, ptr, nleft)) < 0) { // 读取数据
            if (nleft == n) { // 如果是第一次读取就出错了，直接返回错误
                return -1;
            } else { // 如果不是第一次读取就返回已经读取的字节数
                break;
            }
        } else if (nread == 0) { // 读取到文件尾
            break;
        }
        nleft -= nread; // 更新剩余字节数
        ptr += nread; // 更新指针
    }
    return n - nleft; // 返回已经读取的字节数
}

ssize_t writen(int fd, const void *ptr, size_t n)
{
    size_t nleft;
    ssize_t nwritten;

    nleft = n; // 剩余字节数
    while (nleft > 0) {
        if ((nwritten = write(fd, ptr, nleft)) < 0) { // 写入数据
            if (nleft == n) { // 如果是第一次写入就出错了，直接返回错误
                return -1;
            } else { // 如果不是第一次写入就返回已经写入的字节数
                break;
            }
        } else if (nwritten == 0) { // 写入到文件尾
            break;
        }
        nleft -= nwritten; // 更新剩余字节数
        ptr += nwritten; // 更新指针
    }
    return n - nleft; // 返回已经写入的字节数
}