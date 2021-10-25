#include <iostream>

int main() {
    int a,b = 0;
    std::cin >> a >> b;
    int v = a;
    while (v <= b)
    {
        std::cout << v << " ";
        v++;
    }
    
    return 0;
}