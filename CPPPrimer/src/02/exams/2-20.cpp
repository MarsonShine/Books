#include<iostream>

int main()
{
    int i = 42;//
    int *p1 = &i;//定义p1指针，并赋值指向i的地址
    *p1 = *p1 * *p1;//修改p1指针指向的地址的值（指针的指针），将值修改为42*42
}