# C# 中 GetHashCode 设置规则

## GetHashCode 是用来干什么的

只干一件事：将对象放置到一个哈希表中。

## 为什么所有 Object 对象第一个方法就是这个

类型系统中的每个对象都应该提供一个 GetType 方法，这是非常有意义的。在 CLR 类型系统中一个关键的特征就是数据是具有自我描述的能力。以及所有对象的 ToString 方法都是有意义的，这样就能将自己以字符串的形式输出，这在调试是很有用的。它可以用来比较对象之间的相等，这看起来是合理的。但是为什么每个对象都能哈希自身将其插入到哈希表中呢？还是每个对象都是。

如果我们今天从头开始重新设计类型系统，哈希表可能会有所不同，也许会用一个 IHashable 接口。但是，在设计 CLR 类型系统时，**还没有泛型类型**，因此需要一个通用哈希表来存储任何对象。

## 哈希表和类似的数据结构如何使用 GetHashCode

思考一个 Set 抽象数据类型。我们可能要在它上面进行很多操作，如两个基本的操作，一个是插入新项至 Set，另一个操作是检查目标项在集合中是否存在。我们想当集合数据量很大的时候也要能够快速查询。我们用一个例子来实现一下：

```c#
class Set {
	private List list = new List();
	public void Insert(T item)
    {
    	if (!Contains(t))
      	    list.Add(item);
    }
    
    public bool Contains(T item)
    {
        foreach(T member in list)
          if (member.Equals(item))
            return true;
        return false;
    }
}
```

这里省略了所有错误检查，我们假设它是不为 null 的。我们可能还要实现一些接口。这里简化了一部分，让我们专注于哈希部分。

包容测试是线性的；如果列表中有一万个元素，那么我们必须查看这一万个元素是否在列表中。这不能很好地扩展。

可以用空间换取时间。解决办法是用多个简短的小集合，称为 "buckets"，还要快速算出我们查找的项在哪个 buckets 中：

```c#
class Set
{
    private List[] buckets = new List[100];
    public void Insert(T item)
    {
        int bucket = GetBucket(item.GetHashCode());
        if (Contains(item, bucket))
          return;
        if (buckets[bucket] == null)
        buckets[bucket] = new List();
        buckets[bucket].Add(item);
    } 
    public bool Contains(T item)
    {
    	return Contains(item, GetBucket(item.GetHashCode());
    }
    private int GetBucket(int hashcode)
    {
        unchecked
        {
            // hash code 可以是负数，所以对应的余数也能是负数
            // 使用无符号整数进行计算，以确保我们保持正整数
            return (int)((uint)hashcode % (uint)buckets.Length);
        }
    }
    private bool Contains(T item, int bucket)
    {
        if (buckets[bucket] != null)
        foreach(T member in buckets[bucket])
          if (member.Equals(item))
            return true;
        return false;
    }
}
```

现在在集合中有上千个项，我们通过上百个 bucket 查询，每个有上百个项。Containers 操作只需要遍历百次即可。

emmmm，平均上。希望如此。

我们还可以做的更聪明；只需要当它填满的时候重新自行拓张，bucket 也会很好的自我调整，来确保 bucket 长度在平均值以内。此外，出于技术原因，**通常最好将 bucket 的长度设置为素数**，而不是100。我们可以对哈希表做很多改进。但是这种的哈希表的实现大致就可以了，保持简单就行。

从这段代码工作的位置开始，我们可以推断出 GetHashCode 的规则和指导原则：

### **规则：相同的项具有相等的散列值**

如果两个对象相等，那么它们必定会有相同的哈希码；或者说如果两个对象不同，那么其哈希码必定不同。

理由很简单。假设两个相同的对象有不同的哈希码，如果你将第一个对象插入至 set 中，然后接着插入第二个项。如果你询问 set 第二个项是否存在于集合中，按照上面的查找方式是查不到的。

