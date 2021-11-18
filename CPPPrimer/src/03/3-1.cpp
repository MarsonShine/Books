#include<iostream>
#include<string>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;
int main()
{
    int i = 0;
    cout << i << endl;

    // 读取一行
    string line;
    while (getline(cin,line))
    {
        cout << line << endl;
    }
    
    // 非空
    while (getline(cin,line))
    {
        if (!line.empty())
            cout << line << endl;
    }
    // 取字符串的长度
    if (line.size() > 80)  // size()返回的是size_t类型，是一个无符号数，切记不要将它与有符号数混用。因为负数值会自动转成无符号数，这时值已经变了。
        cout << "line value length more than 80" << endl;
    return 0;
}