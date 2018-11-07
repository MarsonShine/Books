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

### 对大于 `System.IntPtr.Size` 的参数应用 `in`修饰符到 `readonly struct`

`in` 关键字补充了已经存在的 `ref` 和 `out` 关键字来按引用传递参数。`in` 关键字也是按引用传递参数，但是调用这个参数的方法不能修改这个值。

值类型作为方法签名参数传到调用的方法中，且没有用下面的修饰符时，是会发生拷贝操作的。每一个修饰符指定这个变量是按引用传递的，避免了拷贝。以及每个修饰符都表达不同的意图：

- `out`：这个方法设置参数的值来作为参数。
- `ref`：这个方法也可以设置参数的值来作为参数。
- `in`：这个方法作为参数无法修改这个参数的值。

增加 `in` 修饰符按引用传递参数以及申明通过按引用传值来避免数据的拷贝的意图。说明你不打算修改这个作为参数的对象。

对于只读的那些大小超过 `IntPtr.Size` 的值类型来说，这个经验经常能提高性能。例如有这些值类型（sbyte，byte，short，ushort，int，uint，long，ulong，char，float，double，decimal 以及 bool 和 enum），任何潜在的性能收益都是很小的。实际上，如果对于小于 `IntPtr.Size` 的类型使用按引用个传递，性能可能会下降。

下面这段 demo 展示了计算两个点的3D空间的距离

```c#
public static double CalculateDistance ( in Point3D point1, in Point3D point2) {
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;

    return Math.Sqrt (xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
}
```

这个方法有两个参数结构体，每个都有三个 `double` 字段。1个 double 8 个字节，所以每个参数含有 24 字节。通过指定 `in` 修饰符，你传递了 4 个字节或 8 个字节的参数引用，4 还是 8字节取决平台结构（32位 一个引用 2 字节，64位一个引用 4字节）。这看似大小差异很小，但是当你的应用程序在高并发，高循环的情况下调用这个函数，那么性能上的差距就很明显了。

`in` 修饰符也很好的补充了 `out` 和 `ref` 其他方面。你不能创建仅修饰符（in，out，ref）不同的方法重载。这个新的特性拓展了已经存在 `out` 和 `ref` 参数原来相同的行为。像 `ref` 和 `out` 修饰符，值类型由于应用了 `in` 修饰符而无法装箱。

`in` 修饰符能应用在任何成员信息上：方法，委托，lambda表达式，本地函数，索引，操作符。

`in` 修饰符还有在其他方面的特性，在参数上用 `in` 修饰的参数值你能使用字面量的值或者常数。不像 `ref` 和 `out` 参数，你不必在调用方用 `in`。下面这段代码展示了两个调用 `CalculateDistance` 的方法。第一个变量使用两个按引用传递的局部变量。第二个包括了作为这个方法调用的一部分创建的临时变量。

```c#
var distance = CalculateDistance (point1,point2);
var fromOrigin = CalculateDistance(point1,new Point3D());
```

这里有一些方法，编译器会强制执行 read-only 签名的 in 参数。第一个，被调用的方法不能直接分配一个 in 参数。它不能分配到任何 in 字段，当这个值是值类型的时候。另外，你也不能通过 ref 和 out 修饰符来传递一个 in 参数到任何方法上。这些规则都应用在 in 修饰符的参数，前提是提供一个值类型的字段以及这个参数也是值类型的。事实上，这些规则适用于多个成员访问，前提是所有级别的成员访问的类型都是结构体。编译器强制执行在参数中传递的 struct 类型，当它们的 struct 成员用作其他方法的参数时，它们是只读变量。

使用 in 参数能避免潜在拷贝方面的性能开销。它不会改变任何方法调用的语义。因此，你无需在调用方（call site）指定 in 修饰符。在调用站省略 in 修饰符会让编译器进行参数拷贝操作，有以下几种原因：

- 存在隐式转换，但不存在从参数类型到参数类型的标识转换。
- 参数是一个表达式，但是没有已知的存储变量。
- 存在一个不同于已经存在或者是不存在 in 的重载。这种情况下，通过值重载会更好匹配。

这些规则当你更新那些已有的并且已经用 read-only 引用参数的代码非常有用。在调用方法里面，你可以通过值参数（value paramters）调用任意成员方法。在那些实例中，会拷贝 in 参数。因为编译器会对 in 参数创建一个临时的变量，你可以用 in 指定默认参数的值。下面这段代码指定了origins（point 0,0）作为默认值作为第二个参数：

```c#
private static double CalculateDistance2 ( in Point3D point1, in Point3D point2 = default) {
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;
    return Math.Sqrt (xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
}
```

编译器会通过引用传递只读参数，指定 in 修饰符在调用方法的参数上，就像下面展示的代码：

