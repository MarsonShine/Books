# 函数指针

指针可以指向函数，这就是函数指针。函数指针提供了非常强大的存储和向代码传递引用的功能。

如果现在有一个函数定义如下：

```
int fun(int x, int* p);
```

然后我们可以声明一个指针 fp，并赋值为上面这个函数：

```c
int (*fp)(int, int*);
fp = fun;
```

`(*f)` 这表明 f 是一个指针；`(*f)(int*)` 这表明 f 是一个指向函数的指针，这个函数以 int* 作为参数。最外围的 `int` 表示函数返回的类型。要注意，`(*fp)` 这个括号是必须要的，这样才能识别出这是函数指针，如果去掉括号则变成了申明了 `int* f(int*)` 这么一个函数，输入参数为 `int*`，返回参数为 `int*`。