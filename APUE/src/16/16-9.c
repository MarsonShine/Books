#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <netdb.h>
#include <arpa/inet.h>

int main(int argc, char *argv[]) {
    if (argc != 3) {
        fprintf(stderr, "Usage: %s <hostname> <service>\n", argv[0]);
        exit(EXIT_FAILURE);
    }

    struct addrinfo hints, *result, *rp;
    int sfd, s, port;
    char ipstr[INET6_ADDRSTRLEN];

    // 初始化 addrinfo 结构体
    memset(&hints, 0, sizeof(struct addrinfo));
    hints.ai_family = AF_UNSPEC;    /* 允许 IPv4 或 IPv6 */
    hints.ai_socktype = SOCK_STREAM; /* 流式套接字 */
    hints.ai_flags = 0;
    hints.ai_protocol = 0;          /* 任意协议 */

    // 获取地址信息
    s = getaddrinfo(argv[1], argv[2], &hints, &result);
    if (s != 0) {
        fprintf(stderr, "getaddrinfo: %s\n", gai_strerror(s));
        exit(EXIT_FAILURE);
    }

    // 遍历地址信息链表
    for (rp = result; rp != NULL; rp = rp->ai_next) {
        void *addr;
        char *ipver;

        // 获取指向地址的指针
        if (rp->ai_family == AF_INET) { /* IPv4 */
            struct sockaddr_in *ipv4 = (struct sockaddr_in *)rp->ai_addr;
            addr = &(ipv4->sin_addr);
            ipver = "IPv4";
            port = ntohs(ipv4->sin_port);
        } else { /* IPv6 */
            struct sockaddr_in6 *ipv6 = (struct sockaddr_in6 *)rp->ai_addr;
            addr = &(ipv6->sin6_addr);
            ipver = "IPv6";
            port = ntohs(ipv6->sin6_port);
        }

        // 将地址转换为字符串并打印
        inet_ntop(rp->ai_family, addr, ipstr, sizeof(ipstr));
        printf("%s: %s:%d\n", ipver, ipstr, port);
    }

    freeaddrinfo(result); /* 释放地址信息链表 */
    exit(EXIT_SUCCESS);
}