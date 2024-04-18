# 无锁数据结构

在执行多线程程序时，无锁数据结构可保证至少一个线程的进度，从而帮助你避免死锁。

在今年的编程语言设计和实现会议（http://www.cs.umd.edu/~pugh/pldi04/）上，Michael Maged 展示了世界上第一个无锁内存分配器[^7]，它在许多测试中超越了其他更复杂、精心设计的基于锁的分配器。

这是近期出现的众多无锁数据结构和算法中的最新一个。

## “无锁”是什么意思？

这正是我不久前会问的问题。作为一个正宗的主流多线程程序员，我对基于锁的多线程算法很熟悉。在经典的基于锁的编程中，每当你需要共享一些数据时，你就需要序列化对它的访问。更改数据的操作必须看起来是原子的，这样其他线程就不会干预破坏你数据的不变性。甚至像 `++count_`（其中 `count_` 是一个整型）这样简单的操作也必须被锁定。自增实际上是一个三步（读取、修改、写入）操作，它不一定是原子的。

简而言之，在基于锁的多线程编程中，你需要确保任何可能受到竞态条件影响的共享数据操作都通过锁定和解锁互斥锁来原子化。好处是，只要互斥锁被锁定，你就可以执行任何操作，确信没有其他线程会破坏你的共享状态。

正是这种“任意性”——在锁定互斥锁时你可以做的任何事情——也是有问题的。例如，你可以读取键盘或执行一些缓慢的I/O操作，这意味着你延迟了任何等待同一互斥锁的其他线程。更糟糕的是，你可能决定想要访问一些其他共享数据，并尝试锁定它的互斥锁。如果另一个线程已经锁定了那个最后的互斥锁并想要访问你的线程已经持有的第一个互斥锁，那么两个进程就会“死锁”。

在无锁编程中，你不能将几乎任何事情都原子化地完成。只有一小部分特定的操作可以被原子化，这个限制使得无锁编程变得更加困难。（事实上，全世界可能只有大约半打无锁编程专家，而本人并不在其中。但幸运的是，本文将为你提供基本工具、参考资料和热情，帮助你成为其中一员。）这种稀缺框架的奖励是，你可以提供关于线程进度和线程间交互的更好保证。但是，在无锁编程中可以原子化的“小集合”是什么呢？事实上，如果存在这样的集合，那么能够允许实现任何无锁算法的最小原子化基元是什么？

如果你认为这是一个足够基本的问题，值得给回答者颁发奖项，那么其他人也是这么认为的。2003年，Maurice Herlihy 因他 1991 年开创性的论文“无等待同步”（见http://www.podc.org/dijkstra/2003.html，其中也包含了论文的链接）而获得了 Edsger W. Dijkstra 分布式计算奖。在他的杰出论文中，Herlihy 证明了哪些原语是好的，哪些是不好的，用于构建无锁数据结构。这使得一些看似热门的硬件架构立即过时，同时澄清了未来硬件中应实现哪些同步原语。

例如，Herlihy 的论文给出了不可能性结果，表明像 test-and-set、swap、fetch-and-add 甚至原子队列等原子操作对于正确同步超过两个线程是不够的。（这很令人惊讶，因为具有原子推入和弹出操作的队列似乎提供了相当强大的抽象。）但好消息是，Herlihy 也给出了普遍性结果，证明了一些简单的构造足以为实现任意数量线程的任何无锁算法。

最简单、最受欢迎的通用原语，也是我在整个过程中使用的，是“比较并交换”（CAS）操作：

```c++
template <class T>
bool CAS(T* addr, T expected, T value) {
    if (*addr == expected) {
        *addr = value;
        return true;
    }
    return false;
}
```

`CAS` 比较内存地址的内容与预期值，如果比较成功，则将内容替换为新值。整个过程是原子的。许多现代处理器实现了 `CAS` 或等效的原语，适用于不同的位长度（这就是我们将其作为模板的原因，假设实现使用元编程来限制可能的 Ts）。经验法则是，`CAS` 可以原子地比较并交换的位数越多，用它实现无锁数据结构就越容易。今天的大多数32位处理器都实现了64位 `CAS`；例如，Intel 的汇编语言称之为`CMPXCHG8`（你一定喜欢这些汇编助记符）。

