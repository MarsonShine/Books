#include <iostream>
#include <string>
#include <string.h>

int main()
{
    std::cout << strlen("hello""world") << std::endl;
    std::cout << strlen("hello" "world") << std::endl;
    char *concat_string = new char[strlen("hello" "world") + 1]();

    strcat(concat_string, "hello ");
    strcat(concat_string, "world");
    std::cout << concat_string << std::endl;
    delete [] concat_string;

    // std::string
    std::string str1{ "hello " }, str2{ "world" };
    std::cout << str1 + str2 << std::endl;
}