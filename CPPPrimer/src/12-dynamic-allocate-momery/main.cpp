#include <memory>

int main()
{
    // int i, *pi1 = &i, *pi2 = nullptr;
    // double *pd = new double(33), *pd2 = pd;
    // // delete i; // delete只能删除指针
    // // delete pi1; // 错误，pi1指向一个为初始化的局部变量
    // delete pi2; // 正确，删除一个null指针是可以的
    // delete pd; // 正确
    // // delete pd2; // 错误，pd2指向的内存已经释放

    // std::shared_ptr<int> p1 = new int(); // error
    std::shared_ptr<int> p(new int(100)); // 必须强制直接初始化

    int *q = p.get();
    {
        std::shared_ptr<int>(p); // 复制
    }
    int foo = *p;

    std::shared_ptr<int> sp;
    sp.reset(new int(2048));
}