## 一个警告

通常，C++文章会伴随着 C++ 代码片段和示例。理想情况下，该代码是标准 C++，并且“泛型编程”力求达到这一理想。

然而，在编写多线程代码时，提供标准 C++ 代码示例是不可能的。线程在标准C++中不存在，你不能编写不存在的东西。因此，本文的代码是“伪代码”，不打算作为可移植编译的标准C++代码。以内存屏障为例。真实代码将需要是算法的汇编语言翻译，或者至少在所谓的“内存屏障”——处理器依赖的魔法，强制内存读取和写入的正确顺序——中散布一些C++代码。我不想通过解释内存屏障来进一步分散对无锁数据结构的讨论。如果你感兴趣，请参考 Butenhof 的优秀书籍[^3]或简短介绍[^6]。在这里，我假设编译器和硬件不会引入奇怪的优化（例如，在单线程假设下，消除一些“冗余”的变量读取，这是一个有效的优化）。技术上，这被称为“顺序一致性”模型，其中读取和写入按照源代码中执行和看到的确切顺序进行[^8]。

## 无锁与锁定的对比

“无等待”（wait-free）程序可以在有限的步骤内完成，无论其他线程的相对速度如何。

“无锁”（lock-free）程序保证了至少一个执行程序的线程的进展。这意味着一些线程可能会被任意延迟，但保证每个步骤至少有一个线程取得进展。因此，系统作为一个整体总是在取得进展，尽管一些线程可能比其他线程进展得慢。基于锁的程序无法提供上述任何保证。如果任何线程在持有互斥锁时被延迟，等待相同互斥锁的线程就无法取得进展；在一般情况下，锁定算法容易受到死锁的影响——每个线程都等待另一个线程锁定的互斥锁——以及活锁的影响，每个线程都试图躲避对方的锁定行为，就像两个在走廊里试图擦肩而过的人，但最终却同步地左右摇摆。我们人类很擅长以笑声结束这种情况；然而，处理器通常喜欢这样做，直到重启将它们分开。

无等待和无锁算法从它们的定义中获得了更多优势：

- **线程终止免疫**：任何在系统中被强制终止的线程都不会延迟其他线程。
- **信号免疫**：C和C++标准禁止信号或异步中断调用许多系统例程，如 `malloc`。如果中断在被中断的线程同时调用 `malloc`，可能会导致死锁。有了无锁程序，就不再有这样的问题了：线程可以自由地交错执行。
- **优先级反转免疫**：当一个低优先级线程持有一个互斥锁的锁，而一个高优先级线程需要这个锁时，就会发生优先级反转。这种棘手的冲突必须由操作系统内核解决。无等待和无锁算法对此类问题免疫。

## 无锁 WRRM Map

专栏写作的好处之一是定义缩写词，所以让我们定义 WRRM（Write Rarely Read Many，写入少读取多）映射为比它们被修改的次数更多地被读取的映射。示例包括对象工厂[^1]、许多观察者设计模式的实例[^5]、货币名称到汇率的映射，这些映射被查找很多次，但只有相对较慢的流更新，以及其他各种查找表。

WRRM  映射可以通过 `std::map` 或标准之后的 `unordered_map`（http://www.open-std.org/jtcl/sc22/wg21/docs/papers/2004/n1647.pdf）实现，但正如我在《现代C++设计》中所论证的，`assoc_vector`（一个排序的向量或对）是WRRM 映射的好候选，因为它用更新速度交换查找速度。无论使用哪种结构，我们的无锁方面都是独立的；让我们只称后端为 `Map<Key, Value>`。此外，对于本文的目的，迭代是不相关的——映射只是提供查找键或更新键值对的方式的表。

为了回顾一下锁定实现看起来如何，让我们将 `Map` 对象与 `Mutex` 对象结合起来：

