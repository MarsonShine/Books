# 类型加载器设计

作者：Ladi Prosek - 2007

# 1. 简介

在基于类的面向对象系统中，类型是描述单个实例将包含的数据以及它们将提供的功能的模板。如果不先定义对象的类型<sup>1</sup>，就无法创建对象。如果两个对象是同一类型的实例，则称它们具有相同的类型。它们定义了完全相同的成员集这一事实并不会使它们以任何方式相关联。

上一段也可以用来描述典型的 C++ 系统。CLR 的一个重要附加特性是提供完整的运行时类型信息。为了“管理”托管代码并提供类型安全的环境，运行时必须随时知道任何对象的类型。这种类型信息必须可以随时获得，而无需进行大量计算，因为类型标识查询预计会相当频繁（例如，任何类型转换都涉及查询对象的类型标识，以验证转换是否安全且可以进行）。

这种性能要求排除了任何字典查找方法，只剩下以下高级架构。

![Figure 1](images/typeloader-fig1.png)

图 1 抽象的高级对象设计

除了实际的实例数据外，每个对象都包含一个类型 ID，它只是一个指向表示该类型的结构的指针。这个概念类似于 C++ 的 v-table 指针，但我们现在称为 TYPE 的结构（稍后将更精确地定义它）包含的不仅仅是一个 v-table。例如，它必须包含有关层次结构的信息，以便可以回答“is-a”包含关系问题。

<sup>1</sup> C# 3.0 中称为“匿名类型”的特性允许你在不显式引用类型的情况下定义对象 - 只需直接列出其字段即可。别被这个骗了，实际上编译器在幕后为你创建了一个类型。

## 1.1 相关阅读

[1] Martin Abadi, Luca Cardelli, A Theory of Objects, ISBN
978-0387947754

