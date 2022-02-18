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

