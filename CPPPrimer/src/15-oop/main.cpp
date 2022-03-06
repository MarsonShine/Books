#include "Bulk_quote.h"
// #include "Disc_quote.h"



int main()
{
    Quote item;
    Bulk_quote bulk;
    Quote *p = &item;
    // p = &bulk;
    // Quote &r = bulk; // 派生类隐式转换为基类
    
    // Disc_Quote bulk2; // 不允许使用抽象类型定义
    // Quote item(bulk2);
    // 如果禁止别的类集成，类似c#的seale关键字

    // 虚函数执行是动态运行时决定的，因为不知道绑定的参数到底是基类还是派生类。
    // 但是如果我们想要指定类函数，不走动态绑定虚函数机制时，我们可以通过下面方式指定
    // double undiscounted = baseP->Quote::net_price(42); // 强行调用Quote的net_price虚函数。
}