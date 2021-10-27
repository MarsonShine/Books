#include <iostream>
int main() {
    int v = 10;
    int firstVal = 10, secondVal = 0;
    while (v > -1)
    {
        if (firstVal == v)
        {
            std::cout << "第一个数" << std::endl;
        } else {
            firstVal = secondVal;
            secondVal = v;
        }
        std::cout << "前一个数" << firstVal << " 后一个数" << secondVal << std::endl;
        // std::cout << v << " ";
        v--;
    }
    std::cout << std::endl;
    
    return 0;
}