#include<iostream>
#include<string>
#include<vector>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;
using std::vector;

void add_vector()
{
    vector<int> v1;
    for (int i = 0; i != 100; i++)
    {
        v1.push_back(i);
    }
    // 从标准输入中读取单词，将其作为vector对象的元素存储
    string word;
    vector<string> text;
    while (cin >> word)
    {
        text.push_back(word);
    }
    
}

void read_vector()
{
    vector<int> vect{ 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    for (auto &i : vect)
    {
        i *= i;
    }
    for (auto i : vect)
    {
        cout << i << " ";
    }
    cout << endl;  
}

int main()
{
    add_vector();
    read_vector();

    vector<int> v{ 1,2,3,4,5,6 };
    if (v.empty()) {
        v.push_back(1);
    }
    if (v.size() == 10) {
        cout << "full" << endl;
    }

    // 以10分为一个分数段统计成绩的数量：0-9，10-19，...,90-99,100
    vector<unsigned> scores(11, 0); // 11个分数段，都初始化为0
    unsigned grade;
    while (cin >> grade)
    {
        if (grade <= 100) 
            ++scores[grade/10]; // 将对应分数段的人数加1
    }
    
    return 0;
}