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

    double *p = new double;
    DebugDelete dd;
    dd(p); // 调用DebugDelete::operator()(double*);
    int *ip = new int;
    DebugDelete()(ip);

    std::unique_ptr<int, DebugDelete> p(new int, DebugDelete()); // 传递一个删除器
    std::unique_ptr<std::string, DebugDelete> sp(new std::string, DebugDelete());

    // 控制实例化，显式申明
    // 为什么要控制实例化？
    // 当引用模板时，就会将其进行实例化。而很多时候并不仅仅只是在单个文件引用到模板。那么在正常情况下，有多个文件中使用到了相同的模板就会有相同的实例，那么这样其实是不必要的，也是有开销的（所谓的C#的范型类型爆炸）
    // 这个时候就可以通过添加关键字extern显示实例化来避免这种开销
    // 当编译器遇到extern关键字时，它不会在本文件中生成实例化代码，而是表明其它文件位置有该实例化的一个非extern申明（定义）
    extern template class vector<std::string>; // 实例化申明，定义必须是在其它的文件中，必须要出现在任何实例化此版本的代码之前
    template class std::vector<Sales_data>; // 实例化定义，编译器将会根据定义生成代码

}

