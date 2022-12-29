#include <pwd.h>
#include <stddef.h>
#include <string.h>

// 在使用 getpwent 查看完口令文件后，一定要调用 endpwent 关闭这些文件。
struct passwd *getpwnam(const char *name)
{
    struct passwd *ptr;

    setpwent();

    while ((ptr = getpwent()) != NULL)
        if (strcmp(name, ptr->pw_name) == 0)
            break;
    endpwent();
    return(ptr);
}