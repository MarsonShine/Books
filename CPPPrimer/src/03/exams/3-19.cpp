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
    vector<int> v(10,42);
    vector<int> v2{42,42,42,42,42,42,42,42,42,42};
    vector<int> v3;
    for (size_t i = 0; i < 10; i++)
    {
        v3.push_back(42);
    }
    
    return 0;
}