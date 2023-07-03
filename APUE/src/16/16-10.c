// 支持重试的 connect
#include "apue.h"
#include <sys/socket.h>

#define MAXSLEEP 128

int connect_retry(int domain, const struct sockaddr *addr, socklen_t alen)
{
    int numsec, fd;

    // Try to connect with exponential backoff. 指数退避算法
    for (numsec = 1; numsec <= MAXSLEEP; numsec <<= 1)
    {
        if (connect(fd, addr, alen) == 0)
        {
            // Connection accepted.
            return (fd);
        }
        close(fd);

        // Delay before trying again.
        if (numsec <= MAXSLEEP / 2)
            sleep(numsec);
    }
    return (-1);
}