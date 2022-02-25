// 编写单词技术器
#include <iostream>
#include <map>
#include <string>
#include <algorithm>
#include <cctype>

using std::string;
using std::cin;
using std::cout;
using std::remove_if;
using Map = std::map<std::string, std::size_t>;

auto count() -> Map
{
    Map counts;
    for (string w; cin >> w; ++counts[w]);
    return counts;
}

int main()
{
    
}