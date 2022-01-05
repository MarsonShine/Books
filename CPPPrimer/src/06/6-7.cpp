#include<string>
using std::string;
// 函数指针
bool lengthCompare(const string &, const string &);

// 函数指针
bool(*pf)(const string &, const string &);

// 函数指针当参数传递
void useBigger(const string &s1, const string &s2, bool (*pf)(const string &, const string &));
// 等价上面的申明
void useBigger(const string &s1, const string &s2, bool pf(const string &, const string &));

// 返回类型为函数指针
using F = int(int*, int);   // F 是函数类型
using PF = int(*)(int*, int);   // PF 是指针类型
// 申明
PF f1(int);
F f2(int);  // F 是函数类型，f2 不能返回一个函数，只能返回函数的指针
F *f1(int);
// F *f1(int) 等价于
int (*f1(int))(int*, int);  // 由内向外阅读理解：f1有形参列表，所以f1是函数，f1前面有*，所以f1返回一个指针；然后指针的类型也有形参列表，所以这个指针是个函数指针，该函数返回类型是int
// 等价
auto f1(int) -> int(*)(int*, int);

int main()
{
    pf = lengthCompare; // 定义
    pf = &lengthCompare; // 同上

    // 调用
    bool b1 = pf("hello", "goodbye");   // 调用 lengthCompare 函数
    bool b2 = (*pf)("hello", "goodbye"); // 同上
    bool b3 = lengthCompare("hello", "goodbye"); // 等价调用

}