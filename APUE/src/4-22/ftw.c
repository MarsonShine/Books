#include "apue.h"
#include <dirent.h>
#include <limits.h>

typedef int Myfunc(const char *, const struct stat *, int);

static Myfunc myfunc;
static int myftw(char *, Myfunc *);
static int dopath(Myfunc *);
static long nreg, ndir, nblk, nchr, nfifo, nslink, nsock, ntot;

int main(int argc, char const *argv[])
{
    int ret;
    if (argc != 2)
        err_quit("usage: ftw <stating-pathname>");
    ret = myftw(argv[1], myfunc);

    ntot = nreg + ndir + nblk + nchr + nfifo + nslink + nsock;
    if (ntot == 0)
        ntot = 1; // 避免 0 整除
    printf("regular files   = %7ld, %5.2f %%\n", nreg, nreg * 100.0 / ntot);
    printf("directories     = %7ld, %5.2f %%\n", ndir, ndir * 100.0 / ntot);
    printf("block special   = %7ld, %5.2f %%\n", nblk, nblk * 100.0 / ntot);
    printf("char special    = %7ld, %5.2f %%\n", nchr, nchr * 100.0 / ntot);
    printf("FIFOs           = %7ld, %5.2f %%\n", nfifo, nfifo * 100.0 / ntot);
    printf("symbolic links  = %7ld, %5.2f %%\n", nslink, nslink * 100.0 / ntot);
    printf("sockets         = %7ld, %5.2f %%\n", nsock, nsock * 100.0 / ntot);
}

/**
 * 从 "pathname" 开始一层层降序.
 * caller 的 func 函数每个文件都会调用
 */
#define FTW_F 1   // 文件
#define FTW_D 2   // 目录
#define FTW_DNR 3 // 目录，没有读的权限
#define FTW_NS 4  // file that we can't stat

static char *fullpath; // 包含完整 fullname 的所有文件
static size_t pathlen;

static int myftw(char *pathname, Myfunc *func)
{
    fullpath = path_alloc(&pathlen); // 分配 path_max + 1 个字节
    if (pathlen <= strlen(pathname))
    {
        pathlen = strlen(pathname) * 2;
        if ((fullpath = realloc(fullpath, pathlen)) == NULL)
            err_sys("realloc failed");
    }
    strcpy(fullpath, pathname);
    return (dopath(func));
}

/*
 * 从“fullpath”开始沿着层次结构向下
 * 如果"fullpath"不是目录，则lstat()它，调用func()，并返回。
 * 对于目录，对于目录中的每个名称，我们递归地调用自己。
 */
static int /* we return whatever func() returns */
dopath(Myfunc *func)
{
    struct stat statbuf;
    struct dirent *dirp;
    DIR *dp;
    int ret, n;
    if (lstat(fullpath, &statbuf) < 0)
        return (func(fullpath, &statbuf, FTW_NS));
    if (S_ISDIR(statbuf.st_mode) == 0) /* 不是一个目录 */
        return (func(fullpath, &statbuf, FTW_F));
    
    /*
    * 如果是目录，首先调用 func()
    * 然后处理在文件夹中的所有 filename
    */
    if ((ret == func(fullpath, &statbuf, FTW_D)) != 0) {
        pathlen *=2;
        if ((fullpath == realloc(fullpath, pathlen)) == NULL)
            err_sys("realloc failed");
    }
    fullpath[n++] = '/';
    fullpath[n] = 0;
    if ((dp = opendir(fullpath)) == NULL) /* 无法读该目录 */
        return (func(fullpath, &statbuf, FTW_DNR));
    while ((dirp = readdir(dp)) != NULL)
    {
        if (strcmp(dirp->d_name, ".") == 0 ||
            strcmp(dirp->d_name, "..") == 0)
            continue;   /* 忽略 . .. */
        strcpy(&fullpath[n], dirp->d_name); /* 追加名称 */
        if ((ret = dopath(func)) != 0)
            break;
    }
    fullpath[n-1] = 0;
    if (closedir(dp) < 0)
        err_ret("can't close directory %s", fullpath);
    return ret;
}
static int myfunc(const char *pathname, const struct stat *statptr, int type)
{
    switch (type)
    {
    case FTW_F:
        switch (statptr->st_mode & S_IFMT)
        {
        case S_IFREG:
            nreg++;
            break;
        case S_IFBLK:
            nblk++;
            break;
        case S_IFCHR:
            nchr++;
            break;
        case S_IFIFO:
            nfifo++;
            break;
        case S_IFLNK:
            nslink++;
            break;
        case S_IFSOCK: 
            nsock++
            break;
        case S_IFDIR: /* 目录 type = FTW_D */
            err_dump("for S_IFDIR for %s", pathname);
        }
        break;
    case FTW_D:
        ndir++;
        break;
    case FTW_DNR:
        err_ret("can't read directory %s", pathname);
        break;
    case FTW_NS:
        err_ret("stat error for %s", pathname);
        break;
    default:
        err_dump("unkonwn type %d for pathname %s", type, pathname);
    }
    return(0);
}
