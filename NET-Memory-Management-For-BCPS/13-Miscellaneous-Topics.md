# 其他主题

到目前为止，本书所有章节都聚焦于.NET内存管理的不同方面，尤其是垃圾回收器的工作原理。此刻您已掌握了理解这套机制底层原理所需的大部分知识。之所以说“大部分”，是因为受限于书籍篇幅，当然还存在一些未涉及的细节。这些知识始终贯穿着实践技巧和各种诊断场景，但为了保持章节清晰且篇幅合理，部分更进阶的实践内容被暂时搁置。本章与下一章将专门探讨这些主题，堪称.NET内存管理的“精华指南”，完全聚焦于内存管理的高级实践。随着.NET生态中性能敏感型代码的普及（尤其是 `Span`                                                                                                                                                                                       及其相关技术的广泛应用），这些技巧正被越来越多开发者采用。

鉴于本章的补充性质，内容采用模块化结构编排。您可以自由选读感兴趣的部分，但我们强烈建议您逐节阅读！

## 依赖句柄

除已知的句柄类型外，.NET Framework 4.0（及.NET Core）还引入了一种未提及的特殊句柄——依赖句柄。它在主对象（primary）与依赖对象（secondary）之间建立双向关联，实现生命周期绑定：

- 从依赖对象到主对象表现为弱引用（不影响生命周期，依赖对象不会阻止主对象被回收）。
- 从主对象到依赖对象表现为强引用（只要主对象存活，依赖对象就会保持活跃）。

凭借这些特性，依赖句柄成为非常灵活的工具，例如可以实现动态“附加字段”的功能。实际上这正是其设计初衷，后文将具体展示。

与其他句柄类型不同，依赖句柄不能通过 `GCHandle API` 创建。自.NET 6+起，可通过 `System.Runtime.CompilerServices` 命名空间下的 `DependentHandle` 类实现。

代码清单13-1 `DependentHandle` 使用示例

```csharp
static object _target = new object();  

[MethodImpl(MethodImplOptions.AggressiveOptimization)]  
static void TestDependent()  
{
    var dependent = new Object(); 
    var dependentWeakRef = new WeakReference(dependent);
    var dependentHandle = new DependentHandle(_target, dependent);
    
    Console.WriteLine(dependentHandle.Target == null); // False
    Console.WriteLine(dependentWeakRef.IsAlive); // True
    
    // 原子化操作获取主对象和依赖对象
    var (target2, dependent2) = dependentHandle.TargetAndDependent;
    
    _target = null; // 清除主对象引用
    GC.Collect(2); // 强制执行完全回收
    Console.WriteLine(dependentHandle.Target == null); // True
    Console.WriteLine(dependentWeakRef.IsAlive); // False
    
    dependentHandle.Dispose(); // 释放句柄资源
}
```

在清单13-1中，我们创建两个对象并通过 `DependentHandle` 绑定其生命周期。即使“dependent”对象不再被直接引用（仅通过 `WeakReference` 弱引用），只要“_target”存在它就保持活跃。当“_target”引用被清除后，“dependent”随即变为不可达状态。注意务必调用 `Dispose()` 释放依赖句柄占用的资源。

依赖句柄（Dependent handles）在运行时内部有多种用途，例如支持在调试器的“编辑并继续”功能中添加字段。调试器无法直接修改对象的运行时布局来新增字段，因为堆上可能已存在该对象的实例。因此，依赖句柄会在这种场景下维护它们之间的生命周期关联关系。

另一种使用依赖句柄的方式（也是.NET 6之前的唯一方式）是通过包装类 `ConditionalWeakTable`。正如其源代码注释所述，它提供“对运行时生成对象字段的编译器支持”，并“允许DLR和其他语言编译器在运行时为托管对象实例附加任意‘属性’”。`ConditionalWeakTable` 以字典形式组织，其中键存储目标对象，值存储附加的“属性”（依赖对象）。字典键采用弱引用，不会阻止这些对象被回收（与普通字典键不同）。当键对象被回收时，`ConditionalWeakTable` 会自动移除对应的字典条目。

`ConditionalWeakTable` 的API设计直观，与常规泛型 `Dictionary<TKey, TValue>` 类似（见代码清单13-2）。通过 `Add` 方法会创建一个新的底层依赖句柄，将值实例“添加”到键实例上。由于键必须唯一（通过 `Object.ReferenceEquals` 比较），该类每个托管对象只能附加单个值（如需附加多个属性，可将元组或集合作为值存储）。如代码清单13-2所示，可通过 `TryGetValue` 方法尝试获取与给定键关联的值。

代码清单13-2 `ConditionalWeakTable` 使用示例

```csharp
class SomeClass   
{ public int Field; }  

class SomeData   
{ public int Data; }  

public static void SimpleConditionalWeakTableUsage()   
{ 
    // SomeClass(主对象)与SomeData(次对象)间的依赖句柄
    var weakTable = new ConditionalWeakTable<SomeClass, SomeData>();  
    var obj1 = new SomeClass();  
    var data1 = new SomeData();  
    var obj1weakRef = new WeakReference(obj1);  
    var data1weakRef = new WeakReference(data1);  

    weakTable.Add(obj1, data1);  // 若键已存在会抛出异常
    weakTable.AddOrUpdate(obj1, data1);  

    GC.Collect();  
    Console.WriteLine($"{obj1weakRef.IsAlive} {data1weakRef.IsAlive}");  // 输出True True  

    if (weakTable.TryGetValue(obj1, out var value))  
    { 
        Console.WriteLine(value.Data);  
    }  

    GC.KeepAlive(obj1);  
    GC.Collect();  
    Console.WriteLine($"{obj1weakRef.IsAlive} {data1weakRef.IsAlive}");  // 输出False  
}
```

