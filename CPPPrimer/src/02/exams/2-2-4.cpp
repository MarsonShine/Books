#include<iostream>

int reused = 42;
int main()
{
    // 一般作用域
    int sum = 0;
    for (int val = 0; val <= 10; ++val)
    {
        sum += val;
    }

    std::cout << "Sum of 1 to 10 inclusive is "
              << sum << std::endl;

    // 嵌套作用域
    int unique = 0;
    std::cout << reused << " " << unique << std::endl;
    int reused = 0; // 新建一个局部作用域变量，这会覆盖同名全局作用域变量
    std::cout << reused << " " << unique << std::endl; // 输出0
    // 如果想引用全局作用域,前缀添加::
    std::cout << ::reused << " " << unique << std::endl;

    int i = 0, sum2 = 0;
    for (int i = 0; i != 10; i++)
        sum2 += i;  // 此处的 i 是内层作用域的 i
    std::cout << i << " " << sum2 << std::endl; // 这行作用域与外围同级，所以引用的是外围的 i
    
    return 0;
}