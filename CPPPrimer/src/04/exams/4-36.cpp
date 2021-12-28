// 类型强转
// cast-name<type>(expression)
// 如：
// static_cast: 任何具有明确定义的类型转换，只要不包含const，都可以用static_cast
// dynamic_cast: 支持运行时类型识别
// const_cast: 只能改变底层的const
// reinterpret_cast
int main()
{
    // 类型强转
    int i = 2;
    double d = 2.23;
    i *= d; // int * double 得出来的结果是 double

    // 类型转换
    i *= static_cast<int>(d);   // 这样就会将d转换为int在运算
    return 0;
}
