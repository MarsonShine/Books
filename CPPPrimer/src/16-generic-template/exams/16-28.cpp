struct Delete
{
    template<typename T>
    auto operator() (T* p) -> T* const
    {
        delete p;
    }
};

template<typename, typename> class unique_ptr;

template<typename T, typename D = Delete> class unique_ptr
{
    friend void swap<T, D>(unique_ptr<T, D>& l, unique_ptr<T, D>& r);
public:
    // 禁止拷贝构造函数
    unique_ptr(const unique_ptr<T, D>&) = delete;
    // 禁止复制运算符
    unique_ptr& operator=(const unique_ptr&) = delete;
    // 默认构造函数
    unique_ptr() = default;
    explicit unique_ptr(T* up): ptr(up) { } // 禁止隐式的类型转换

    // 移动
    unique_ptr(unique_ptr<T, D> && up) noexcept : ptr(up.ptr) { up.ptr = nullptr; }
    // 移动赋值
    unique_ptr& operator=(unique_ptr<T, D> &&rup) noexcept;
    // 
    unique_ptr& operator=(std::nullptr_t n) noexcept;
    // 操作符重载
    T& operator *() const { return ptr; }
    T* operator ->() const { return &this->operator*(); }
    operator bool() const { return ptr ? true : false; }
private:
    T* ptr = nullptr;
    D deleter = D();
};