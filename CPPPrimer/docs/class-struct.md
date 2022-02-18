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

## 类的最佳实践

**建议通过构造函数给成员设置初始值**；这里有两方面的原因：

1. 效率问题；前者初始化类的实例就已经对成员进行初始化，而通过赋值操作，是要先进行初始化再赋值。
2. 避免因成员初始化顺序导致的意料之外的编译错误；因为时常会有引用那些需要的成员信息，但是却没有及时初始化。

成员初始化的顺序是与成员在类中的摆放顺序有关的。第一个成员先被初始化，然后是第二个...。

**如果类中定义了其它构造函数，那么最好跟着定义个默认的构造函数**

## 委托构造函数

相当于C#构造函数的

```c#
public class Far {
	public Far() : this("empty", 28, "private address") { }
	public Far(string name, int age, string address) {...}
}
```

而c++的写法几乎一样

```c++
class Far {
public:
	// 非委托构造函数使用对应的实参初始化成员
	Far(std::string s, unsigned cnt, double price):
		bookNo(s), units_sold(cnt), revenue(cnt * price) { }
	// 其余的构造函数都委托给另一个构造函数
	Far(): Far("", 0, 0) {}
	Far(std::string s): Far(s, 0, 0) {}
	Far(std::istream &is): Far() { read(is, *this); }
	...
}
```

## 类型转换

C++支持*一步转换*。即允许由类型A转向B，不允许A直接转向C，而是先通过A转换B类型，然后由B类型转C类型。

如下转换方式

```c++
string null_book = "9-999-99999-9";
item.combine(null_book);
```

上面这种方式是合法的，`null_book`对象会隐士转换成`Sales_data`类型对象，并将`bookNo`设置为"9-999-99999-9"，`units_sold`以及`revenue`都设置为默认值0。

但是下面这种方式就是错误的：

```c++
item.combine("9-999-9999-9");
```

这种就不能由字符串直接转换成`Sales_data`对象了。而只能通过下面的方式转换：

```c++
item.combine(string("9-999-99999-9"));
//或者
item.combine(Sales_data("9-999-99999-9"));
```

> 这种隐式转换其实就是调用了对应参数类型的构造函数。
>
> 关于如何抑制这种隐式转换：给构造函数添加`explicit`：
>
> ```c++
> explicit Sales_data(const std::string &s): bookNo(s) { }
> explicit Sales_data(std::istream&);
> ```
>
> 这样设置之后上面的`item.combine(string("9-999-99999-9"))`就会报错。
>
> ⚠️关键字`explicit`只对单个参数的构造函数有效。

### 判断字面量类型

用内置函数`is_literal_type<className>`判断一个类是否是字面量类型：

```c++
struct Data {
    int ival;
    std::string s;
};

int main()
{
	std::cout << std::boolalpha;
	std::cout << std::is_literal_type<Data>::value << std::endl;
	return 0;
}

//output false
```

## 静态成员

静态变量的用法与约束绝大部分与C#是一样的，来说点不一样的。

c++在定义静态成员信息时，可以选择在类内部，也可以在类外部。但是在外部时需要注意，不能重复用关键字`static`定义，因为在类的内部已经用该关键字对其变量进行申明了。

```c++
class Account {
public:
	static void rate(double); // 申明
...
};

// 外部定义
void Account::rate(double newRate) {
	interestRate = newRate;
}
```

### 最佳实践

即使一个常量静态数据成员在类内部被初始化了，通常情况下也要在类的外部定义一下该成员。

```c++
class Example
{
private:
    /* data */
public:
    static constexpr double rate = 6.5;
    static const int vecSize = 20;
    // static std::vector<double> vec; // 不能在括号内指定类内初始化式
    static std::vector<double> vec; 
};

constexpr double Example::rate;
std::vector<double> Example::vec(Example::vecSize);
```

