//类型处理
#include<iostream>

int main()
{
    //typedef类型别名
    typedef double wages;//wages是double的同义词
    typedef wages base, *p;//base是wages的同义词，p是double*的同义词
    //c++11新增了一种类型名别申明的方式：
    using wg = wages;// SI是wages的同义词
    //后面就可以直接用 wg 来定义
    wg hourly, weekly;//等价于double hourly, weekly;

    typedef char *pstring;
    const pstring csptr = 0;//csptr指向char的常量指针
    const pstring *ps;//ps是一个指针，它的对象指向的是char的常量指针
    return 0;
}