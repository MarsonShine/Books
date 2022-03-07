#include "exam_template.h"
#include <iostream>
#include <vector>
#include <list>
#include <string>

int main()
{
    std::vector<int> v = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    auto is_in_vector = v.cend() != exam16::find(v.cbegin(), v.cend(), 5);
    std::cout << (is_in_vector ? "found\n" : "not found\n");

    std::vector<std::string> v2 = { "aa", "bb", "cc", "dd", "ee", "ff", "gg"};
    is_in_vector = v2.cend() != exam16::find(v2.cbegin(), v2.cend(), "cc");
    std::cout << (is_in_vector ? "found\n" : "not found\n");
}