若未调用代码清单13-2中的 `GC.KeepAlive`，在第一次 `GC.Collect` 后 `obj1` 和 `data1` 实例可能已被回收（如第8章所述，当JIT编译器决定激进回收根引用时）。反之，如果我们改为调用 `GC.KeepAlive(data1)` 来保持次对象（值）而非主对象（键）存活，第一个 `Console.WriteLine` 很可能会输出 False True——此时由于没有引用持有键对象，它已被回收。

需要注意的是，每个托管对象（键）只能关联单个值的限制来自 `ConditionalWeakTable`，而非依赖句柄本身。因此如代码清单13-3所示，完全可以通过多个 `ConditionalWeakTable` 实例为同一对象添加多个“值”。

列表13-3. `ConditionalWeakTable` 使用示例

```c#
var obj1 = new SomeClass();
var weakTable1 = new ConditionalWeakTable<SomeClass, SomeData>();
var weakTable2 = new ConditionalWeakTable<SomeClass, SomeData>();
var data1 = new SomeData();
var data2 = new SomeData();
weakTable1.Add(obj1, data1);
weakTable2.Add(obj1, data2);
```

底层依赖句柄的弱引用表现为长弱引用（第12章已介绍），因此即使目标对象正在被终结，它们仍会维持目标对象与依赖对象之间的关系（见列表13-4）。这使您能正确处理对象复活场景。

列表13-4. 依赖句柄的终结行为

```c#
class FinalizableClass : SomeClass
{
	~FinalizableClass() { 
        Console.WriteLine("~FinalizableClass"); 
    }
}

public static void FinalizationUsage()
{

    ConditionalWeakTable<SomeClass, SomeData> weakTable = new ConditionalWeakTable<SomeClass, SomeData>();
    var obj1 = new FinalizableClass();
    var data1 = new SomeData();
    var obj1weakRef = new WeakReference(obj1, trackResurrection: true);
    var data1weakRef = new WeakReference(data1, trackResurrection: true);
    weakTable.Add(obj1, data1);
    GC.Collect();
    Console.WriteLine("obj1weakRef.IsAlivedata1weakRef.IsAlive");//输出 True True
    GC.KeepAlive(obj1);
    GC.Collect();
    Console.WriteLine("{obj1weakRef.IsAlive} {data1weakRef.IsAlive}"); // 输出 True True      
    GC.KeepAlive(obj1);      
    GC.Collect();      
    Console.WriteLine("obj1weakRef.IsAlivedata1weakRef.IsAlive");//输出 True True
    GC.KeepAlive(obj1);
    GC.Collect();
    Console.WriteLine("{obj1weakRef.IsAlive} {data1weakRef.IsAlive}"); // 输出 True True
    GC.WaitForPendingFinalizers();
    GC.Collect();
    Console.WriteLine($"{obj1weakRef.IsAlive} {data1weakRef.IsAlive}"); // 输出 False False
}
```

在WinDbg中，依赖句柄被视为常规句柄，因此可用 `!gchandles` SOS命令进行分析（见列表13-5）。由于 `ConditionalWeakTable` 内部容器是可终结的，您也常会在终结队列中见到它（见列表13-6）。

列表13-5. `!gchandles` SOS扩展命令运行结果（针对类似列表13-3的代码）

```
> !gchandles -stat
...
Handles:
Strong Handles: 11
Pinned Handles: 1
Weak Long Handles: 40
Weak Short Handles: 6
Dependent Handles: 2
> !gchandles -type Dependent
Handle Type Object Size Data Type
00000292abfe1bf0 Dependent 00000292b034d188 24 00000292b034d448 SomeClass
00000292abfe1bf8 Dependent 00000292b034d188 24 00000292b034d430 SomeClass
Statistics:
MT Count TotalSize Class Name
00007fff033166b8 2 48 SomeClass
Total 2 objects
```

清单13-6.  `!finalizequeue` SOS扩展命令执行结果（针对类似清单13-3的代码）

```
> !finalizequeue
...
Statistics for all finalizable objects (including all objects ready for finalization):
MT Count TotalSize Class Name
...
00007fff03429678 2 112 System.Runtime.CompilerServices.
ConditionalWeakTable<SomeClass, SomeData>+Container
Total 32 objects, 2,152 byte
```

> 依赖句柄（Dependent handles）可用于实现缓存或弱事件模式。在前者场景中，只要关联对象存活，就可以缓存与该对象相关的数据。在后者场景中，可以将处理程序（委托）的生命周期与目标对象生命周期进行适当绑定（关于弱事件模式的完整描述参见第12章）。清单13-7展示了Windows Presentation Foundation中 `WeakEventManager` 类的部分实现。为了将委托生命周期与其目标对象绑定，这里使用了 `ConditionalWeakTable`（由 `_cwt` 字段表示）。通过这种方式，只要目标对象存活，委托列表就会保持活跃状态。

清单13-7.  `ListenerList` 类方法（摘自WPF的 `WeakEventManager` 类）

```c#
public void AddHandler(Delegate handler)
{
    object target = handler.Target;
    // 将记录添加到主列表
    list.Add(new Listener(target, handler));
    AddHandlerToCWT(target, handler);
}

void AddHandlerToCWT(object target, Delegate handler)
{
    // 将处理程序添加到CWT——这能确保处理程序在目标对象存活期间保持活跃
    // 同时不会延长目标对象本身的生存期
    object value;
    if (!_cwt.TryGetValue(target, out value))
    {
        // 99%的情况——目标对象仅单次监听
        _cwt.Add(target, handler);
    }
    else
    {
        // 1%的情况——目标对象多次监听
        // 我们将委托存储在列表中
        List list = value as List;
        if (list == null)
        {
            // 惰性初始化列表，并添加旧处理程序
            Delegate oldHandler = value as Delegate;
            list = new List();
            list.Add(oldHandler);
            // 将列表作为新值存入CWT
            _cwt.Remove(target);
            _cwt.Add(target, list);
        }
        // 将新处理程序添加到列表
        list.Add(handler);
    }
}
```

