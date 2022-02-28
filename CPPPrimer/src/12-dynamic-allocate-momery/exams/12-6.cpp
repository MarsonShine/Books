#include <iostream>
#include <vector>

using std::vector;

std::ostream& print(std::vector<int>* ptr) {
    for (auto &&i : *ptr)
    {
        std::cout << i << " ";
    }
    return std::cout;
}

int main()
{
    auto *dynamicObj = new std::vector<int>{ };
    for (int i; std::cout << "请输入数字：\n", std::cin >> i; dynamicObj->push_back(i));
    print(dynamicObj) << std::endl;

    return 0;
}