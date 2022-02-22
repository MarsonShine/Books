#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <list>
#include <numeric>

int main()
{
    int a1[] = {0,1,2,3,4,5,6,7,8,9};
    int a2[sizeof(a1)/sizeof(*a1)]; // a2与a1大小一样
    // 拷贝
    auto ret = std::copy(std::begin(a1), std::end(a1), a2); // a1的内容拷贝给a2

    // 将所有为0的元素替换为10
    std::replace(std::begin(a1), std::end(a1), 0, 10);
    // 上述会更改原来的容器元素内容，如果使用的是 replace_copy 则不会更改原来的容器内容
    std::vector<int> vcs;
    std::replace_copy(std::cbegin(a1), std::cend(a1), std::back_inserter(vcs), 0, 10);
}