注意，如果两个对象具有相同的哈希码，则它们必须相等，这并不是一个规则。只有大约 40 亿个可能的哈希码，但显然有超过 40 亿个可能的对象。仅 10 个字符的字符串就超过了 40 亿个。因此根据鸽子洞原理（Pigeonhole Principle）：如果你有 40 亿个鸽子洞可以容纳超过40 亿个鸽子，那么至少一个鸽子洞有两只鸽子，至少有两个不相等的对象共享相同的哈希码。

### **准则：GetHashCode 返回的整型永远不能改变**

理想情况下，可变对象的哈希码应该仅**从不能变化的字段计算**，因此对象的哈希值在其整个生命周期内是相同的。

然而，这只是理想化的指导方针；实际规则是：

### 规则：当对象包含在依赖于散列代码保持稳定的数据结构中时，GetHashCode 返回的整型必须是不变的

让一个对象的 hashcode 值能根据对象的可变字段而可变是允许的，尽管这样有风险。如果你有一个对象并且插入到哈希表中，可变的对象的哈希码以及维护在哈希表中的哈希码也是要求在可协商的来确保在哈希表中的对象是不可变的。这个协议怎么样是取决于自己的。

如果对象的哈希码在哈希表中是可变得，那么很显然在 Containes 方法就会停止工作。根据上面得代码显式，你 Insert 一个值，你改变了这个项，然后你要查询它是否存在于集合中，那么它就会找不到。

记住，对象可以以你意想不到得方式插入哈希表中。很多 LINQ 序列查询操作内部就使用了哈希表。在迭代 LINQ 查询返回操作期间不要修改对象，永远也不要这么做。

### 规则：消费 GetHashCode 的时间不应过长或跨程序域

假设你有一个 Customer 对象，有 `Name`，`Address` 等字段。如果你想在两个不同的进程中在两个对象提取相同的数据，它们返回的哈希码是不同的。也就是说同一个程序我假设在今天启动，然后关闭明天启动，同一个对象返回的哈希码是有可能不同的。如不同的 CLR 版本对相同字符串的哈希码也有可能是不一样的。可以详见 [String.GetHashCode](https://docs.microsoft.com/en-us/dotnet/api/system.string.gethashcode?redirectedfrom=MSDN&view=net-5.0#System_String_GetHashCode)

### 规则：GetHashCode 永远不要抛出异常，必须要返回

获取哈希代码只是计算一个整数；没有理由会失败。GetHashCode 的实现应该能够处理对象的任何合法配置。偶尔会得到这样的回应：“但是我想在我的GetHashCode 中抛出 NotImplementedException，以确保对象永远不会被放入哈希表;我不希望这个对象被放到哈希表中”。OK，但是前一条准则的最后一句话适用；这意味着，由于性能原因，您的对象不能成为许多 LINQ-to-objects 查询的结果，这些查询在内部使用哈希表。

因为它不会抛出异常，所以最终它必须返回一个值。将 GetHashCode 实现到无限循环中既不合法也不明智。

当对可能被递归定义并包含循环引用的对象进行散列时，这一点尤其重要。如果对 Alpha 对象的属性 Beta 值进行哈希，哈希 Beta 之后反过来在哈希 Alpha，然后就这么永远循环(如果您使用的架构可以优化尾部调用)，要么耗尽堆栈并使进程崩溃。

### 准则：GetHashCode 的实现要非常快

GetHashCode 的关键在于优化查找操作;如果对 GetHashCode 的调用比一次查看这一万个条目要慢，那么您并没有获得性能上的提高。

### 准则：GetHashCode 的实现必须是高性能的

不单指执行速度，分配的内存也要要求尽可能地小

### 准则：哈希码的分布必须是“随机的”

特别要注意 “xor”。通过异或将哈希码组合在一起是很常见的，但这并不一定是一件好事。假设你有一个送货地址和家庭地址字符串的一个数据结构。在单独的字符串进行哈希是很好的，如果把它们两个字符串在相同的时候，哈希码在异或的时候就会得到 0。所以**当数据结构中存在冗余时，“异或” 可能会造成或加剧分布问题。**

# 原文地址

https://ericlippert.com/2011/02/28/guidelines-and-rules-for-gethashcode/