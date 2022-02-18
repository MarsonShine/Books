#include <string>
#include <iostream>

struct Data
{
    int ival;
    std::string s;
};

int main()
{
    std::cout << std::boolalpha;
    std::cout << std::is_literal_type<int>::value << std::endl;
    std::cout << std::is_literal_type<Data>::value << std::endl; // string 不是字面量，所以Data不是字面量类型
    return 0;
}
