# 关联容器

简而言之就是C#中的字典集(`Dictionary`)和哈希集(`HashSet`)。是通过key来访问值的。关联容器有很多内置的容器类型，如下表列出来的类型。

| 容器类型           | 描述                               |
| ------------------ | ---------------------------------- |
| map                | key-value键值对存储                |
| set                | key存储，不允许重复                |
| multimap           | key可重复的map                     |
| multiset           | key可重复的set                     |
| unordered_map      | 哈希函数组成的map，无序            |
| unordered_set      | 哈希函数组成的set，无序            |
| unordered_multimap | key可重复的哈希函数组成的map，无序 |
| unordered_multiset | key可重复的哈希函数组成的set，无序 |

其中用到最多的就是`map`和`set`。

## map

单词出现重复次数的例子：

```c++
map<string, size_t> word_count;
string word;
while (cin >> word) {
	++word_count[word];
}
for (const auto &w : word_count)
	cout << w.first << " occurs " << w.second << ((w.second - 1) ? " times" : " time") << endl;
```

其中word就作为map的key，`size_t`在map定义的时候就会默认初始化0，所以`++word_count[key]`就是key映射的value。

初始化操作

```c++
map<string, string> ms = {
    {"Marsonshine", "Shine"},
    {"Summer", "Zhu"},
    {"Happy", "Xi"}
};
```

## set

结合上面的例子进行拓展，在统计单词出现的重复次数时，再加一个指定忽略的单词列表：只要是在这个单词列表中，就不要统计。

```c++
map<string, size_t> word_count;
set<string> ignore_words = { "The", "But", "And", "Or", "An", "A", 
														 "the", "But", "and", "or", "an", "a"} // 当然我们也可以直接用标准函数tolower来达到忽略大小写的目的
string word;
while (cin >> word) {
		if (ignore_words.find(word) == ignore_words.end()) // 不在ignore_words中，则统计
				++word_count[word];
}
...
```

初始化：`set<string> s = { "Marsonshine", "Summerzhu", "HappyXi"}`
> map与vector有什么不同？
>
> map是关联容器，vector是顺序容器。

## 自定义类类型key

关于自定义类类型，如`map<Person, Person>`。这种需要对类提供操作运算符。如果没有定义，c++也可以让用户自己定义的比较函数，如`sort`函数的第三个参数一样。

如果类`Person`中没有重载比较运算符，则可以通过比较函数方式

```
bool isOlder(const Person &p1, const Person &p2)
{
		return p2.age < p1.age;
}
```

## 其它

我们现在已经知道multiset/multimap是允许存储重复的key的。<font color="red">那么在存储相同的key的同时还能保证key有序</font>。我们可以通过传递比较函数来实现这一点

```c++
std::multiset<Person, decltype(isOlder)*> ps(isOlder);
```

还有一个类型初始化和结构方式看起来跟map是一样的——`pair`。

为什么说是看起来一样，因为pair内部构造与map完全不一样。pair内部就是两个范型的属性`first,second`，分别对应`pair<T1,T2>`的T1,T2。而在取值的时候，就能看出明显的区别了，也直接说明了内部结构完全不一样。

```c++
pair<string, int> p{"marsonshine", 15};
// 取值
std::cout << "第一个参数：first = " << p.first << std::endl;
std::cout << "第二个参数：second = " << p.second << std::endl;
```

跟c#的Tuple<T>是不是很相似

------

给map添加值的时候，主要注意，插入的值实际上是`pair`对象:

```c++
// 插入map
maps.insert({"summer zhu", 26});
maps.insert(pair<string, int>("marsonshine", 28));
maps.insert(std::make_pair("happy xi", 1));
maps.insert(map<string, int>::value_type("family", 4));
```

> ⚠️
>
> 值得注意并且有趣的是，map的下标访问和遍历解引用得到的值类型是不一样的。map下标访问得到的是value_type类型，而迭代解引用得到的是pair类型。
>
> 以map<string, int>为例，下标访问返回的是int类型，迭代解引用返回的是`pair<string, int>`类型；

## 一个map示例

给定一个string，将其转换为另一个string。程序输入两个文件，第一个文件是转换规则，第二个文件是转换内容。每次转换内容时都要应用这个规则。如单词转换文件内容是：

```
// 规则
brb be right back
k okay?
y way
r are
u you
pic picture
thk thanks,
18r later
```

转换的内容：

```
where r u
y dont u send me a pic
k thk 18r
```

程序输出：

```
where are you
why dont you send me a picture
okay? thanks later
```

