# 从原理理解 .NET 11 的 Runtime Async 与 C# Union Type

.NET 11 新增的两个很值得认真研究的能力，一个在运行时层，一个在语言层：**runtime async** 与 **union type**。前者重塑了 `async/await` 的执行模型，后者补上了 C# 长期缺失的“闭合类型集合”表达能力。

如果只停留在表面，你可能只会记住两句话：

- runtime async：调用栈更干净了。
- union type：C# 终于也有点像 discriminated union 了。

这两句话都不算错，但都不足以帮助你真正理解它们。**真正值得掌握的，是它们各自想解决的老问题、旧方案为什么会长那样、新方案把哪部分职责迁移了，以及这种迁移为什么会带来今天看到的收益。**

## 为什么这两个功能应该放在一起看

表面上它们分属不同层次：

- runtime async 属于 CLR 与编译器协作的执行模型；
- union type 属于 C# 类型系统与模式匹配。

但从设计哲学上看，它们做的是同一件事：**把一种过去只能靠编译器技巧或库层约定模拟的能力，提升为平台原生理解的抽象。**

- runtime async：把异步控制流从“编译器生成状态机”推进到“运行时原生管理挂起与恢复”。
- union type：把“值只能是几种类型之一”从“接口/基类/OneOf 风格约定”推进到“语言可验证的闭合集合”。

这就是理解它们的总入口。

## 一、Runtime Async：先理解旧 async 为什么必须是状态机

### 同步函数为什么天然容易实现

同步函数的执行模型非常朴素：

1. 调用方把控制权交给被调用方。
2. 被调用方拥有一个正常的栈帧。
3. 局部变量活在这个栈帧里。
4. 返回时栈帧销毁。

这套模型的前提是：**函数不会中途“暂停一会儿，未来再从这里继续”。**

### `await` 真正打破了什么

`await` 的麻烦不在“它是异步的”，而在它要求函数具备一种同步函数天然没有的能力：

> 执行到一半时挂起，等未来某个时刻满足条件，再从原来的位置继续执行。

一旦允许“中途挂起”，就立刻出现三个底层问题：

1. 以后从哪一行继续？
2. 继续时哪些局部变量还要保留？
3. 结果、异常、continuation 要如何关联？

这正是传统调用栈不直接提供的能力。

### 为什么编译器生成状态机是一个合理解法

于是早期 C# async 采取了最务实的做法：**把 async 方法编译成状态机。**

一个看似线性的 async 方法：

```csharp
async Task<int> FooAsync()
{
    int x = 1;
    await BarAsync();
    return x + 1;
}
```

在底层会被改写成近似这样的思路：

- 一个状态字段，记录当前阶段；
- 若干字段，保存跨 `await` 仍要活下来的局部变量；
- 一个 `MoveNext()`，负责按状态恢复执行；
- 一个 builder，负责挂 continuation、回填结果和异常。

也就是说，**编译器把源码中的“顺序写法”翻译成了一个手工协程对象。**

这套方案很成功，因为它在不修改 CLR 基本执行模型的前提下，把 async/await 做出来了。

## 二、旧 async 的代价：为什么你会看到脏栈和奇怪的调试体验

当编译器把 async 方法降级成状态机后，运行时真正执行的就不再是“一个普通方法顺着跑完”，而是：

1. 初始化状态机
2. 启动 builder
3. 进入 `MoveNext`
4. 命中 `await`
5. 注册 continuation
6. 将来 continuation 回来后再次进入 `MoveNext`

因此，调试器和 profiler 在**实时执行期间**看到的调用栈，天然会混入大量基础设施帧，例如：

- `MoveNext`
- `AsyncMethodBuilderCore.Start`
- 其他编译器/运行时协作细节

所以“栈不干净”不是 UI 问题，而是**底层执行结构本来就暴露了这些层次**。

### 为什么异常栈和实时栈要分开看

官方文档特别提醒：runtime async 最直观的改善，是**实时调用栈**，例如：

- 调试器的 Call Stack 窗口
- profiler 看到的活动栈
- `new StackTrace()` 生成的执行时栈信息

但 `catch (Exception ex)` 中看到的异常栈，很多时候在传统 async 下已经经过编译器额外清理，因此有无 runtime async 时不一定呈现出同等级别的差异。

所以应该记住：

- **实时栈**：runtime async 改善最明显。
- **异常栈**：不一定出现同幅度变化。

这个区分非常重要。

## 三、Runtime Async 的本质：不是“没有状态”，而是“状态改由运行时原生管理”

