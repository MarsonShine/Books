#include <vector>
#include <string>
#include <initializer_list>
#include <memory>
#include <exception>

using std::string; using std::vector;

class StrBlob {
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