#include <iostream>
#include <memory>
#include <sstream>

template <typename T> std::string debug_rep(const T& t);
template <typename T> std::string debug_rep(T* tp);

std::string debug_rep(const std::string &s);
std::string debug_rep(char* p);
std::string debug_rep(const char *p);

template <typename T> std::string debug_rep(T* tp)
{
    std::ostringstream ret;
    ret << "pointer: " << tp;

    if (tp)
    {
        ret << " " << debug_rep(*tp);
    }
    else ret << " null pointer ";
    return ret.str();
}

template <typename T> std::string debug_rep(const T& t)
{
    std::ostringstream ret;
    ret << t;
    return ret.str();
}

std::string debug_rep(const std::string &s)
{
    return '"' + s + '"';
}

std::string debug_rep(const char *p)
{
    return debug_rep(std::string(p));
}