第一次接触 runtime async，最容易产生一个误解：

> 既然不再生成状态机，那是不是就没有状态机了？

更准确的说法应该是：

> 只要方法能挂起并恢复，就一定需要状态；变化不是“状态消失”，而是“状态不再主要由编译器生成一个显式状态机类型来承担，而是由运行时直接理解和管理”。 

异步挂起永远离不开这些信息：

- 当前暂停到哪一步
- 哪些局部变量要跨挂起保活
- continuation 何时、如何恢复
- 结果和异常如何传回

runtime async 真正做的，是把这些信息从“编译器降级产物”提升成“CLR 可直接识别的执行协议”。

## 四、Runtime Async 具体改变了什么

### 1. async 方法被当成一种特殊方法执行

公开设计资料表明，runtime async 会让编译器生成带有特殊 async 标记的方法，并通过 `AsyncHelpers.Await(...)` 之类 helper 把挂起点编码到 IL 中。

这说明两件事：

1. 编译器并没有完全退出舞台；
2. 但运行时已经不再把 async 仅仅当作“普通方法 + 库调用”，而是理解成一种专门执行模式。

### 2. 编译器和运行时重新分工

在 runtime async 下，编译器仍然负责：

- 正常完成 C# 语义绑定；
- 把 `await` 降到符合 runtime async 协议的 IL；
- 对 `catch`/`finally` 里的 `await` 做必要重写。

运行时则负责：

- 识别 async 方法；
- 管理挂起与恢复；
- 维护更符合源码结构的实时栈；
- 在内部做统一优化。

所以 runtime async 不是“编译器完全不做事”，而是**职责边界后移**。

### 3. 为什么调用栈会更干净

因为运行时现在知道哪些帧是逻辑 async 帧，哪些只是实现细节。过去它只能看见编译器展开后的状态机结构，现在它可以更接近源码的角度来表达执行链。

因此像下面这样的链路：

```csharp
await OuterAsync();
```

在执行时更容易呈现成：

```text
InnerAsync
MiddleAsync
OuterAsync
Main
```

而不是混入大量 builder / 状态机框架帧。

### 4. 为什么调试体验也会变好

当调试器 step over 一个 `await` 时，以前它需要跨越编译器生成的状态机基础设施。runtime async 让运行时和调试器都更容易把“挂起/恢复”看成语言层逻辑，而不是在生成代码里来回跳。

于是你得到的是更自然的：

- 断点绑定
- 单步执行
- 调用栈展示

### 5. 性能收益应该怎样理解

runtime async 的收益方向主要是：

- 更积极复用 continuation 对象；
- 减少不必要的状态保存；
- 降低 async 密集型代码中的分配压力；
- 为进一步优化 async 链路铺路。

但正确预期不是“所有 async 代码都会立刻暴涨性能”，而是：

> async 的底层表达变得更适合统一优化，因此可观察性和性能空间同时变好。

## 五、Runtime Async 的边界与限制

它目前仍是预览功能，需要在 `net11.0` 项目中显式启用：

```xml
<PropertyGroup>
  <Features>runtime-async=on</Features>
</PropertyGroup>
```

从 Preview 3 开始，不再需要额外写：

```xml
<EnablePreviewFeatures>true</EnablePreviewFeatures>
```

同时也要意识到，它并不是“无限制保存任意执行现场”的魔法。公开设计资料里仍有不少边界，例如：

- 并非所有 Task-like 返回类型都进入 runtime async 模式；
- 某些 `byref` / `ref-like` 场景不能跨 suspension 点安全保留；
- 低层 IL 特性仍有约束；
- `catch` / `finally` 中的 `await` 需要特殊重写策略。

这背后的原理很简单：**只要要跨挂起保存状态，就一定要回答“哪些状态可以被安全、稳定、可恢复地保存”这个问题。**

## 六、Union Type：C# 长期缺失的到底是什么

如果说 runtime async 解决的是执行模型问题，那么 union type 解决的就是**建模问题**。

很多业务场景都需要表达：

> 一个值只能是若干种可能类型之一，并且这个集合是闭合的。

例如：

- 支付方式：`CreditCard | PayPal | BankTransfer`
- 结果：`Success | Error`
- 参数：`T | IEnumerable<T>`
- 消息：若干固定消息类型之一

注意这里最重要的词是：**闭合**。

## 七、为什么 `object`、接口、基类都不能完美表达这个需求

### `object` 的问题：太宽

