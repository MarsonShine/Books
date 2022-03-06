#include <string>
#include <iostream>

class Quote
{
private:
    std::string bookNo;
protected:
    double price = 0.0;
public:
    Quote() = default;
    Quote(const std::string &book, double sales_price): bookNo(book), price(sales_price) {}

    std::string isbn() { return bookNo; }
    virtual double net_price(std::size_t n) const { return n * price; }
    virtual ~Quote() = default;
};
