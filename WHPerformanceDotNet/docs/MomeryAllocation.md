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

“结构” 所占用的内存少。除了对象的额外开销，由于内存压力增大，就会发生更高频率的垃圾回收。并且我们要知道，**结构体的内存分配内部的地址是连续的。CPU 周边拥有多级缓存，对连续的内存顺序访问会有优化。**

## 结构体的 Equals 和 GetHashCode

在使用结构时，我们一定要注意就是要**重写 Equals 和 GetHashCode 方法**。如果只是单独重写 Equals 方法，性能还不能发挥极致，因为这个方法会对值类型进行装箱操作（类型转换）。因此还要实现范型版本 `Equals(T other)` 方法。`IE quatable<T>` 接口就是为此存在的。**所有的值类型都应该实现这个接口**。

```c#
public struct Vector : IEquatable<Vector> {
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

## 接口分发（Dispatch）

当我们第一次通过接口调用方法时，.NET 必须找出是哪一个具体的类的方法来执行调用。**首先会调用一段存根代码（Stub），为实现了该接口的对象找到正确的方法。经过数次查找之后，CLR 会发现总是在调用同一个具体的类型，Stub 代码就会由间接调用简化为几条直接调用的汇编指令。**这几条指令被称为 Monomorphic Stub。

当然 Monomorphic Stub 也会检测到调用错误的方法。如果调用了其他类型的对象，那么 CLR 就会创建新类型的 Monomorphic Stub 替换掉 Stub 代码。

如果情况很复杂，需要调用很多类型，可预测性也不高（比如一个接口的实现类有多种类），**那么 Stub 代码会变成使用哈希表来选择方法的 Polymorphic Stub**。

如果上述情况对程序的确产生了性能影响，那么可以用以下两种方式优化：

- 避免通过公共接口调用对象方法
- 找出基础公共接口，然后用抽象基类替代

## 防止装箱

装箱是需要耗费 CPU 开销来进行对象分配、数据复制和强制类型转换的。严重的会导致 GC 的次数增加，增加 GC 堆的负担，从而导致大量的内存分配。

要尽量杜绝结构类型集成接口，然后引用接口调用。这样会发生装箱，增加 CPU 开销。

如果一个结构要以参数形式多次传递，那么这个结构类型应该改成引用类型。

最后，**传递值类型的引用是不会发生装箱操作，传给方法的是值类型的地址**。

## for vs foreach

对于数组和 List 集合来说，for 和 foreach 其实对于编译器来说都是一样的，.net 编译器会把 foreach 转换成 for 循环。

但是要注意，IEnumerable 类型的 foreach 会有很大的性能开销。

## is 操作符 vs as 操作符

is 操作符会对类型转换进行判断，并返回布尔类型值。

as 操作符会直接将类型转换成目标类型，失败会返回 null。

强制类型转换会造成性能损失，代价很大。我们可以用上面提到的 is，as 操作符转换。但千万要注意不要像下面一样转换：

```c#
if ( a is Foo) {
	Foo f = (Foo) a;
}
```

这样会执行两次类型转换。而是应该直接用 as 操作符转换，然后判断转换的对象是否为 null：

```c#
Foo f = a as Foo;
if( f != null) {
	// .. logic
}
```

## 委托的开销

委托是特殊的类，编译器会把委托类生成一个新的类。这里面有两个开销：**构造开销**和**调用开销**。调用开销在绝大多数情况下与普通方法调用差不多。前面说，委托会被编译器生成一个类对象，构造一个委托的开销要比方法调用高得多。我们在使用委托的时候应该只做**一次构造，并把对象缓存起来**。比如比较下面这段代码：

```c#
private delegate int MathOp(int x, int y);
private static int Add(int x, int y) => x + y;
private static int DoOperation(MathOp op, int x, int y) => op(x, y);

public static void Start() {
    // 1
    for (int i = 0; i < 10; i++) {
        DoOperation(Add, 1, 2);
    }

    // 2
    MathOp op = Add;
    for (int i = 0; i < 10; i++) {
        DoOperation(op, 1, 2);
    }
}
```

第二种虽然看起来只是把 Add 方法缓存到一个局部变量，但实际上里面细节牵扯到内存分配。这个通过查看 IL 即可知道。第一种在循环内部每次都会构造一个 Add 方法，而第二种会在循环外构造一次 Add 方法。这样第二种只会分配一次委托的对象内存。

```c#
// 第一种
// loop start (head: IL_001f)
IL_0005: nop
IL_0006: ldnull
IL_0007: ldftn int32 MomeryAllocation.DelegateOverhead::Add(int32, int32) /* 06000001 */
IL_000d: newobj instance void MomeryAllocation.DelegateOverhead/MathOp::.ctor(object, native int) /* 06000017 */
... 
  
