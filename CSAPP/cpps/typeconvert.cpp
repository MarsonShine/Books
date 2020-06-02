// 类型转换
#include <stdio.h>

using namespace std;

int main()
{
    short int v = -12345;
    unsigned short uv = (unsigned short) v;
    printf("v = %d, uv = %u\n", v, uv);
}