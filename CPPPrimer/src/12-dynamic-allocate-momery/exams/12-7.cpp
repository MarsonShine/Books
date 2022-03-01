#include <iostream>
#include <vector>

using std::vector;

std::ostream& print(std::shared_ptr<vector<int>> ptr) {
    for (auto &&i : *ptr)
    {
        std::cout << i << " ";
    }
    return std::cout;
}

int main()
{
    auto dynamicObj = std::make_shared<vector<int>>();
    for (int i; std::cout << "请输入数字：\n", std::cin >> i; dynamicObj->push_back(i));
    print(dynamicObj) << std::endl;

    return 0;
}