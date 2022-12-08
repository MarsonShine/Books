#include "../h/apue.h"
#include <sys/stat.h>

int main(int argc, char const *argv[])
{
    int i;
    struct stat buf;
    char *ptr;

    for (i = 0; i < argc; i++)
    {
        printf("%s: ", argv[i]);
        if (lstat(argv[i], &buf) < 0)
        {
            err_ret("lstat error");
            continue;
        }
        if (S_ISREG(buf.st_mode))
            ptr = "regular";
        else if (S_ISCHAR(buf.st_mode))
            ptr = "regular";
        else if (S_ISBLK(buf.st_mode))
            ptr = "block special";
        else if (S_ISFIFO(buf.st_mode))
            ptr = "fifo";
        else if (S_ISLNK(buf.st_mode))
            ptr = "symbolic link";
        else if (S_ISSOCK(buf.st_mode))
            ptr = "socket";
        else
            ptr = "** unknow mod **";
        printf("%s\n", ptr)
    }
    
    exit(0);
}