`object` 确实能装任意值，但它表达不了“合法值只能是这些类型”。  
于是调用方永远要做防御式判断，编译器也无法做穷尽分析。

所以 `object` 解决的是承载，不是约束。

### 接口 / 基类的问题：不闭合

接口和抽象基类能把值限制到某个家族里，但它们通常没有“全集封闭”语义。编译器没法默认世界上只剩当前已知的几个实现。

所以你可以写模式匹配，但编译器很难说：

> 你已经处理完所有可能情况了。

这就是为什么继承层次擅长表达“共享抽象”，却不擅长表达“有限全集”。

### 第三方联合类型库的问题：不是语言级事实

OneOf、Result 之类库可以很好用，但终究仍是库类型。语言和编译器不把它们天然当作“闭合集合”，就很难给出真正一等公民的穷尽检查、nullability 推理和 IDE 体验。

## 八、Union Type 的关键突破：把“闭合集合”变成编译器可推理事实

在 C# 15 预览中，你可以写：

```csharp
public record class CreditCard(string Number, string Holder, DateOnly Expiry);
public record class PayPal(string Email);
public record class BankTransfer(string Iban, string Bic);

public union PaymentMethod(CreditCard, PayPal, BankTransfer);
```

这个声明最重要的信息不是“有三个类型”，而是：

> `PaymentMethod` 的值空间被语言定义为这三种 case type 的闭合集。

一旦编译器知道这个事实，就能自然推出几类能力：

### 1. 从 case type 到 union 的隐式转换

```csharp
PaymentMethod method = new PayPal("alice@example.com");
```

因为 `PayPal` 是合法 case type，所以这种赋值天然成立。

### 2. `switch` 的穷尽匹配

```csharp
string Describe(PaymentMethod method) => method switch
{
    CreditCard c => $"Card: {c.Number}",
    PayPal p => $"PayPal: {p.Email}",
    BankTransfer b => $"Bank: {b.Iban}",
};
```

这里如果你漏掉某个 case，编译器可以给出警告。  
这就是 union 最大的价值：**把遗漏处理从运行时风险提前到编译期反馈。**

### 3. 更强的 nullability 分析

编译器知道 union 的 case 集合，也知道 `Value` 当前的 null-state，因此能做比 `object` / 普通接口层次更精确的静态分析。

## 九、Union Type 的底层心智模型：它不是魔法容器，而是一种被语言识别的模式

官方文档对 union 的实现思路非常坦率：`union` 关键字只是最方便的声明方式，但语言真正识别的是一组 union 约定。

默认生成的 union 更偏向一种“意见明确”的实现：

- 通常生成 struct；
- 通过 `object? Value` 保存内容；
- 为每个 case type 生成对应创建入口和隐式转换。

这意味着 union 的底层语义更接近：

> 一个持有“当前内容值”的包装体，编译器知道它只允许持有若干 case type。

### 这为什么是一个好设计

因为它实现了三个目标：

1. 语义简单；
2. 易与现有模式匹配衔接；
3. 不把语言设计绑死在某一种存储布局上。

### 代价是什么

如果 case type 包含值类型，例如：

```csharp
public union IntOrString(int, string);
```

那么默认实现里 `int` 通常需要装箱到 `object`。这说明 union 默认设计优先的是**统一语义与易用性**，而不是对每个值类型场景都追求最极致布局。

## 十、为什么又允许“自定义 union”

这是 C# union 很有意思的一点：**语言特性和默认声明语法是分离的。**

也就是说：

- 你可以用 `union` 关键字快速声明；
- 也可以自己定义一个 class/struct，只要满足 `[Union]` 约定模式，就能获得 union 行为。

这么设计有两个重要好处：

### 1. 现有库类型能逐步接入

语言不用要求所有人都迁移到某个固定生成类型，只要满足 union 约定，就能享受语言行为。

### 2. 高性能场景可以自定义存储

如果你不希望所有值类型 case 都走 `object` 装箱路径，可以自定义更紧凑的表示，并配合：

- `HasValue`
- `TryGetValue(out T)`

之类的非装箱访问模式，让编译器在匹配时走更强类型、更少装箱的路径。

这说明 union 的本质不是“一个新的 BCL 容器类”，而是**语言识别并利用的一组结构模式**。

## 十一、Union Type 最容易误解的地方：匹配对象是谁？

一个很重要但很容易忽略的细节是：

> 对 union 做模式匹配时，除 `var` 和 `_` 等少数例外外，模式默认匹配的是 union 的内容 `Value`，而不是 union 外壳本身。

