// 字符串的基本操作
#include <iterator>
#include <iostream>
#include <string>
#include <cstddef>

using std::cout; 
using std::endl; 
using std::string;
// oldValue替换为newValue
auto replace_with(string &s, string const& oldValue, string const& newValue)
{
    for (auto cur = s.begin(); cur <= s.end() - oldValue.size(); ) {
        cout << string{ cur, cur + oldValue.size() } << endl;
        if (oldValue == string{ cur, cur + oldValue.size() })
            cur = s.erase(cur, cur + oldValue.size()),
            cur = s.insert(cur, newValue.begin(), newValue.end()),
            cur += newValue.size();
        else
            ++cur;
    }
}

int main()
{
    string s{ "To drive straight thru is a foolish, tho courageous act." };
    replace_with(s, "tho", "though");
    replace_with(s, "thru", "through");
    cout << s << endl;

    return 0;
}