# IO

输出运算符 `<<`：接受两个对象，左边的对象是一个 ostream 对象，右侧就是要打印的值。

输入运算符`>>`：接受两个对象，左边是一个 istream 对象，右侧就是要输入的对象值。

## 引用对象的方法

`std::cout` 以及 `std::cin`：前缀 std::name 表示 cout 是在命名空间为 std 中的。这样就可以防止同样的函数名字一样的冲突问题。

# ASCII 码表
https://www.asciitable.com/

# VSCode 使用 C++11 编译选项

点击已安装的 VSCode C/C++ 拓展项，鼠标右键点击”拓展设置“，找到配置项`C_Cpp.default.compilerArgs`添加如下选项：

```json
{
	"C_Cpp.default.compilerArgs": [
        "-g",
        "${file}",
        "-std=c++11",
        "-o",
        "${fileDirname}/${fileBasenameNoExtension}"
   ]
}
```

## C++编译器

编译器分两步处理类：首先编译成员的申明，然后才轮到成员函数体（如果有的话）。因此，成员函数体可以随意使用类中的其它成员二无需在意这些成员出现的次序。