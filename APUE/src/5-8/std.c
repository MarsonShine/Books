#include "apue.h"
/*
使用 getc 和 putc 将标准输入复制到标准输出
*/

int main(int argc, char const *argv[])
{
    int c;
    while ((c = getc(stdin)) != EOF))
    {
        if (putc(c, stdout) == EOF)
            err_sys("output error");    
    }

    if (ferror(stdin))
        err_sys("input error");
    
    exit(0);
}