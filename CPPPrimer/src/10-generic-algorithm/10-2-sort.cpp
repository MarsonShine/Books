#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <list>
#include <numeric>
using std::string; using std::vector;

void elimDups(vector<string> &words)
{
    // 按字典排序
    std::sort(words.begin(), words.end());
    // 出现重复内容的
    auto end_unique = std::unique(words.begin(), words.end());
    // 删除重复元素
    words.erase(end_unique, words.end());
}

int main()
{
    vector<string> vcs{"the","quick","red","fox","jumps","over","the","slow","red","turple"};
    elimDups(vcs);
    return 0;
}