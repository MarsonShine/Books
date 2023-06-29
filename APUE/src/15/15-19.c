// 通过标准I/O来实现协同进程
#include "apue.h"

int main(void)
{
    int int1,int2;
    char line[MAXLINE];

    while (fgets(line, MAXLINE, stdin) != NULL) { // fgets 引起标准I/O库分配一个缓冲区
        if (sscanf(line, "%d%d", &int1, &int2) == 2) { // 从标准输入读取数据
            if (printf("%d\n", int1 + int2) == EOF) { // 输出数据
                err_sys("printf error");
            }
        } else {
            if (printf("invalid args\n") == EOF) { // 输出数据
                err_sys("printf error");
            }
        }
    }
}