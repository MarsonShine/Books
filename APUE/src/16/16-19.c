// 无连接的客户端
#include "apue.h"
#include <netdb.h>
#include <errno.h>
#include <sys/socket.h>

#define BUFLEN 128
#define TIMEOUT 20

void sigalrm(int signo)
{
}

void print_uptime(int sockfd, struct addrinfo *aip)
{
    int n;
    char buf[BUFLEN];

    buf[0] = 0;
    // 发送数据
    if (sendto(sockfd, buf, 1, 0, aip->ai_addr, aip->ai_addrlen) < 0) // 无连接发送数据 sendto
        err_sys("sendto error");
    // 设置SIGALRM信号的超时时间，避免调用 recvfrom 无限期阻塞
    alarm(TIMEOUT);
    // 接收数据
    if ((n = recvfrom(sockfd, buf, BUFLEN, 0,NULL,NULL)) < 0)
    {
        if (errno != EINTR)
            alarm(0);
        err_sys("recv error");
    }
    // 关闭SIGALRM信号
    alarm(0);
    // 打印数据
    write(STDOUT_FILENO, buf, n);
}

int main(int argc, char *argv[])
{
    struct addrinfo *ailist, *aip;
    struct addrinfo hint;
    int sockfd, err;
    struct sigaction sa;

    if (argc != 2)
        err_quit("usage: ruptime hostname");
    // 设置SIGALRM信号的处理函数
    sa.sa_handler = sigalrm;
    sa.sa_flags = 0;
    sigemptyset(&sa.sa_mask); // 清空信号屏蔽字
    // 注册SIGALRM信号的处理函数
    if (sigaction(SIGALRM, &sa, NULL) < 0)
        err_sys("sigaction error");
    // 初始化addrinfo结构体
    memset(&hint, 0, sizeof(hint));
    hint.ai_flags = 0;
    hint.ai_family = 0;
    hint.ai_socktype = SOCK_DGRAM; // 无连接套接字
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
        // 创建套接字
        if ((sockfd = socket(aip->ai_family, SOCK_DGRAM, 0)) < 0)
            err = errno;
        else
        {
            // 打印服务器运行时间
            print_uptime(sockfd, aip);
            exit(0);
        }
    }
    // 打印错误信息
    fprintf(stderr, "can't contact %s: %s\n", argv[1], strerror(err));
    exit(1);
}