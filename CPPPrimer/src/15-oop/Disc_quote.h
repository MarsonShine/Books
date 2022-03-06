#include "Quote.h"

class Disc_Quote : public Quote { // 因为Disc_quote对象中纯虚函数net_price，所以该类无法直接实例化，就相当于c#的abstract class
public:
    Disc_Quote() = default;
    Disc_Quote(const std::string& book, double price, std::size_t qty, double disc) : Quote(book, price), quantity(qty), discount(disc) {}
    double net_price(std::size_t) const = 0; // = 0 表示纯虚函数
private:
    std::size_t quantity = 0;   // 享受折扣时的购买量
    double discount = 0.0;  // 折扣
}

class Bulk_Disc_Quote : public Disc_Quote
{
public:
    Bulk_Disc_Quote() = default;
    Bulk_Disc_Quote(const std::string& book, double price, size_t qty, double disc) :
        Disc_Quote(book, price, qty, disc) {}
    // 覆写抽象基类的纯虚函数
    double net_price(std::size_t) const override;
};

// 数量受限的折扣策略
class Limit_Disc_Quote : public Disc_Quote {
public:
    Limit_Disc_Quote() = default;
    Limit_Disc_Quote(const std::string& book, double price, std::size_t max_number, double disc) :
        Disc_Quote(book, price, max_number, disc) { }
    double net_price(std::size_t) const override;
};
double Limit_Disc_Quote::net_price(std::size_t number) const override
{
    double discLoc = 1;
    if (number < quantity) discLoc = 1 - discount;

    return number * price * discLoc;
}