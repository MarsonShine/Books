# 内存分配

## Class

类一般在堆中分配内存，并通过指针的间接引用进行访问，所以传递对象的代价很小，因为只需要复制其引用的（指针）即可（一般是 4 或 8 个字节）。

一个空对象有一些固定的开销，32 位系统进程需要 8 个字节，64 位系统进程需要 16 个字节。这部分开销包含了

- 指向访问表的指针（类型对象指针）
- 同步索引块字段

一个空对象分配具体的内存大小为 12 字节（32 系统）或 24 个字节（64 位系统）。因为 .NET 会进行内存对齐

## Struct

结构体不比类，没有引用的开销，就是结构对象内部的所有字段内存大小之和。如果结构体对象被申明在方法内部并作为局部变量，那么内存会在堆中分配。如果你把结构体当作参数传递，那么就会逐字节地复制进去。

## 内存计算

拿具体的例子说明问题。假定在一个 64 位系统中，有一个数据结构包含 16 个字节的数据，数组长度为 1000000。那么对象数组所占用的总空间为：

Class：16 字节数组开销 +（ 8 字节指针 * 1000000）+ （16 字节数据 + 16 字节开销）* 1000000 = 40 MB

Struct：16 字节数组开销 + （16 字节数据 * 1000000） = 16 MB

## 如何用 C# 代码计算一个对象的内存大小

首先以空对象为例

```c#
class Empty {
}

// program.cs
const int size = 1000 * 1000;
// var empty = new Empty();
var before = GC.GetTotalMemory(true);
var empty = new Empty();
var after = GC.GetTotalMemory(true);

var diff = after - before;

Console.WriteLine("空对象内存大小：" + diff);
GC.KeepAlive(empty);
```

“结构” 所占用的内存少。除了对象的额外开销，由于内存压力增大，就会发生更高频率的垃圾回收。并且我们要知道，结构体的内存分配内部的地址是连续的。CPU 周边拥有多级缓存，对连续的内存顺序访问会有优化。

## 结构体的 Equals 和 GetHashCode

在使用结构时，我们一定要注意就是要**重写 Equals 和 GetHashCode 方法**。如果只是单独重写 Equals 方法，性能还不能发挥极致，因为这个方法会对值类型进行装箱操作（类型转换）。因此还要实现范型版本 `Equals(T other)` 方法。`IE quatable<T>` 接口就是为此存在的。**所有的值类型都应该实现这个接口**。

```c#
public class Vector : IEquatable<Vector> {
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public int Magnitude { get; set; }

    // override object.Equals
    public override bool Equals(object obj) {
        if (obj == null || GetType() != obj.GetType()) {
            return false;
        }

        return this.Equals((Vector) obj);
    }

    public bool Equals([AllowNull] Vector other) {
        return this.X == other.X &&
            this.Y == other.Y &&
            this.Z == other.Z &&
            this.Magnitude == other.Magnitude;
    }

    // override object.GetHashCode
    public override int GetHashCode() {
        return X ^ Y ^ Z ^ Magnitude;
    }
}
```

