/*
创建消息队列和 UNIX 域套接字，并为每个消息队列开启了一个线程。然后在一个无限循环中用 poll 来轮询选择一个套接字端点。
当某个套接字可用时，程序可以从套接字中读取数据并把消息打印在标准输出上。
*/

#include "apue.h"
#include <poll.h>
#include <pthread.h>
#include <sys/msg.h>
#include <sys/socket.h>

#define NQ 3 // 队列数量
#define MAXMSZ 512 // 消息最大尺寸
#define KEY 0x123 // 消息队列键

struct threadinfo {
    int qid;
    int fd;
};

struct mymesg {
    long mtype;
    char mtext[MAXMSZ];
};

void *helper(void *arg)
{
    int n;
    struct mymesg m;
    struct threadinfo *tip = arg;

    for (;;) {
        memset(&m, 0, sizeof(m));
        if ((n = msgrcv(tip->qid, &m, MAXMSZ, 0, MSG_NOERROR)) < 0) {
            err_sys("msgrcv error");
        }
        if (write(tip->fd, m.mtext, n) < 0) {
            err_sys("write error");
        }
    }
}

int main()
{
    int i, n, err;
    int fd[2];
    int qid[NQ];
    struct pollfd pfd[NQ];
    struct threadinfo ti[NQ];
    pthread_t tid[NQ];
    char buf[MAXMSZ];

    // 创建队列
    for (i = 0; i < NQ; i++) {
        if ((qid[i] = msgget((KEY + i), IPC_CREAT | 0666)) < 0) {
            err_sys("msgget error");
        }
        printf("queue ID %d is %d\n", i, qid[i]);

        // 创建 UNIX 域套接字
        if (socketpair(AF_UNIX, SOCK_DGRAM, 0, fd) < 0) {
            err_sys("socketpair error");
        }
        pfd[i].fd = fd[0]; // 读端
        pfd[i].events = POLLIN; // 可读事件

        ti[i].qid = qid[i];
        ti[i].fd = fd[1]; // 写端
        if ((err = pthread_create(&tid[i], NULL, helper, &ti[i])) != 0) {
            err_exit(err, "pthread_create error");
        }
    }

    for (;;) {
        if (poll(pfd, NQ, -1) < 0) {
            err_sys("poll error");
        }
        for (i = 0; i < NQ; i++) {
            if (pfd[i].revents & POLLIN) {
                if ((n = read(pfd[i].fd, buf, sizeof(buf))) < 0) {
                    err_sys("read error");
                }
                buf[n] = 0;
                printf("queue id %d, message %s\n", qid[i], buf);
            }
        }
    }
    exit(0);
}