> 在标记阶段，依赖句柄需要特殊扫描方式，因为它们可能形成复杂的依赖关系，单次扫描往往不足。假设句柄表中按序存储三个依赖句柄：对象C指向对象A，B指向C，A指向B。若已确定对象A的可达性（标记为可达），首次扫描仅会标记B为可达。第二次扫描将标记C为可达（因为此时GC已知B可达）。第三次扫描不会改变任何状态（A已被标记），至此整个分析终止。对于数百万个依赖句柄及其复杂依赖关系，这种多轮次扫描理论上可能带来开销。此外，与其他类型句柄不同，依赖句柄目前不会被GC“分代”处理（仅因实现复杂度而未实现），这意味着即便Gen 0回收也会扫描所有依赖句柄。因此请谨慎使用依赖句柄。
>
> 若想深入探究.NET Core中的实现细节，可从 `gc_heap::background_scan_dependent_handles` 和 `gc_heap::scan_dependent_handles` 方法入手。这两个方法及其调用的 `GcDhReScan、GcDhUnpromotedHandlesExist` 都有详细注释。标记阶段开始时调用的 `GcDhInitialScan` 方法，其相关注释同样揭示了依赖句柄的实现机制。

> 关于依赖句柄的机制用一句话来总结就是：
>
> **依赖句柄（Dependent Handle）让你把两个对象的生命周期绑定起来，主对象在，副对象不回收；主对象没了，副对象也能自动回收。**
>
> A是主对象（primary），B是依赖对象（secondary）。当发生GC回收时，如果只剩下“依赖句柄”持有A和B：
>
> - 如果A可以被回收（没有其它强引用），则B也可以回收。
> - 如果A不能回收，则B也暂时不能回收，即便B没有其它强引用。
>
> 应用场景：
>
> **ConditionalWeakTable**
>
> - .NET 的 `ConditionalWeakTable<TKey, TValue>` 就是依赖句柄的经典应用。
> - 你可以给某个对象（TKey）“外接”一个附加对象（TValue），只要主对象（TKey）还活着，附加值（TValue）就不会被回收。
> - 当主对象被回收，附加值也会被回收，不会内存泄漏。
>
> **CLR 框架底层**
>
> - 一些框架要保证某些辅助资源或缓存对象不会早于主对象被销毁，就会用依赖句柄。

## 线程本地存储

普通的静态变量在单个应用程序域(AppDomain)内表现为全局变量，应用程序中的每个线程都可以访问它们。因此，通常需要采用多线程同步技术来确保线程安全地访问这些变量。然而，还存在另一种全局数据形式——线程本地存储(TLS)，其特点是每个线程都拥有独立的数据副本。换句话说，这些变量虽然像全局变量一样通过相同的名称或标识符被所有线程访问，但每个线程存储的实际数据却是相互隔离的。这种机制避免了同步问题，因为每个值仅能被关联的线程访问。

目前在.NET中，有三种使用线程本地存储的方式：

- 线程静态字段(Thread static fields)：作为静态字段使用，并额外标记 `[ThreadStatic]` 特性。
- `ThreadLocal` 类型：封装线程静态字段的辅助类。
- 线程数据槽(Thread data slots)：通过 `Thread.SetData` 和 `Thread.GetData` 方法实现。

.NET官方文档明确指出，线程静态字段的性能远优于数据槽，在可能的情况下应优先选用。我们将深入探究这两种技术的底层实现以理解差异所在。此外，静态字段是强类型的(像其他.NET字段一样具有明确类型)，而数据槽始终基于 `Object` 类型操作；对于命名数据槽，还需要使用基于字符串的标识符——这两种情况都可能导致编译时难以发现的潜在问题。

### 线程静态字段

使用线程静态字段非常简单，只需为普通静态字段添加 `ThreadStatic` 特性即可。线程静态字段既支持值类型也支持引用类型（见代码清单13-8）。在本例中，尽管两个不同线程读取的是相同的线程静态字段，但它们获取的是独立的值。因此一个线程会输出 “Worker 1:1”，而另一个线程输出“Worker 2:2”。若这两个静态字段仅是普通静态字段，多线程写入时就会发生竞态条件，最终存储的是不确定的1和2的混合值。

代码清单13-8 线程静态字段使用示例

```csharp
class SomeData  
{ 
    public int Field;  
}  

class SomeClass  
{ 
    [ThreadStatic]  
    private static int threadStaticValueData;  
    [ThreadStatic]  
    private static SomeData threadStaticReferenceData;  

    public void Run(object param)  
    { 
        int arg = int.Parse(param.ToString());  
        threadStaticValueData = arg;  
        threadStaticReferenceData = new SomeData() { Field = arg };  
        while (true)  
        { 
            Thread.Sleep(1000);  
            Console.WriteLine($"Worker {threadStaticValueData}:{threadStaticReferenceData.Field}.");  
        }  
    }  

    static void Main(string[] args)  
    { 
        SomeClass runner = new SomeClass();  
        Thread t1 = new Thread(new ParameterizedThreadStart(runner.Run));  
        t1.Start(1);  
        Thread t2 = new Thread(new ParameterizedThreadStart(runner.Run));  
        t2.Start(2);  
        Console.ReadLine();  
    }  
}
```

