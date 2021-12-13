#include<iostream>
#include<string>
#include<vector>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;
using std::vector;

int main()
{
    vector<int> v;
    for (int i; cin >> i; v.push_back(i));

    vector<string> v2;
    string s;
    while (cin >> s)
    {
        v2.push_back(s);
    }
    
    
    return 0;
}