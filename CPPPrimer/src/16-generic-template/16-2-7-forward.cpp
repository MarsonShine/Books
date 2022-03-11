#include <iostream>
#include <utility>

template <typename F, typename T1, typename T2>
void flip(F f, T1 t1, T2 t2)
{
    f(t1, t2);
}

void f(int v1, int& v2) {
    std::cout << v1 << " " << ++v2 << std::endl;
}

template <typename F, typename T1, typename T2>
void flip2(F f, T1 &&t1, T2&& t2) {
    f(t1, t2);
}

void g(int &&v1, int& v2) {
    std::cout << v1 << " " << ++v2 << std::endl;
}

template <typename F, typename T1, typename T2>
void flip3(F f, T1 &&t1, T2&& t2) {
    f(std::forward<T2>(t2), std::forward<T1>(t1));
}

int main()
{
    int i = 100;
    std::cout << i << std::endl;
    f(42, i);
    std::cout << " after " << i << std::endl;

    int j = 100;
    std::cout << j << std::endl;
    flip(f, 41, j);
    std::cout << " after " << j << std::endl;

    int k = 100;
    std::cout << k << std::endl;
    flip2(f, 41, k);
    std::cout << " after " << k << std::endl;

    // flip2(g, k, 42); // 报错 cannot bind rvalue reference of type 'int&&' to lvalue of type 'int'
    flip3(g, k, 42);
}