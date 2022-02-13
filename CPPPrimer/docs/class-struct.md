# Class vs Struct

class 和 struct 都可以定义类，但是有一些细微的差别。

首先就是默认访问权限的差别。

**类可以在它的第一个访问说明符之前定义成员，对于这种成员的访问权限依赖于类定义的方式。**

如果我们是用`struct`关键字，则定义在第一个访问说明符之前的成员是 public 的；相反，如果我们是用的`class`关键字，则这些成员是 private 的。

**所以为了统一编程规范，当我们希望定义类的所有成员都是 public 时，使用 struct；反之，如果希望定义类的所有成员都是 private 时，使用 class。**

## 封装

封装是实现与接口的分离。它隐藏了类型的实现细节。(在c++中，封装是通过将实现放在类的私有部分来实现的)。

这种做法的优势：

- 用户代码不会无意的破坏封装好的对象信息（最大限定的保护封装对象）
- 封装类的实现可以随着时间的推移而改变，而不需要改变用户级代码。

### 有元函数

友元的定义：友元是允许访问其非public成员的一种机制。它与类成员是一样的。

像下面这样的非成员函数接口申明是无法正常编译的

```c++
class Sales_data {
public:
    ...
private:
    double avg_price() const { return units_sold ? revenue/units_sold : 0; }
    std::string bookNo;
    unsigned units_sold = 0;
    double revenue = 0.0;
};
// 非成员函数,接口申明
Sales_data add(const Sales_data&,const Sales_data&);
std::istream &read(std::istream&, Sales_data&);
std::ostream &print(std::ostream&, const Sales_data&);
```

由于`add`、`read`、`print`函数虽然是类的接口的一部分，但他们不是类的成员。

那么该如何做才能让程序正常编译呢？

可以通过在类内添加有元函数申明：

```c++
class Sales_data {
// 为Sales_data的非成员函数所做的友元申明
friend Sales_data add(const Sales_data&, const Sales_data&);
friend std::istream &read(std::istream&, Sales_data&);
friend std::ostream &print(std::ostream&, const Sales_data&);
public:
    ...
private:
    double avg_price() const { return units_sold ? revenue/units_sold : 0; }
    std::string bookNo;
    unsigned units_sold = 0;
    double revenue = 0.0;
};
// 非成员函数,接口申明
Sales_data add(const Sales_data&,const Sales_data&);
std::istream &read(std::istream&, Sales_data&);
std::ostream &print(std::ostream&, const Sales_data&);
```

这样设置了`friend`关键字后，`add`、`read`、`print`函数就能访问类内的私有变量了。

> 友元的申明仅仅只是指定了访问权限，不是一个通常意义上的函数申明。
>
> 如果希望类的用户能够调用某个友元函数，那么就必须要在类外对友元函数的申明再进行一次申明。

### 友元的统一规范

通常我们要把友元的申明与类本身放到同一个文件夹，一般是头文件夹中（类的外部）。

### 友元的好处与坏处

#### 好处

- 被调用的函数可以引用类作用域中的类成员，而不需要显式地用类名作为前缀(`Sales_data::member`)。
- 可以方便的访问非public的成员
- 更可读的

#### 坏处

- 丢失了封装带来的好处，降低了可维护性
- 代码冗长，类内部和类外部都要声明

