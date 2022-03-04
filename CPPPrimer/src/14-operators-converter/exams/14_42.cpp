#include <functional>
#include <vector>
#include <algorithm>
#include <string>

// 使用标准库对对象及适配器定义一条表达式，让
// 1 统计大于1024的值有多少个
// 2 找到第一个不等于pooh的字符串
// 3 将所有值乘以2

int main()
{
    std::vector<std::string> ivec {};
    std::count_if(ivec.cbegin(), ivec.cend(), std::bind(std::greater<int>(),1024));
    std::find_if(ivec.cbegin(), ivec.cend(), std::bind(std::not_equal_to<std::string>(), "pooh"));
    std::transform(ivec.begin(), ivec.end(), ivec.begin(), std::bind(std::multiplies<int>(), 2));
}