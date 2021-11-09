#include<iostream>

std::string global_str;
int global_int;
int main()
{
    int local_int;
    std::string local_str;

    std::cout << "local_int = " << local_int << "global_int = " << global_int << "local_str = " << local_str << "global_str = " << global_str << std::endl;
    return 0;
}