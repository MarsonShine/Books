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

> map与vector有什么不同？
>
> map是关联容器，vector是顺序容器。

