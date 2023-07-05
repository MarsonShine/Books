// 面向连接的服务器
#include "apue.h"
#include <netdb.h>
#include <errno.h>
#include <syslog.h>
#include <sys/socket.h>

#define BUFLEN 128
#define QLEN 10

#ifndef HOST_NAME_MAX
#define HOST_NAME_MAX 256
#endif

extern int initserver(int, const struct sockaddr *, socklen_t, int);

void serve(int sockfd)
{
    int clfd;
    FILE *fp;
    char buf[BUFLEN];

    set_cloexec(sockfd);
    for (;;)
    {
        // 接受客户端连接
        if ((clfd = accept(sockfd, NULL, NULL)) < 0)
        {
            syslog(LOG_ERR, "ruptimed: accept error: %s", strerror(errno));
            exit(1);
        }
        // 设置文件流
        set_cloexec(clfd);
        // 打开文件
        if ((fp = popen("/usr/bin/uptime", "r")) == NULL)
        {
            sprintf(buf, "error: %s\n", strerror(errno));
            send(clfd, buf, strlen(buf), 0);
        }
        else
        {
            // 读取文件内容
            while (fgets(buf, BUFLEN, fp) != NULL)
                send(clfd, buf, strlen(buf), 0);
            pclose(fp);
        }
        close(clfd);
    }
}

int main(int argc, char *argv[])
{
    struct addrinfo *ailist, *aip;
    struct addrinfo hint;
    int sockfd, err, n;
    char *host;

    if (argc != 1)
        err_quit("usage: ruptimed");
    // 获取主机名
    if ((n = sysconf(_SC_HOST_NAME_MAX)) < 0)
        n = HOST_NAME_MAX;
    if ((host = malloc(n)) == NULL)
        err_sys("malloc error");
    // 获取主机名
    if (gethostname(host, n) < 0)
        err_sys("gethostname error");
    // 初始化addrinfo结构体
    memset(&hint, 0, sizeof(hint));
    hint.ai_flags = AI_CANONNAME;
    hint.ai_family = 0;
    hint.ai_socktype = SOCK_STREAM;
    hint.ai_protocol = 0;
    hint.ai_addrlen = 0;
    hint.ai_canonname = NULL;
    hint.ai_addr = NULL;
    hint.ai_next = NULL;
    // 获取主机名对应的地址信息
    if ((err = getaddrinfo(host, "ruptime", &hint, &ailist)) != 0)
    {
        syslog(LOG_ERR, "ruptimed: getaddrinfo error: %s", gai_strerror(err));
        exit(1);
    }
    // 遍历地址信息
    for (aip = ailist; aip != NULL; aip = aip->ai_next)
    {
        // 初始化服务器套接字
        if ((sockfd = initserver(SOCK_STREAM, aip->ai_addr, aip->ai_addrlen, QLEN)) >= 0)
        {
            // 服务器进程
            serve(sockfd);
            exit(0);
        }
    }
    exit(1);
}