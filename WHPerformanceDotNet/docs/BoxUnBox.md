# 再谈装箱拆箱

装箱是将值类型包装到堆中的对象内的过程，这样就可以传递对象的引用。而拆箱就是装箱的逆过程，取出队中的原始值到栈种。

装箱会消耗 CPU 时间，导致对象分配，数据拷贝。更多的则是给 GC 带来了负担。

而检查我们写的代码有没有发生装箱，最直接有效的方法就是转成 IL 查看指令是否有 “box/unbox" 指令。

## 避免装箱

值对象不要集成接口，实现多态。这样在转换接口的时候，会自动发生装箱

```c#
interface INameable { 
    string Name { get; set; } 
}
struct Foo : INameable { 
	public string Name { get; set; } 
}

Foo foo = new Foo() { Name = "Bar" };
INameable nameable = foo;	// 这里会产生装箱
```

### Ref  关键字返回和本地变量

C# 7 引入了一个新的语法糖 —— `ref` 返回关键字，它能够直接以安全代码的方式访问内存。在此之前是通过不安全代码的指针来访问私有字段可以达到相同效果的，但是标准方式的代码会导致值拷贝，稍后我们会看到。使用 `ref` return，你可以完全以安全代码来达到正确的类抽象以及直接访问内存这种好处。

```c#
int value = 13;
ref int refValue = value;
refValue = 14;
```

像上面这种例子，value 值最后是多少呢？答案是14，因为 refValue 实际上指向的是 value 变量的内存地址。

这个功能可以用在获得一个类的私有数据的引用：

```c#
AvoidBoxed.Vector v = new AvoidBoxed.Vector();
ref int mag = ref v.Magnitude;
mag = 3;

int nonRefMag = v.Magnitude;
mag = 4;
Console.WriteLine($"mag: {mag}");
Console.WriteLine($"nonRefMag: {nonRefMag}");

// mag: 4
// nonRefMag: 3
```

第一个值分配是建立在底层之下的（Vector.magnitude 私有字段）。`nonRefMag` 值的分配是很有趣的，尽管 `nonRefMag` 是一个通过 `ref-return` 的属性，但是由于调用这个属性的时候并没有通过关键字 ref 申明，所以 `nonRefMag` 值会得到原始值的一个拷贝。所以可以看到，尽管底层类的内存直接被访问（`ref int mag = ref v.Magnitude`）并且修改，但是 `nonRefMag` 的值保留只是它最初接收的值。要记住，调用方法的方式与方法申明的方式都是很重要的。

也可以直接通过 ref 来指定一个数组的内存地址。下面这个例子是将数组中间位置归零。在没有 `ref` 方式来做这个事就像这样：

```c#
static void ZeroMiddleValue(int[] arr)
{
    int midIndex = GetMidIndex(arr);
    arr[midIndex] = 0;
}

private static int GetMidIndex(int[] arr)
{
    return arr.Length / 2;
}
```

带 ref 关键字的版本与之很相似：

```c#
static void RefZeroMiddleValue(int[] arr)
{
    ref int middle = ref GetRefMidIndex(arr);
    middle = 0;
}

private static ref int GetRefMidIndex(int[] arr)
{
    return ref arr[arr.Length / 2];
}
```

通过 ref 的功能，我们可以编写像将方法体写在左边，然后给它赋值这种表达式：`GetRefMiddle(arr) = 0`

因为 `GetRefMiddle` 返回的是一个引用，不是值，你可以给它赋值。

通过上面的这些简单例子，您看了可能觉得这种方式对性能的提升应该没有什么太大关系。这对于小的一次性事件来说是这样。大的收益主要是来自于在内存种重复引用一个地址，避免数组偏移或者避免值的拷贝。

举个更加有利的例子，当你使用一个可变的结构时，你可以使用 `ref return` 来避免值拷贝：

```c#
struct Point3d
{
    public double x;
    public double y;
    public double z;
    public string Name { get; set; }
}

class Vector2
{
    private Point3d location;
    public Point3d Location { get; set; }
    public ref Point3d RefLocation => ref location;

    public int Magnitude { get; set; }
}
```

由于您要更改 `location` 的值为原始值 (0,0,0)。不用 `ref return`，那么您将意味着通过调用 `Location` 就会发生值拷贝，并赋值为 0，然后调用赋值器将它放回去，像这样：

```c#
private static void SetVectorToOrigin(Vector vector) {
	Point3d location = vector.Location; 
	pt.x = 0; 
	pt.y = 0; 
	pt.z = 0; 
	vector.Location = pt;
}
```

通过使用 `ref return` 你可以避免这种复制：

```c#
private static void RefSetVectorToOrigin(Vector vector) {
	ref Point3D location = ref vector.RefLocation;
	location.x = 0; 
	location.y = 0; 
	location.z = 0;
}
```

性能效果随着结构尺寸大小变化决定的——越大，非 `ref-return` 方法运行的时间就越慢。

一般情况下应该很少会用到这个特性，但是一旦你要用到这种功能，请切记下面这些场景：

- 修改以属性形式暴露的结构体中的字段
- 直接访问数组地址
- 重复访问相同的内存地址

> 注意：ref-value 对 async 方法无效。