```c++
// WRRMMap的锁定实现
template <class K, class V>
class WRRMMap {
    Mutex mtx_;
    Map<K, V> map_;
public:
    V Lookup(const K& k) {
        Lock lock(mtx_);
        return map_[k];
    }
    void Update(const K& k, const V& v) {
        Lock lock(mtx_);
        map_[k] = v;
    }
};
```

为了避免所有权问题和悬空引用（这可能以后会给我们带来更大的麻烦），`Lookup` 通过值返回其结果。坚如磐石——但代价很大。每次查找都会锁定/解锁 `Mutex`，尽管（1）并行查找不需要相互锁定，（2）根据规范，`Update` 被调用的频率远低于 `Lookup`。现在让我们尝试提供一个更好的 WRRMMap 实现。

## 垃圾回收器在哪里？

实现无锁 WRRMMap 的第一次尝试基于这样一个想法：

- 读取完全没有锁定。
- 更新复制整个映射，更新副本，然后尝试用旧映射进行 `CAS`。只要 `CAS` 操作不成功，复制/更新/`CAS` 过程就会在循环中重试。
- 由于 `CAS` 在可以交换的字节数上有限制，`WRRMMap` 将 `Map` 存储为指针，而不是 `WRRMMap` 的直接成员。

```c++
// WRRMMap的第一次无锁实现
// 只有在你有垃圾回收器的情况下才有效
template <class K, class V>
class WRRMMap {
    Map<K, V>* pMap_;
public:
    V Lookup(const K& k) {
        //看，没有锁
        return (*pMap_)[k];
    }
    void Update(const K& k, const V& v) {
        Map<K, V>* pNew = 0;
        do {
            Map<K, V>* pOld = pMap_;
            delete pNew;
            pNew = new Map<K, V>(*pOld);
            (*pNew)[k] = v;
        } while (!CAS(&pMap_, pOld, pNew));
        //不要删除pMap_;
    }
};
```

它有效！在循环中，`Update` 例程对映射进行了完整的副本，添加了新条目，然后尝试交换指针。重要的是执行 `CAS` 而不是简单赋值；否则，以下事件序列可能会破坏映射：

- 线程A复制映射。
- 线程B也复制了映射并添加了一个条目。
- 线程A添加了另一个条目。
- 线程A用它的映射版本替换映射——一个不包含B添加的任何内容的版本。

有了 `CAS`，事情就变得相当整洁了，因为每个线程都说：“假设自从我上次查看以来映射没有变化，复制它。否则，重新开始。”

这使得 `Update` 无锁但不是无等待，根据我之前的定义。如果有多个线程同时调用`Update`，任何特定线程都可能无限循环，但始终保证某个线程会成功更新结构，因此每个步骤都在取得全局进展。幸运的是，`Lookup` 是无等待的。

在垃圾回收环境中，我们就完成了，本文将以一个乐观的语调结束。然而，如果没有垃圾回收，接下来就会有很多痛苦。这是因为你不能随意处理旧的 `pMap_`；如果你试图删除它的时候，许多其他线程正在疯狂地通过 `Lookup` 函数查找 `pMap_` 里面的东西怎么办？你看，垃圾回收器将能够访问所有线程的数据和私有栈；它将很好地了解何时不再浏览未使用的 `pMap_` 指针，并会很好地清理它们。没有垃圾回收器，事情变得更加困难。实际上，变得更困难了，事实证明，确定性内存释放是无锁数据结构中的一个基本问题。

## 写入锁定 WRRM Maps

为了理解我们对手的恶性，尝试一个经典的引用计数实现，看看它在哪里失败是有益的。那么，想象一下，给映射的指针关联一个引用计数，并让 `WRRMMap` 存储指向这样形成的结构的指针：

```c++
template <class K, class V>
class WRRMMap {
    typedef std::pair<Map<K, V>*, unsigned> Data;
    Data* pData_;
    ...
};
```

