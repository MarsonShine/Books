#include <map>
#include <functional>
#include <string>
#include <iostream>

int add(int i, int j) { return i + j; }
std::string add(std::string a, std::string b) { return a + b; }
// lambda
auto mod = [](int i, int j){ return i % j; };

struct divide
{
    int operator()(int i, int j) { return i / j; }
};

int main()
{
    auto f = (int(*)(int, int)){}; // 函数指针
    std::map<std::string, int (*)(int, int)> binOps;
    binOps.insert({"+", add});
    binOps.insert({"%", mod});
    binOps.insert({"/", divide()}); // divide 不是函数指针


    // functional
    std::function<int(int, int)> f1 = add;
    std::function<int(int, int)> f2 = mod;
    std::function<int(int, int)> f3 = divide();
    std::function<int(int, int)> f4 = [](int i, int j) -> int { return i * j; };

    std::cout << f1(5, 5) << std::endl;
    std::cout << f2(10, 2) << std::endl;
    std::cout << f3(2, 5) << std::endl;
    std::cout << f4(3, 3) << std::endl;

    // 上述map可以改成function
    std::map<std::string, std::function<int(int, int)>> binOps2;
    binOps2.insert({"+", add});
    binOps2.insert({"/", divide()});
    binOps2.insert({"*", [](int i, int j) { return i * j; }});

    // 调用
    binOps2["+"](2,3);
    binOps2["/"](4,5);
    binOps2["*"](6,7);

    // 二义性，方法重载
    std::function<int(int, int)> f1 = add; // error，具体指哪个add函数？
    // 函数名字不行，那就用函数指针
    int (*fadd)(int, int) = add; // 将整数类型参数的add函数地址赋值给fadd
    binOps2.insert({"+", fadd});
    // 也可以直接通过lambda，直接显式通过参数，返回类型决定函数
    std::map<std::string, std::function<std::string(std::string, std::string)>> binOps3;
    binOps3.insert({"+", [](std::string a, std::string b) { return add(a, b); }});
}