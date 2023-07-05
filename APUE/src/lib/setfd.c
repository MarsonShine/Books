#include "apue.h"
#include <fcntl.h>

// 将文件描述符fd设置为close-on-exec（COE）标志。COE标志的作用是在调用exec函数时自动关闭文件描述符，以避免子进程继承父进程的文件描述符，从而提高安全性和效率。
int
set_cloexec(int fd)
{
	int		val;

	if ((val = fcntl(fd, F_GETFD, 0)) < 0)
		return(-1);

	val |= FD_CLOEXEC;		/* enable close-on-exec */

	return(fcntl(fd, F_SETFD, val));
}