太好了。现在，`Lookup` 自增 `pData_->second`，随意搜索映射，然后自减了`pData_->second`。当引用计数达到零时，`pData_->first`可以被删除，然后`pData_` 本身也可以被删除。听起来万无一失，除了……除了它是“愚蠢的”（或者“万无一失”的反义词是什么）。想象一下，就在某个线程注意到引用计数为零并开始删除 `pData_` 的时候，另一个线程……不，更好的是：成千上万的线程刚刚加载了垂死的 `pData_`，并且即将阅读它！不管一个方案有多聪明，它都会遇到这个基本的困境——要读取数据的指针，你需要增加一个引用计数；但计数器必须是数据本身的一部分，所以它不能在不访问指针的情况下读取。这就像一个电篱笆，关闭按钮在篱笆的顶部：要安全地爬过篱笆，你需要先禁用它，但要禁用它，你需要先爬过它。

所以让我们考虑其他适当删除旧映射的方法。一种解决方案是等待然后删除。你看，随着处理器时代（毫秒）的流逝，旧的 `pMap_` 对象将被越来越少的线程查找；这是因为新的查找使用新的映射；一旦在 `CAS` 之前活跃的查找完成，`pMap_` 就可以下地狱了。因此，一个解决方案是将旧的 `pMap_` 值排队到某个“巨蟒(boa serpent)”线程，该线程在一个循环中，比如说，睡眠 200 毫秒，醒来并删除最近的映射，然后回去再睡 200 毫秒。

这不是一个理论上安全解决方案（尽管在界限内它实际上可能很好）。一个讨厌的事情是，如果出于某种原因，一个查找线程被延迟，巨蟒线程可能会在该线程的脚下删除映射。这可以通过始终给巨蟒线程分配比其他线程更低的优先级来解决，但总的来说，这个解决方案有一种难以消除的臭味。如果你同意我，认为很难用正脸为这种技术辩护，那么让我们继续。

其他解决方案[^4]依赖于一个扩展的 `DCAS` 原子指令，它能够比较并交换两个在内存中不连续的字：

```c++
template <class T1, class T2>
bool DCAS(T1* p1, T2* p2,
           T1 e1, T2 e2,
           T1 v1, T2 v2) {
    if (*p1 == e1 && *p2 == e2) {
        *p1 = v1; *p2 = v2;
		return true;
	}
	return false;
}
```

自然，这两个位置将是指针和它自己的引用计数。`DCAS` 已经在摩托罗拉 68040 处理器中实现（非常低效），但其他处理器没有实现。因此，基于 `DCAS` 的解决方案被认为主要具有理论价值。

 确定性销毁的第一种解决方案是依赖于要求较低的 `CAS2`。同样，许多32位机器实现了64位 `CAS`，通常被称为 `CAS2`。（因为它只操作连续的字，`CAS2` 显然比 `DCAS` 的威力小。）首先，让我们将引用计数存储在它所保护的指针旁边：

```c++
template <class K, class V>
class WRRMMap {
    typedef std::pair<Map<K, V>*, unsigned> Data;
    Data data_;
    ...
};
```

（请注意，这一次计数器位于它所保护的指针旁边，这种设置消除了前面提到的困境。你很快就会看到这个设置的代价。）

然后，让我们修改 `Lookup`，在访问映射之前增加引用计数，并在之后减少。在下面的代码片段中，为了简洁起见，我忽略了异常安全性问题（可以使用标准技术来处理）。

```c++
V Lookup(const K& k) {
    Data old;
    Data fresh;
    do {
        old = data_;
        fresh = old;
        ++fresh.second;
    } while (CAS(&data_, old, fresh));
    V temp = (*fresh.first)[k];
    do {
        old = data_;
        fresh = old;
        --fresh.second;
    } while (CAS(&data_, old, fresh));
    return temp;
}
```

最后，`Update` 在引用计数为1的窗口机会期间用新的映射替换映射。

```c++
void Update(const K& k,
            const V& v) {
    Data old;
    Data fresh;
    old.second = 1;
    fresh.first = 0;
    fresh.second = 1;
    Map<K, V>* last = 0;
    do {
        old.first = data_.first;
        if (last != old.first) {
            delete fresh.first;
            fresh.first = new Map<K, V>(old.first);
            fresh.first->insert(make_pair(k, v));
            last = old.first;
        }
    } while (!CAS(&data_, old, fresh));
    delete old.first;
}
```

