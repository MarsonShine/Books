#include<iostream>
#include<string>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;

void handleEveryCharInString() {
    string s("Hello World !!!");
    decltype(s.size()) punct_cnt = 0;
    for (auto c : s)
    {
        if (ispunct(c)) // 判断是否是标点符号
        {
            ++punct_cnt;
        }    
    }
    cout << punct_cnt << "punctuation characters in " << s << endl;
    
    // 修改s中的元素,需要将循环变量设置成引用类型
    for (auto &c : s)
    {
        c = toupper(c);
    }
    cout << s << endl;

    // 如何处理部分字符串
    if (!s.empty())
        cout << "第一个字符: " << s[0] << "最后一个字符: " << s[s.size() - 1] << endl;
    // 方法2，迭代
    
}

// 将0-15数字转换成十六进制
void convertHex() {
    const string hexdigits = "0123456789ABCDEF";
    cout << "Enter a series of numbers between 0 and 15"
        << " separated by spaces. Hit ENTER when finished: "
        << endl;

    string result;  // 保存十六进制的结果
    // size_type 是无符号类型
    string::size_type n;    // 保存从输入流读取的数
    while (cin >> n)
    {
        if (n < hexdigits.size())
            result += hexdigits[n];
    }
    cout << "Your hex number is: " << result << endl;
}

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
    // string s5 = "hello," + ", "; // 两个运算对象都不是string对象，都是字面值

    // 处理每个字符，如判断字符串中有多少个感叹号
    handleEveryCharInString();
    return 0;
}