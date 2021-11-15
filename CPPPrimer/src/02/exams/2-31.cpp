#include<iostream>

int main()
{
    const int v2 = 0;
    int v1 = v2;
    int *p1 = &v1, &r1 = v1;
    const int *p2 = &v2, *const p3 = &i, &r2 = v2;

    r1 = v2; 
    p1 = p2;// 不能将const int *类型赋值给 int *类型
    p2 = p1;// 可以将int*转成const int*
    p1 = p3;// 不能将const int *类型赋值给 int *类型
    p2 = p3;
}