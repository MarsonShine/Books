#include <memory>
#include <string>
#include <iostream>

int main()
{
    // 定义数组
    int *arry = new int[10];
    // 释放动态数据
    delete [] arry; // [] 是必须的

    // 智能指针+动态数组
    std::unique_ptr<int[]> arrayPtr(new int[10]);
    // 直接下标访问
    auto ele = arrayPtr[0];
    // 自动销毁
    arrayPtr.release(); // 调用release方法就会自动用delete [] 销毁指针

    std::allocator<std::string> alloc_string;
    int n = 100;
    auto const p = alloc_string.allocate(n); // 分配n个未初始化的string
    // 按需分配(初始化)对象
    auto q = p; // q指向最后构造的元素之后的位置
    alloc_string.construct(q++, 10, 'a'); // 初始化对象并自增指针位置
    alloc_string.construct(q++, "bbb");

    std::cout << *p << std::endl;
    std::cout << *q << std::endl;

    // 销毁元素（但没有回收释放内存）
    while (p != q)
    {
        alloc_string.destroy(q--);
    }
    // 释放内存地址
    alloc_string.deallocate(p, n);
}