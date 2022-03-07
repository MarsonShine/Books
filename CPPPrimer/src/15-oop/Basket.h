#include <memory>
#include <ostream>
#include <set>
#include <Quote.h>

class Basket {
public:
    Basket() = default;
    void add_ietm(const std::shared_ptr<Quote> &sale) { item.insert(sale); }
    void add_item(const Quote& scopy) { items.insert(std::shared_ptr<Quote>(scopy.clone()))}
    void add_item(Quote&& smove) { items.insert(std::shared_ptr<Quote>(smove.clone())) };
    double total_receipt(std::ostream&) const;
private:
    static bool compare(const std::shared_ptr<Quote> &lq, const std::shared_ptr<Quote> &rq)
    {
        return lq->isbn() < rq->isbn();
    }
    std::multiset<std::shared_ptr<Quote>, decltype(compare)*> items{compare};
};
double Basket::total_receipt(std::ostream &out) const {
    double sum = 0.0;
    for (auto item = items.cbegin(); item != items.cend(); item = items.upper_bound(*item)) { // upper_bound 函数跳过相同key的下一个位置。即该迭代器返回与item关键字相等的元素集合中最后一个元素的下一个位置。
        sum += print_total(out, **item, items.count(*item));
    }
    out << "总价：" << sum << std::endl;
    return sum;
}