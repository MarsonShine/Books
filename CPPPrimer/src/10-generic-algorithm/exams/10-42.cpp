#include <iostream>
#include <algorithm>
#include <vector>
#include <list>
#include <string>

// list 去重
int main()
{
    std::list<std::string> ls{"a", "aa", "aa", "aa", "aass", "asssssaa", "aa"};
    ls.sort();
    ls.unique();

    for (auto &e : ls)
    {
        std::cout << e << " ";
    }
    std::cout << std::endl;


    // list 自定义排序
    ls.sort([](const std::string& a, const std::string& b) { return a.size() <= b.size(); });
}