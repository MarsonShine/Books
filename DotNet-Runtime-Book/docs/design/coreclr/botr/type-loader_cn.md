# 类型加载器设计

作者：Ladi Prosek - 2007

# 1. 简介

在一个以类为基础的面向对象系统中，类型是模板，用来描述各个实例将包含的数据，以及它们将提供的功能。在未先定义其类型之前，不可能创建一个对象¹。若两个对象都是同一类型的实例，则称它们属于同一类型。它们恰好定义了完全相同的一组成员，并不会使它们以任何方式相关联。

上一段同样可以用来描述一个典型的 C++ 系统。CLR 还额外具备一个至关重要的特性：可以获得完整的运行时类型信息。为了“管理”托管代码并提供类型安全的环境，运行时必须在任何时刻都知道任何对象的类型。这样的类型信息必须在无需大量计算的情况下即可获得，因为类型标识查询预计会相当频繁（例如，任何类型转换都涉及查询对象的类型标识，以验证该转换是安全且可执行的）。

这一性能要求排除了任何字典查找（dictionary look up）方案，从而留下如下高层架构。

![Figure 1](./asserts/typeloader-fig1.png)

图 1 抽象的高级对象设计

除了实际的实例数据外，每个对象都包含一个类型 ID，它只是一个指向表示该类型的结构的指针。这个概念类似于 C++ 的 v-table 指针，但我们现在称为 TYPE 的结构（稍后将更精确地定义它）包含的不仅仅是一个 v-table。例如，它必须包含有关层次结构的信息，以便可以回答“is-a”包含关系问题。

> <sup>1</sup> C# 3.0 中称为“匿名类型”的特性允许你在不显式引用类型的情况下定义对象 - 只需直接列出其字段即可。别被这个骗了，实际上编译器在幕后为你创建了一个类型。

## 1.1 相关阅读

[1] Martin Abadi, Luca Cardelli, A Theory of Objects, ISBN
978-0387947754

