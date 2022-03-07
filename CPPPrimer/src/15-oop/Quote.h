#include <string>
#include <iostream>
#include <ostream>

class Quote
{
private:
    std::string bookNo;
protected:
    double price = 0.0;
public:
    Quote() = default;
    Quote(const std::string &book, double sales_price): bookNo(book), price(sales_price) {}

    std::string isbn() const { return bookNo; }
    double print_total(std::ostream& cout, const Quote &item, size_t n)
    {
        double ret = item.net_price(n);
        cout << "ISBN:" << item.isbn() << " # sold: " << n << " total due:" << ret << std::endl;
        return ret;
    }
    virtual double net_price(std::size_t n) const { return n * price; }
    virtual ~Quote() = default;

    // 模拟虚拷贝
    virtual Quote* clone() const & { return new Quote(*this); }
    virtual Quote* clone() && { return new Quote(std::move(*this)); }
};
