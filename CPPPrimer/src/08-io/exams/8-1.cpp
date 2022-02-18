// #ifndef _8_1_cpp
// #define _8_1_cpp
#include<iostream>

std::istream& func(std::istream &is) {
    std::string buf;
    while (is >> buf)
    {
        std::cout << is.rdstate() << std::endl;
        std::cout << buf << std::endl;
    }
    is.clear();
    return is;
}

int main() 
{
    func(std::cin);
    return 0;
}