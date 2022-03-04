// 函数调用运算符
// 表示absInt可以直接调用函数，参数为一个int
struct absInt
{
    int operator() (int val) const {
        return val < 0 ? -val : val;
    }
};
