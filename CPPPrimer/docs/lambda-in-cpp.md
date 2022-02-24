# Lambda表达式

一个Lambda表达式表示一个可调用的执行单元。可以理解为未命名的**内联函数**。

格式为`[capture list] (parameter list) -> return type { function body }`

- capture list, 捕获列表，是lambda所在函数中定义的局部变量的列表
- return type, 返回类型
- function body, lambda函数体
- parameter list, 参数列表

```c++
auto f = [] { return 42; };
// 调用lambda
f() // 返回42
```

也可以直接传递匿名lambda函数体：

```c++
stable_sort(vcs.begin(), vcs.end(), [](const string &s1, const string &s2) 
{ return s1.size() < s2.size(); });
```

> 如果要想在lambda函数体中引用它所在函数的局部变量，这个时候就需要在捕捉列表(capture list)中列出要引用的局部变量。

## for_each

```c++
for_each(wc, words.begin(), [](const string &s) { cout << s << " "; });
cout << endl;
```

隐式与显式捕获参数

隐式捕获分为两种，值捕获和引用捕获。

值捕获在捕获列表(capture list)中输入`=`

```c++
// 值捕获
wc = find_if(words.begin(), words.end(), 
						[=](const string &s) { return s.size() >= sz; });
```

引用捕获在捕获列表(capture list)中输入`&`

```c++
for_each(words.begin(), words.end(), 
				[&, c](const string &s) { os << s << c; });

// 显式捕获和隐式捕获混用
for_each(words.begin(), words.end(),
				[=, &os](const string &s) { return os << s << c; });
```

##

## 可变lambda以及显示指定

## 返回值

在定义lambda表达式时，传递的参数都是值拷贝的，所以一般是不允许改变其值的

```c++
void ff() 
{
		size_t v1 = 42;
		auto f = [v1]() { return ++v1; }; // error: 表达式必须是可修改的左值
		v1 = 0;
		auto j = f();
		cout << j << endl;
}
```

只有在函数体前增加关键字`mutable`即可：

```c++
void ff() 
{
		size_t v1 = 42;
		auto f = [v1]() mutable { return ++v1; }; // error: 表达式必须是可修改的左值
		v1 = 0;
		auto j = f();
		cout << j << endl;
}
```

### 指定lambda返回类型

前面提到的所有lambda表达式都没有指定返回返回值的，这是因为表达式隐式指定了返回值类型。<font color="orange">默认情况下，如果一个lambda表达式包含`return`之外的任何语句，则编译器就会当做返回`void`</font>。

```c++
transform(vi.begin(), vi.end(), vi.begin(), [](int i) { return i < 0 ? -i : i; });
```

上面这个例子就是隐式推测出返回的是`int`类型。~~但是如果是下面的例子就会提示编译错误~~：

```c++
transform(vi.begin(), vi.end(), vi.begin(), [](int i) { if (i < 0) return -i; else return i; });
// 显式指定返回类型
transform(vi.begin(), vi.end(), vi.begin(), [](int i) -> int { if (i < 0) return -i; else return i; });
```

> ⚠️
>
> 上述lambda表达式函数体在我自己的vscode编辑器里是可以编译通过的。
>
> 看样子应该是现代编译器解决了书中描述的这个问题。

### 类Javascript的函数参数绑定bind

```c++
auto newCallable = bind(callable, arg_list);
```

简单的理解就是将函数`callable`以及函数参数列表`arg_list`绑定给新的`newCallable`函数变量。调用`newCallable`实际上就是调用的`callable`。

```
auto check6 = bind(check_size, _1, 6);
string s = "hello";
bool b1 = check6(s); // 等同于调用 check_size(s, 6);
```

上面的例子`_1`是一个占位符，表示`check6`的第一个参数。

```c++
auto wc = find_if(words.begin(), words.end(), [sz](const string &a) { ... });
// bind版本
auto wc = find_if(words.begin(), words.end(), bind(check_size, _1, sz));
```

bind占位符`_n`，这个表示参数的调用位置。如：

```c++
auto g = bind(f, a, b, _2, c, _1);
```

上述定义表示g这个可调用对象有两个参数，即`g(x, y)`。而x就表示占据`_1`的位置，y就表示占据`_2`的位置。等价于调用f函数的五个参数：`f(a, b, y, c, x)`。

上面这事bind的使用都是针对值的，如果针对引用的话就不能这么写。需要借助`ref`函数，以调用之前的`print`函数为例：

```c++
ostream &print(ostream &os, const string &s, char c)
{
		return os << s << c;
}

// 以下调用是错误的
for_each(words.begin(), words.end(), bind(print, os, _1, ' ')); //error,os是引用对象
// 正确用法
for_each(words.begin(), words.end(), bind(print, ref(os), _1, ' '));
```

## 深入理解Lambda

当定一个lambda表达式时，编译器会自动生成一个与lambda对应新的类类型，该类型还有一些对象：就是定义lambda是那些参数。

//TODO

