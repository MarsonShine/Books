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

    //auto 类型推断
    int i = 0, &r = i;
    auto a = r;
    const int ci = i, &cr = ci;
    auto b = ci;
    auto c = cr;
    auto d = &i;
    auto e = &ci;

    const auto f = ci;
    auto &g = ci;
    auto &h = 42;   // 不能为非常量引用绑定字面值
    const auto &j = 42;

    a = 42; b = 42; c = 42;
    d = 42;
    g = 42;

    // decltype
    const int ci = 0, &cj = ci;
    decltype(ci) x = 0;
    decltype(cj) y = x;
    decltype(cj) z; // 引用必须初始化

    decltype((i)) x;// decltype((x)) 双层括号一定是引用
    return 0;
}