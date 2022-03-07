#include <Query_base.h>

class WordQuery : public Query_base {
    friend class Query; // Query使用WordQuery构造函数
    WordQuery(const std::string &s) : query_word(s) { }
    QueryResult eval(const TextQuery& t) const override {
        return t.query(query_word);
    }
    std::string rep() const override {
        return query_word;
    }

    std::string query_word; // 要查找的单词
};
inline 
Query::Query(const std::string& s): q(new WordQuery(s)) { }

class NotQuery : public Query_base {
    friend Query operator~(const Query&);
    NotQuery(const Query& q): query(q) {}
    std::string rep() const override {
        return "~(" + query.rep() + ")";
    }
    QueryResult eval(const TextQuery&) const override;
    Query query;
};
inline
Query operator~(const Query& operand)
{
    return std::shared_ptr<Query_base>(new NotQuery(operand));
}

class BinaryQuery : public Query_base {
protected:
    BinaryQuery(const Query& lq, const Query& rq, std::string s) {}
    std::string rep() const { 
        return "(" + lq.rep() + " "
        + opSymbol + " " + rq.req() + ")";
    }
    Query lq, rq;
    std::string opSymbol;
};

class AndQuery : public BinaryQuery {
    friend Query operator&(const Query&, const Query&);
    AndQuery(const Query& lq, const Query rq) : BinaryQuery(lq, rq, "&") {}
    QueryResult eval(const TextQuery&) const override;
};
inline
Query operator&(const Query& lq, const Query& rq) {
    return std::shared_ptr<Query_base>(new AndQuery(lq, rq));
};

class OrQuery : public BinaryQuery {
    friend Query operator|(const Query& lq, const Query rq);
    OrQuery(const Query& lq, const Query& rq) : BinaryQuery(lq, rq, "|") {}
    QueryResult eval(const TextQuery&) const override;
};
inline
Query operator|(const Query& lq, const Query& rq) {
    return std::shared_ptr<Query_base>(new OrQuery(lq, rq));
}
QueryResult OrQuery::eval(const TextQuery& query) const {
    auto r = rq.eval(query), l = lq.eval(query);
    auto lines = std::make_shared<set<line_number>>(l.begin(), l.end());
    // 插入右侧运算对象所得的行号
    lines->insert(r.begin(), r.end());
    // 返回新的QueryResult，表示并集
    return QueryResult(rep(), lines, l.get_file());
}