
#include<string>
#include<iostream>
using std::string;

// 尽量使用常量引用
string::size_type find_char(string &s, char c, string::size_type &occurs);  // 这样第一个形参要改成 const string &s

bool is_sentence(const string &s)
{
    string::size_type ctr = 0;
    return find_char(s, '.', ctr) == s.size() - 1 && ctr == 1;
}

int main()
{
    string::size_type ctr;
    find_char("Hello World",'o', ctr);
    return 0;
}

// 数组形参都是以指针的形式传递的。
// 以下三种数组的传参方式都是一样的
void print(const int*);
void print(const int[]);
void print(const int[10]);

// 数组引用形参
// 要区分数组的引用和引用的数组
void print(int (&arr)[10]) {    // 形参是数组的引用
    for (auto elem : arr)
        std::cout << elem << std::endl;
}

// 引用的数组，编译器提示错误：不允许使用引用的数组
void print(int &arr[10]) {

}