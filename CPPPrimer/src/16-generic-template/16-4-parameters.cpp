#include <iostream>

template<typename T>
std::ostream &print(std::ostream &os, const T &t) {
    return os << t;
}

template<typename T, typename ...Args>
std::ostream& print(std::ostream &os, const T &t, const Args&... rest) {
    os << t <<", ";
    return print(os, rest...); // 递归调用，将rest的第一个参数绑定给T，剩下的绑定给rest
}