[2] Andrew Kennedy ([@andrewjkennedy](https://github.com/andrewjkennedy)), Don Syme ([@dsyme](https://github.com/dsyme)), [Design and Implementation of Generics
for the .NET Common Language
Runtime][generics-design]

[generics-design]: http://research.microsoft.com/apps/pubs/default.aspx?id=64031

[3] [ECMA Standard for the Common Language Infrastructure (CLI)](https://www.ecma-international.org/publications-and-standards/standards/ecma-335)

## 1.2 设计目标

类型加载器（有时称为类加载器，严格来说这并不正确，因为类只是类型的一个子集——即引用类型——加载器也会加载值类型）的最终目的是构建表示它被要求加载的类型的数据结构。加载器应具有以下属性：

- 快速类型查找（[module, token] => handle 和 [assembly, name] => handle）。
- 优化的内存布局，以实现良好的工作集大小、缓存命中率和 JIT 代码性能。
- 类型安全 - 不会加载格式错误的类型，并抛出 TypeLoadException。
- 并发性 - 在多线程环境中具有良好的扩展性。

# 2 类型加载器架构

加载器的入口点数量相对较少。虽然每个单独入口点的签名略有不同，但它们都具有相似的语义。它们接受元数据 **token（令牌）** 或 **name（名称）** 字符串形式的类型/成员名称，令牌的作用域（**module（模块）** 或 **assembly（程序集）**），以及一些附加信息（如标志）。它们以 **handle（句柄）** 的形式返回已加载的实体。

在 JIT 编译期间通常会有很多对类型加载器的调用。考虑：

```csharp
object CreateClass()
{
    return new MyClass();
}
```

在 IL 中，MyClass 使用元数据令牌引用。为了生成对负责实际实例化的 `JIT_New` 辅助函数的调用，JIT 将要求类型加载器加载该类型并返回其句柄。然后，此句柄将作为立即数直接嵌入到 JIT 代码中。类型和成员通常在 JIT 时而不是运行时解析和加载的事实也解释了有时容易遇到的令人困惑的行为，例如：

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

CLR 中最通用的类型标识是 `TypeHandle`。它是一个抽象实体，封装了指向 `MethodTable`（表示“普通”类型，如 `System.Object` 或 `List<string>`）或 `TypeDesc`（表示 byref、指针、函数指针和泛型变量）的指针。它构成了类型的标识，即当且仅当两个句柄表示相同的类型时，它们才相等。为了节省空间，`TypeHandle` 包含 `TypeDesc` 的事实是通过将指针的第二低位设置为 1（即 (ptr | 2)）而不是使用额外的标志来指示的<sup>2</sup>。`TypeDesc` 是“抽象的”，具有以下继承层次结构。

![Figure 2](images/typeloader-fig2.png)

图 2 TypeDesc 层次结构

**`TypeDesc`**

抽象类型描述符。具体的描述符类型由标志确定。

**`TypeVarTypeDesc`**

表示类型变量，即 `List<T>` 或 `Array.Sort<T>` 中的 `T`（参见下面关于泛型的部分）。类型变量永远不会在多个类型或方法之间共享，因此每个变量都有其唯一的所有者。

**`FnPtrTypeDesc`**

表示函数指针，本质上是指向返回类型和参数的类型句柄的可变长度列表。它最初仅由托管 C++ 使用。C# 自 C# 9 起支持它。

**`ParamTypeDesc`**

此描述符表示 byref 和指针类型。Byref 是应用于方法参数的 `ref` 和 `out` C# 关键字的结果<sup>3</sup>，而指针类型是在不安全 C# 和托管 C++ 中使用的指向数据的非托管指针。

**`MethodTable`**

这是目前运行时中最核心的数据结构。它表示不属于上述类别之一的任何类型（这包括基元类型和泛型类型，包括“开放”和“封闭”）。它包含有关类型的所有需要快速查找的信息，例如其父类型、实现的接口和 v-table。

**`EEClass`**

`MethodTable` 数据分为“热”和“冷”结构，以提高工作集和缓存利用率。`MethodTable` 本身旨在仅存储程序稳定状态所需的“热”数据。`EEClass` 存储通常仅由类型加载、JIT 编译或反射所需的“冷”数据。每个 `MethodTable` 指向一个 `EEClass`。

此外，`EEClass` 由泛型类型共享。多个泛型类型 `MethodTable` 可以指向单个 `EEClass`。这种共享对可以存储在 `EEClass` 上的数据施加了额外的限制。

**`MethodDesc`**

毫无疑问，这个结构描述了一个方法。实际上它有几种变体，具有相应的 `MethodDesc` 子类型，但其中大多数实际上超出了本文档的范围。可以说有一个名为 `InstantiatedMethodDesc` 的子类型，它在泛型中起着重要作用。有关更多信息，请参阅 [**Method Descriptor Design**](method-descriptor.md)。

**`FieldDesc`**

类似于 `MethodDesc`，此结构描述一个字段。除了某些 COM 互操作场景外，EE 根本不关心属性和事件，因为它们归根结底归结为方法和字段，只有编译器和反射生成和理解它们，以便提供那种语法糖般的体验。

<sup>2</sup> 这对调试很有用。如果 `TypeHandle` 的值以 2、6、A 或 E 结尾，则它不是 `MethodTable`，必须清除额外的位才能成功检查 `TypeDesc`。

<sup>3</sup> 请注意，`ref` 和 `out` 之间的区别仅在于参数属性。就类型系统而言，它们都是相同的类型。

## 2.1 加载级别 (Load Levels)

当类型加载器被要求加载指定的类型时（例如通过 typedef/typeref/typespec **令牌**和 **Module** 标识），它不会一次性原子地完成所有工作。相反，加载是分阶段进行的。这样做的原因是，类型通常依赖于其他类型，要求其完全加载才能被其他类型引用会导致无限递归和死锁。考虑：

```csharp
class A<T> : C<B<T>>
{ }

class B<T> : C<A<T>>
{ }

class C<T>
{ }
```

这些是有效类型，显然 `A` 依赖于 `B`，`B` 依赖于 `A`。

加载器最初创建表示类型的结构，并使用无需加载其他类型即可获得的数据对其进行初始化。当此“无依赖”工作完成后，可以从其他地方引用该结构，通常是通过将指向它们的指针粘贴到其他结构中。之后，加载器逐步进行，用越来越多的信息填充结构，直到最终得到完全加载的类型。在上面的示例中，`A` 和 `B` 的基类型将由不包含另一种类型的近似值表示，并稍后由真实类型替换。

确切的半加载状态由所谓的加载级别描述，从 CLASS\_LOAD\_BEGIN 开始，以 CLASS\_LOADED 结束，中间有几个中间级别。[classloadlevel.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/classloadlevel.h) 源文件中有关于各个加载级别的丰富且有用的注释。

有关加载级别的更详细说明，请参阅 [Design and Implementation of Generics for the .NET Common Language Runtime][generics-design]。

### 2.1.1 类型加载器内的加载级别使用
在类型加载器中，在类型加载器的各个部分操作时，对于可以使用什么类型加载级别适用各种不同的规则。

#### 2.1.1.1 `ClassLoader::CreateTypeHandleForTypeDefThrowing` 和 `MethodTableBuilder::BuildMethodTableThrowing` 内的代码
在执行 `ClassLoader::CreateTypeHandleForTypeDefThrowing` 中的代码时，在调用 `MethodTableBuilder::BuildMethodTableThrowing` 之前，任何逻辑都不能依赖于正在加载的类型的 `MethodTable`。这是因为这些是构建 `MethodTable` 的例程。

这有各种含义，但最明显的是，正在加载的类型的基类型以及任何关联的接口或字段类型不能加载超过 `CLASS_LOAD_APPROXPARENTS`，否则会有触发 `TypeLoadException` 的风险。例如，如果我们将 Base 类型加载到 `CLASS_LOAD_EXACTPARENTS`，那么我们就无法加载派生自类型 `B<A>` 的类型 `A`。此规则存在例外，并且是实际实现类型加载过程所必需的，但通常应避免，因为它们会导致与 ECMA 规范不符的行为。

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

在无泛型的世界里，一切都很美好，每个人都很开心，因为每个普通（非 `TypeDesc` 表示）类型都有一个指向其关联 `EEClass` 的 `MethodTable`，而 `EEClass` 又指回 `MethodTable`。类型的所有实例在其偏移量 0 处的第一个字段中包含一个指向 `MethodTable` 的指针，即在引用值看到的地址处。为了节省空间，表示类型声明的方法的 `MethodDesc` 被组织在由 `EEClass` 指向的块链接列表中<sup>4</sup>。

![Figure 3](images/typeloader-fig3.png)

图 3 具有非泛型方法的非泛型类型

<sup>4</sup> 当然，当托管代码运行时，它不会通过在块中查找来调用方法。调用方法是一个非常“热”的操作，通常只需要访问 `MethodTable` 中的信息。

### 2.2.1 术语

**泛型参数 (Generic Parameter)**

将被另一种类型替换的占位符；`List<T>` 声明中的 `T`。有时称为形式类型参数。泛型参数具有名称和可选的泛型约束。

**泛型参数 (Generic Argument)**

被替换为泛型参数的类型；`List<int>` 中的 `int`。请注意，泛型参数也可以用作泛型实参。考虑：

```csharp
List<T> GetList<T>()
{
    return new List<T>();
}
```

该方法有一个泛型参数 `T`，它被用作泛型列表类的泛型实参。

**泛型约束 (Generic Constraint)**

泛型参数对其潜在泛型实参施加的可选要求。不具有所需属性的类型不得替换泛型参数，这是由类型加载器强制执行的。有三种泛型约束：

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

所有泛型实参都是 `System.__Canon` 的实例化。`System.__Canon` 是 **corlib** 中定义的内部类型，其任务只是广为人知并且不同于任何其他可用作泛型实参的类型。具有规范实例化的类型/方法被用作所有实例化的代表，并携带所有实例化共享的信息。由于 `System.__Canon` 显然不能满足相应泛型参数对其可能具有的任何约束，因此相对于 `System.__Canon`，约束检查是特殊情况，并忽略这些违规。

### 2.2.2 共享

随着泛型的出现，运行时加载的类型数量往往会更高。尽管具有不同实例化的泛型类型（例如 `List<string>` 和 `List<object>`）是不同的类型，每个都有自己的 `MethodTable`，但事实证明它们可以共享大量信息。这种共享对内存占用和性能都有积极影响。

![Figure 4](images/typeloader-fig4.png)

图 4 具有非泛型方法的泛型类型 - 共享 EEClass

目前，包含引用类型的所有实例化都共享相同的 `EEClass` 及其 `MethodDesc`。这是可行的，因为所有引用的大小相同 - 4 或 8 字节 - 因此所有这些类型的布局都是相同的。该图说明了 `List<object>` 和 `List<string>` 的这种情况。规范 `MethodTable` 是在第一个引用类型实例化加载之前自动创建的，并且包含热的但非实例化特定的数据，如非虚槽。仅包含值类型的实例化不共享，并且每个此类实例化类型都有其自己未共享的 `EEClass`。

到目前为止加载的表示泛型类型的 `MethodTable` 缓存在其加载器模块<sup>5</sup>拥有的哈希表中。在构建新实例化之前会查询此哈希表，确保永远不会有两个或更多 `MethodTable` 实例表示同一类型。

有关泛型共享的更多信息，请参阅 [Design and Implementation of Generics for the .NET Common Language Runtime][generics-design]。

<sup>5</sup> 对于从 NGEN 映像加载的类型，情况会稍微复杂一些。
