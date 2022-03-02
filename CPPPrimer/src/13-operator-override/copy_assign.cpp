#include <string>

class Sales_data {
public:
    Sales_data& operator=(const Sales_data&);
private:
//
std::string bookNo;
unsigned units_sold = 0;  //某本书的销量
double revenue = 0.0;   //总销量
};

// 重载赋值运算符
Sales_data& Sales_data::operator=(const Sales_data &sd)
{
    bookNo = sd.bookNo;
    units_sold = sd.units_sold;
    revenue = sd.revenue;
    return *this;
}