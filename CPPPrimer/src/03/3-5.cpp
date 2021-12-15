#include<string>
#include<iostream>
using std::string;
using std::cout;
using std::cin;

int main()
{
    int ia[] = {1,2,3,4,5,6,7,8,9};
    auto ia2(ia);   // ia2 是一个整形指针，指向ia第零个元素

    // 可以通过自增指针实现迭代器
    int arr[] = {1,2,3,4,5,6,7,8,9};
    int *p = arr;
    ++p;// arr下一个元素

    //begin end 头尾判断
    int ia3[] = {1,2,3,4,5,6,7,8,9};
    int *beg = std::begin(ia3); //指向ia3的首个元素指针
    int *last = std::end(ia3);  //指向ia3的最后一个元素指针
    while (beg != last && *beg >= 0)
    {
        ++beg;
    }
    
    return 0;
}