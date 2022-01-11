#include<string>
using std::string;

// typedef 类型别名
typedef string ArrT[10];
ArrT& func1(ArrT& arr);

// 尾置返回类型
auto func2(ArrT& arr) -> string(&)[10];

// decltype
string arrS[10];
decltype(arrS) *func3(ArrT& arr);

// 修改 arrPtr 函数，使其返回数组的引用
int odd[] = {1, 3, 5, 7, 9};
int even[] = {0, 2, 4, 6, 8};

decltype(odd)& arrPtr(int i) 
{
    return (i % 2) ? odd : even;
}