#include "apue.h"
#include <fcntl.h>

int main(int argc, char const *argv[])
{
    if (argc != 2)
        err_quit("usage: a.out <pathname>");
    if (access(argv[1], R_OK) < 0)
        err_quit("access error for %s", argv[1]);
    else 
        printf("read access ok\n");
    if (open(argv[1], O_RDONLY) < 0)
        err_ret("open error for %s", argv[1]);
    else 
        printf("open for reading ok\n");
    
    exit(0);
}
