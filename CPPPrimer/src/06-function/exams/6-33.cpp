// 递归
#include<iostream>
#include<string>
#include<vector>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;
using std::vector;
using Iter = vector<string>::const_iterator;

void printVectorRecurison(Iter begin,Iter end) {
    if (begin != end)
    {
        cout << *begin << " ";
        printVectorRecurison(++begin,end);
    }
    
}

void printVector(vector<string> list)
{
    auto iter1 = list.begin();
    auto iter2 = list.end();
    printVectorRecurison(iter1,iter2);
}

int main()
{
    printVector(vector<string>{"marsonshine", "summerzhu", "and", "our", "daughter"});
}