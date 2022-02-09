#ifndef _7_06_h
#define _7_06_h

#include <string>
#include <iostream>

struct Sales_data
{
    std::string const& isbn() const { return bookNo; };
    Sales_data& combie(const Sales_data&);

    std::string bookNo;
    unsigned units_sold = 0;
    double revenue = 0.0;
};

// 成员函数
Sales_data& Sales_data::combie(const Sales_data& rhs)
{
    units_sold += rhs.units_sold;
    revenue += rhs.revenue;
    return *this;
}

// 非成员函数
std::istream &read(std::istream &is, Sales_data &item)
{
    double price = 0;
    is >> item.bookNo >> item.units_sold >> price;
    item.revenue = price * item.units_sold;
    return is;
}

std::ostream &print(std::ostream &os, const Sales_data &item)
{
    os << item.isbn() << " " << item.units_sold << " " << item.revenue;
    return os;
}

Sales_data add(const Sales_data &lhs, const Sales_data &rhs)
{
    Sales_data sum = lhs;
    sum.combie(rhs);
    return sum;
}


#endif