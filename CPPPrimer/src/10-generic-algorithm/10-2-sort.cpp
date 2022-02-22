#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <list>
#include <numeric>
using std::string; using std::vector; using std::cout; using std::endl;

void elimDups(vector<string> &words)
{
    // 按字典排序
    std::sort(words.begin(), words.end());
    // 出现重复内容的
    auto end_unique = std::unique(words.begin(), words.end());
    // 删除重复元素
    words.erase(end_unique, words.end());
}

bool isShorter(const string &s1, const string &s2)
{
    return s1.size() < s2.size();
}
int main()
{
    vector<string> vcs{"the","quick","red","fox","jumps","over","the","slow","red","turple"};
    elimDups(vcs);
    // 采用稳定排序
    std::stable_sort(vcs.begin(), vcs.end(), isShorter);
    for (const auto &s : vcs)
    {
        cout << s << " "; // 打印每个元素
    }
    cout << endl;
    return 0;
}