// add2.c
#include "apue.h"

int main(void)
{
    int n,int1,int2;
    char line[MAXLINE];

    while ((n = read(STDIN_FILENO, line, MAXLINE)) > 0) { // 从标准输入读取数据
        line[n] = 0;
        if (sscanf(line, "%d%d", &int1, &int2) == 2) { // 从标准输入读取数据
            sprintf(line, "%d\n", int1 + int2); // 格式化数据
            n = strlen(line);
            if (write(STDOUT_FILENO, line, n) != n) { // 输出数据
                err_sys("write error");
            }
        } else {
            if (write(STDOUT_FILENO, "invalid args\n", 13) != 13) { // 输出数据
                err_sys("write error");
            }
        }
    }
    exit(0);
}

// 编译成可执行文件 add2