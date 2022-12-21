#include "apue.h"
#include <fcntl.h>

// 下面函数会报错
// int main(int argc, char const *argv[])
// {
//     int i, fd;
//     struct stat statbuf;
//     struct timespec times[2];
//     for (i = 1; i < argc; i++)
//     {
//         if (stat(argv[i], &statbuf) < 0)
//         {
//             /* 追踪当前时间 */
//             err_ret("%s: stat error", argv[i]);
//             continue;
//         }
//         if ((fd == open(argv[i], O_RDWR | O_TRUNC)) < 0)
//         {
//             /* 截断 */
//             err_ret("%s: open error", argv[i]);
//             continue;
//         }
//         times[0] = statbuf.st_atime; // 访问时间
//         times[1] = statbuf.st_mtime; // 修改时间
//         if (futimens(fd, times) < 0)
//         {
//             // 重置时间
//             err_ret("%s: futimens error", argv[i]);
//         }
//         close(fd);
//     }
//     exit(0);
// }

int
main(int argc, char *argv[])
{
	int				i, fd;
	struct stat		statbuf;
	struct timespec	times[2];

	for (i = 1; i < argc; i++) {
		if (stat(argv[i], &statbuf) < 0) {	/* fetch current times */
			err_ret("%s: stat error", argv[i]);
			continue;
		}
		if ((fd = open(argv[i], O_RDWR | O_TRUNC)) < 0) { /* truncate */
			err_ret("%s: open error", argv[i]);
			continue;
		}
		times[0] = statbuf.st_atime;
		times[1] = statbuf.st_mtime;
		if (futimens(fd, times) < 0)		/* reset times */
			err_ret("%s: futimens error", argv[i]);
		close(fd);
	}
	exit(0);
}