```c#
private static void DemoCalculateDistanceForExplicit (Point3D point1, Point3D point2) {
    var distance = CalculateDistance ( in point1, in point2);
    distance = CalculateDistance ( in point1, new Point3D ());
    distance = CalculateDistance (point1, in Point3D.origin);
}
```

这种行为能够更容易的接受 in 参数，随着时间的推移，大型代码库中性能会获得提高。首先就要添加 in 到方法签名上。然后你可以在调用端添加 in 修饰符以及新建一个 `readonly struct` 类型来使编译器避免在更多未知创建防御拷贝的副本。

in 参数被设计也能使用在引用类型或数字值。然而，在这种情况的性能收益是很小的。

### 不要使用易变的结构体作为 in 参数

下面描述的技术主要解释了怎样通过返回引用以及传递的值引用避免数据拷贝。当参数类型是已经申明的 `readonly struct` 类型时，这些技术都能很好的工作。否则，编译器在很多非只读参数的场景下必须新建一个**防御拷贝（defensive copies）**副本。考虑下面这段代码，他计算 3D 点到原地=点的距离：

```c#
private static double CalculateDistance ( in Point3D point1, in Point3D point2 = default) {
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;
    return Math.Sqrt (xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
}
```

`Point3D` 是非只读结构类型（readonly-ness struct）。在这个方法体中，有 6 个不同的属性访问调用。第一次检查时，你可能觉得这些访问都是安全的。在这之后，一个 get 读取器不能修改这个对象的状态。但是这里没有语义规则让编译器这样做。它只是一个通用的约束。任何类型都能实现 get 读取器来修改这个内部状态。没有这些语言保证，在调用任何成员之前，编译器必须新建这个参数的拷贝副本来作为临时变量。这个临时变量存储在栈上，这个参数的值的副本在这个临时变量中存储，并且每个成员访问的值都会拷贝到栈上,作为参数。在很多情况下，当参数类型不是 `readonly struct` 时，这些拷贝都会对性能有害，以至于通过值传递要比通过只读引用（readonly reference）传递快。

相反，如果距离计算方法使用不变结构，`ReadonlyPoint3D`，就不需要临时变量：

```c#
private static double CalculateDistance3(in ReadonlyPoint3D point1, in ReadonlyPoint3D point2 = default)
{
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;

    return Math.Sqrt(xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
}
```

当你用 `readonly struct` 修饰的成员时，编译器会自动生成更多高效代码：`this` 引用，而不是接受者的副本拷贝，in 参数总是按引用传递到成员方法中。当你使用 `readonly struct` 作为 in 参数时，这种优化会节省内存。

你可以查看程序的demo，在实例代码仓库 [samples repository](https://github.com/dotnet/samples/tree/master/csharp/safe-efficient-code/benchmark) 中，它展示了使用 [Benchmark.net](https://www.nuget.org/packages/BenchmarkDotNet/) 比较性能的差异。它比较了传递易变结构的值和引用，易变结构的按值传递和按引用传递。使用不变结构体的按引用传递是最快的。

### 使用 `ref struct` 类型在单个堆栈帧上处理块和内存

一个语言相关的特性是申明值类型的能力，该值类型必须约束在单个堆栈对上。这个限制能让编译器做一些优化。主要推动这个特性体检在 `Span<T>`以及相关的结构。你从使用这些新添加的以及更新的.NET API，如 `Span<T>` 类型来完成性能的提升。

你可能有相同的要求，在内存中使用 `stackalloc` 或者当使用来自于内存的交互操作API。你就为这些需求能定义你自己的 ref struct 类型。

#### `readonly ref struct` 类型

声明一个 `readonly ref` 结构体，它联合了 `ref struct` 和 `readonly struct` 两者的收益。通过只读的元素内存被限制在单个的栈中，并且只读元素内存无法被修改。

### 总结

使用值类型能最小化的内存分配：

- 在局部变量和方法参数中值类型存储在栈上分配
- 对象的值类型成员做为这个对象的一部分分配在栈上，并不是一个单独的分配操作。
-  存储返回的值类型是在栈上分配

不同于引用类型在相同场景下：

- 存储局部变量和方法参数的引用类型分配在堆上，。引用存在栈。
- 存储对象的成员变量是引用类型，它作为这个对象的一部分在堆上分配内存。而不是单独的分配这个引用。
- 存储返回的值是引用类型，堆分配内存。存储引用的值存储在栈上。

最小化的内存分配要权衡。当结构体内存大小超过引用大小时，就要拷贝更多的内存。一个引用类型指定 64 字节或者是 32 字节，它取决于平台架构。

这些权衡/折中通常对性能影响很小。然而大对象结构体或大对象集合，对性能影响是递增的。特别在循环和经常调用的地方影响特别明显。

这些C#语言的增强是为了关键算法的性能而设计的，内存分配问题成为了主要的优化点。你会发现你无需经常使用这些特性在你写的代码中。然而，这些增强在 .NET 中接受。越来越多的 API 会运用到这些特性，你将看到你的应用程序性能的提升。