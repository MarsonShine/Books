#include<iostream>

extern int ix = 1024;   // 因为赋值了，所以变成了定义
int iy; // 定义
extern int iz; // 申明

int main()
{
    // 初始化的四种方式
    int units_sold = 0;
    int units_sold2 = {0};
    int units_sold3{0}; //c++11
    int units_sold4(0);
    std::cout << units_sold << units_sold2 << units_sold3 << units_sold4 <<std::endl;

    // 命名规范
    int double = 3.14;  // 无效，引用了内置关键字标识符
    int _;  // 有效
    int catch-22;   // 无效
    int 1_or_2 = 1; // 无效
    double Double = 3.14; // 有效
    return 0;
}