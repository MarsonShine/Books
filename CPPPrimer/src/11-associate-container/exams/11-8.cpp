#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
using std::string; using std::vector; using std::cin; using std::cout;

int main()
{
    vector<string> words = { "aa" , "bb", "cc"};
    string w;
    cin >> w;
    if (std::find(words.begin(),words.end(), w) != words.cend())
    {
        cout << w << " 已存在";
    }
}