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

size_t make_plural(size_t count, const string &a, const string &b) 
{
    //TODO
}

void biggies(vector<string> &words, vector<string>::size_type sz)
{
    elimDups(words);
    std::stable_sort(words.begin(), words.end(), [](const string &a, const string &b)
                                                    { return a.size() < b.size(); });
    // 获取一个迭代器，指向第一个满足size() >= sz的元素
    auto wc = std::find_if(words.begin(), words.end(), [sz](const string &a)
                                                        { return a.size() >= sz;});
    // 计算满足 size >= sz 的元素数目
    auto count = words.end() - wc;
    cout << count << " " << make_plural(count, "word", "s")
        << " of length " << sz << " or longer " << endl;
}

void ff() 
{
    size_t v1 = 42;
    auto f = [v1]() { return ++v1; };
    v1 = 0;
    auto j = f();
    cout << j << endl;
}

void ff_mutable() 
{
    size_t v1 = 42;
    auto f = [v1]() mutable { return ++v1; };// ok
    v1 = 0;
    auto j = f();
    cout << j << endl; // 输出43
}

void ff_refference()
{
    size_t v1 = 42;// 局部变量
    auto f = [&v1] { return ++v1; };
    v1 = 0;
    cout << f() << endl; // 输出1
}

void lambda_returntype(vector<string> &words)
{
    std::transform(words.begin(), words.end(), words.begin(), [](int i) { return i < 0 ? -i : i; });
}

void lambda_returntype_error(vector<string> &words)
{
    std::transform(words.begin(), words.end(), words.begin(), [](int i)
                                                                { if (i < 0) return -i; else return i; });

    auto lambda1 = [](int i) {
        if (i < 0)
            return -i;
         else
            return i;
    };
}