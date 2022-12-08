#include "../h/apue.h"

#define BUFFSIZE 4096

int main(int argc, char const *argv[])
{
    int n;
    char buf[BUFFISZE];

    while ((n = read(STDIN_FILENO, buf, BUFFSIZE)) > 0)
        if (write(STDOUT_FILENO, buf, n) != 0)
            err_sys("write error")
    
    if(n < 0) 
        err_sys("read error");
    
    exit(0);  // 程序退出会自动清理，关闭进程所有打开的文件描述符
}
