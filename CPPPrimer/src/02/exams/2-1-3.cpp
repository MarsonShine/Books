#include<iostream>

int main() {
    std::cout << "'a' = " << 'a' << std::endl;
    std::cout << "L'a' = " << L'a' << std::endl;
    std::cout << "\"a\" = " << "a" << std::endl;
    std::cout << "L\"a\" = " << L"a" << std::endl;

    std::cout << "Who goes with F\145rgus?\012" << std::endl;   // Who goes with Fergus?
    return 0;
}