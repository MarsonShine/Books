#include<iostream>
#include<string>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;

int main()
{
    // 字符串比较
    string str = "Hello";
    string phrase = "Hello World";
    string slang = "Hiya";
    cout << "str < phrase = " << std::boolalpha << (str < phrase) << endl;
    cout << "str < slang = " << std::boolalpha << (str < slang) <<endl;
    cout << "phrase < slang = " << std::boolalpha << (phrase < slang) <<endl;

    string s1 = "hello, ", s2 = "world\n";
    string s3 = s1 + s2;
    // 等价
    s1 += s2;
    string s4 = s1 + ", ";
    string s5 = "hello," + ", "; // 两个运算对象都不是string对象，都是字面值
    return 0;
}