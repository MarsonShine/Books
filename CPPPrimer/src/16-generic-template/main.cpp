#include <iostream>
#include <vector>
#include "template.h"

int main()
{
    std::cout << compare(1, 0) << std::endl;
    
    std::vector<int> vec1{1, 2, 3};
    std::vector<int> vec2{4, 5, 6};
    std::cout << compare(vec1, vec2) << std::endl;

    compare("hi", "marsonshine"); // 会调用字符串长度+1（终结符）
}