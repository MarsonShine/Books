#include <memory>
#include <vector>

template<typename T> class Blob {
public:
    typedef T value_type;
    typedef typename std::vector<T>::size_type size_type;

    // 构造函数
    Blob() : data(std::make_shared<std::vector<T>>()) { }
    Blob(std::initializer_list<T> il);

    // 模板构造函数，接收两个迭代器
    template<typename It> Blob(It b, It e);

    // ... 与之前的成员信息写法一样
private:
    std::shared_ptr<std::vector<T>> data;
    void check(size_type i, std::string &msg) const;
};

template<typename T> Blob<T>::Blob(std::initializer_list<T> il) : 
    data(std::make_shared<std::vector<T>>(il)) { }

template<typename T>
template<typename Iterator>
Blob<T>::Blob(Iterator b, Iterator e) : data(std::make_shared<std::vector<T>>(b, e)) { }