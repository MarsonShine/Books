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
    for (size_t pos = 0; pos <= s.size() - oldValue.size(); ) {
        if (s[pos] == oldValue[0] && s.substr(pos, oldValue.size()) == oldValue)
        {
            s.replace(pos, oldValue.size(), newValue);
            pos += newValue.size();
        }
        else
            ++pos;
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