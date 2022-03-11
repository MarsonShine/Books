#include <iostream>
#include <vector>
#include <cstring>

template<typename T>
std::size_t count(std::vector<T> const& vec, T value)
{
    auto count = 0;
    for (auto const& item : vec)
        if (value == item) ++count;
    return count;    
}

// 模板特例话,当参数是const char*类型的时候只会匹配到这个函数，而不是上面的模板方法
template<>
std::size_t count(std::vector<const char*> const& vec, const char* value)
{
    auto count = 0u;
    for (auto const &item : vec)
    {
        if (0 == strcmp(value, item)) ++count;
    }
    return count;
}