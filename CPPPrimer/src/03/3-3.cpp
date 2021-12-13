#include<iostream>
#include<string>
#include<vector>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;
using std::vector;

void init_vector()
{
    vector<string> v1{"a", "an", "the"};
    // 创建指定数量的元素
    vector<int> int_vec(10, -1); //10 个int类型的元素，每个都被初始化为-1
    vector<string> svec(10, "hi!"); //申明10个string的元素，每个元素都初始化为"hi!"
    
}

int main()
{
    init_vector();
    return 0;
}