// 初始化一个套接字端点供服务器进程使用
#include "apue.h"
#include <errno.h>
#include <sys/socket.h>

int initserver(int type, const struct sockaddr *addr, socklen_t alen, int qlen)
{
    int fd, err = 0;

    // 创建套接字
    if ((fd = socket(addr->sa_family, type, 0)) < 0)
        return (-1);
    // 绑定套接字
    if (bind(fd, addr, alen) < 0)
        goto errout;
    // 如果是流式套接字，则监听套接字
    if (type == SOCK_STREAM || type == SOCK_SEQPACKET)
    {
        if (listen(fd, qlen) < 0)
            goto errout;
    }
    return (fd);

errout:
    err = errno;
    close(fd);
    errno = err;
    return (-1);
}