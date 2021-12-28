#include<iostream>
using std::string;
using std::cout;
using std::cin;

// 验证数字是否偶数
bool even(int num) {
    return num % 2 == 0;
}

int main()
{
    int n;
    cin >> n;
    auto isEven = even(n);
    cout << n << "是" << (isEven ? "偶数" : "奇数") << std::endl;
    return 0;
}
