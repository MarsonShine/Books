#include "template2.h"
#include <vector>
#include <string>
#include <functional>

int main() {
    std::vector<std::string> vc1;
    auto r = template2::fcn(vc1.begin(), vc1.end()); // 返回string&

    auto r2 = template2::fcn2(vc1.begin(), vc1.end()); // 返回string
    // 函数指针
    int (*fcompare)(const int&, const int&) = template2::compare;
    auto e = fcompare(1, 2);

    // 函数返回函数（高阶函数
    auto f2 = [](const std::function<int(int)> &f1) {
        return [=](int param) {
            return f1(param) * param;
        };
    };
    
    auto f3 = [](const std::function<int(int)> &f1) {
        return [=](int param) {
            return [=](int pa) {
                f1(param) * param * pa;
            };
        };
    };

    std::function<int(int)> f = [](int a) { return a * 5; };
}