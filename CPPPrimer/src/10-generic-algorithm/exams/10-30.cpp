// 读取输入的整数序列，然后排序，最后将其输出
#include <iostream>
#include <vector>
#include <algorithm>
#include <iterator>
using std::vector;

int main()
{
    vector<int> vec(std::istream_iterator<int>(std::cin), std::istream_iterator<int>());
    std::sort(vec.begin(), vec.end());
    std::copy(vec.cbegin(), vec.cend(), std::ostream_iterator<int>(std::cout, " "));
}