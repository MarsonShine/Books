#include <string>
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