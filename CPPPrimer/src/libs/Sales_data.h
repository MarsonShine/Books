#ifndef SALES_DATA_H
#define SALES_DATA_H
#include <istream>
#include <string>

// Sales_data 接口应该包含以下操作：
// 一个 isbn 成员函数
// combine 成员函数，用于将一个Sales_data对象加到另一个对象上
// add 函数，执行两个Sales_data对象的加法
// read 函数，将数据从istream读入到Sales_data对象中
// print 函数，将Sales_data对象的值输出到ostream

// 成员函数与普通函数差不多。成员函数的申明必须在类的内部，它的定义即可以在类的内部也可以在类的外部。
// 作为接口组成部分的非成员函数，如add、read和print等，它们的申明和定义都放在类的外部

// 定义在内部的函数都是隐式内联的
struct Sales_data
{
public:
    std::string isbn() const { return bookNo;}
    std::string isbn2() const { return this->bookNo;}  // 等价上面
    Sales_data& combie(const Sales_data&);
    double avg_price() const;
private:
    //
    std::string bookNo;
    unsigned units_sold = 0;  //某本书的销量
    double revenue = 0.0;   //总销量
};  //struct最后必须要加个分号，这是因为类体后面可以紧跟变量名表示对该类型对象的定义
// Sales_data 的非成员函数
Sales_data add(const Sales_data&,const Sales_data&);
std::ostream &print(std::ostream&, const Sales_data&);
std::istream &read(std::istream&, Sales_data&);

double Sales_data::avg_price() const {
    if (units_sold) 
        return revenue/units_sold;
    else 
        return 0;
}
#endif

