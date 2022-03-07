#include <string>
#include <memory>
#include "Query.h"

class Query {
    friend Query operator~(const Query&);
    friend Query operator|(const Query&, const Query&);
    friend Query operator&(const Query&, const Query&);
public:
    Query(const std::string&);
    QueryResult eval(const TextQuery &t) const { return q->eval(t); }
    std::string rep() const
    {
        std::cout << "Query::rep() \n";
        return q->rep();
    }
private:
    Query(std::shared_ptr<Query_base> query) : q(query) {}
    std::shared_ptr<Query_base> q;
};
inline
std::ostream & operator<<(std::ostream out, const Query &query) {
    return out << query.rep();
}

class Query_base
{
    friend class Query;
private:
    virtual QueryResult eval(const TextQuery&) const = 0; // 纯虚函数
    virtual std::string rep() const = 0; // 纯虚函数
protected:
    using line_number = TextQuery::LineNumber;
    virtual ~Query_base() = default;
};
