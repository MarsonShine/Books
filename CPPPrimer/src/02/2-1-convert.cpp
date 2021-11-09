// 类型转换
#include<iostream>

int main(){
    unsigned char c = -1;
    // signed char c2 = 256;
    printf("%d\n",c);   //255

    int i = 42;
    if (i)  // 自动转换为bool
    {
        i = 0;
    }
    
    // 含有无符号类型的表达式
    unsigned u = 10;
    i = -42;
    std::cout << i + i << std::endl; //-84
    std::cout << u + i << std::endl;
    return 0;
}