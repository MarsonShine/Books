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



## 深入理解Lambda

当定一个lambda表达式时，编译器会自动生成一个与lambda对应新的类类型，该类型还有一些对象：就是定义lambda是那些参数。

//TODO