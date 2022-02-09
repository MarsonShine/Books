#include <iostream>
#include <string>
using std::cin; using std::cout; using std::endl; using std::string;

struct Sales_data
{
    string bookNo;
    unsigned units_sold = 0;
    double revenue = 0.0;

    // 7-2 添加combie和ibsn成员
    std::string isbn() const { return this->bookNo; }
    Sales_data& combie(const Sales_data&);
};

// 定义成员函数
Sales_data& Sales_data::combie(const Sales_data& rhs)
{
    units_sold += rhs.units_sold;
    revenue += rhs.revenue;
    return *this;
}