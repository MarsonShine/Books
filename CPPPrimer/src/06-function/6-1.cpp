#include<stdio.h>
#include<iostream>
// 局部静态变量
// 其生命周期是跟随程序的终止而结束（销毁）
size_t count_calls() {
    static size_t ctr = 0;
    return ++ctr;
}

size_t count_calls2() {
    size_t ctr = 0;
    return ++ctr;
}

// 函数申明
// 与函数定义不同，函数申明只需要定义名称和签名，无需函数体。
// 一般是在头文件.h中函数申明，这样能确保同一函数名一致。
// 详见 ./head/example.h

// 分离式编译（separate compilation），允许把程序分割到几个文件中去，每个文件独立编译。
// CC factMain.cc fact.cc # generates factMain.exe or a.out
// CC factMain.cc fact.cc -o main # generates main or main.exe
// 然后编译器负责把对象文件链接到一起形成可执行文件。
// 完整编译执行过程
// CC -c factMain.cc # generates factMain.o
// CC -c fact.cc # generates fact.o
// CC factMain.o fact.o # generates factMain.exe or a.out
// CC factMain.o fact.o -o main # generates main or main.exe

int main()
{
    for (size_t i = 0; i != 10; ++i)
    {
        std::cout << count_calls() << std::endl;
    }

    std::cout << "局部变量" << std::endl;
    for (size_t i = 0; i != 10; ++i)
    {
        std::cout << count_calls2() << std::endl;
    }
    
    
    return 0;
}