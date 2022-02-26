#include <set>
#include <map>
#include <string>
#include <utility>
#include <iostream>
#include "libs/Sales_data.h"
using std::map; using std::set; using std::string; using std::pair;

bool compareIsbn(const Sales_data& a, const Sales_data& b)
{
    return a.isbn() < b.isbn();
}

int main()
{
    // 初始化
    map<string, string> ms = {
        {"Marsonshine", "Shine"},
        {"Summer", "Zhu"},
        {"Happy", "Xi"}
    };
    set<string> s = { "Marsonshine", "Summerzhu", "HappyXi"};
    // 在相同的key下能按照compare保持有序性
    std::multiset<Sales_data, decltype(compareIsbn)*> bookStore(compareIsbn);

    // pair
    pair<string, int> p{"marsonshine", 15};
    std::cout << "第一个参数：first = " << p.first << std::endl;
    std::cout << "第二个参数：second = " << p.second << std::endl;

    set<string>::key_type;
    map<string,std::vector<int>>::key_type;
    map<string,std::vector<int>>::value_type;
    map<string,std::vector<int>>::mapped_type;

    // 基本操作

    // 迭代
    set<int> iset = {1,2,3,4,5,6,7,8,9};
    set<int>::iterator set_it = iset.begin();
    if (set_it != iset.end())
    {
        *set_it = 42; // error: set 类型不允许修改key
        std::cout << *set_it << std::endl;
    }
    
    map<string, int> maps;
    // 遍历
    auto map_it = maps.cbegin();
    while (map_it != maps.cend())
    {
        std::cout << map_it->first << " occurs "
                    << map_it->second << " times " << std::endl;
        ++map_it;
    }
    
}