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

## 泛型算法-插入迭代器

在容器中写入元素，可以用`fill，fill_n`函数

```c++
fill(vec.begin(), vec.end(), 0); // 将每个元素重置为0
fill(vec.begin(), vec.begin() + vec.size()/2, 10); // 指定vec指定的子序列重置为10
fill_n(vec.begin(), vec.size(), 0);
```

要注意，在调用`fill_n`函数时，目标容器一定要初始化；即容器中要有元素，如下面的调用就会报错

```c++
vector<int> vec; // 空vector
fill(vec.begin(), 10, 0); // 修改vec中的10个元素；报错，vec中不存在数据
```

### 插入迭代器——back_inserter

```
vector<int> vec; // 空vector
auto it = back_inserter(vec);// 通过 it 的复制将值插入到容器vec中
*it = 10; // vec插入了元素10

// 添加10个元素
fill_n(back_inserter(vec), 10, 0);
```

### 排序与去重

给定一个程序，进行排序与去重

```c++
void elimDups(vector<string> &words)
{
    // 按字典排序
    std::sort(words.begin(), words.end());
    // 出现重复内容的
    auto end_unique = std::unique(words.begin(), words.end());
    // 删除重复元素
    words.erase(end_unique, words.end());
}

int main()
{
    vector<string> vcs{"the","quick","red","fox","jumps","over","the","slow","red","turple"};
    elimDups(vcs);
    return 0;
}
```

1. 首先生成原始序列：`"the","quick","red","fox","jumps","over","the","slow","red","turple"`

2. 经过调用排序函数，序列变成`"fox","jumps","over","quick","red","red","slow","the","the","turple"`

3. 调用unique去重，序列变成"fox","jumps","over","quick","red","slow","the","turple",<font color="red">"the",""</font>;而此时，unique返回的迭代器指针就是指向`turple`后一个位置。

   > 注意调用unique其实并没有删除重复的元素，只是覆盖相邻的值。所以我们能看到第三步标红的值的变化

4. 调用erase真正的删除元素，可以发现容器集合size变小了

### 定制函数——自定义排序

c++内置的排序默认是使用元素类型的`<`运算符。如果我们需要其他排序方式（如按照`>`或未定义类型`<`运算符）呢。

我们可以通过传递一个**排序函数**作为参数，如以上面的以字符的字典排序的例子，现在我们改为以字符串长度排序，首先定一个比较函数：

```c++
bool isShorter(const string &s1, const string &s2)
{
		return s1.size() < s2.size();
}
// 调用
std::sort(words.begin(), words.end(), isShorter);
```

如果想保留多种排序规则，则需要用到稳定排序。

> 关于排序算法是否稳定的概念，具体详见：[排序算法](https://github.com/MarsonShine/AlgorithmsLearningStudy/blob/master/docs/Sorts.md)

