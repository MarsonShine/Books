#include<iostream>
#include<string>
#include<vector>
using std::begin; using std::end; using std::cout; using std::endl; using std::vector;
// 比较两个vector是否相等
// 比较两个数组是否相等
bool compare(int* const arr_begin, int* const arr_end, int* const arr2_begin, int* const arr2_end) {
    if ((arr_end - arr_begin) != (arr2_end - arr2_begin)) // 判断两个集合长度是否不一样
    {
        return false;
    }
    else {
        for (int* i = arr_begin, *j = arr2_begin; (i != arr_end) && (j != arr2_end); i++,j++)
        {
            if(*i != *j) return false;
        }
    }
    return true;
}

int main()
{
    int arr[] = {0,1,2};
    int arr2[] = {0,1,2};
    if(compare(begin(arr), end(arr), begin(arr2), end(arr2))) {
        cout << "The two arrays are equal." << endl;
    }
    return 0;
}