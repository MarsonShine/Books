#ifndef SALES_DATA_H
#define SALES_DATA_H
#include <istream>
#include <string>

struct Sales_data
{
    std::string bookNo;
    unsigned units_sold = 0;
    double revenue = 0.0;
};  //struct最后必须要加个分号，这是因为类体后面可以紧跟变量名表示对该类型对象的定义
#endif