例如：

```csharp
var text = pet switch
{
    Dog d => d.Name,
    Cat c => c.Name,
    Bird b => b.Name,
    null => "no pet",
};
```

这里匹配的焦点，是里面包着的具体值。

理解这一点后，很多行为就不再奇怪：

- 为什么 `Dog d` 能直接匹配；
- 为什么 `null` 处理的是内容为空；
- 为什么某些“拿 union 自己去匹配 union 类型”的写法不如预期。

## 十二、为什么 `null` 分支在 union 里经常绕不过去

默认 union 形态通常是 struct，而 struct 有合法的 `default` 值。  
这意味着即便你没有显式放入任何 case，union 自身也可能处于“存在一个实例，但内容为 `null`”的状态。

所以像下面这样的代码经常是必要的：

```csharp
Pet pet = default;

var description = pet switch
{
    Dog d => d.Name,
    Cat c => c.Name,
    Bird b => b.Name,
    null => "no pet",
};
```

这提醒我们：**union 的 null 语义不仅取决于 case type 列表，也取决于 union 自身是 struct 还是 class，以及当前 `Value` 的状态。**

## 十三、把 Runtime Async 和 Union Type 放回同一张图里

现在可以回头看这两个功能的共同性了。

### Runtime Async 解决的是：

> “异步挂起/恢复以前主要靠编译器把源码翻译成状态机对象来表达。”

### Union Type 解决的是：

> “闭合类型集合以前主要靠 `object`、继承层次或第三方库来模拟。”

它们共同体现出同一个趋势：

**.NET 平台在把经常出现且足够重要的高层抽象，逐步下沉成运行时或语言的一等公民。**

一旦抽象被平台原生理解，就会同时带来：

- 更正确的表达能力；
- 更强的静态分析；
- 更好的 IDE 与调试体验；
- 更大的运行时优化空间。

## 十四、今天应该如何使用和看待它们

### 对 runtime async 的正确预期

它不是一个新语法，而是 `async/await` 执行模型的升级。  
你最先体会到的价值通常是：

1. 实时调用栈更干净；
2. 调试体验更自然；
3. async 密集场景的分配与内部调度更有优化空间。

### 对 union type 的正确预期

它不是简单的“又一种封装类”，而是让业务模型中的“有限可能性集合”终于能被 C# 原生表达。

你最先体会到的价值通常是：

1. API 契约更准确；
2. `switch` 更安全；
3. 新增 case 后，编译器会帮助你找到漏处理的位置。

## 十五、当前预览阶段需要知道的现实细节

### Runtime Async

在 .NET 11 Preview 3 中，需要在项目文件中启用：

```xml
<PropertyGroup>
  <Features>runtime-async=on</Features>
</PropertyGroup>
```

而 `net11.0` 项目从 Preview 3 开始不再需要额外的：

```xml
<EnablePreviewFeatures>true</EnablePreviewFeatures>
```

### Union Type

union type 从 .NET 11 Preview 2 开始可体验，目前仍属于预览特性。要尝试它，通常需要：

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <LangVersion>preview</LangVersion>
</PropertyGroup>
```

按 Preview 3 的 release notes，当前还需要在项目中自行提供 `UnionAttribute` 和 `IUnion` 的 polyfill。

一个最小可用版本如下：

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute;

    public interface IUnion
    {
        object? Value { get; }
    }
}
```

补上之后，就可以开始尝试：

```csharp
public record class Cat(string Name);
public record class Dog(string Name);

public union Pet(Cat, Dog);
```

## 结语

如果只记 API 和开关，这两个功能都很容易被低估。

但从原理上看，它们都非常有代表性：

- runtime async 说明 .NET 正在把异步从“编译器技巧”推进为“运行时原生能力”；
- union type 说明 C# 正在把“值属于有限若干种类型之一”推进为“语言可验证事实”。

它们都不只是“好用一点”的小功能，而是在**降低实现细节泄漏、强化平台原生理解能力**。

这正是现代语言和运行时演进最值得关注的方向之一。

## 参考资料

- .NET 11 Runtime 新特性：`runtime async` 官方说明
  
  <https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-11/runtime>
  
- C# union types 官方语言参考  

  <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/union>

- C# 15 union types 官方博客
  <https://devblogs.microsoft.com/dotnet/csharp-15-union-types/>
  
- Runtime async 运行时设计草案
  <https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md>
  
- Roslyn 对 runtime async 的编译器设计说明
  <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Runtime%20Async%20Design.md>