// 第二种
// ldftn 先加载 add 到字段中，然后 new 一个委托对象，方法指向上一个字段
IL_0029: ldftn int32 MomeryAllocation.DelegateOverhead::Add(int32, int32) /* 06000001 */
IL_002f: newobj instance void MomeryAllocation.DelegateOverhead/MathOp::.ctor(object, native int) /* 06000017 */
IL_0034: stloc.0
IL_0035: ldc.i4.0
IL_0036: stloc.3
IL_0037: br.s IL_0048
// loop start (head: IL_0048)
  IL_0039: nop
  IL_003a: ldloc.0
  IL_003b: ldc.i4.1
  IL_003c: ldc.i4.2
  IL_003d: call int32 MomeryAllocation.DelegateOverhead::DoOperation(class MomeryAllocation.DelegateOverhead/MathOp, int32, int32) /* 06000002 */
```

> 我尝试使用 dotnet build --configuration Release 发现在 release 模式下针对这种情况像 `for(var i = 0; i <= GetEnumerable().Count(); i++)` 会自动优化的。结果发现与 debug 是一致的。

## 代码生成（ILGenerator）技术提升性能

以下内容来自《编写高性能 .NET 代码》

```c#
static bool CallMethodTemplate(object extensionObj, string argument)
{
	var extension = (DynamicLoadExtension.Extension)extensionObj;
	return extension.DoWork(argument);
}
```

现在我们要生成上面这段模板代码，首先我们看下 IL 代码：

```c#
.locals init {
	[0] class [DynamicLoadExtension]DynamicLoadExtension.Extension extension
}

IL_0000: ldarg.0
IL_0001: castclass [DynamicLoadExtension]DynamicLoadExtension.Extension
IL_0006: stloc.0
IL_0007: ldloc.0
IL_0008: ldarg.1
IL_0009: callvirt instance bool
[DynamicLoadExtension]DynamicLoadExtension.Extension::DoWork(string)
IL_000e: ret
```

用 IL 手动生成的代码为如下：

```c#
private static T GenerateMethodCallDelegate<T>(
	MethodInfo methodInfo,
	Type extensionObj,
	Type returnType,
	Type[] parameterTypes
) where T : class
{
	var dynamicMethod = new DynamicMethod("Invoke_" + methodInfo.Name, returnType, parameterTypes, true);
	var ilGenerator = dynamicMethod.GetILGenerator();
	ilGenerator.DeclareLocal(extensionType);
	// 第一个参数为 this 参数 
	ilGenerator.Emit(OpCodes.Ldarg_0);
	// 类型转换
	ilGenerator.Emit(OpCodes.Castclass, extensionType);
	// 方法参数
	ilGenerator.Emit(OpCodes.Stloc_0);
	ilGenerator.Emit(OpCodes.Ldloc_0);
	ilGenerator.Emit(OpCodes.Ldarg_1);
	ilGenerator.EmitCall(OpCodes.Callvirt, methodInfo, null);
	ilGenerator.Emit(OpCodes.Ret);
	
	object del = dynamicMethod.CreateDelegate(typeof(T));
	return (T)del;
}
```

解释一下上面 IL 操作：

1. 申明局部变量
2. 将 arg0（this 指针）压入堆栈（LdArg_0 指令）
3. 将 arg0 类型转换为右侧的类型，并将结果压入堆栈
4. 弹出栈顶数据并保存在局部变量中（Stloc_0 指令）
5. 将局部变量压入堆栈（Ldloc_0 指令）
6. 将 arg1（字符串参数）压入堆栈
7. 调用 DoWork 方法
8. 返回

优化点：我们可以把堆栈中经过类型转换的对象弹出，然后又马上压回堆栈。要把这种有关局部变量的操作都移除，这样就可以优化了。另外还有一点，上面方法调用的是 `Callvirt` ，我们知道这个方法不是虚方法，所以我们可以直接改为 `Call`。改版后的代码如下：

```c#
var ilGenerator = dynamicMethod.GetILGenerator();
// 对象的 this 参数
ilGenerator.Emit(OpCodes.Ldarg_0);
// 转换为正确的类型
ilGenerator.Emit(OpCodes.Castclass, extensionType);
// 实际的参数
ilGenerator.Emit(OpCodes.Ldarg_1);
ilGenerator.EmitCall(OpCodes.Call, methodInfo, null);
ilGenerator.Emit(OpCodes.Ret);
```

