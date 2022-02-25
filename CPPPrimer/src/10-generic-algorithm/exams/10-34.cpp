#include <string>
#include <iostream>
#include <vector>
#include <algorithm>
#include <iterator>
// 逆序打印vector
int main()
{
    std::vector<int> vec {1,2,3,4,5,6,7,8,9};
    std::ostream_iterator<int> out_iter(std::cout, " ");
    std::copy(vec.crbegin(), vec.crend(), out_iter);
}