#include <iostream>

int main()
{
    std::cout << "Enter two numbers:" << std::endl; // 输出文字，并换行
    int v1 = 0,v2 = 0;
    std::cin >> v1 >> v2;   // 读取用户输入的数字 v1，v2
    std::cout << "The sum of " << v1 << " and " << v2 << " is "<< v1+v2 <<std::endl;
    return 0;
}

// 编译
// g++ -o iomain iomain.cpp