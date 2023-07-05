// 输出 APUE 第三版第16章的图16-18 中的程序代码
// 面向连接的服务器程序（即面向连接的套接字程序）
#include "apue.h"
#include <netdb.h>
#include <errno.h>
#include <syslog.h>
#include <sys/socket.h>

#define QLEN 10

#ifndef HOST_NAME_MAX
#define HOST_NAME_MAX 256
#endif

extern int initserver(int, const struct sockaddr *, socklen_t, int);

void serve(int sockfd)
{
    int clfd ,status;
    pid_t pid;

    set_cloexec(sockfd);
    for (;;)
    {
        // 接受客户端连接
        if ((clfd = accept(sockfd, NULL, NULL)) < 0)
        {
            syslog(LOG_ERR, "ruptimed: accept error: %s", strerror(errno));
            exit(1);
        }
        // 创建子进程
        if ((pid = fork()) < 0)
        {
            syslog(LOG_ERR, "ruptimed: fork error: %s", strerror(errno));
            exit(1);
        }
        else if (pid == 0)
        {
            // 子进程
            // 设置文件流
            set_cloexec(clfd);
            // 执行服务
            dup2(clfd, STDOUT_FILENO);
            dup2(clfd, STDERR_FILENO);
            close(clfd);
            execl("/usr/bin/uptime", "uptime", (char *)0);
            exit(0);
        }
        else
        {
            // 父进程
            close(clfd);
            waitpid(pid, &status, 0);
        }
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
    // 设置日志
    daemonize("ruptimed");
    // 设置地址信息
    hint.ai_flags = AI_CANONNAME;
    hint.ai_family = 0;
    hint.ai_socktype = SOCK_STREAM;
    hint.ai_protocol = 0;
    hint.ai_addrlen = 0;
    hint.ai_canonname = NULL;
    hint.ai_addr = NULL;
    hint.ai_next = NULL;
    // 获取地址信息
    if ((err = getaddrinfo(host, "ruptime", &hint, &ailist)) != 0)
    {
        syslog(LOG_ERR, "ruptimed: getaddrinfo error: %s", gai_strerror(err));
        exit(1);
    }
    // 遍历地址信息
    for (aip = ailist; aip != NULL; aip = aip->ai_next)
    {
        // 初始化服务器
        if ((sockfd = initserver(SOCK_STREAM, aip->ai_addr, aip->ai_addrlen, QLEN)) >= 0)
        {
            // 执行服务
            serve(sockfd);
            exit(0);
        }
    }
    exit(1);
}
