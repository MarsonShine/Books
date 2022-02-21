# C++集合类

 

| 集合类型     | 描述                                                         |
| ------------ | ------------------------------------------------------------ |
| vector       | 可变大小数组。支持快速随机访问（数组下标）。在尾部插入或删除元素 |
| deque        | 双端队列。支持快速随机访问。在头尾位置添加/删除元素          |
| list         | 双向链表。只支持双向的顺序访问。在任何位置都能添加/插入元素  |
| forward_list | 单向链表。只支持单向顺序访问。在链表的任何位置都能添加/插入元素 |
| array        | 固定大小数组。支持快速随机访问（数组下标）。不能添加或删除元素 |
| string       | 与vector容器相似，但专用于保存字符。在尾部插入/删除元素      |

具体选择哪个容器是根据具体的需求来的。只到上面的容器的数据结构特征，就能因地制宜的选择合适的数据结构。

在大多数情况下，使用vector是没问题的。

> 如果不确定使用哪个容器，那么也可以一律使用 vector 和 list 公共的操作：使用迭代器，不使用下标随机访问。

## 集合迭代

迭代一个集合，需要两个迭代器`begin`、`end`。它们引用同一个容器的元素，可以自增长`begin`到达`end`。这说明已经遍历完了。

例子：遍历一个`vector<int>`对象：

```c++
bool contains(vector<int>::const_iterator first, vector<int>::const_iterator last, int value) 
{
    // 遍历
    for (; first != last; ++first)
        if (*first == value) return value; // 通过解引用访问迭代器元素
    return false;
}

int main()
{
    vector<int> cs = init;
    contains(cs.begin(), cs.end(), 5);
}
```

`begin`,`end`迭代器还有多个版本：

- `cbegin`,`cend`表示常数迭代器，迭代的元素无法修改
- `crbegin`,`crend`表示常数反转迭代器

## 初始化容器

有两种方式创建/初始化容器：

1. 直接拷贝：初始化新容器时，传递已有的容器对象。这样方式必须要满足<font color="red">容器类型和元素类型一致</font>。
2. 范围拷贝：初始化容器时，传递已有容器的迭代器（begin、end）。这种方式不需要容器类型和元素类型一致，只要能将<font color="red">拷贝的元素转换为目标容器元素类型</font>即可。

看如下例子：

```c++
list<string> authors = { "marsonshine", "summerzhu", "happyxi" };
vector<const char*> articles = { "a", "b", "c" };
// 直接拷贝
list<string> list2(authers); // 正确，容器类型和元素类型一致
deque<string> authList(authers); // 错误，容器类型不一致
vector<string> words(articles); // 错误，元素类型不一致
// 范围拷贝
forward_list<string> words(articles.begin(), articles.end()); // 正确，char*元素类型可以转换为目标容器元素类型string
```

容器还有一个构造函数重载，传递begin以及目标元素值，表明从第一个元素一直拷贝目标元素值（不包括）。

```c++
deque<string> authList(authors.begin(), it);	// 拷贝元素，直到（不包括）元素 it
```

## 交换元素swap

容器之间可以进行元素交换（swap）：

```c++
vector<string> svec1(10);
vector<string> svec2(24);
swap(svec1, svec2); // 交换svec1和svec2之间的元素
```

> ⚠️swap作为交换容器内容的操作，叫交换过程中实际上不会对元素进行拷贝，而是直接将容器的内部数据结构进行交换。
>
> 所以swap操作元素本身不会交换，操作效率非常高。array除外，swap会交换元素，与array里的内容成线性比。

## 容器的比较

C++容器的比较都支持比较运算符，即`==,>,<,!=`。

但我个人认为这种比较符出了`==`符合人类直觉，其他的都不符合，并且我也没想到有什么场景需要用到这些比较运算符。

## 高效字符串操作

每次读取一个字符存入一个`string`中，而且知道最少需要读取100个字符，应该如何高效的操作？

因为已经知道了尺寸大小，所以我们可以在初始化字符串时，可以申明一个capacity容量，这样能避免前期不必要的字符拷贝与内存占用（resize）

```c++
string s1;
s1.reserve(120);
cout << s1.capacity() << endl << s1.size() << endl;
```

## 容器适配器

C++标准库定义了三个容器适配器：`stack`,`queue`,`priority_queue`，它们都接收顺序容器来封装成三种容器适配器。
