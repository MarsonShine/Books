#include <string>
#include <memory>
#include <utility>

class StrVec
{
private:
    // 定义下标运算符
    std::string& operator [](std::size_t n) { return elements[n]; }
    const std::string& operator[](std::size_t n) const { return elements[n]; }
    static std::allocator<std::string> alloc;
    std::string *elements; // 数组头元素位置
    std::string *first_free; // 最后一个元素后的位置（即第一个空闲位置）
    std::string *cap; // 指向数组尾后位置的指针
    void chk_n_alloc() { 
        if (size() == capacity()) reallocate();
    }
    void free();
    // 重新分配
    void reallocate();
    void reallocate_move();
    // pair返回两个指针，分别表示新空间的开始位置和拷贝的尾后位置
    std::pair<std::string*, std::string*> alloc_n_copy(const std::string*, const std::string*);
public:
    StrVec() : elements(nullptr), first_free(nullptr), cap(nullptr) { };
    StrVec(const StrVec&);
    StrVec& operator=(const StrVec&);
    ~StrVec();
    void push_back(const std::string&);
    size_t size() { return first_free - elements; }
    size_t capacity() const { return cap - elements; }
    std::string *begin() const { return elements; }
    std::string *end() const { return first_free; }
};

StrVec::~StrVec()
{
    free();
}
void StrVec::push_back(const std::string &s)
{
    chk_n_alloc();
    alloc.construct(first_free++, s);
}
std::pair<std::string*, std::string*> StrVec::alloc_n_copy(const std::string *ls, const std::string *rs)
{
    auto data = alloc.allocate(rs - ls);
    return {data, std::uninitialized_copy(ls, rs, data)}; //uninitialized_copy返回的值最后一个构造元素的位置
}
// 释放内存
// destroy元素，然后释放alloc分配的内存
void StrVec::free()
{
    // 如果数组存在值
    if (elements) {
        for (auto p = first_free; p != elements;)
            alloc.destroy(--p);
        alloc.deallocate(elements, capacity());
    }
}
StrVec::StrVec(const StrVec &sv)
{
    auto tmp = alloc_n_copy(sv.begin(), sv.end());
    elements = tmp.first;
    first_free = cap = tmp.second;
}
StrVec &StrVec::operator=(const StrVec &rsv)
{
    auto tmp = alloc_n_copy(rsv.begin(), rsv.end());
    free();
    elements = tmp.first;
    first_free = cap = tmp.second;
    return *this;
}
void StrVec::reallocate()
{
    // 内存大小翻倍
    auto new_capacity = size() ? 2 * size() : 1;
    // 分配新内存
    auto new_data = alloc.allocate(new_capacity);
    // 移动具体元素
    auto new_first_free = new_data;
    auto elem = elements;
    for (size_t i = 0; i != size(); ++i)
        alloc.construct(new_first_free++, std::move(*elem++));
    free();
    elements = new_data;
    first_free = new_first_free;
    cap = elements + new_capacity;
}
// 通过移动迭代器的方式实现地址移动而非值拷贝
void StrVec::reallocate_move()
{
    // 内存大小翻倍
    auto new_capacity = size() ? 2 * size() : 1;
    // 分配新内存
    auto first = alloc.allocate(new_capacity);
    // 移动元素
    auto last = std::uninitialized_copy(std::make_move_iterator(begin()), std::make_move_iterator(end()), first);
    free();
    elements = first;
    first_free = last;
    cap = elements + new_capacity;
}