`Update` 的工作方式是这样的。它定义了现在熟悉的 `old` 和 `fresh` 变量。但这次`old.last`（计数）从未从 `data_.last` 分配；它总是 1。这意味着 `Update` 循环，直到它有机会用另一个计数为1的指针替换一个计数为1的指针。用普通英语来说，循环说：“我将用一个新的、更新的映射替换旧映射，并留意任何其他对映射的更新，但只有在现有映射的引用计数为1时，我才进行替换。”变量 `last` 及其相关代码只是一种优化：如果旧映射没有被替换（只有计数），则避免一遍又一遍地重建映射。

很整洁，对吧？并不是那么多。`Update` 现在被锁定了：它需要等待所有 `Lookup` 完成才有机会更新映射。无锁数据结构的所有好属性都随风而去了。特别是，很容易将 `Update` 饿死：只要以足够高的速率查找映射——引用计数就永远不会下降到 1。所以你到目前为止真正拥有的不是 `WRRM`（写入少读取多）映射，而是WRRMBNTM（写入少读取多但不是太多）映射。

## 结论

无锁数据结构是有希望的。它们在线程终止、优先级反转和信号安全性方面表现出良好的属性。它们从不会发生死锁或活锁。在测试中，最近的无锁数据结构在很大程度上超越了它们的锁定对应物[^9]。然而，无锁编程是棘手的，特别是在内存分配方面。垃圾回收环境是一个加分项，因为它有停止和检查所有线程的手段，但如果你想要确定性销毁，你需要硬件或内存分配器的特殊支持。在下一期的“泛型编程”专栏中，我将探讨如何优化 `WRRMMap`，使其在执行确定性销毁的同时保持无锁。

如果这一期的垃圾回收映射和 WRRMBNTM 映射让你不满意，这里有一个省钱小贴士：不要去看《异形大战铁血战士》的电影，除非你喜欢“烂到搞笑”的电影。

## 引用

[^1]: Alexandrescu, Andrei. *Modern C++ Design*, Addison-Wesley Longman, 2001.
[^2]: Alexandrescu, Andrei. "Generic<Programming>:yasli::vector Is On the Move," *C/C++ Users Journal*, June 2004.
[^3]: Butenhof, D.R. *Programming with POSIX Threads*, Addison-Wesley, 1997.
[^4]: Detlefs, David L., Paul A. Martin, Mark Moir, and Guy L. Steele, Jr. "Lock-free Reference Counting," *Proceedings of the Twentieth Annual ACM Symposium on Principles of Distributed Computing*, pages 190-199, ACM Press, 2001. ISBN 1-58113-383-9.
[^5]: Gamma, Erich, Richard Helm, Ralph E. Johnson, and John Vlissides. *Design Patterns: Elements of Resusable Object-Oriented Software*, Addison-Wesley, 1995.
[^6]: Meyers, Scott and Andrei Alexandrescu. "The Perils of Double-Checked Locking." *Dr. Dobb's Journal*, July 2004.
[^7]: Maged, Michael M. "Scalable Lock-free Dynamic Memory Allocation," *Proceedings of the ACM SIGPLAN 2004 Conference on Programming Language Design and Implementation*, pages 35-46. ACM Press, 2004. ISBN 1-58113-807-5.
[^8]: Robison, Arch. "Memory Consistency & .NET," *Dr. Dobb's Journal*, April 2003.
[^9]: Maged, Michael M. "CAS-Based Lock-Free Algorithm for Shared Deques," *The Ninth Euro-Par Conference on Parallel Processing*, LNCS volume 2790, pages 651-660, August 2003.

# 原文链接

[Lock-Free Data Structures | Dr Dobb's (drdobbs.com)](https://www.drdobbs.com/lock-free-data-structures/184401865)

[^7]: 
[^3]: 
[^6]: 
[^8]: 
[^9]: 