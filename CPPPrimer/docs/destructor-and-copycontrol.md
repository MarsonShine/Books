# 析构器与拷贝控制

## 析构器

析构函数负责释放类内的非static类型的资源。

析构函数在触发运行的时候，可以先触发函数体，最后按照属性的顺序依次释放。当一个对象被使用最后一次时，就会触发析构函数进行清理工作。如果用户自己没有定义析构函数，那么编译器就会自动合成一个析构函数,这个函数体为空，被称为”合成析构函数“。<font color="red">合成析构函数还是能正常（隐式）进行成员信息的销毁。</font>

```c++
class Sales_data
{
public:
	~Sales_data(){ }
}
```

上面就是一个合成析构函数的定义，在对象最后一次调用结束后，就会触发析构函数，就会按照属性的定义顺序依次销毁。

**析构函数自身并不直接销毁成员对象。成员是在调用析构函数体之后有个隐式的析构阶段，在这个阶段会对这些对象进行销毁。**

> 最佳实践：
>
> 当一个类类型需要定义析构函数时，最好连拷贝构造函数和拷贝赋值构造函数也一起定义。因为如果没有定义这两个函数，编译器就会自动生成合成拷贝构造函数和拷贝赋值构造函数。那么这样就相当于允许多个对象指向相同的内存。这个共享的内存被释放时，这多个对象就会因为析构函数全都要释放，这样就会发生多次释放同一个内存对象，就会发生系统错误：释放已经释放的内存

### 练习题

下面这段代码总共发生了几次析构函数

```c++
bool bcn(const Sales_data &trans, const Sales_data accum)
{
		Sales_data item1(*trans), item2(accum);
		return item1.isbn() != item2.isbn();
}
```

这里面其实有三次，item1、item2、accum。

我们其实可以通过在初始化和析构函数中输出响应的内容来看程序运行过程中构造函数和析构函数调用情况

```c++
struct X
{
    X() {std::cout << "X()" << std::endl; }
    X(const X&) { std::cout << "X(const X&)" << std::endl; }
    X& operator=(const X&) { std::cout << "X& operator=(const X&)" << std::endl; return *this; }
    ~X() {std::cout << "~X()" << std::endl; }
};

void f(const X &rx, X x) // X x 一次初始化 ～ 一次析构
{
    std::vector<X> vec;
    vec.reserve(2);
    vec.push_back(rx); // 初始化 ～ 析构
    vec.push_back(x); // 初始化 ～ 析构
}

int main() {
    X *x = new X;// 默认构造函数
    f(*x, *x); // f 函数结束，x rx x 析构
    delete x; // *x 析构

    return 0;
}
```

从启动到函数结束，共发生四次构造函数调用，四次析构函数调用。我用注释标记起来了。

<font color="red">要注意，const 标记的成员对象是不会发生构造函数，这是发生的数据的复制操作，而第二个参数是值，所以会发生数据的复制，即隐式调用了带参构造函数。</font>

## 阻止拷贝

我们前面了解到流`iostream`类实现了阻止拷贝的功能，其目的是为了阻止多个流对象拥有相同的IO缓冲。那么如何做到呢？

就是将拷贝构造函数和拷贝赋值构造函数定义为了**删除函数（deleted function）**。

删除函数：定义了该函数，但是无法使用它。

```c++
struct NoCopy
{
    NoCopy() = default;
    NoCopy(const NoCopy&) = delete;
    NoCopy& operator=(const NoCopy&) = delete;
};
```

这样通过赋值给`delete`关键字，就表示禁止调用拷贝构造函数和拷贝赋值构造函数。

## 数据交换

我们再给两个对象进行数据交换时，总共会发生三次操作：两次拷贝，一次赋值：

```c++
IntPtr temp = v1;
v1 = v2;
v2 = temp;
```

其实我们还可以通过直接交换指针指向的内存地址即可，不需要进行数据交互，效率会比上面的方式高

```c++
int *temp = v1.ps;
v1.ps = v2.ps;
v2.ps = temp;
```

可以定义自己的swap函数

```c++
inline void swap(HasPtr &lh, HasPtr &rh)
{
    using std::swap;
    swap(lh.ps, rh.ps);
    swap(lh.i, rh.i);
}

int main() {
		std::swap; // 优先匹配类型一致的比较函数，没有匹配就会取标准库中的swap
		HasPtr a;
    HasPtr b;
    swap(a, b);
}
```

## 数据移动

右值引用`&&`:必须绑定到右值的引用；

左值引用:非右值引用的常规引用；