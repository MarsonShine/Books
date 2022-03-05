# 类型转换

同类型拷贝一样，c++提供了`转换构造函数`和`类型转换运算符`两种操作，这两个操作用在一起就成为了`类类型转换`。

格式：`operator type() const;`

**类型转换函数必须是成员函数**。

```c++
class SmallInt {
public:
    SmallInt(int i = 0) : val(i) {
        if (i < 0 || i > 255)
            throw std::out_of_range("Bad SmallInt value");
    }
    operator int() const { return val; }
private:
    std::size_t val;
};

// 调用
SmallInt sint;
sint = 4; // 调用SmallInt(int)构造函数
sint + 3; // sint隐式转换为int与3计算
```

上述例子定义了两个类型转换：一个是转换构造函数，将int转换为SmallInt类型。另一个是类型转换运算符。将SmallInt转换为int。

如果不想隐式转换，那么就要给类型转换运算符添加`explicit`关键：

```c++
explicit int() const { return val; }
```

这个时候再执行`sint+3`就会报错：无法进行隐式的类型转换。只能通过显式类型转换

```c++
static_cast<int>(sint) + 3;
```

这个时候我们就很好理解，判断输入流内容是否有效的这种写法了：

```c++
cin << i
if (cin) {
	// 输入有效
	...
}
```

类型转换运算符刚好也说明了为什么cin输入流为什么可以隐式的转换为bool类型：

```c++
// istream
operator bool() const {return __ok_;}
```

## 函数匹配与重载运算符

现在有如下这种情况：

```c++
class SmallInt
{
    friend SmallInt operator+(const SmallInt&, const SmallInt &);
private:
    /* data */
    std::size_t val;
public:
    SmallInt(int i = 0) : val(i) {};
    ~SmallInt();

    operator int() const { return val; }
};
```

该类有一个隐式的int转换为SmallInt的类型转换构造函数，也重载了操作运算符`+`。现在进行如下调用

```c++
SmallInt s1, s2;
SmallInt s3 = s1 + s2;
int i = s3 + 0; // 二义性
```

结果就是编译器会提示二义性错误。这里面涉及内置类型的运算符和自定义类重载的运算符重叠，那么为什么编译器为什么不知道到底调用的是SmallInt的+重载符还是int类型的内置+操作运算符。

`SmallInt s3 = s1 + s2`这里很明显，`s1`和`s2`都是SmallInt类型，所以调用的就是其成员函数重载运算符。

`int i = s3 + 0`;这里由于`SmallInt`还提供了隐式类型转换操作符，所以`s3`可以隐式转换`int`，所以这里可以调int的运算操作符。但是又因为0可以隐式转换为`SmallInt`，所以也可以调`SmallInt`的成员函数重载运算符。这里就产生了二义性了。
