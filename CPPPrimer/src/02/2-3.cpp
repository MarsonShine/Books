#include<iostream>
#include<cstdlib>

int main()
{
    int iVal = 1024;
    int &refVal = iVal; //变量refVal指向iVal的空间地址，即都是同一个对象。

    int &refVal4 = 10;  //引用不能直接引用一个数字，而是要引用一个对象。
    double dval = 1.34;
    int &refVal5 = dval;//引用类型不正确，refValue5复制必须是一个int类型。
    return 0;
}

void pointer()
{
    int iVal = 24;
    int *p = &iVal; //指针p存储的是指向iVal的地址，或者说是 p 指向ival的指针
    std::cout << *p << std::endl;
};

void operate() 
{
    int i = 42;
    int &r = i; //与i是同一个对象，取名为r
    int *p;// 申明一个空指针
    p = &i;// 取址运算符，取i的地址复制给指针p
    *p = i;// 解引用符
    int &r2 = *p;//申明r2，初始化值为指针p的值（解引用符）
}

void emptyPoint()
{
    int *p1 = nullptr;//c++11,推荐写法
    int *p2 = 0;
    int *p3 = NULL;
}

void voidPoint()
{
    double obj = 3.14, *pd = &obj;
    void *pv = &obj;// obj可以是任意对象
    pv = pd;//pv可以存放任意类型的指针
}