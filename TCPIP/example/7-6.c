// 将程序后台服务化，以下是 deamon 的实现方式
#include<stdbool.h>

bool daemonize()
{
    /*创建子进程，关闭父进程，这样可以使程序在后台运行*/
    pid_t pid = fork();
    if(pid < 0) {
        return false;
    }else if(pid > 0) {
        exit(0)
    }

    /*设置文件权限掩码。当进程创建新文件（使用open(const char*pathname,int flags,mode_t mode)系统调用）时，文件的权限将是 mode＆0777*/
    umask(0);
    /*创建新的会话，设置本进程为进程组的首领*/
    pid_t sid = setsid();
    if(sid < 0)
    {
        return false;
    }
    // 切换工作目录
    if((chdir("/")) < 0)
    {
        return false;
    }
    /*关闭标准输入、输出、错误设备*/
    close(STDIN_FILENO);
    close(STDOUT_FILENO);
    close(STDERR_FILENO);
    /*关闭其它已经打开的文件描述符，代码省略*/
    /*将标准输入、输出、错误都输出到指定的文件路径*/
    open("/dev/null",O_RDONLY);
    open("/dev/null",O_RDWR);
    open("/dev/null",O_RDWR);
    return true;
}