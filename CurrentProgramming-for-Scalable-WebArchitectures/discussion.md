# 讨论

我们已经看到并发如何在可扩展的 Web 架构的不同阶段影响编程。此外，分布式系统的使用已被确认为可扩展性、性能和可用性的必要条件。现在，让我们退后一步，思考一下为什么并发和分布式编程实际上如此具有挑战性，与传统编程如此不同的原因。

传统编程基于冯-诺依曼架构思想。我们只有一个存储器、一个处理器，程序就是一串指令。程序运行时，我们只有一条执行路径。我们可以不假思索地访问和更改状态，因为只有我们的单一执行流程会使用状态。一致性永远不会受到影响。这对许多程序来说都是一种有效的抽象，并提供了非常明确的含义和契约。当程序或机器崩溃时，整个进程基本上都会丢失，但除了编程错误外，我们从未遇到过部分故障。

一旦我们增加了额外的执行流程，就需要在单台机器上调度多个活动的多任务处理。只要这些流程不共享任何状态，通用抽象就不受影响。但是，如果我们允许状态在活动之间共享，并使用抢占式调度，这种抽象就会变得不完整。尽管一个执行路径仍然是顺序处理的，我们不能对交错的顺序做出假设。因此，不同的调度周期产生不同的交错，这反过来影响共享数据的访问顺序。一致性面临风险。尤其是共享的、可变的状态的概念，在这个模型中体现得模棱两可。当状态由单一执行流隔离时，可以无风险地修改它。一旦状态被共享，它的共享可变性就成了主要问题。不可变状态可以共享，因为它不存在一致性风险。如果我们仍然想保持我们之前的抽象，我们需要使用锁这样的原语来同步对共享的、可变的状态的访问。当我们把处理器换成另一个多核处理器时，不确定性的程度大大增加。我们现在可以实现真正的并行性，多个执行流程实际上可以在物理上并行运行。顺便说一句，这也改变了单一内存假设，因为多核引入了多级层次的缓存，但大多数程序员不会受到这种低级修改的影响。这是因为这种情况完全由操作系统、编译器和运行时环境所覆盖。由于真正的并行性，如果它们共享状态或相互影响，执行流之间的同步和协调变得至关重要。

顺序执行的概念理念仍然可以保持,尽管它已经与硬件实际执行环境没有多少共同之处了。尤其是在使用并发编程时,不同执行流的交错、重叠和同步并不明显。这种固有的不确定性没有反映在这个模型中。当使用过于粗粒度的锁定时,我们最终会得到一个类似于单核/单处理器系统的执行模型。强制执行严格的可串行化最终会导致所有并发活动的顺序执行。当一个顺序程序在单核机器上运行时,有这样一种概念是合理的：程序完全控制着其周围的环境。当应用程序暂停时,环境不会发生变化。真正的并行性打破了这种观念。程序不再对执行环境拥有独占控制权。不依赖于任何线程操作,其他线程可能会改变环境。

只要使用简洁的锁定机制或更高级别的抽象来防止竞争条件,共享状态仍然可以为并行运行的多个活动所管理和基本可用。像 TM 这样的并发抽象消除了实际复杂性的大部分。通过添加更多 CPU 内核和硬件资源,我们可以扩展应用程序以提供额外的吞吐量、并行性,并逐步降低延迟。

然而,在某个点上,这种方法就不再有意义了。一方面,硬件资源是有限的 —— 至少在物理上是如此。根据 [Amdahl 定律](https://zh.wikipedia.org/zh-cn/%E9%98%BF%E5%A7%86%E8%BE%BE%E5%B0%94%E5%AE%9A%E5%BE%8B),通过添加更多内核所能获得的最大预期改进也是有限的。另一方面,在某个规模上,会有一些非功能性需求变得相关：可扩展性、可用性和可靠性。我们希望进一步扩展应用程序,尽管我们不能再扩展单个节点。崩溃仍然会停止我们的整个应用程序。但我们可能希望应用程序具有弹性,即使机器发生故障也能继续提供服务。使用多台机器,产生分布式系统,变得不可避免。

分布式系统的引入完全改变了局面。我们现在有多个节点同时执行代码，这是真正并行的进一步增强形式。最令人头疼的变化是状态的概念。在分布式系统中，我们没有任何类似于全局状态的东西。每个节点都有自己的状态，并可以与远程节点通信，以获取其他状态。然而，每当接收到一个状态时，它都反映了另一个节点过去的状态，因为在此期间，该节点可能已经改变了自己的状态。此外，与远程节点通信会产生访问本地状态时不会产生的副作用。这不仅限于消息响应的无限延迟，还包括任意消息排序的结果。此外，在分布式系统中，个别节点可能会发生故障，网络也可能被分区。这与单台机器上的本地计算相比，会产生完全不同的故障模型。

很明显,我们最初基于顺序计算的模型现在已经完全破裂了。复杂程度比以前高出许多倍。我们有基础的并发性和并行性、无边界的、无序的通信,既没有全局状态也没有全局时间。与本地调用相反,发送到远程节点的操作天生就是异步的。我们需要新的抽象来处理分布式系统固有的这些属性,以便在其之上构建有用的应用程序。通常有两种截然相反的方法来应对这一挑战。

一种方法旨在重建尽可能多的先前假设和契约。基于复杂的同步协议,全局状态的概念被重新建立,一致性再次得到保证。通过使用协调机制,我们还可以对抗非确定性,在极端情况下,强制对分散到多个节点的代码片段进行隔离的顺序执行。RPC 恢复了函数调用的概念,允许以与调用本地函数相同的方式隐式调用远程函数。

另一种主要方法是接受分布式系统的本质缺陷,并将它们直接纳入编程模型。这主要涉及到承认异步性,因此,拒绝全局状态的一致性。最终,异步性将分布式系统固有的复杂性暴露给程序员。这种方法也更倾向于显式语义,而不是透明性。节点只有本地状态和来自其他节点的可能过时的状态,因此全局状态的概念被替换为对状态的个人和概率视图。

简而言之,前一种方法使用非常复杂但通常也很耗力的抽象来隐藏复杂性。然而,通过逃避异步性,这种方法放弃了最初分布式带来的一些优势。强制同步需要大量的协调开销,这反过来浪费了分布式系统的大量资源和能力。此外,如果提供的抽象不合适,它们会使分布式应用程序的开发变得更加困难。例如,当 RPC 抽象假装远程实体实际上是本地实体时,程序员无法意识到运行时不可避免的网络故障的后果。

后一种方法将复杂性暴露出来,并迫使程序员显式处理它们。显然,这种方法更具挑战性。另一方面,通过接受异步性、故障和不确定性,可以实现提供所需鲁棒性的高性能系统,但也体现了分布式应用程序的真正表现力。

通常情况下，没有一种方法是普遍优越的。我们在前几章中学习的许多概念往往属于其中一种方法。事实上，许多现有的分布式系统通过在适当时应用高级抽象来综合使用这两种方法的思想，但在误导时不放弃复杂性。