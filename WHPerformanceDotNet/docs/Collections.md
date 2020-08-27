# 集合的实现与复杂度

- `Dictionary` 字典集，通过哈希表实现，插入和查找的时间复杂度是 `O(1)`
- `SortedDictionary` 有序字典集，是通过二叉查找树来实现的，所以时间复杂度为 O(log n)
- `SortedList` 通过数组排序实现的。O(log n) 的时间复杂度，但是它在插入时的最坏时间复杂度为 O(n)。如果你要插入随机元素，你需要屏藩的调整容量大小以及移动已存在的数据。如果你是要有序的插入所有元素，那么使用它则是最快的。

上面三个集合，只有 `SortedList` 要求很小的内存。其它两个会要更多的内存访问，但是在插入时间上平均要好些。选择使用哪个具体依赖你的应用程序的要求。

- `HashSet` 使用哈希表实现，所以插入，查找删除操作都是 O(1)
- `SortedSet`  使用二叉查找树实现，故时间复杂度为 O(log n)
- `List` 插入时间复杂度为 O(1)，但是删除和查找的时间复杂为 O(n)
- `Stack` 和 `Queue` 只能插入或者从中删除，所以所有操作的时间复杂度为 O(1)
- `LinkedList` O(1) 的插入和删除，经常避免被用在大量的基元类型操作，因为它会给你添加的每个元素分配新的 `LinkedListNode<T>` 对象，这会浪费开销。
- `BitArray` 表示位的数组。你可以单独的设置位以及再整个数组对象上执行布尔逻辑。如果你只需要 32 位的数据，那么就用 `BitVector32`，它会更快并且开销更小（因为是结构体）

# 字典集键值比较

在操作字典集时 `Dictionary<string,object>`，如果要判断某个键值是否存在，进而做下一步的操作，我们很容易会这么写：

```c#
var key = "myKey";
var dic = new Dictionary<string, object>();
...
foreach(var kvp in dic) {
	if(kvp.Key.ToUpper() == key.ToUpper()) {
		...
	}
}
```

这样比较会对 GC 造成压力以及内存浪费，给 CPU 带来不小的开销。特别是 `ToUpper()` 操作。其实我们可以选择设置比较键值是忽略大小写敏感设置。

```c#
var keytoLookup = "myKey"; 
var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); 
... object val; 
if (dict.TryGetValue(keyToLookup, out val)) { 
	... 
}
```

# 特殊的集合 —— String

字符串是不变的，每次更改字符串都会重新创建一个新的字符串。所以为了高效，设置字符串尽量保持简短，或者以其它形式来替代字符串。

字符串的比较函数效率从高到低为：

```c#
string.Compare(a, b, StringComparsion.Oridinal);

string.Compare(a, b, StringComparison.OridinalIgnoreCase);

string.Compare(a, b, StringComparison.CurrentCulture);
```

`string.Equals` 是字符串一种特殊的例子，只有当你不关心排序顺序的时候你可以用这个。在很多时候它并没有多高效，但是它更可读，会使你代码更加好看。

## 字符串格式化 Format

尽量避免乃至不要使用 `string.Format`，此方法会导致装箱对应的参数，调用内部的自定义格式化器，这会增加内存分配和 CPU 的开销。直接使用字符串连接符

```
string message = string.Format("The file {0} was {1} successfully.", filename, operation);
// 使用下面替代
string message = "The file " + filename + " was " + operation + " successfully";
```

## ToString

要小心所有类的 `ToString` 方法。如果幸运的话，你调用它会返回一个已经缓存好的字符串。这种情况是存在的，比如 `IPAddress` 类就缓存了一次生成的字符串。但是这个生成字符串的过程又是昂贵的。所以当你调用 `ToString` 时如果每次都是返回新的字符串，那么这会很浪费 CPU 的时间，并且也会影响 GC 的频率。

当你在设计自己的类时，你要把 `ToString` 的调用情况也要考虑进来。如果经常调用，那就要确保你生成的字符串是简短的。如果你仅仅只是帮助调试用的，那么就没什么关系了。

## 字符串转换

在任何情况下都要避免字符串转换，因为字符串处理都是 CPU 密集型计算过程，都是重复，耗内存的操作。如果真的需要转换，那么一定要记得调用不会抛出异常的方法，比如用 `int.TryParse` 代替 `int.Parse` 。还有很多跟这个签名一样的都是如此。

## SubStrings

字符串的截断操作会返回全新的字符串，会给这个字符串分配内存。如果是在一个以 `char` 数组的形式组成的部分操作，可以使用 `ReadOnlySpan<T>` ，它可以表示底层数组的一部分：

```c#
{
...
	ReadOnlySpan<char> subString = "NonAllocationSubString".AsSpan().Slice(13);
	PrintSpan(subString);
...
}

private static void PrintSpan<T>(ReadOnlySpan<T> span) {
	for (int i = 0; i < span.Length; i++) {
		T val = span[i];
		Console.Write(val);
		if (i < span.Length -1) {
			Console.Write(", ");
		}
	}
	Console.WriteLine();
}
```

