#ifndef _7_15_h
#define _7_15_h
#include <string>

class Person {
    std::string name;
    std::string address;
    Person() = default;
    Person():name(""), address("") {}
    Person(std::string const&n,std::string const&ad)
    {
        name = n;
        address = ad;
    };

public:
    // 采用尾置返回类型
    // 返回字符串的常数引用，可以获得更好的性能
    auto get_name() const -> std::string const& { return this->name;}
    auto get_addr() const -> std::string const& { return address; }
    std::string get_addr2() const { return address;}
};

#endif