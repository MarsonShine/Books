#include "apue.h"
#include <netdb.h>
#include <errno.h>
#include <sys/socket.h>

#define BUFLEN 128

extern int connect_retry(int, int, int, const struct sockaddr *, socklen_t);

void print_uptime(int sockfd)
{
    int n;
    char buf[BUFLEN];

    while ((n = recv(sockfd, buf, BUFLEN, 0)) > 0)
        write(STDOUT_FILENO, buf, n);
    if (n < 0)
        err_sys("recv error");
}

// 用于从服务器获取正常运行时间的客户端命令
int main(int argc, char *argv[])
{
    struct addrinfo *ailist, *aip;
    struct addrinfo hint;
    int sockfd, err;

    if (argc != 2)
        err_quit("usage: ruptime hostname");
    // 初始化addrinfo结构体
    memset(&hint, 0, sizeof(hint));
    hint.ai_flags = 0;
    hint.ai_family = 0;
    hint.ai_socktype = SOCK_STREAM;
    hint.ai_protocol = 0;
    hint.ai_addrlen = 0;
    hint.ai_canonname = NULL;
    hint.ai_addr = NULL;
    hint.ai_next = NULL;
    // 获取主机名对应的地址信息
    if ((err = getaddrinfo(argv[1], "ruptime", &hint, &ailist)) != 0)
        err_quit("getaddrinfo error: %s", gai_strerror(err));
    // 遍历地址信息
    for (aip = ailist; aip != NULL; aip = aip->ai_next)
    {
        // 连接套接字
        if ((sockfd == connect_retry(aip->ai_family, SOCK_STREAM, 0, aip->ai_addr, aip->ai_addrlen)) < 0)
            err = errno;
        else
        {
            // 打印服务器运行时间
            print_uptime(sockfd);
            exit(0);
        }
    }
    // 打印错误信息
    fprintf(stderr, "can't connect to %s: %s\n", argv[1], strerror(err));
}