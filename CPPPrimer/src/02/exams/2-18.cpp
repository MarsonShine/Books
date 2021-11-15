#include<iostream>

int main()
{
    int p = 20, q = 30;
    int *p1 = &p, *p2 = p1;
    // 更改指针的值
    p1 = &q;// p1指向q的地址
    // 更改指针所指的对象的指
    *p1 = 25;

}