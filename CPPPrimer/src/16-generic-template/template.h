#include <string>
#include <vector>
#include <initializer_list>
#include <memory>
#include <exception>
#include <map>
#include <iostream>
// 范型模版编程，定义和申明一般都写在同一个头文件中

template <typename T>
int compare(const T& v1, const T& v2) {
    if (v1 < v2) return -1;
    if (v2 < v1) return 1;
    return 0;
}

// 非类型参数
template <unsigned N, unsigned M>
int compare(const char(&p1)[N], const char(&p2)[M]) {
    return std::strcmp(p1, p2);
}

template <typename TRequest, typename TResponse> TResponse calc(TRequest& request);

// inline 和 constexp
template <typename T> inline T min(const T&, const T&);

// 类模板
template <typename T> class Blob {
public:
    typedef T value_type;
    typedef typename std::vector<T>::size_type size_type;
    // 构造函数
    Blob();
    Blob(std::initializer_list<T> il);
    // blob元素数量
    size_type size() const { return data->size(); }
    bool empty() const { return data->empty(); }
    void push_back(cosnt T& t) { data->push_back(T); }
    // 移动pushback
    void push_back(T&& t) { data->push_back(std::move(t)); }
    void pop_back();
    // 元素访问
    T& back();
    T& operator[](size_type i);
private:
    std::shared_ptr<std::vector<T>> data;
    void check(size_type i, const std::string& msg) const;
};
// 定义
template <typename T>
void Blob<T>::check(size_type i, const std::string& msg) const
{
    if (i >= data->size())
        throw std::out_of_range(msg);
}
T& Blob<T>::back()
{
    check(0, "back empty");
    return data->back();
}
T& Blob<T>::operator[](size_type i)
{
    check(i, "out of range");
    return (*data)[i];
}
void Blob<T>::pop_back()
{
    check(0, "pop back empty");
    return data->pop_back();
}
Blob<T>::Blob(): data(std::make_shared<T>()) { }
Blob<T>::Blob(std::initializer_list<T> il) : 
    data(std::make_shared<T>(il)) { }

// 类模板简化模板类名
template <typename T> class BlobPtr {
public:
    BlobPtr& operator++(); // 递增运算符，简写，会自动返回BlobPtr<T>
    BlobPtr& operator--(); // 递减运算符
private:
    std::shared_ptr<std::vector<T>> check(std::size_t, const std::string&) const;
    // 弱引用，底层vector可能已经被销毁
    std::weak_ptr<std::vector<T>> wptr;
    std::size_t curr;
    // ...
};

// 模板友元
template <typename T> class Pal; // 前置申明
class C {
    friend class Pal<C>; // 用类C实例化的Pal是C的一个友元
    // Pal2的所有实例都是C的友元，即无需前置申明
    template <typename T> friend class Pal2;
};
template <typename T> class C2 {
    friend class Pal<T>; // Pal的模板申明必须在作用域内
    template <typename X> friend class Pal2; // Pal2所有实例都是C2的友元，这个时候模板类型参数必须与C2定义的范型类型不同
    friend class Pal3;
};

// 模板类型别名
template<typename T> using twin = std::pair<T, T>;
template<typename T> using partNo = std::pair<T, unsigned>;

// T::size_type * p;
// 上面有两种解释：1. size_type为T类型的static数据成员。 2. 类型成员
// 所以上面表达式就有两种解读：
// 1. size_type 与 p 相乘
// 2. T类型的size_type类型成员变量 p
// 那么如何告诉编译器分辨这两种情况呢——用typename关键字
template<typename T>
typename T::value_type top(const T& c)
{
    if (!c.empty())
        return c.back();
    return typename T::value_type();
}

template<class T> class Bar {
    
}
typedef char CType;
template<typename CType> CType f5(CType a);

// 成员模板
class DebugDelete {
public:
    DebugDelete(std::ostream &os = std::cerr) : os(s) { }
    template<typename T> void operator()(T *t) const {
        os << "deleting unique_ptr" << std::endl; delete t;
    }
private:
    std::ostream &os;
}

// 模板构造函数
template<typename T>
template<typename It> 
Blob<T>::Blob(It b, It e): data(std::make_shared<std::vector<T>>(b, e)) { }