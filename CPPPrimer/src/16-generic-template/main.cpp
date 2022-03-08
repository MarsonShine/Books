#include <iostream>
#include <vector>
#include <map>
#include "template.h"

// 类模板外定义
template <typename T>
BlobPtr<T> BlobPtr<T>::operator++(int)
{
    BlobPtr ret = *this; // 等价于
    BlobPtr<T> ret2 = *this;
    ++*this;
    return ret;
}

int main()
{
    std::cout << compare(1, 0) << std::endl;
    
    std::vector<int> vec1{1, 2, 3};
    std::vector<int> vec2{4, 5, 6};
    std::cout << compare(vec1, vec2) << std::endl;

    compare("hi", "marsonshine"); // 会调用字符串长度+1（终结符）

    Blob<std::string> articles = {"a", "an", "the"};

    //// 模板类型别名
    typedef Blob<std::string> StrBlob;
    // 无法直接给未知的类型别名，因为模板类本就不是类型。
    twin<int> win_loss;

    partNo<int> pn;
}

