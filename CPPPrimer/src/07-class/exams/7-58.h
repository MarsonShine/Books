#include <vector>

class Example
{
private:
    /* data */
public:
    static constexpr double rate = 6.5;
    static const int vecSize = 20;
    // static std::vector<double> vec; // 不能在括号内指定类内初始化式
    static std::vector<double> vec; 
};

constexpr double Example::rate;
std::vector<double> Example::vec(Example::vecSize);

