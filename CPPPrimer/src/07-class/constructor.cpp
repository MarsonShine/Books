#include <iostream>
#include <stdio.h>
using std::string;std::istream;

struct Sales_data 
{
    // 新增的构造函数
    Sales_data() = default; // 默认的构造函数，合成的默认构造函数
    Sales_data(const string &s) :bookNo(s){}
    // 构造函数初始值列表
    Sales_data(const string &s, unsigned n,double p): bookNo(s), units_sold(n), revenue(p*n) {}
    Sales_data(std::istream &);

    std::string const& isbn() const { return bookNo; };
    Sales_data& combie(const Sales_data&);

    std::string bookNo;
    unsigned units_sold = 0;
    double revenue = 0.0;
};

std::istream &read(std::istream &is, Sales_data &item)
{
    double price = 0;
    is >> item.bookNo >> item.units_sold >> price;
    item.revenue = price * item.units_sold;
    return is;
}

// 外部定义构造函数
Sales_data::Sales_data(std::istream &is)
{
    read(is, *this);
}