原生线程静态字段存在一个陷阱——若静态字段包含初始化器，该初始化器仅在执行静态构造函数的线程上触发一次。换言之，只有首次使用该类型的线程才会正确初始化线程静态字段，其他线程看到的将是字段的默认值（见代码清单13-9）。由于这一特性，`SomeOtherClass.Run` 方法会输出“Worker 44”和“Worker 0”，这可能令人十分意外。

代码清单13-9 线程静态字段初始化的意外情况示例

```csharp
class SomeOtherClass  
{ 
    [ThreadStatic]  
    private static int threadStaticValueData = 44;  

    public void Run()  
    { 
        while (true)  
        { 
            Thread.Sleep(1000);  
            Console.WriteLine($"Worker {threadStaticValueData}");   // 输出Worker 44或Worker 0
        }  
    }  

    static void Main(string[] args)  
    { 
        SomeOtherClass runner = new SomeOtherClass();  
        Thread t1 = new Thread(runner.Run);  
        t1.Start();  
        Thread t2 = new Thread(runner.Run);  
        t2.Start();  
    }  
}
```

> 尽管 ThreadStatic 表示给这个字段在每个线程上有独立的值，互不影响。但是，初始值的行为却不一致。
>
> ```c#
> [ThreadStatic]
> private static int threadStaticValueData = 44;
> ```
>
> 目的是想让每个线程初始值都是 `44`。但是它被标记了 ThreadStatic 的字段只会在主线程执行一次静态初始化器（即 `static SomeOtherClass()`），所以其它线程访问的时候会是默认值 0。实际上它等价于这个写法：
>
> ```c#
> class SomeOtherClass {
>     [ThreadStatic]
>     private static int threadStaticValueData;
>     static SomeOtherClass() { threadStaticValueData = 44; }
> }
> ```

为解决这些问题，.NET Framework 4.0引入了`ThreadLocal<T>`类，它提供了更优且更确定性的初始化行为。我们可以通过构造函数传入值工厂方法，当首次访问`Value`属性时会延迟初始化类实例（见代码清单13-10）。

代码清单13-10 `ThreadLocal` 使用示例

```csharp
class SomeOtherClass  
{ 
    private static ThreadLocal<int> threadValueLocal = new ThreadLocal<int>(() => 44, trackAllValues: true);  

    public void Run()  
    { 
        while (true)  
        { 
            Thread.Sleep(1000);  
            Console.WriteLine($"Worker {threadStaticValueData}:{threadValueLocal.Value}.");  
            Console.WriteLine(threadValueLocal.Values.Count);  
            threadValueLocal.Value = threadValueLocal.Value + 1;  
        }  
    }  
}
```

`ThreadLocal<T>` 可以存储在非静态字段中，从而为类的每个实例跟踪不同的线程静态值。此外，通过向构造函数的`trackAllValues` 参数传入 `true`，`ThreadLocal<T>` 能追踪所有已初始化的值，后续可通过 `Values` 属性遍历所有当前值。但需谨慎使用此功能，这可能导致本应线程隔离的引用实例在线程间传递。

该特性的典型应用场景是在多线程环境中独立聚合各线程的值。例如要实现线程安全的计数器，有三种方案：用简单锁保护计数器、使用原子操作、或通过 `ThreadLocal<T>` 让每个线程操作自己的子计数器。我们通过基准测试来比较这三种实现（见代码清单13-11）。

代码清单13-11 三种线程安全计数器的实现方式

```c#
[MemoryDiagnoser]
public class CounterBenchmark
{
    [Params(10_000, 100_000, 1_000_000)]
    public static int Iterations;
    public static int NumberOfThreads = 32;

    [Benchmark]  
    public long InterlockedIncrement()  
    {  
        static void increment(StrongBox<long> counter) => Interlocked.Increment(ref counter.Value);  
        static long getResult(StrongBox<long> counter) => Interlocked.Read(ref counter.Value);  
        return Run(increment, getResult, new StrongBox<long>());  
    }  

    [Benchmark]  
    public long Lock()  
    {  
        static void increment(StrongBox<long> counter) { lock (counter) { counter.Value++; } }  
        static long getResult(StrongBox<long> counter) => Interlocked.Read(ref counter.Value);  
        return Run(increment, getResult, new StrongBox<long>());  
    }  

    [Benchmark]  
    public long ThreadLocal()  
    {  
        static void increment(ThreadLocal<long> counter) => counter.Value++;  
        static long getResult(ThreadLocal<long> counter)  
        {  
            long result = 0;  
            foreach (var value in counter.Values) { 
                result += value; 
            }  
            return result;  
        }  
        return Run(increment, getResult, new ThreadLocal<long>(trackAllValues: true));  
    }  

    private static long Run<T>(Action<T> increment, Func<T, long> getResult, T state)  
    {  
        var threads = new Thread[NumberOfThreads];  
        for (int i = 0; i < NumberOfThreads; i++)  
        {  
            threads[i] = new Thread(() =>  
            {  
                for (int j = 0; j < Iterations; j++) { 
                    increment(state); 
                }  
            });  
            threads[i].Start();  
        }  
        foreach (var thread in threads) { 
            thread.Join(); 
        }  
        return getResult(state);  
    }
}
```

`Run` 函数会启动32个线程，每个线程调用指定次数的“increment”函数，最后调用“getResult”函数获取结果。

需要注意的是这个基准测试有些非常规。它使用局部函数来减少重复代码，使书中示例更简洁易读。在实际基准测试中，应避免任何可能影响结果的操作，即使这意味着要复制粘贴大量代码。

> 部分读者可能是第一次见到 `StrongBox`。这个类型在.NET中已存在很长时间（自3.5版本起！）但鲜为人知。它本质上就是一个包含单个字段的泛型类，主要用于需要装箱值类型但又要保持强类型的情况（不能直接转为 `Object`）。可以将其视为可变的 `Tuple`。本例中计数器需要装箱以实现多线程共享。

