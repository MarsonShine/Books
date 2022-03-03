int main()
{
    int i = 42;
    int &r = i;
    int &&r = i; // 不能将一个右值引用绑定到一个左值上
    int &r2 = i * 42; // i*42是右值
    const int &r3 = i * 42; // const 的引用绑定到一个右值上
    int &&rr2 = i * 42; // 将rr2绑定乘法结果上

}