#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <list>
#include <numeric>

int main()
{
    std::vector<int> vcs{1,2,3,4,5,6,7,8,9};
    auto sum = std::accumulate(vcs.cbegin(), vcs.cend(), 0);

    std::vector<double> vcs{1.0,2.0,3.0,4.0,5.0,6,7,8,9};
    auto sumd = std::accumulate(vcs.cbegin(), vcs.cend(), 0); // 不会报错，但是最后的结果会由double隐式转换成int
    auto sumd2 = std::accumulate(vcs.cbegin(), vcs.cend(), 0.0);
}