[2] Andrew Kennedy ([@andrewjkennedy](https://github.com/andrewjkennedy)), Don Syme ([@dsyme](https://github.com/dsyme)), [Design and Implementation of Generics
for the .NET Common Language
Runtime][generics-design]

[generics-design]: http://research.microsoft.com/apps/pubs/default.aspx?id=64031

[3] [ECMA Standard for the Common Language Infrastructure (CLI)](https://www.ecma-international.org/publications-and-standards/standards/ecma-335)

## 1.2 设计目标

类型加载器（type loader，有时也被称为 class loader——严格来说并不准确，因为 class 只是类型的一个子集，即引用类型，而加载器也会加载值类型） 的最终目的，是构建它被要求加载的类型所对应的数据结构。加载器应具备以下属性：

- 快速类型查找（[module, token] => handle 和 [assembly, name] => handle）。
- 优化的内存布局，以实现良好的工作集大小、缓存命中率和 JIT 代码性能。
- 类型安全 - 不会加载格式错误的类型，并抛出 TypeLoadException。
- 并发性 - 在多线程环境中具有良好的扩展性。

# 2 类型加载器架构

加载器的入口点数量相对较少。虽然每个单独入口点的签名略有不同，但它们都具有相似的语义。它们接受元数据 **token（令牌）** 或 **name（名称）** 字符串形式的类型/成员名称，token 的作用域（**module（模块）** 或 **assembly（程序集）**），以及一些附加信息（如标志）。它们以 **handle（句柄）** 的形式返回已加载的实体。

在 JIT 编译期间通常会有很多对类型加载器的调用。考虑：

```csharp
object CreateClass()
{
    return new MyClass();
}
```

在 IL 中，MyClass 通过一个元数据 token 来引用。为了生成对负责实际实例化的 `JIT_New` helper 的调用，JIT 会请求类型加载器加载该类型并返回一个指向它的 handle。随后，这个 handle 会被作为一个立即数直接嵌入到 JIT 生成的代码中。类型与成员通常在 JIT 时而非运行时（run-time）被解析并加载，这也解释了像下面这种代码中容易遇到的、令人困惑的行为：

```csharp
object CreateClass()
{
    try {
        return new MyClass();
    } catch (TypeLoadException) {
        return null;
    }
}
```

如果 `MyClass` 加载失败，例如因为它应该在另一个程序集中定义，而在最新的构建中被意外删除，那么此代码仍将抛出 `TypeLoadException`。catch 块没有捕获它的原因是它从未运行过！异常发生在 JIT 编译期间，并且只能在调用 `CreateClass` 并导致其被 JIT 编译的方法中捕获。此外，由于内联，JIT 编译触发的时间点可能并不总是很明显，因此用户不应期望并依赖确定性行为。

## 关键数据结构

CLR 中最通用的类型指示方式是 TypeHandle。它是一个抽象实体，封装了一个指针：要么指向 MethodTable（表示诸如 System.Object 或 List<string> 之类的“普通”类型），要么指向 TypeDesc（表示 byref、指针、函数指针以及泛型变量）。它构成了类型的标识：当且仅当两个 handle 表示同一类型时，它们才相等。为节省空间，TypeHandle 包含的是 TypeDesc 这一事实通过把指针的次低位设为 1 来表示（即 `(ptr | 2)`），而不是使用额外的标志位²。TypeDesc 是“抽象的”，并具有如下继承层次结构。

![Figure 2](./asserts/typeloader-fig2.png)

图 2 TypeDesc 层次结构

**`TypeDesc`**

抽象类型描述符。具体的描述符类型由标志确定。

**`TypeVarTypeDesc`**

表示类型变量，即 `List<T>` 或 `Array.Sort<T>` 中的 `T`（参见下面关于泛型的部分）。类型变量永远不会在多个类型或方法之间共享，因此每个变量都有其唯一的所有者。

**`FnPtrTypeDesc`**

表示函数指针，本质上是一个可变长度的 TypeHandle 列表，用于引用返回类型与参数类型。它最初只被托管 C++ 使用。C# 从 C# 9 起支持它。

**`ParamTypeDesc`**

该描述符表示 byref 与指针类型。byref 是将 C# 的 `ref` 与 `out` 关键字应用于方法参数后得到的结果³；而指针类型则是指向数据的非托管指针，用于 C# 的 unsafe 与托管 C++。

**`MethodTable`**

这无疑是运行时最核心的数据结构。它表示任何不属于上述类别的类型（包括基元类型，以及泛型类型，无论是“开放（open）”还是“封闭（closed）”）。它包含关于该类型所有需要被快速查找的信息，例如其父类型、已实现的接口以及 v-table。

**`EEClass`**

`MethodTable` 数据分为“热”和“冷”结构，以提高工作集和缓存利用率。`MethodTable` 本身旨在仅存储程序稳定状态所需的“热”数据。`EEClass` 存储通常仅由类型加载、JIT 编译或反射所需的“冷”数据。每个 `MethodTable` 指向一个 `EEClass`。

此外，`EEClass` 由泛型类型共享。多个泛型类型 `MethodTable` 可以指向单个 `EEClass`。这种共享对可以存储在 `EEClass` 上的数据施加了额外的限制。

**`MethodDesc`**

毫无疑问，这个结构描述了一个方法。实际上它有几种变体，具有相应的 `MethodDesc` 子类型，但其中大多数实际上超出了本文档的范围。可以说有一个名为 `InstantiatedMethodDesc` 的子类型，它在泛型中起着重要作用。有关更多信息，请参阅 [**Method Descriptor Design**](method-descriptor_cn.md)。

**`FieldDesc`**

类似于 `MethodDesc`，此结构描述一个字段。除了某些 COM 互操作场景外，EE 根本不关心属性和事件，因为它们归根结底归结为方法和字段，只有编译器和反射生成和理解它们，以便提供那种语法糖般的体验。

<sup>2</sup> 这对调试很有用。如果 `TypeHandle` 的值以 2、6、A 或 E 结尾，则它不是 `MethodTable`，必须清除额外的位才能成功检查 `TypeDesc`。

<sup>3</sup> 请注意，`ref` 和 `out` 之间的区别仅在于参数属性。就类型系统而言，它们都是相同的类型。

## 2.1 加载级别 (Load Levels)

当类型加载器被要求加载某个指定类型（例如通过一个 typedef/typeref/typespec token 与一个 Module 来标识）时，它不会一次性以原子方式完成所有工作。加载会分阶段进行。原因在于：该类型通常依赖其他类型；若要求它在能被其他类型引用之前必须完全加载，就会导致无限递归与死锁。考虑：

```csharp
class A<T> : C<B<T>>
{ }

class B<T> : C<A<T>>
{ }

class C<T>
{ }
```

这些是有效类型，显然 `A` 依赖于 `B`，`B` 依赖于 `A`。

加载器起初会创建表示该类型的结构，并用无需加载其他类型即可获得的数据初始化它们。当这部分“无依赖（no-dependencies）”工作完成后，这些结构就可以被其他地方引用，通常做法是把指向它们的指针塞进其他结构。之后，加载器以增量步骤推进，不断向这些结构填充更多信息，直到最终抵达一个完全加载的类型。在上面的例子中，A 与 B 的基类型会先用一种不包含对方类型的近似表示，之后再用真实内容替换。

具体的“半加载”状态由所谓的 load level 描述：从 CLASS_LOAD_BEGIN 开始，以 CLASS_LOADED 结束，中间还有若干级别。关于各个 load level 的丰富且有用的注释位于 [`classloadlevel.h`](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/classloadlevel.h) 源文件中。

更详细的 load level 说明请参见 [*Design and Implementation of Generics for the .NET Common Language Runtime*。](http://research.microsoft.com/apps/pubs/default.aspx?id=64031)

> ### 1. 为什么不会编译失败？
>
> 首先，我们要区分两种“循环”：
>
> - **非法的循环继承（编译器拦截）：** `class A : B { } class B : A { }`。这种代码连编译都过不去，因为继承关系必须是一个有向无环图（DAG）。
> - **合法的泛型循环：** `class A<T> : C<B<T>>`。这里 `A` 并不是直接继承自 `B`，而是继承自 `C`。这在类型拓扑上是合法的，但在**实例化**时会产生依赖环。
>
> ### 2. 加载器是如何“作弊”的？（Load Levels 的精髓）
>
> 如果 CLR 加载器是“完美主义者”，要求“在有人引用我之前，我必须完全准备好（完全加载）”，那么你的例子就会导致死锁：
>
> 1. 加载 `A<int>`，发现需要先加载基类 `C<B<int>>`。
> 2. 为了加载 `C<B<int>>`，必须先加载 `B<int>`。
> 3. 加载 `B<int>`，发现需要先加载基类 `C<A<int>>`。
> 4. 为了加载 `C<A<int>>`，必须先加载 `A<int>`…… **（死循环开始）**
>
> **为了打破这个死循环，加载器引入了“中间状态”（Load Levels）：**
>
> 1. **第一步：占座（CLASS_LOAD_BEGIN）** 加载器先为 `A` 创建一个极其简陋的“壳子”（MethodTable 内存块的地址）。此时 `A` 就像是一个刚挖了地基的房子，还没封顶，更不能住人。但重要的是：**它已经有一个唯一的地址了**。
> 2. **第二步：递交名片** 当加载 `B` 发现需要 `A` 时，加载器不再尝试“完全加载 `A`”，而是说：“喏，这是 `A` 的地基地址，你先拿去引用着。虽然 `A` 还没盖好，但你已经知道它在哪了。”
> 3. 第三步：分批施工
>    - `B` 拿到了 `A` 的地址，完成了它自己的“地基”阶段。
>    - 现在 `A` 也可以拿到 `B` 的地址，完成后续的施工（计算对象大小、布局字段等）。
> 4. **最后：全面交房（CLASS_LOADED）** 等到所有相互依赖的类型的“地基”、“框架”、“装修”都分阶段交叉完成了，加载器才把它们的状态全部标记为 `CLASS_LOADED`。
>
> 那么这种方式会不会只是将错误滞后至运行期呢？——当实例化的时候还是会死循环而发生错误？
>
> ### 1. 为什么实例化不会无限循环？（关键在于：引用类型）
>
> 在 .NET 中，**类（Class）是引用类型**。这意味着在内存布局上，一个类无论包含多少字段，或者它的基类有多复杂，它在被引用时，本质上只是一个**固定大小的指针**（64位系统下是 8 字节）。
>
> 我们再看看上面的例子：
>
> ```
> class A<T> : C<B<T>> { }
> class B<T> : C<A<T>> { }
> ```
>
> - **物理层面：** 当你要 `new A<int>()` 时，CLR 需要知道 `A<int>` 有多大。
> - **计算逻辑：** `A<int>` 的大小等于 `C<B<int>>` 的大小 + `A` 自己的字段。
> - **递归：** `C<B<int>>` 的大小是固定的（假设 C 没有字段，那它就是一个基础对象的大小）。**关键点就在这里：** 加载器在计算 `A` 的布局时，只需要知道 `B` 是一个类。只要知道 `B` 是类，那么在 `C` 里面引用 `B` 的地方就是一个 8 字节的指针空间。它**不需要**立刻知道 `B` 内部具体的细节。
>
> **如果是结构体（Struct），结果就完全不同了：**
>
> ```
> struct S1 { S2 field; }
> struct S2 { S1 field; }
> ```
>
> 这种情况，**编译器会直接报错**。为什么？因为结构体是值类型，`S1` 嵌套 `S2` 要求知道 `S2` 的大小，`S2` 又要求知道 `S1` 的大小。这在物理上是无法实现的（无限大小）。这就是真正的“死逻辑”，不论怎么用 Load Levels 都救不回来。
>
> ### 2. Load Levels 是在“解耦”初始化顺序
>
> Load Levels 的存在不是为了掩盖错误，而是为了**分步完成验证**。
>
> 你可以把 `CLASS_LOADED`（完全加载）想象成“通电”状态。
>
> 1. **Level A (创建壳子):** `A` 和 `B` 都通了电，知道彼此的存在和地址。
> 2. **Level B (解析父类):** `A` 顺着地址找到了 `B`，`B` 顺着地址找到了 `A`。
> 3. **Level C (确定内存布局):** 因为大家都是引用类型（指针），所以每个人都算出了自己需要的内存大小。
> 4. **Final (完成):** 逻辑闭环，验证通过。
>
> **一旦达到了 `CLASS_LOADED` 级别，这个类型就和任何普通类型一样稳定了。** 当你执行 `new A<int>()` 时：
>
> - 内存分配器（Allocator）已经明确知道要分多少字节。
> - GC 已经明确知道这个对象里哪些地方存的是指针。
> - 没有任何动态的、不确定的因素。

### 2.1.1 类型加载器内的加载级别使用
在类型加载器中，在类型加载器的各个部分操作时，对于可以使用什么类型加载级别适用各种不同的规则。

#### 2.1.1.1 `ClassLoader::CreateTypeHandleForTypeDefThrowing` 和 `MethodTableBuilder::BuildMethodTableThrowing` 内的代码
在执行 `ClassLoader::CreateTypeHandleForTypeDefThrowing` 中的代码时，在调用 `MethodTableBuilder::BuildMethodTableThrowing` 之前，任何逻辑都不能依赖于正在加载的类型的 `MethodTable`。这是因为这些例程正是构造 MethodTable 的例程。

这会带来多种影响，最明显的是：在不引发 TypeLoadException 风险的前提下，正在加载类型的基类型，以及任何相关接口或字段类型，都不能被加载到超过 CLASS_LOAD_APPROXPARENTS 的级别。例如，如果我们把基类型加载到 CLASS_LOAD_EXACTPARENTS，那么我们就无法加载一个从类型 `B<A>` 派生的类型 A。对此规则确实存在例外，并且为了真正实现类型加载过程也必须有例外，但一般应当避免，因为它们会导致与 ECMA 规范不一致的行为。

#### 2.1.1.2 `ClassLoader::DoIncrementalLoad` 内的代码
在 `DoIncrementalLoad` 期间运行的代码通常允许要求类型加载到正在增量加载到的级别，**或者**正在加载的类型已经达到的级别。这里的区别在于类型之间的关系是循环的还是非循环的。循环关系（如类型与其类型参数的关系）只能加载到低于所需加载级别的级别。非循环关系可以要求加载到增量操作最终将达到的加载级别。

例如，类型与其基类型的关系是非循环的，因为类型不能传递地成为其自己的确切基类型。
但是，类型与其基类型的实例化参数的关系可能是循环的。

作为上述规则的示例，考虑类型 `class A : B<A> {}`。
当将类 `A` 加载到 `CLASS_LOAD_EXACTPARENTS` 时，我们可以要求基类型 `B<A>` 加载到 `CLASS_LOAD_EXACTPARENTS`，因为这是一种非循环关系，但是当我们将 `B<A>` 加载到 `CLASS_LOAD_EXACTPARENTS` 时，我们不能要求类型 `A` 加载到 `CLASS_LOAD_EXACTPARENTS`，因为这会导致循环问题，因此将 `B<A>` 加载到 `CLASS_LOAD_EXACTPARENTS` 只能强制 `A` 加载到 `CLASS_LOAD_APPROXPARENTS`。

在 `ClassLoader::DoIncrementalLoad` 中运行的代码遵循一个相当直接的模式，即代码可以依赖于加载到特定加载级别的类型，当增量加载过程在给定级别完成时，正在加载的类型会在加载级别上增加。

#### 2.1.1.3 `PushFinalLevels` 内的代码
类型加载的最后两个级别通过 `PushFinalLevels` 处理，它遵循一组不同的规则。`PushFinalLevels` 运行的代码，为了提高级别，只能依赖于其他类型加载到低于所需级别的级别。但是，在将类型标记为达到更高级别之前，`PushFinalLevels` 可以要求其他类型也完成到新级别的 `PushFinalLevels` 算法。只有一旦确认所有类型都已达到新级别，才能将整组类型标记为达到新级别。

### 2.1.2 类型加载器外的加载级别使用
在一般情况下，当不在类型加载器部分的代码中操作时，最好简单地忽略加载级别，并简单地请求完全加载的类型。这应该是默认且在功能上始终正确的选择。但是，出于性能原因，可以只请求部分加载的类型，这就要求类型的用户确保他们的代码不依赖于完全加载的状态。

## 2.2 泛型

在没有泛型的世界里，一切都很美好：每个普通类型（不由 TypeDesc 表示的类型）都有一个 MethodTable 指向其关联的 EEClass，而 EEClass 又指回该 MethodTable。该类型的所有实例都把一个指向 MethodTable 的指针作为其第一个字段，位于偏移 0，即处于作为引用值所看到的那个地址。为节省空间，表示该类型声明的方法的 MethodDesc 会被组织成一条由 EEClass 指向的 chunk 链表⁴。

![Figure 3](./asserts/typeloader-fig3.png)

图 3 具有非泛型方法的非泛型类型

<sup>4</sup> 当然，当托管代码运行时，它不会通过在块中查找来调用方法。调用方法是一个非常“热”的操作，通常只需要访问 `MethodTable` 中的信息。

### 2.2.1 术语

**泛型参数 (Generic Parameter)**

一个占位符，将被另一个类型替换；例如 List<T> 声明中的 T。有时也称为形式类型参数（formal type parameter）。泛型参数有一个名称，以及可选的泛型约束。

**泛型实参（Generic Argument）**

被用来替换泛型参数的类型；例如 List<int> 中的 int。注意，泛型参数也可以被用作实参。考虑：

```csharp
List<T> GetList<T>()
{
    return new List<T>();
}
```

该方法有一个泛型参数 `T`，它被用作泛型列表类的泛型实参。

**泛型约束 (Generic Constraint)**

对泛型参数可能的泛型实参所施加的可选要求。不满足所需属性的类型不能被用于替换该泛型参数，并由类型加载器强制执行。泛型约束有三类：

1. 特殊约束
  - 引用类型约束 - 泛型实参必须是引用类型（相对于值类型）。C# 中使用 `class` 关键字来表达此约束。

    ```csharp
    public class A<T> where T : class
    ```

  - 值类型约束 - 泛型实参必须是不同于 `System.Nullable<T>` 的值类型。C# 使用 `struct` 关键字。

    ```csharp
    public class A<T> where T : struct
    ```

  - 默认构造函数约束 - 泛型实参必须具有公共无参数构造函数。C# 中由 `new()` 表示。

    ```csharp
    public class A<T> where T : new()
    ```

2. 基类型约束 - 泛型实参必须派生自（或直接是）给定的非接口类型。显然，仅使用零个或一个引用类型作为基类型约束是有意义的。

    ```csharp
    public class A<T> where T : EventArgs
    ```

3. 实现接口约束 - 泛型实参必须实现（或直接是）给定的接口类型。可以给出零个或多个接口。

    ```csharp
    public class A<T> where T : ICloneable, IComparable<T>
    ```

上述约束通过隐式 AND 组合，即泛型参数可以被约束为派生自给定类型，实现多个接口，并具有默认构造函数。声明类型的所有泛型参数都可用于表达约束，从而引入参数之间的相互依赖关系。例如：

```csharp
public class A<S, T, U>
	where S : T
	where T : IList<U> {
    void f<V>(V v) where V : S {}
}
```

**实例化 (Instantiation)**

替换泛型类型或方法的泛型参数的泛型实参列表。每个已加载的泛型类型和方法都有其实例化。

**典型实例化 (Typical Instantiation)**

纯粹由类型或方法自己的类型参数组成且顺序与参数声明顺序相同的实例化。每个泛型类型和方法都存在且仅存在一个典型实例化。通常，当人们谈论开放泛型类型时，他们指的是典型实例化。例如：

```csharp
public class A<S, T, U> {}
```

C# `typeof(A<,,>)` 编译为 ``ldtoken A`3``，这使得运行时加载在 `S`、`T`、`U` 处实例化的 ``A`3``。

**规范实例化 (Canonical Instantiation)**

一种实例化，其中所有泛型实参都是 `System.__Canon`。`System.__Canon` 是 corlib 中定义的内部类型，它的任务仅仅是“众所周知”并且与任何可能被用作泛型实参的其他类型都不同。采用规范实例化的类型/方法被用作所有实例化的代表，并携带所有实例化共享的信息。由于 `System.__Canon` 显然不可能满足各自泛型参数可能对其施加的任何约束，因此约束检查会针对 `System.__Canon` 做特殊处理并忽略这些违例。

### 2.2.2 共享

随着泛型的出现，运行时加载的类型数量往往会更高。尽管具有不同实例化的泛型类型（例如 `List<string>` 和 `List<object>`）是不同的类型，每个都有自己的 `MethodTable`，但事实证明它们可以共享大量信息。这种共享对内存占用和性能都有积极影响。

![Figure 4](./asserts/typeloader-fig4.png)

图 4 具有非泛型方法的泛型类型 - 共享 EEClass

目前，所有包含引用类型的实例化都会共享同一个 EEClass 及其 MethodDesc。这之所以可行，是因为所有引用的大小都相同——4 或 8 字节——因此这些类型的布局一致。图中以 List<object> 与 List<string> 说明了这一点。规范 MethodTable 会在第一个引用类型实例化被加载之前自动创建，并包含那些“热”但与实例化无关的数据，例如非虚槽（non-virtual slots）。只包含值类型的实例化不会被共享，每个这样的实例化类型都会获得一个不共享的 EEClass。

到目前为止加载过的、表示泛型类型的 MethodTable 会缓存在其加载器 module⁵ 所拥有的一个哈希表中。在构造新的实例化之前会先查询该哈希表，以确保永远不会存在两个或更多个表示同一类型的 MethodTable 实例。

有关泛型共享的更多信息，请参阅 [Design and Implementation of Generics for the .NET Common Language Runtime][generics-design]。

<sup>5</sup> 对于从 NGEN 映像加载的类型，情况会稍微复杂一些。
