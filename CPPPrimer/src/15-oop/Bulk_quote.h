#include <string>
#include <iostream>
#include "Quote.h"

class Bulk_quote : public Quote {   // 继承自Quote
public:
    Bulk_quote() = default;
    Bulk_quote(const std::string&, double, std::size_t, double);
    Bulk_quote(const std::string& bookNo, double p, std::size_t qty, double disc) : Quote(bookNo, p), min_qty(qty), discount(disc) {}
    double net_price(std::size_t) const;
private:
    std::size_t min_qty = 0; // xian t
    double discount = 0.0;  // 折扣
};

double Bulk_quote::net_price(std::size_t cnt) const
{
    if (cnt >= min_qty) {
        return cnt * (1 - discount) * price;
    }
    return cnt * price;
}