#include <iostream>
#include <string>
#include <vector>
using std::string; using std::vector; using std::endl;

auto sum_for_int(const vector<string> vcs)
{
    int sum = 0;
    for (auto &&s : vcs)
    {
        sum += std::stoi(s);
    }
    return sum; 
}

auto sum_for_float(const vector<string> vcs)
{
    int sum = 0;
    for (auto &&s : vcs)
    {
        sum += std::stof(s);
    }
    return sum; 
}

int main()
{
    std::vector<std::string> v = { "1", "2", "3", "4.5" };
    std::cout << sum_for_int(v) << std::endl;
    std::cout << sum_for_float(v) << std::endl;

    return 0;
}