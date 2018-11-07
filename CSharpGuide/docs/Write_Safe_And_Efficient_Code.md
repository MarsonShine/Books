## 编写安全高效的C#代码

值类型的优势能避免堆分配。而劣势就是往往伴随的数据的拷贝。这就导致了在大量的值类型数据很难的最大化优化这些算法操作（因为伴随着大量数据的拷贝）。而在C#7.2 中就提供了一种机制，它通过对值类型的引用来使代码更加安全高效。使用这个特性能够最大化的减小内存分配和数据复制操作。

这个新特性主要是以下几个方面：

1. 声明一个 `readonly struct` 来表示这个类型是不变的，能让编译器当它做参数输入时，会保存它的拷贝。
2. 使用 `ref readonly` 。当返回一个值类型，且大于 [IntPtr.Size](https://docs.microsoft.com/en-us/dotnet/api/system.intptr.size#System_IntPtr_Size) 时以及存储的生命周期要大于这方法返回的值的时候。
3. 当用 `readonly struct` 修饰的变量/类大小大于  [IntPtr.Size](https://docs.microsoft.com/en-us/dotnet/api/system.intptr.size#System_IntPtr_Size) ，那么就应该作为参数输入来传递它来提高性能。
4. 除非用 `readonly` 修饰符来声明，永远不要传递一个 `struct` 作为一个输入参数（`in parameter`），因为它可能会产生副作用，从而导致它的行为变得模糊。
5. 使用 `ref struct` 或者 `readonly ref struct`，例如  [Span](https://docs.microsoft.com/en-us/dotnet/api/system.span-1) 或 [ReadOnlySpan](https://docs.microsoft.com/en-us/dotnet/api/system.readonlyspan-1)  以字节流的形式来处理内存。

这些技术你要面对权衡这值类型和引用类型这两个方面带来的影响。引用类型的变量会分配内存到堆内存上。值类型变量只包含值。他们两个对于管理资源内存来说都是重要的。值类型当传递到一个方法或是从方法中返回时都会拷贝数据。这个行为还包括拷贝值类型成员时，该值的值（ **This behavior includes copying the value of `this` when calling members of a value type.** ）。这个开销视这个值类型对象数据的大小而定。引用类型是分配在堆内存的，每一个新的对象都会重新分配内存到堆上。这两个（值类型和引用）操作都会花费时间。

### `readonly struct`来申明一个不变的值类型结构

用 readonly 修饰符声明一个结构体，编译器会知道你的目的就是建立一个不变的结构体类型。编译器就会根据两个规则来执行这个设计决定：

1. 所有的字段必须是只读的 readonly。
2. 所有的属性必须是只读的 readonly，包括自动实现属性。

以上两条足已确保没有readonly struct 修饰符的成员来修改结构的状态—— struct 是不变的

```c#
readonly public struct ReadonlyPoint3D {
    public ReadonlyPoint3D (double x, double y, double z) {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }
}
```

### 尽可能面对大对象结构体使用 `ref readonly struct` 语句

当这个值不是这个返回方法的本地值时，可以通过引用返回值。通过引用返回的意思是说只拷贝了它的引用，而不是整个结构。下面的例子中 `Origin` 属性不能使用 `ref` 返回，因为这个值是正在返回的本地变量：

```c#
public ReadonlyPoint3D Origin => new ReadonlyPoint3D(0,0,0);
```

然而，下面这个例子的属性就能按引用返回，因为返回的值一个静态成员：

```c#
private static ReadonlyPoint3D origin = new ReadonlyPoint3D(0,0,0);
//注意：这里返回是内部存储的易变的引用
public ref ReadonlyPoint3D Origin => ref origin;
```

你如果不想调用者修改原始值，你可以通过 `readonly ref` 来修饰返回值：

```c#
 public ref readonly ReadonlyPoint3D Origin3 => ref origin;
```

返回 `ref readonly` 能够让你保存大对象结构的引用以及能够保护你内部不变的成员数据。

作为调用方，调用者能够选择 `Origin` 属性是作为一个值还是 按引用只读的值（`ref readonly`）：

```c#
var originValue = Point3D.Origin;
ref readonly var originReference = ref Point3D.Origin;
```

在上面这段代码的第一行，把 Point3D 的原始属性的常数值 `Origin` 拷贝并复制数据给originValue。第二段代码只分配了引用。要注意，`readonly` 修饰符必须是声明这个变量的一部分。因为这个引用是不允许被修改的。不然，就会引起编译器编译错误。

`readonly` 修饰符在申明的 originReference 是必须的。

编译器要求调用者不能修改引用。企图直接修改该值会引发编译器的错误。然而，编译器却无法知道成员方法修改了结构的状态。为了确定对象没有被修改，编译器会创建一个副本并用它来调用成员信息的引用。任何修改都是对**防御副本（defensive copy）**的修改。