#include <iostream>

int main()
{
    int v1 = 0, v2 = 0;
    std::cout << "The sum of " << v1;
              << " and " << v2;
              << " is " << v1 + v2 <<std::endl
    return 0;
}

// 写法错误，不满足 cout 表达式的格式要求。第二个 << 的左边必须是 ostream
