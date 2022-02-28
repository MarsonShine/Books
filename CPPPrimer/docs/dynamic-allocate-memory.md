# 动态内存

一般编写的代码所定义的变量和对象都有严格的生命周期。在程序启动时创建，关闭时释放；进入到定义的程序快时创建，退出当前块时释放。那些static变量都是程序第一次使用时创建，在程序结束时关闭。

除了这些对象之外，还有一些是需要动态分配的内存，不是分配在栈内存上的，而是分配在堆（Heap）中的。这些对象是不受编译器管控的，对象的释放行为是需要我们自己定义的。所以这要求我们对此要非常谨慎，稍微不注意就可能发生对象一直没有释放，内存得不到回收导致内存泄漏；又或是提前回收导致程序空指针的运行时错误。

为此c++提供了智能指针（smart point）类型来帮助我们做这些事情。

c++提供了三种动态分配内存的指针：

- shared_ptr：允许多个指针指向同一个对象，即共享对象；
- unique_ptr：只允许独占对象
- weak_ptr：弱引用，指向的是shared_ptr指针。

动态分配时直接使用这三种指针类型：

```c++
auto sharedObj = shared_ptr<string>("marsonshine");
auto uniqueObj = unique_ptr<string>("summerzhu");
```

最佳初始化的方法就是使用`make_shared`分配内存

```c++
shared_ptr<string> p = make_shared<string>("happyxi");
shared_ptr<string> p2 = make_shared<string>(10, '9'); // 999999999
```

**在用智能指针初始化类型对象时，传的参数就是调用对应的类型的构造函数的参数**。如上述的`make_shared<string>(10, '9')`就是调用的`string(10, '9')`构造函数。

shared_ptr内部还维护了一个计数器变量用来记录引用的数量。在赋值和拷贝的时候都会更新这个计数器。

```
auto p = make_shared<string>("marsonshine");// 此时计数器=1
auto q(p);	// q是p的拷贝，此时计数器=2
```

当给指针赋予新值或是退出当前作用域时，计数器就会递减1。

```c++
auto p = make_shared<string>("marsonshine"); // 计数器=1
p = q; // 
```

上面例子中p指针重新赋值，那么原来指针计数器递减1，q这个对象计数器递增1，如果r原来所指的对象引用数为0就会自动释放。而控制对象的释放是通过析构函数实现的。

指针对象在容器中如果不用了要记得用`erase`删除。

## 分配内存和删除内存分配

关键字`new`完成动态内存的分配：

```c++
int *p = new int;	// 未初始化的指针对象
string *ps = new string; // 初始化为空string
string *ps2 = new string(); // 初始化值为空stirng
int *p2 = new int(); // 初始化值为0
```

关键字`delete`释放内存：

```c++
int i, *pi1 = &i, *pi2 = nullptr;
double *pd = new double(33), *pd2 = pd;
delete i; // delete只能删除指针
delete pi1; // 错误，pi1指向一个为初始化的局部变量
delete pi2; // 正确，删除一个null指针是可以的
delete pd; // 正确
delete pd2; // 错误，pd2指向的内存已经释放
```

`delete pd2`错误的原因是因为pd2指向的指针对象就是pd，而pd已经被释放了，所以无法再次释放已经释放的内存对象。

> ⚠️
>
> 在内存分配的时候，如果发现机器内存已经不够了，这个时候new就会出现`bad_alloc`错误。如果想要不报运行时错误，我们可以更成下面的定义语句：
>
> ```c++
> int *p = new (nothrow) int;
> ```
>
> 上述定义如果内存不够了，就会返回一个空指针。所以我们在调用指针对象时，一定要时刻记得判断指针是否为空指针。

在释放内存对象时，这个指针对象会变成一个无效值的**空悬指针（dangling pointer）**，就是指针指向的地址还在，但是值无效了。这样就无法继续使用该指针了，但是如果想要继续使用的话可以选择将要删除的指针赋值为`nullptr`。

> 最佳实践：delete动态内存对象，最好都要显式赋值给nullptr

