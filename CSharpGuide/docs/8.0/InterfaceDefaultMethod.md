# 接口默认实现

首先我们要了解的就是接口默认实现是怎样通过 `Type.GetInterfaceMap(Type)` 实现的，其实它所作的事就像下面代码一样：

```c#
private static void ShowInterfaceMapping(Type implemetation, Type @interface) {
    InterfaceMapping map = implemetation.GetInterfaceMap(@interface);
    Console.WriteLine($"{map.TargetType}: GetInterfaceMap({map.InterfaceType})");
    for (int counter = 0; counter < map.InterfaceMethods.Length; counter++) {
        MethodInfo im = map.InterfaceMethods[counter];
        MethodInfo tm = map.TargetMethods[counter];
        Console.WriteLine($"   {im.DeclaringType}::{im.Name} --> {tm.DeclaringType}::{tm.Name} ({(im == tm ? "same" : "different")})");
        Console.WriteLine("       MethodHandle 0x{0:X} --> MethodHandle 0x{1:X}",
            im.MethodHandle.Value.ToInt64(), tm.MethodHandle.Value.ToInt64());
        Console.WriteLine("       FunctionPtr  0x{0:X} --> FunctionPtr  0x{1:X}",
            im.MethodHandle.GetFunctionPointer().ToInt64(), tm.MethodHandle.GetFunctionPointer().ToInt64());
    }
    Console.WriteLine();
}
```

通过运行调用上述方法我们得到输出结果

```
//ShowInterfaceMapping(typeof(CNormal), @interface: typeof(INormal));
//ShowInterfaceMapping(typeof(CDefault), @interface: typeof(IDefaultMethod));
//ShowInterfaceMapping(typeof(CDefaultOwnImpl), @interface: typeof(IDefaultMethod));

TestApp.CNormal: GetInterfaceMap(TestApp.INormal)
   TestApp.INormal::Normal --> TestApp.CNormal::Normal (different)
       MethodHandle 0x7FF993916A80 --> MethodHandle 0x7FF993916B10
       FunctionPtr  0x7FF99385FC50 --> FunctionPtr  0x7FF993861880

TestApp.CDefault: GetInterfaceMap(TestApp.IDefaultMethod)
   TestApp.IDefaultMethod::Default --> TestApp.IDefaultMethod::Default (same)
       MethodHandle 0x7FF993916BD8 --> MethodHandle 0x7FF993916BD8
       FunctionPtr  0x7FF99385FC78 --> FunctionPtr  0x7FF99385FC78

TestApp.CDefaultOwnImpl: GetInterfaceMap(TestApp.IDefaultMethod)
   TestApp.IDefaultMethod::Default --> TestApp.CDefaultOwnImpl::TestApp.IDefaultMethod.Default (different)
       MethodHandle 0x7FF993916BD8 --> MethodHandle 0x7FF993916D10
       FunctionPtr  0x7FF99385FC78 --> FunctionPtr  0x7FF9938663A0
```

我们可以看到 `CDefault` 上的 `IDefaultMethod` 接口和方法的实现是相同的（MethodHandle 地址）。你也可以看到，在其他情况下接口方法映射到一个不同的方法实现。

那现在让我们往底层看，我们用 WinDBG 以及 SOS 拓展来查找它在运行期内部使用的数据结构

首先，我们聚焦于方法 `MethodHandle` 我们来 dump  接口 `INormal` 的信息：

```

```

# 接口的变更历史

从 C#8.0 之前对接口与 抽象类的区别一直停留在：

- 抽象类可以包含实现代码。接口只能申明方法不能实现方法。
- 一个类只可以集成一个抽象类，可以集成任意个接口。
- 抽象类可以有访问标识符（public，protected，private 等），而接口不能有访问标识符，默认是 public 的。
- 抽象类可以有字段，索引，属性，构造函数，析构函数，方法，事件等。接口只能有属性，方法 ，事件和索引器（没有字段，构造函数，析构函数）
- 抽象类可以有静态成员。接口不能有静态成员。

C#8.0 之后的变化：

第一点已经不再适用，接口在 C#8.0 允许有实现方法，被称为“默认实现”，即派生类如果没有实现此方法，默认调用的是接口中的方法实现。属性也可以实现默认的 getter 方法。

第二点仍然适用

第三点已经不再适用，接口在 C#8.0 之后允许有访问标识符了，当然不申明标识符仍然是 public。但是接口现在能提供私有成员，这只限制在接口内部访问。

第四点其实大部分都是适用的，接口可以有属性，方法，事件和索引器，在接口实例中还是不允许字段和构造函数。但是现在允许接口定义静态成员变量，如静态字段，构造函数。

第五点已经不再适用

