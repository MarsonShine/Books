#ifndef SALES_DATA_H
#define SALES_DATA_H
#include <istream>
#include <string>
using std::istream; std::ostream;

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
    friend std::istream &read(std::istream&, Sales_data&);
    friend std::ostream &operator<<(std::ostream&, const Sales_data&);
    friend std::istream &operator>>(std::istream&, Sales_data&);
    friend Sales_data operator+(const Sales_data&, const Sales_data &);
    friend bool operator==(const Sales_data &lhs, const Sales_data &rhs);
public:
    Sales_data& operator+=(const Sales_data&);
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
std::ostream &operator<<(std::ostream&, const Sales_data&);
std::istream &operator>>(std::istream&, Sales_data&);
Sales_data operator+(const Sales_data&, const Sales_data&);

// 在类的外部定义成员函数必须与类内的函数申明一致
double Sales_data::avg_price() const {
    if (units_sold) 
        return revenue/units_sold;
    else 
        return 0;
}
Sales_data& Sales_data::combie(const Sales_data &rhs) {
    units_sold += rhs.units_sold;
    revenue += rhs.revenue;
    return *this;   // 返回函数对象的引用
}
istream &read(istream &is, Sales_data &item)
{
    double price = 0;
    is >> item.bookNo >> item.units_sold >> price;// 非成员函数，所以不可访问类成员信息
    item.revenue = price * item.units_sold;
    return is;

}
Sales_data add(const Sales_data &lhs,const Sales_data &rhs)
{
    Sales_data sum = lhs;   //lhs拷贝至新的变量sum
    sum.combie(rhs);
    return sum;
}
std::ostream& operator<<(std::ostream &cout, const Sales_data &rhs)
{
    cout << rhs.isbn() << " " << rhs.units_sold << " "
        << rhs.revenue << " " << rhs.avg_price();
    return cout;
}
std::istream &operator>>(std::istream &cin, Sales_data &item)
{
    double price;
    cin >> item.bookNo >> item.units_sold >> price;
    if (cin) { // 检查输入是否成功
        item.revenue = price * item.units_sold;
    } else
        item = Sales_data(); // 失败就赋予默认对象
    
    return cin;
}
Sales_data& Sales_data::operator+=(const Sales_data &a)
{
    revenue += a.revenue;
    units_sold += a.units_sold;
    return *this;
}
Sales_data operator+(const Sales_data &a, const Sales_data &b)
{
    Sales_data sum = a;
    sum += b;
    return sum;
}
inline bool 
operator==(const Sales_data &lhs, const Sales_data &rhs)
{
    // must be made a friend of Sales_data
    return lhs.isbn() == rhs.isbn() &&
           lhs.revenue == rhs.revenue &&
           lhs.units_sold == rhs.units_sold;
}

inline bool 
operator!=(const Sales_data &lhs, const Sales_data &rhs)
{
    return !(lhs == rhs); // != defined in terms of operator==
}
#endif

