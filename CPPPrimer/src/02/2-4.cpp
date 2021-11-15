#include<iostream>

extern const int bufSize = 1024;// 通过关键字extern暴露外面，避免在各自的文件夹下申明这个变量

int main()
{
    // const int bufSize = 1024;//申明一个无法更改的变量，（常量）

    // int i = 42;
    // const int &r1 = i;
    // const int &r2 = 42;
    // const int &r3 = r1 * 2;

    // int &r4 = i * i;// 为什么编译器会禁止这种行为？

    double dval = 3.14;
    const int &ri = dval;

    int null = 0, *p = &null;
    return 0;
}