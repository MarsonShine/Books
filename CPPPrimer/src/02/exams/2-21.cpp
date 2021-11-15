#include<iostream>

int main()
{
    int i = 0;
    double* dp = &i;// int*指针类型的值不能用于初始化double*类型的实体，也就是说指针赋值时类型必须是安全的。
    int *ip = i;//具体的值无法直接赋值给指针对象
    int *p = &i;
}