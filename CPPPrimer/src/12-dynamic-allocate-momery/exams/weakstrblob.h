#include <vector>
#include <string>
#include <initializer_list>
#include <memory>
#include <exception>

using std::string; using std::vector;
class WeakStrBlob;

class StrBlob {
    friend class WeakStrBlob;
    WeakStrBlob begin();
    WeakStrBlob end();
    
public:
    typedef vector<string>::size_type size_type;
    StrBlob():data(std::make_shared<vector<string>>()) { }
    StrBlob(std::initializer_list<string> il):data(std::make_shared<vector<string>>(il)) { }
    size_type size() const { return data->size(); }
    bool empty() const { return data->empty(); }
    // 添加和删除元素
    void push_back(const string& s) { data->push_back(s); }
    void pop_back();
    // 访问
    string& front();
    string& back();
private:
    std::shared_ptr<vector<string>> data;
    // 检查data是否有数据
    void check(size_type i, const string &msg) const;
};

void StrBlob::check(size_type i, const string &msg) const {
    if (i >= data->size())
        throw std::out_of_range(msg);
}
void StrBlob::pop_back() {
    check(0, "空对象");
    return data->pop_back();
}
string& StrBlob::front() {
    check(0, "空对象");
    return data->front();
}
string& StrBlob::back() {
    check(0, "空对象");
    return data->back();
}

class WeakStrBlob {
public:
    // 成员访问运算符,解引用运算符
    string& operator*() const { 
        auto p = check(curr, "dereference past end");
        return (*p)[curr];
    }
    // 箭头运算符
    string* operator->() const {
        return &this->operator*();
    }
    WeakStrBlob(): curr(0) { }
    WeakStrBlob(StrBlob&a, size_t sz = 0): wptr(a.data), curr(sz) { }
    string deref() const;
    WeakStrBlob& incr(); // 前缀递增
private:
    std::shared_ptr<vector<string>> check(std::size_t, const string&) const;
    // 检查data是否有数据
    std::weak_ptr<vector<string>> wptr;
    std::size_t curr;
};

std::shared_ptr<vector<string>> WeakStrBlob::check(size_t i, const string &s) const {
    auto ret = wptr.lock();
    if (!ret)
    {
        throw std::runtime_error("unbound StrBlobPtr");
    }
    if (i >= ret->size())
    {
        throw std::out_of_range(s);
    }
    return ret;
}

string WeakStrBlob::deref() const
{
    auto p = check(curr, "dereference past end");
    auto ptr = *p; //vector
    return (*p)[curr];
}

WeakStrBlob& WeakStrBlob::incr()
{
    check(curr, "increment past end of WeakStrBlob");
    ++curr;
    return *this;
}