#include <vector>
using std::vector;

bool contains(vector<int>::const_iterator first, vector<int>::const_iterator last, int value) 
{
    // 遍历
    for (; first != last; ++first)
        if (*first == value) return value; // 通过解引用访问迭代器元素
    return false;
}

int main() {

}