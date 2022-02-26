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

    // 插入map
    auto is = maps.insert(pair<string, int>("marsonshine", 28));
    maps.insert({"summer zhu", 26});
    maps.insert(std::make_pair("happy xi", 1));
    maps.insert(map<string, int>::value_type("family", 4));

    std::multimap<string, int> multip;
    multip.insert({"marsonshine", 28});
    multip.insert({"marsonshine", 29}); // ok

    // 删除
    map<string, int> names = {
        {"marson", 28},
        {"marson", 29},
        {"summer", 27},
        {"marsonshine", 20}
    };
    auto cnt = names.erase("marson");
    assert(cnt == 2);
    auto cnt2 = names.erase("summer");
    assert(cnt2 == 1);

    for (auto &i : names) // 遍历解引用，得到是pair类型
    {
        
    }
    auto n = names[0]; // 下表索引访问，得到的是value_type类型，即int
    
    
}