#include <string>
using std::string;

int main() {
    const char *cp = "Hello World!!! "; // 以空字符串结尾
    char notnull[] = { 'H', 'i'};
    string s1(cp); // 拷贝cp整个内容
    string s2(notnull, 2);  // 拷贝两个字符
    string s3(notnull); // 因为不是以空字符串结尾，所以结果是未定义
    string s4(cp + 6, 5);   // 从第六个位置开始，拷贝5个字符
    string s5(cp, 6, 5);    // 同上
    string s6(cp, 6);   // 从第六个位置开始直到字符串结束
    string s7(s1, 6, 20);   // 从第六个位置开始，拷贝20个字符，因为20已经超过目标字符串长度，所以效果等同于s6
    string s8(s1, 16);  // 超出了s1的最大范围，报out-of-range错误

    // 数据类型转换
    string ss = "pi = 3.14";
    auto d = std::stod(ss.substr(ss.find_first_of("+-.0123456789"))); // string to double
    
    
}