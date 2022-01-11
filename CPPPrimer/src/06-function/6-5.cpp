// 函数内联，常数表达式
// 常量表达式有且只能有一个 return
constexpr int new_sz() { return 42;}
constexpr int foo = new_sz();   // constexpr 是隐式指定为内联函数
// 也可以是 return 表达式
constexpr size_t scale(size_t cnt) { return new_sz() * cnt;}