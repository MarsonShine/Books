#include<iostream>
#include<string>
using std::string;

void a() {
    string s1,s2;
    std::cin >> s1 >> s2;
    if (s1 == s2) {
        std::cout << "s1 == s2 = " << s1 << std::endl;
    } else if (s1 > s2) {
        std::cout << "max value = " << s1 << std::endl;
    } else {
        std::cout << "max value = " << s2 << std::endl;
    }
}

void b() {
    string s1,s2;
    std::cin >> s1 >> s2;
    if (s1 == s2) {
        std::cout << "s1 == s2 = " << s1 << std::endl;
    } else {
        auto l1 = s1.size(), l2 = s2.size();
        if (l1 < l2) {
            std::cout << "max length value = " << l2 << std::endl;
        } else {
            std::cout << "max length value = " << l1 << std::endl;
        }
    }
}

int main()
{
    a();
    b();
    return 0;
}