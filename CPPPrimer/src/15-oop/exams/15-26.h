#include <string>
#include <iostream>

class Quote
{
    friend bool operator!=(const Quote&, const Quote&);
public:
    Quote() {
        std::cout << "Quote合成构造函数 \n";
    }
    Quote(std::string &b, double p) :
        bookNo(b), price(p) { std::cout << "Quote两个参数构造函数 \n"; }
    
    // 拷贝构造函数
    Quote(const Quote& q) : bookNo(q.bookNo), price(q.price) { std::cout << "Quote拷贝构造函数 \n"; }
    // 移动构造函数
    Quote(Quote&& qmove) noexcept : bookNo(std::move(qmove.bookNo)), price(std::move(qmove.price)) { 
        std::cout << "Quote移动构造函数";
    }
    // 拷贝复制运算符
    Quote& operator=(const Quote& rq) {
        if (this != &rq)
        {
            bookNo = rq.bookNo;
            price = rq.price;
        }
        std::cout << "Quote拷贝复制运算符 oprator= \n";
        return *this;
    }
    // 移动复制运算符
    Quote& operator=(Quote&& rq) noexcept {
        if (*this != rq) { // 需要添加友元的!=运算符
            bookNo = std::move(rq.bookNo);
            price = std::move(rq.price);
        }
        std::cout << "Quote移动复制运算符 operator=(&&) \n";
    }
    std::string isbn() const { return bookNo; }
    virtual double net_price(std::size_t n) const { return n * price; }
    virtual void debug() const;
    virtual ~Quote() {
        std::cout << "Quote析构函数\n";
    }
private:
    std::string bookNo;
protected:
    double price = 0.0;
};
inline
bool operator!=(const Quote& lq, const Quote& rq)
{
    return lq.bookNo != rq.bookNo && lq.price != rq.price;
}

class Disc_Quote : public Quote { // 因为Disc_quote对象中纯虚函数net_price，所以该类无法直接实例化，就相当于c#的abstract class
public:
    Disc_Quote() = default;
    Disc_Quote(std::string& book, double price, std::size_t qty, double disc) : Quote(book, price) {
        quantity = qty; 
        discount = disc;
    }
    double net_price(std::size_t) const = 0; // = 0 表示纯虚函数
private:
    std::size_t quantity = 0;   // 享受折扣时的购买量
    double discount = 0.0;  // 折扣
};

class Bulk_Quote : public Disc_Quote {
public:
    using Disc_Quote::Disc_Quote; // 继承Disc_Quote的构造函数，编译器会自动生成与基类同等的构造函数，等价于下面注释的构造函数，如果派生类还有自己的数据成员，就会初始化默认值
    // Bulk_Quote() { std::cout << "Bulk_Quote合成构造函数\n"; }
    // Bulk_Quote(std::string &b, double p, std::size_t qty, double disc) :
    //     Disc_Quote(b, p, qty, disc) { std::cout << "Bulk_Quote死参构造函数\n"; }
    // 拷贝构造函数
    Bulk_Quote(const Bulk_Quote& bcopy) : Disc_Quote(bcopy) { std::cout << "Bulk_Quote拷贝构造函数\n"; }
    // 移动构造函数
    Bulk_Quote(Bulk_Quote&& bmove) noexcept : Disc_Quote(std::move(bmove)) { std::cout << "Bulk_Quote移动构造函数\n"; }
    // 拷贝赋值运算符
    Bulk_Quote& operator=(const Bulk_Quote& rbq)
    {
        Disc_Quote::operator=(rbq); // 显式调用基类的拷贝赋值运算符
        std::cout << "Bulk_Quote拷贝赋值运算符\n";
        return *this;
    }
    // 移动复制运算符
    Bulk_Quote& operator=(Bulk_Quote&& rbq)
    {
        Disc_Quote::operator=(rbq);
        std::cout << "Bulk_Quote移动复制运算符\n";
        return *this;
    }
    double net_price(std::size_t n) const override;
    void debug() const override;

    ~Bulk_Quote() override
    {
        std::cout << "Bulk_Quote析构函数\n";
    }
private:

};