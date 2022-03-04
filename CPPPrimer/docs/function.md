# 函数调用运算符和function类

我们在[lambda](lambda-in-cpp.md)文章中已经知道，编译器会为lambda生成一个类，并绑定了一个类函数：

```c++
class ShorterString {
public:
	bool operator()(const string &a, const string &b) const { return a.size() < b.size(); }
}
```

里面的`bool operator()(...)`就是重载的**函数调用运算符**。这样就能达到直接调用的效果，与c#的`delegate`如出一辙:

```c++
auto f = ShorterString(); // f 就是函数，接收两个const string 参数，返回类型位 bool
f("aaa", "aaaaaaaaa");
```

根据上面我们可以继续拓展，带参数（状态）的场景

```c++
class PrintString {
public:
	PrintString(ostream &o = cout, char c = ' ') : os(o), separator(c) {}
	bool operator()(const string &a, const string &b) const { return a.size() < b.size(); }
private:
	ostream &os;
	char separator;
}	
```

调用端

```c++
PrintString printer;
printer(s);
PrintString errors(cerr, '\n');
errors(s);
```

## Function

在介绍Function之前，我们先看下面的例子：

```c++
int add(int i, int j) { return i + j; }

auto mod = [](int i, int j){ return i % j; };

struct divide
{
    int operator()(int i, int j) { return i / j; }
};

int main()
{
  	add(1, 2);
  	mod(1, 2);
  	divide()(1, 2);
}
```

这上面的表达式有什么相同与不同呢？

不难发现，这些类和方法和lambda的调用形式都是一致的，即都是接收两个int参数，返回一个int。如果我们想要实现根据不同的表达式（key）来计算相同参数的不同结果要怎么做呢？

通过之前的[容器(collection)](collection.md)学习就知道通过map就很容易的实现：

```c++
map<string, int(*)(int, int)> opts;
opts.insert({"+", add});
opts.insert({"%", mod});
opts.insert({"/", divide()});
// 调用
opts["+"](1, 2);
opts["%"](1, 2);
opts["/"](1, 2);
```

但是实际上编译器会报错，因为`divede()`不是函数指针类型。

这个时候就是`function`类出场的时候了，解决的就是上述这个问题的：

```c++
std::function<int(int, int)> f1 = add;
std::function<int(int, int)> f2 = mod;
std::function<int(int, int)> f3 = divide();
std::function<int(int, int)> f4 = [](int i, int j) -> int { return i * j; };
```

这个时候我们还要注意另一种情况，上面的赋值都是通过函数名字赋值的，那么如果存在函数重载呢？函数名称相同的时候，如果还是象上面那样赋值是不行的，得明确的告诉编译器传递的是具体哪个函数，这个时候我们可以通过传函数指针实现：

```c++
// 二义性，方法重载
std::function<int(int, int)> f1 = add; // error，具体指哪个add函数？
// 函数名字不行，那就用函数指针
int (*fadd)(int, int) = add; // 将整数类型参数的add函数地址赋值给fadd
binOps2.insert({"+", fadd});
// 也可以直接通过lambda，直接显式通过参数，返回类型决定函数
std::map<std::string, std::function<std::string(std::string, std::string)>> binOps3;
```

也可以直接通过lambda明确要传的函数参数是什么

```c++
binOps3.insert({"+", [](std::string a, std::string b) { return add(a, b); }});
```