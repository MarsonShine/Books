#include <vector>
#include <memory>

template <typename T> class Blob {
public:
    typedef T value_type;
    typedef typename std::vector<T>::size_type size_type;
    Blob();
    Blob(std::initializer_list<T> il);

    size_type size() const { return data->size(); }
    bool empty() const { return data->empty(); }

    void push_back(const T &t) { return data->push_back(t); }
    void push_back(T &&t) { return data->push_back(std::move(t)); }
    void pop_back();

    T& back();
    T& operator[](size_type);

    const T& back() const;
    const T& operator[](size_type) const;
private:
    std::shared_ptr<std::vector<T>> data;
    void check(size_type i, const std::string &msg) const;
};

// 定义
template<typename T>
Blob<T>::Blob(): data(std::make_shared<std::vector<T>>()) { }

template<typename T>
Blob<T>::Blob(std::initializer_list<T> il): data(std::make_shared<std::vector<T>>(il)) { }

template<typename T>
Blob<T>::pop_back() {
    check(0, "pop_back empty");
    data->pop_back();
}

template<typename T>
T& Blob<T>::back() {
    check(0, "back empty");
    return data->back();
}

template<typename T>
const T& Blob<T>::operator[](size_type i) const
{
    check(i, "i empty");
    auto item = (*data)[i];
    return item;
}

template <typename T>
T& Blob<T>::operator[](size_type i)
{
    check(i, "i empty");
    return (*data)[i];
}

//===============================================================//

template <typename> class BlobPtr;

template <typename T>
bool operator ==(const BlobPtr<T>& lhs, const BlobPtr<T>& rhs);

template <typename T>
bool operator < (const BlobPtr<T>& lhs, const BlobPtr<T>& rhs);


template<typename T> class BlobPtr
{
    friend bool operator ==<T>
    (const BlobPtr<T>& lhs, const BlobPtr<T>& rhs);

    friend bool operator < <T>
    (const BlobPtr<T>& lhs, const BlobPtr<T>& rhs);

public:
    BlobPtr() : curr(0) { }
    BlobPtr(Blob<T>& a, std::size_t sz = 0) :
        wptr(a.data), curr(sz) { }

    T& operator*() const
    {
        auto p = check(curr, "dereference past end");
        return (*p)[curr];
    }

    // prefix
    BlobPtr& operator++();
    BlobPtr& operator--();

    // postfix
    BlobPtr operator ++(int);
    BlobPtr operator --(int);

private:
    // returns  a shared_ptr to the vector if the check succeeds
    std::shared_ptr<std::vector<T>>
         check(std::size_t, const std::string&) const;

    std::weak_ptr<std::vector<T>> wptr;
    std::size_t curr;

};

// prefix ++
template<typename T>
BlobPtr<T>& BlobPtr<T>::operator ++()
{
    // if curr already points past the end of the container, can't increment it
    check(curr, "increment past end of StrBlob");
    ++curr;
    return *this;
}

// prefix --
template<typename T>
BlobPtr<T>& BlobPtr<T>::operator --()
{
    -- curr;
    check(curr, "decrement past begin of BlobPtr");

    return *this;
}


// postfix ++
template<typename T>
BlobPtr<T> BlobPtr<T>::operator ++(int)
{
    BlobPtr ret = *this;
    ++*this;

    return ret;
}

// postfix --
template<typename T>
BlobPtr<T> BlobPtr<T>::operator --(int)
{
    BlobPtr ret = *this;
    --*this;

    return ret;
}

template<typename T> bool operator==(const BlobPtr<T> &lhs, const BlobPtr<T> &rhs) {
    if (lhs.wptr.lock() != rhs.wptr.lock()) {
		throw runtime_error("ptrs to different Blobs!");
	}
	return lhs.i == rhs.i;
}

template<typename T> bool operator< (const BlobPtr<T> &lhs, const BlobPtr<T> &rhs) {
	if (lhs.wptr.lock() != rhs.wptr.lock()) {
		throw runtime_error("ptrs to different Blobs!");
	}
	return lhs.i < rhs.i;
}
