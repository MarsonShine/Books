#include "../h/apue.h"

int main(int argc, char const *argv[])
{
    printf("uid=%d, gid=%d\n",getuid(), getgid());
    return 0;
}