在阅读理解代码后，请花几秒钟猜测哪种实现更快，以及性能差距的数量级。然后查看清单13-12中的测试结果。

清单13-12 .NET 8上的基准测试结果

| Method               | Iterations |         Mean | Allocated |
| -------------------- | ---------- | -----------: | --------: |
| InterlockedIncrement | 10000      |     1.900 ms |   4.65 KB |
| Lock                 | 10000      |    13.274 ms |   4.66 KB |
| ThreadLocal          | 10000      |     1.944 ms |  44.18 KB |
| InterlockedIncrement | 100000     |    92.672 ms |   4.66 KB |
| Lock                 | 100000     |   139.899 ms |   4.75 KB |
| ThreadLocal          | 100000     |     3.614 ms |  45.03 KB |
| InterlockedIncrement | 1000000    |   715.166 ms |   4.71 KB |
| Lock                 | 1000000    | 1,323.899 ms |   5.04 KB |
| ThreadLocal          | 1000000    |    23.600 ms |  20.43 KB |

如你所见，`ThreadLocal` 版本的性能明显优于其他方案，且随着迭代次数的增加，性能差距会进一步扩大。当然这是个极端案例——线程除了递增计数器外不执行任何操作，因此资源争用达到了最大化。在实际场景中，不同方案的对比结果会更加复杂，请务必在决策前进行严谨的性能测试。

`ThreadLocal` 本质上是对线程静态字段的复杂封装。由于其内部结构的额外处理逻辑（参见代码清单13-13），可能会观察到一定的性能损耗。此外，`ThreadLocal` 包含终结器，并在构造和终结过程中获取全局锁。若大量创建实例，可能成为应用程序中显著的资源争用源。不过如果性能并非首要考量，`ThreadLocal` 通常比直接使用线程静态字段更为便捷。

代码清单13-13. `DotNetBenchmark` 对比原始类型和引用类型在线程本地存储中的访问性能——线程静态字段与 `ThreadLocal` 的测试结果

若您确实需要原生线程静态字段的性能，同时要解决初始化问题，可以通过属性包装器实现延迟初始化（参见代码清单13-14）。

代码清单13-14. 线程静态数据初始化问题的解决方案

```csharp
[ThreadStatic]  
private static int? threadStaticData;  
public static int ThreadStaticData  
{  
    get 
    { 
        if (threadStaticData == null) 
            threadStaticData = 44; 
        return threadStaticData.Value; 
    }  
}
```

使用 `ThreadLocal` 时需特别注意存储内容，这可能导致隐蔽的内存泄漏。观察代码清单13-15的示例：

代码清单13-15. `ThreadLocal` 引发内存泄漏的案例

```csharp
internal class Repository  
{  
    private ThreadLocal<List<Item>> _storage = new(valueFactory: () => new List<Item>());  
    public void Add(int value) { _storage.Value.Add(new Item(value, this)); }  

    internal class Item(int value, Repository parent)  
    {  
        public int Value { get; } = value;  
        public Repository Parent { get; } = parent;  
    }  
}  

internal class Program  
{  
    public static void Main()  
    {  
        var reference = AllocateRepository();  
        GC.Collect();  
        GC.WaitForPendingFinalizers();  
        GC.Collect();  
        Console.WriteLine($"Is repository alive: {reference.IsAlive}"); // 输出True  
    }  

    private static WeakReference AllocateRepository()  
    {  
        var repository = new Repository();  
        repository.Add(10);  
        return new WeakReference(repository);  
    }  
}
```

该程序展示了一个使用 `ThreadLocal<List>` 作为内部存储的 `Repository` 类。每个 `Item` 都包含值及其所属 `Repository` 的反向引用。运行时会发现，即使没有任何显式引用，`Repository` 实例仍然存活！如前所述，`ThreadLocal` 本质是线程静态字段的封装——线程静态字段的值会持续存活至关联线程终止。对于传统线程静态字段这通常不成问题，因其使用方式与静态字段类似。但当 `ThreadLocal` 作为实例字段时，其生命周期更短，因此需要在实例不再使用时清理线程静态值。这通过 `Dispose` 方法和作为后备的终结器实现。在泄漏案例中，虽然未调用 `Dispose`，但终结器未能生效的原因在于：列表中的 `Item` 持有了      `Repository`（进而持有 `ThreadLocal` 实例）的引用，导致其始终被根引用。为避免此类问题，请确保正确释放 `ThreadLocal`。

### 线程数据槽

使用线程数据槽简单直接。有两种不同类型的数据槽可用（见代码清单13-16）：

- **命名线程数据槽**：通过 `Thread.GetNamedDataSlot` 方法以字符串名称访问。可以存储并复用该方法返回的 `LocalDataStoreSlot` 实例。
- **未命名线程数据槽**​：只能通过 `Thread.AllocateDataSlot` 方法返回的 `LocalDataStoreSlot` 实例访问。

代码清单13-16 使用线程数据槽的示例

```csharp
public void UseDataSlots() {
    // 命名数据槽
    Thread.SetData(Thread.GetNamedDataSlot("SlotName"), new SomeData());
    object data = Thread.GetData(Thread.GetNamedDataSlot("SlotName"));
    Console.WriteLine(data);
    Thread.FreeNamedDataSlot("SlotName");

    // 未命名数据槽
    LocalDataStoreSlot slot = Thread.AllocateDataSlot();
    Thread.SetData(slot, new SomeData());
    object data = Thread.GetData(slot);
    Console.WriteLine(data);
}
```

