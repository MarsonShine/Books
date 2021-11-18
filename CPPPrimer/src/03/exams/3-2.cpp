#include<iostream>
#include<string>

int main ()
{
    std::string line;
    std::getline(std::cin,line);
    std::cout << "length = " << line.size() << line << std::endl;
    return 0;
}