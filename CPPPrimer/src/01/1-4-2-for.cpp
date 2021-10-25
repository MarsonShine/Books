#include <iostream>

int main() {
    int sum = 0;
    for (int val = 0; val <= 10; val++)
    {
        sum+=val;   //等价于 sum = sum + val;
    }
    std::cout << "Sum of 1 to 10 inclusive is " << sum << std::endl;
    return 0;
    
}