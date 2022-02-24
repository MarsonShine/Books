// 使用流迭代器读取文件，存入vector中
#include <iostream>
#include <fstream>
#include <vector>
#include <string>
#include <iterator>
using std::vector; using std::string; 

void v1() {
    std::ifstream ifs("./tmp/book.txt");
    std::istream_iterator<string> in(ifs), eof; // 绑定文件流
    vector<string> vec;
    std::copy(in, eof, std::back_inserter(vec));

    // output
    std::copy(vec.cbegin(), vec.cend(), std::ostream_iterator<string>(std::cout, "\n"));
}

void v2() {
    std::ifstream ifs("./tmp/book.txt");
    std::istream_iterator<string> in(ifs), eof; // 绑定文件流
    vector<string> vec;

    while (in != eof)
    {
        vec.push_back(*in++);
    }
    
    // 循环打印
    for (auto &i : vec)
    {
        std::cout << i << std::endl;
    }
}

int main()
{
    // return 0;
    v2();
}