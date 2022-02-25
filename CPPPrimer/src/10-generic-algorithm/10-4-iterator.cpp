#include <string>
#include <iostream>
#include <vector>
#include <algorithm>

int main()
{
    std::string s("FIRST,MIDDLE,LAST");
    // 取第一个单词
    auto comma = std::find(s.cbegin(), s.cend(), ',');
    std::cout << std::string(s.cbegin(), comma) << std::endl;
    // 取最后一个单词
    auto rcomma = std::find(s.crbegin(), s.crend(), ',');
    std::cout << std::string(s.crbegin(), rcomma) << std::endl; // 输出TSAL，不是预期的结果，因为反序输出了LAST
    // 正确写法
    std::cout << std::string(rcomma.base(), s.cend()) << std::endl;

}