后文会提到，使用线程数据槽API时会丢失强类型——`Thread.SetData和Thread.GetData` 都要求并返回 `System.Object` 类型的实例。在.NET Framework早期版本中，数据槽作为动态分配线程本地存储的便捷方式提供（无法在运行时向类添加新的线程静态字段）。.NET Framework 4.0引入了 `ThreadLocal` 作为现代且强类型的替代方案。数据槽现在应被视为过时技术。实际上，在.NET Core中，数据槽只是 `ThreadLocal` 的封装器。

### 线程本地存储内部原理

理解线程本地存储的实现机制很重要，因为这可能让人误以为它是某种神奇的、超高速的线程亲和性存储。线程亲和性让人联想到栈，而栈很快，对吧？那么这种特殊的、保存在某些秘密线程相关空间中的线程本地存储，可能更快？实际情况要复杂得多，了解其底层原理有助于理解该技术的优缺点。

首先，操作系统确实为每个线程预留了专用内存区域，称为线程本地存储（TLS）。在Windows中称为TLS，在Linux中称为线程特定数据。该区域非常小（通常不超过一个内存页），组织为指针大小的槽位。Windows保证每个进程至少有64个槽位，最多1088个；Linux的槽位数因发行版而异（glibc通常1024个，musl通常128个）。这些限制非常严格——Windows的64个槽位在64位进程中仅占512字节内存！

因此，说数据“存储在TLS”时需要谨慎。TLS槽位设计用于存储指向常规内存的指针，这在.NET和其他编译器（如C/C++）中都是如此。线程本地存储空间根本不足以直接存储数据。即便如此，这种存储仍具有以下性能优势：

- 频繁访问时，包含TLS槽位的内存页很可能常驻物理内存
- 访问该内存页无需同步，因为仅单个线程可见

在CLR中，定义了一个全局的线程静态变量 `ThreadLocalInfo`（见代码清单13-17）。C++编译器使用单个TLS槽位存储该实例地址（每个底层系统线程存储自己的 `ThreadLocalInfo` 副本）。

代码清单13-17 CoreCLR中的线程本地存储定义

```cpp
#ifndef _MSC_VER
EXTERN_C __declspec(thread) ThreadLocalInfo gCurrentThreadInfo;
#else
EXTERN_C __thread ThreadLocalInfo gCurrentThreadInfo;
#endif
```

`ThreadLocalInfo` 包含以下三类CLR内部数据：

- 表示当前托管线程的非托管 `Thread` 类实例地址——这是最关键部分，在整个运行时中被广泛使用（如通过 `GetThread` 方法）。
- 当前线程代码执行的 `AppDomain` 实例地址——这是性能优化捷径，因为该指针也可从 `Thread` 实例获取。
- 表示线程当前角色的标志（如终结器线程、工作线程、GC线程等）。

> 这三个字段中，仅第一个在现代.NET版本中真正有用。由于无法再创建新 `AppDomain`，无需在线程级别跟踪当前 `AppDomain`。第三个字段是为兼容不支持线程静态变量的旧版Windows而存在，相关数据已迁移到专用线程静态字段以消除间接访问。虽然现在无用，但保留这两个字段是为了与调试器和诊断工具保持兼容。

因此，当使用.NET中的任何线程本地存储技术时，实际只有 `ThreadLocalInfo` 结构的指针存储在TLS中。所有线程静态数据既存在于CLR私有堆也存在于GC堆，与常规静态变量的实现方式类似（见图13-1）。`Thread` 类实例将其线程本地存储相关数据组织到另外两个类中。

![](asserts/13-1.png)

图13-1. .NET中线程本地存储的内部机制。实际存储线程本地数据的位置以灰色标出

- `ThreadLocalBlock`（线程本地块）：为每个应用程序域(AppDomain)创建（因此在.NET Core应用中每个线程仅有一个实例）。它额外维护 `ThreadStaticHandleTable`（线程静态句柄表），该表持有对专用托管数组的强引用句柄，这些数组存储线程静态字段实例的引用。

-  `ThreadLocalModule`（线程本地模块）：为每个应用程序域中的每个模块创建。它包含两个关键数据：

  - 非托管静态数据块：存储所有线程静态的非托管值。为优化内存访问，数据块中的内容采用内存对齐填充。

  - 指向托管数组的指针：该数组存储本模块的静态引用，引用按类型分组存储。

换言之，线程静态数据以下列方式存储：

- 对于引用类型字段：实例通常分配在堆上，其引用存储在由 `ThreadStaticHandleTable` 管理的强引用句柄所维护的专用 `Object[]` 数组中。需特别注意：

  - 同一类型可能存在多个堆分配实例（若这些字段已初始化且非null）——每个运行中的托管线程对应一个实例。

  - 将存在多个堆分配的 `Object[]` 数组来存储上述引用——每个应用程序域、模块及运行中的托管线程各对应一个数组。

- 对于非托管类型字段：这些值存储在非托管内存的静态数据块中。同样会存在多个数据块——每个线程、应用程序域及其内部模块各对应一个。

- 对于结构体：以装箱形式存储在托管堆上，处理方式与前述引用类型相同。

由于模块的类型和静态字段数量在编译时已知，专用 `Object[]` 数组和静态数据块的大小均为预先计算好的常量值。

> 细心的读者可能注意到，在.NET中创建线程可能引发大量分配操作，原因在于线程静态字段。每个应用程序域及其内部模块都可能新建多个 `Object[]` 数组（由于单个应用程序域中托管线程静态字段数量通常较少，这些数组很可能分配在小对象堆SOH中），同时 `ThreadLocalModule` 会分配在CLR私有数据区（包含各模块的静态数据块）。所幸这些结构和数组大多是延迟分配的。

