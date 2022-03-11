#include <iostream>
#include <memory>
#include <sstream>

// always declare first:
template <typename T> 
std::string debug_rep(const T& t);
template <typename T> 
std::string debug_rep(T* p);

std::string debug_rep(const std::string &s);
std::string debug_rep(char* p);
std::string debug_rep(const char *p);

template<typename T> 
std::string debug_rep(const T& t)
{
    std::ostringstream ret;
    ret << t;
    return ret.str();
}

// print pointers as their pointer value, followed by the object to which the pointer points
template<typename T> 
std::string debug_rep(T* p)
{
    std::ostringstream ret;
    ret << "pointer: " << p;

    if (p)
        ret << " " << debug_rep(*p);
    else
        ret << " null pointer";

    return ret.str();
}

// non-template version
std::string debug_rep(const std::string &s)
{
    return '"' + s + '"';
}

// convert the character pointers to string and call the string version of debug_rep
std::string debug_rep(char *p)
{
    return debug_rep(std::string(p));
}

std::string debug_rep(const char *p)
{
    return debug_rep(std::string(p));
}

template<typename T>
std::ostream& print(std::ostream& os, const T& t)
{
    return os << t;
}
template<typename T, typename... Args>
std::ostream& print(std::ostream &os, const T &t, const Args&... rest)
{
    os << t << ",";
    return print(os, rest...);
}

template<typename... TArgs>
std::ostream errorMsg(std::ostream& os, const TArgs... rest)
{
    return print(os, debug_rep(rest)...);
}

int main()
{
    errorMsg(std::cout, 1, 2, 3, 4, 5, 9.9f, "Sss", "other");
}