#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <list>
#include <numeric>

void elimdups(std::vector<std::string> &vs)
{
    std::sort(vs.begin(), vs.end());
    auto new_end = std::unique(vs.begin(), vs.end());
    vs.erase(new_end, vs.end());
}

void biggies_partition(std::vector<std::string> &vs, std::size_t sz)
{
    elimdups(vs);
    auto pivot = std::partition(vs.begin(), vs.end(), [sz](const std::string& a)
                                                        {
                                                            return a.size() >= sz;
                                                        });
    for(auto it = vs.cbegin(); it != pivot; ++it)
    {
        std::cout << *it << " ";
    }

}

int main()
{
    auto sum = [](const int a, const int b) { return a + b; };
    sum(1,2);

    // 捕捉参数
    int a = 10;
    auto sum2 = [a](const int b) { return a + b; };

    std::vector<std::string> v{
        "the", "quick", "red", "fox", "jumps", "over", "the", "slow", "red", "turtle"
    };
    std::vector<std::string> v1(v);
    biggies_partition(v1, 4);
}