例如，在图13-1中，我们展示了其中一个模块的视角——虽然实际可能存在更多 `ThreadLocalModule`，但为了简洁起见并未全部显示。该模块中定义了一些类型，我们重点看 `Type1`（如代码清单13-18所示）。它包含两个原始类型的线程静态字段（long和int类型），这些值存储在 `ThreadLocalModule` 的静态数据块中；另外还有两个 `SomeData` 引用类型的线程静态字段。与常规静态字段类似，这些实例通常分配在堆上，其引用存储在专用的常规对象数组中。图13-1中，`Type1` 的两个字段已为线程1初始化，但（出于演示目的）仅为线程2初始化了第一个字段。

代码清单13-18 图13-1所示的简单类型示例

```csharp
class Type1 
{ 
    [ThreadStatic] private static int static1; 
    [ThreadStatic] private static long static2; 
    [ThreadStatic] private static SomeData static3; 
    [ThreadStatic] private static SomeData static4; 
    ...
}
```

初看可能令人不安：这些本应“线程专属”的静态对象，实际上只是散落在GC堆中的普通对象。但请注意，只要没有异常情况，它们仅对所属托管线程可见（因此仍是线程安全的）。不过它们可能引发伪共享问题（参见第2章），因为这些实例可能位于同一缓存行边界内。

因此，当把TLS视为“快速魔法内存”时，请始终牢记图13-1的机制。实际上，TLS在这里只是实现数据结构线程亲和性的技术细节，其本身并不会加速任何操作。

JIT编译时，会为线程静态字段计算相应偏移量——非托管类型存储在静态块中，引用类型存储在引用数组中。这些偏移量保存在 `MethodTable`相关区域，供JIT编译器计算数据访问地址。实际访问时需要先获取当前线程的 `ThreadLocalModule`，因此线程静态数据的访问会产生显著额外开销（参见带注释的代码清单13-17和13-18）。

代码清单13-19 线程静态非托管变量赋值操作（类似代码清单13-8中的 `threadStaticValueData`）

```
// Assume esi register contains value to store
// Pass the static block index
mov ecx,2
// Accesses ThreadLocalModule data (via TLS-stored pointer)
// As a result, rax contains ThreadLocalModule address
call coreclr!JIT_GetSharedNonGCThreadStaticBaseOptimized
mov rdi,rax
// Store the value:
// 1Ch is an pre-calculated offset in the statics blob, esi contains value to store
mov dword ptr [rdi+1Ch],esi
Listing 13-20. Assigning thread static reference variable (like threadStaticReferenceData in Listing 13-8)
// Assume rbx contains value (reference) to store
// Pass the static block index
mov edx,2
// Accesses ThreadLocalModule inside (via TLS-stored pointer)
// As a result, rax contains reference to an array element where references of that
type begins
call coreclr!JIT_GetSharedGCThreadStaticBaseOptimized
mov rcx,rax
// Store the reference (in rbx) under given array element (in rcx) by calling write barrier
mov rdx,rbx
call JitHelp: CORINFO_HELP_ASSIGN_REF
```

那些间接寻址操作使得访问静态字段（无论是常规静态字段还是线程静态字段）的速度比访问普通字段慢了几个数量级（后者通常只需一两条简单的 `mov` 指令即可完成访问）。

> 若想深入了解.NET中线程本地存储的实现机制，`JIT_GetSharedNonGCThreadStaticBase` 和 `JIT_GetSharedGCThreadStaticBase` 这两个方法是最佳切入点。由JIT生成的方法中经常包含 `INLINE_GETTHREAD` 宏，该宏会从TLS存储中获取 `gCurrentThreadInfo`（即线程静态的 `ThreadLocalInfo` 实例）——例如在Windows系统上，它会使用 `OFFSET__TEB__ThreadLocalStoragePointer` 在当前的线程环境块（TEB）中查找TLS地址。如前所述，`ThreadLocalInfo` 包含指向非托管Thread实例的指针。AppDomain指针和 `m_EETlsData` 字段现已不再使用，仅为保持与调试器的向后兼容性而保留。位于 `.\src\coreclr\vm\threadstatics.h` 文件中的 `ThreadLocalModule` 和 `ThreadStatics` 类型，以及 `.\src\coreclr\vm\threads.h` 文件中的 `ThreadLocalBlock`，包含了处理线程本地存储的核心逻辑。

那么包含线程静态字段的泛型类型如何处理？前文所述的逻辑依赖于编译时已知线程静态字段数量这一前提，但泛型类型并不满足此条件——编译器无法预知会发生多少种泛型类型实例化（每种实例化都可能需要全新的线程静态变量集合）。解决方案与处理泛型类型常规静态字段的方案类似：`ThreadLocalModule` 维护了一个额外的动态指针数组，这些指针指向更小的结构体，其组织方式与 `ThreadLocalModule` 本身相似（参见图13-2及对应的代码清单13-21）。每个 `DynamicEntry` 结构体专用于单个泛型类型实例化，包含与 `ThreadLocalModule` 相同类型的数据。

![](asserts/13-2.png)

图13-2. 泛型类型的线程本地存储内部结构

代码清单13-21. 图13-2所示的简单Some泛型类型

```c#
class Some
{ 
    [ThreadStatic] 
    private static T static1; 
    [ThreadStatic] 
    private static SomeData static2; 
    [ThreadStatic] 
    private static SomeData static3;
}
```

> 从垃圾回收（GC）的角度来看，引用类型的线程静态数据是常规对象，其根植于前文提到的专用 `Object[]` 数组。这些数组由 `ThreadLocalBlock` 维护的强引用句柄保持存活状态。因此，只要对应的线程（Thread）和应用程序域（AppDomain）存在，这些对象就会保持存活。

#### 总结

