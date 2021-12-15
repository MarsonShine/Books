#include<string>
#include<iostream>
using std::string;
using std::cout;
using std::cin;

int main()
{
    string s("some string");
    if (s.begin() != s.end())
    {
        auto it = s.begin(); 
        *it = toupper(*it);
    }
    
    // 将迭代器从一个元素移动另一个元素
    // 利用迭代器将字符串转换为大写
    for (auto it = s.begin(); it!=s.end() && !isspace(*it); ++it)
    {
        *it = toupper(*it);
    }
    cout << s << std::endl;
    return 0;
}