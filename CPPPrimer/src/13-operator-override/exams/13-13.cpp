#include <iostream>
#include <vector>

struct X
{
    X() {std::cout << "X()" << std::endl; }
    X(const X&) { std::cout << "X(const X&)" << std::endl; }
    X& operator=(const X&) { std::cout << "X& operator=(const X&)" << std::endl; return *this; }
    ~X() {std::cout << "~X()" << std::endl; }
};

void f(const X &rx, X x) // X x 一次初始化 ～ 一次析构
{
    std::vector<X> vec;
    vec.reserve(2);
    vec.push_back(rx); // 初始化 ～ 析构
    vec.push_back(x); // 初始化 ～ 析构
}

int main() {
    X *x = new X;
    f(*x, *x); // f 函数结束，x rx x 析构
    delete x; // *x 析构

    return 0;
}