操作系统确实会给每个线程分配一小块专属空间，叫做TLS区。但这块空间实际非常袖珍（如 64 个槽位不到 1kb），往往只有几十到上千个指针槽位。所以它的设计之初就不是用来存储数据的，而是存放指针的，用来定位到真正的数据存储地（实际数据一般是放在普通堆/CLR私有堆中）。这也说明了它的这种多层间接寻址要比普通的字段直接寻址效率要慢得多。

.NET 中线程本地存储的大致结构：

- TLS 槽位：操作系统为每个线程分配有限的槽位，只存放“指向”线程本地结构体的指针（`ThreadLocalInfo`）。
- `ThreadLocalInfo`：包含三个成员：1.指向当前线程 `Thread` 示例的指针。2.AppDomain 指针。3.线程标志（如工作线程、GC线程等）。
- `Thread`：通过前面的 `ThreadLocalInfo` 定位线程对象，其中主要包含两个对象：
  - 线程本地块（`ThreadLocalBlock`）：每个 AppDomain 一个（因此 .NET Core APP 只有一个）。它内部维护一个线程静态句柄表（`ThreadStaticHandleTable`），负责指向专用数组对象 `Object[]`。后者专用于存储线程静态字段的引用类型值的引用。
  - 线程本地模块（`ThreadLocalModule`）：每个 AppDomain 一个。内部维护两个对象：1.非托管静态数据块（存 primitive 类型的线程静态字段值，如 int、long）。2.指向托管数组的指针（存引用类型的线程静态字段的引用）。
- 专用数组 `Object[]`：每个线程，每个模块，每个 AppDomain 都有一个。用来存放引用类型的线程静态字段的引用。

间接寻址的过程：

1. JIT 代码通过 TLS 槽位取到 `ThreadLocalInfo`。
2. 从 `ThreadLocalInfo` 间接拿到 `Thread` 实例。
3. 进而定位到 `ThreadLocalBlock`、`ThreadLocalModule。`
4. 找到专用 `Object[]`，按照偏移量访问目标字段引用。
5. （如果是值类型）则定位到非托管静态数据块的偏移位置。

上述数据流转与寻址过程图如下：

![](asserts/13-2o.png)

### 使用场景

尽管前文对线程数据存储的描述已明确指出其会带来一定开销，但从性能角度看它有一个主要优势——消除了多线程同步需求。线程亲和性（thread affinity）是线程本地存储区别于其他数据类型的核心功能特性。

一般而言，线程本地存储适用于以下场景：

- 存储和管理线程感知数据——例如某些非托管资源可能需要由同一线程获取和释放。
- 利用单线程亲和性——例如：
  - 日志记录或诊断：每个线程可无同步地操作本地诊断数据，避免与其他线程相互干扰（`System.Diagnostics.Tracing.EventSource` 中的 `[ThreadStatic]` 字段 `m_EventSourceExceptionRecurenceCount` 即为典型示例）。
  - 缓存：提供线程本地缓存完全可行，但需注意缓存副本数量会与托管线程数量成正比。第4章介绍的 `StringBuilderCache` 类就是绝佳范例——每个线程都缓存一个小型 `StringBuilder` 实例以实现无需线程同步的高效访问，这比从全局池中获取更高效。`System.Buffers` 命名空间中的 `SharedArrayPool` 也采用了类似的分层缓存机制。

> 绝大多数情况下，线程静态变量无法与异步编程共用，因为异步方法续体（continuation）不保证在原线程执行——异步方法恢复时线程本地数据会丢失（桌面应用UI线程等特殊情况除外，因其续体保证在原线程恢复）。为此，.NET提供了 `ThreadLocal` 的补充方案  `AsyncLocal` 来保持异步执行上下文的数据。但从内存管理角度看，这个类并无特别之处——它是普通类实例，其存储的值通过执行上下文（`ExecutionContext` 类）中的字典维护。

## 托管指针

出于简洁考虑，前文一直回避托管指针的话题（尽管细心读者可能注意到一两处相关注释³）。普通.NET开发者大多只接触对象引用，这已足够应对托管世界的需求——对象通过引用相互关联。如第4章所述，对象引用实质上是类型安全的指针（地址），始终指向对象的 `MethodTable` 引用字段（虽然常说它指向对象起始处，但严格来说对象头位于 `MethodTable` 引用之前）。通过对象引用可以获取整个对象地址，例如GC能通过固定偏移快速访问对象头，字段地址也可利用 `MethodTable` 中的信息轻松计算。

但CLR中还存在另一种指针类型——托管指针（managed pointer）。这类指针更为通用，可指向对象起始处之外的其他位置。ECMA-335标准定义托管指针可指向：

- 局部变量
- 方法参数
- 复合类型字段（即其他类型的字段）
- 数组元素

尽管灵活性更高，托管指针仍是类型化的。指向 `System.Int32` 的托管指针类型在CIL中记为 `System.Int32&`，指向 `SomeNamespace.SomeClass` 实例则记为 `SomeNamespace.SomeClass&`。这种强类型特性使其比可任意转换的非托管指针更安全。

但安全性提升伴随代价。托管指针的使用存在限制：

- 仅允许用于局部变量
- 参数签名
- 方法返回类型
- `ref struct` 字段

直接指出：“它们不能用于字段签名，因为数组的元素类型和装箱托管指针类型的值是不被允许的。将托管指针类型用作方法的返回类型是不可验证的。”

指针运算可能破坏托管指针的安全性，因此它们不会直接暴露在 C# 语言中，除非使用 `unsafe` 关键字。然而，它们仍以受限形式通过 `ref` 关键字存在：自 C# 7 起引入的 `ref` 参数、`ref` 局部变量及 `ref` 返回值。以引用方式传递参数本质上就是底层使用托管指针。因此，托管指针也常被称为 `byref` 类型（或简称 `byref`）。你已在第4章的代码清单4-33和4-34中见过引用传递的示例。