#include<iostream>
#include<string>
#include<vector>
using std::begin; using std::end; using std::cout; using std::endl; using std::vector;

int main() {
    const char ca[] = {'h', 'e', 'l', 'l', 'o'};
    const char *cp = ca; // 指针指向数组第一个位置
    while (*cp)
    {
        cout << *cp << endl;
        ++cp;
    }
    
    return 0;
}