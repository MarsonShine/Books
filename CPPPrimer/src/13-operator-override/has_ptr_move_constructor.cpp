#include <string>

class HasPtr {
public:
    friend void swap(HasPtr&, HasPtr&);
    HasPtr(const std::string &s = std::string()): ps(new std::string(s)), i(0), use(new std::size_t(1)){ }
    // 拷贝构造函数
    HasPtr(const HasPtr& hp) : ps(new std::string(*hp.ps)), i(hp.i), use(hp.use) { 
        ++*use;
    }
    // 移动构造函数
    HasPtr(HasPtr &&hp) noexcept : ps(hp.ps), i(hp.i), use(hp.use) { hp.ps = 0; } // 移动后指针置为0，这样能保证销毁移动后的源对象是安全的。
    HasPtr& operator=(HasPtr& hp) {
        swap(*this, hp);
        return *this;
    }
    ~HasPtr() {
        release();
    }
private:
    std::string *ps;
    int i; 
    std::size_t *use; // 记录引用此对象的引用数
    void release()
    {
        if (--*use == 0) {
            delete ps;
            delete use;
        }
    }
};

inline void swap(HasPtr &lh, HasPtr &rh)
{
    using std::swap;
    swap(lh.ps, rh.ps);
    swap(lh.i, rh.i);
}


int main()
{
    HasPtr a;
    HasPtr b;
    
}