#include <iostream>

int main() {
    int startValue = 50, sum = 0;
    while (startValue ++ < 100)
    {
        sum += startValue;
    }
    std::cout << "50到100的整数和为：" << sum << std::endl;
    return 0;
}