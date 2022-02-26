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

auto ignore(string &s) -> string const&
{
    for (auto& c : s)
    {
        c = tolower(c); // 每个字符小写
        s.erase(remove_if(s.begin(), s.end(), ispunct), s.end());
    }
    return s;
}

auto count_ignore() -> Map
{
    Map counts;
    for (string w; cin >> w; ++counts[ignore(w)]);
    return counts;
}

int main()
{
    
}