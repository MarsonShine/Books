# Reactor 设计与实现：一个用于事件复用的面向对象框架（第二部分）

## 1 引言

这是描述使用面向对象（OO）C++ 包装器封装现有操作系统（OS）进程间通信（IPC）服务的系列文章的第三部分的下半部分。在本文的上半部分，我们介绍了一个客户端/服务器应用程序示例，以激发封装事件复用机制的包装器的实用性。事件复用对于开发能够同时接收和处理来自多个客户端的数据的事件驱动型网络服务器非常有用。上一篇文章还考察了几种替代的 I/O 复用方案的优缺点，如非阻塞I/O、多进程或线程创建，以及同步I/O复用（通过 `select` 和 `poll` 系统调用）。

本文重点介绍一个名为 Reactor 的面向对象框架的设计和实现。 Reactor 提供了一个可移植的接口，用于一组可扩展、可重用且类型安全的 C++ 类，这些类封装并增强了 `select` 和 `poll` 事件复用机制。为了帮助简化网络编程，Reactor 结合了基于同步 I/O 的事件复用和基于定时器的事件。当这些事件发生时， Reactor 会自动调度先前注册对象的方法来执行应用程序指定的服务。

本文的组织如下：第2节描述了 Reactor 框架提供的主要功能；第3节概述了框架的面向对象设计和实现；第4节提出了一个分布式日志记录示例，展示了 Reactor 如何简化并发、事件驱动型网络应用程序的开发；第5节讨论了结束语。

## 2 Reactor 的主要特性

![](../asserts/reactor-pattern-figure.png)

Reactor 提供了一个面向对象的接口，简化了分布式应用程序的开发，这些应用程序利用基于I/O和/或基于定时器的复用机制。以下是 Reactor 框架提供的主要特点：

- **导出统一的面向对象接口**：使用 Reactor 的应用程序不会直接访问底层的 I/O 复用系统调用。相反，它们继承自一个共同的事件处理器（`Event_Handler`）抽象基类，以形成复合的具体派生类（如图1所示）。`EventHandler` 基类指定了一个统一的接口，由处理（1）同步输入、输出和异常以及（2）基于定时器的事件的虚拟方法组成。应用程序创建这些派生类的实例，并将它们与 Reactor 的实例注册。

- **自动化事件处理器调度**：当事件发生时，Reactor 会自动调用预先注册的派生类对象的适当虚拟方法事件处理程序。由于 C++ 对象被注册到 Reactor 中（而不是作为独立的子程序），所以与对象相关联的任何上下文信息在方法调用之间都会被保留。这对于开发保留客户端调用之间的信息的“有状态”服务特别有用。

- **支持透明的可扩展性**： Reactor 及其注册对象的功能可以在不修改或重新编译现有代码的情况下透明地扩展。为了实现这一点， Reactor 框架采用继承、动态绑定和参数化类型，将底层事件复用和服务调度机制与高层应用程序处理策略解耦。例如，低层机制包括（1）检测多个 I/O 句柄上的事件，（2）处理定时器到期，以及（3）响应事件调用适当的方法事件处理器。同样，应用程序指定的策略包括（1）建立连接，（2）数据传输和接收，以及（3）处理来自其他参与主机的服务请求。

- **增加复用**： Reactor 的复用和调度机制可以被许多网络应用程序重用。通过重用而不是重新开发这些机制，开发人员可以自由地集中精力处理更高层次的应用程序相关问题，而不是反复处理底层事件复用的细节。此外，后续的错误修复和增强将由使用 Reactor 组件的所有应用程序透明共享。相反，直接访问 `select` 和 `poll` 的开发人员必须为每个网络应用程序重新实现相同的复用和调度代码。而且，对这段代码的任何修改和改进都必须在所有相关应用程序中手动复制。

- **增强类型安全性**： Reactor 保护应用程序开发人员免受与编程现有 I/O 复用系统调用相关的错误倾向的低级细节的影响。这些细节涉及设置和清除位掩码、处理超时和中断，以及调度“回调”方法。特别是， Reactor 消除了由于滥用 I/O 句柄和 `fd_set` 位掩码而与 `poll` 和 `select` 相关的几个微妙的错误原因。

- **提高可移植性**： Reactor 还保护应用程序免受 `select` 和 `poll` 之间差异的影响，这些差异阻碍了可移植性。如图5所示，无论底层事件复用系统调用是什么， Reactor 都向应用程序导出相同的接口。此外， Reactor 的面向对象架构提高了其自身的内部可移植性。例如，将 Reactor 从基于 `select` 的操作系统平台移植到基于 `poll` 的平台，只需要对框架进行一些明确定义的更改。

除了简化应用程序开发之外， Reactor 还有效地执行其复用和调度任务。特别是，其事件调度逻辑改进了直接使用 `select` 的常见技术。例如，基于 `select` 的 Reactor 使用了一个 `ACE_Handle_Set` 类（在第3.2节中描述），该类在许多情况下避免了逐位检查 `fd_set` 位掩码。《C++ Report》的将来某期文章将实证评估 Reactor 的性能，并将其与直接访问 I/O 复用系统调用的非面向对象 C 语言解决方案进行比较。

## 3 Reactor 的面向对象设计与实现

本节总结了 reactor 框架主要类组件的面向对象设计，重点关注接口和战略设计决策。在适当的情况下，也会讨论战术设计决策和某些实现细节。第3.1节概述了与操作系统平台无关的组件；第3.2节涵盖了平台依赖的组件。Reactor 最初是模仿一个名为 `Dispatcher` 的 C++ 包装器设计的，该包装器用于 `select`，并且包含在InterViews发行版中[^3]。这里描述的 Reactor 框架包括几个额外的功能。例如，Reactor 可以透明地在 System V Release 4 的 `poll` 接口和 `select` 上运行，这些接口在 UNIX 和 PC 平台（通过 WINSOCK API）上都可用。此外，Reactor 框架还包含对多线程的支持。通常， Reactor 的单个实例在任何给定时间在线程中处于活动状态。然而，在一个进程中可能有多个不同的 Reactor 对象实例在单独的线程中运行。该框架提供了必要的同步操作以防止竞态条件[^4]。

### 3.1 与平台无关的类组件

以下段落总结了 Reactor 中的三个与平台无关的类的特点：`ACE_Time_Value`、`ACE_Timer_Queue` 和 `ACE_Event_Handler` 类：

- **ACE_Time_Value**：这个类提供了一个 C++ 包装器，封装了底层操作系统平台的日期和时间结构（例如 UNIX 和 POSIX 平台上的 `struct timeval` 数据类型）。`timeval` 结构包含两个字段，表示秒和微秒。然而，其他操作系统平台使用不同的表示方式，因此 `ACE_Time_Value` 类抽象了这些细节，提供了一个可移植的 C++ 接口。

  ![](../asserts/reactor-pattern-figure1.png)

  `ACE_Time_Value` 类中的主要方法在图 2 中进行了说明。`ACE_Time_Value` 包装器使用运算符重载来简化 Reactor 内基于时间的比较。重载允许使用标准算术语法进行涉及时间比较的关系表达式。例如，以下代码创建了两个 `ACE_Time_Value` 对象，通过将用户提供的命令行参数添加到当前时间，并显示两个对象之间的适当排序关系：

  ```c++
  int main (int argc, char *argv[]) {
      if (argc != 3) {
          cerr << "usage: " << argv[0] << " time1 time2" << endl;
          return 1;
      }
      ACE_Time_Value ct = ACE_OS::gettimeofday();
      ACE_Time_Value tv1 = ct + ACE_Time_Value(ACE_OS::atoi(argv[1]));
      ACE_Time_Value tv2 = ct + ACE_Time_Value(ACE_OS::atoi(argv[2]));
      if (tv1 > tv2) cout << "timer 1 is greater" << endl;
      else if (tv2 > tv1) cout << "timer 2 is greater" << endl;
      else cout << "timers are equal" << endl;
      return 0;
  }
  ```

  `ACE_Time_Value` 类中的方法是为了比较“标准化”的时间量而实现的。标准化调整 `timeval` 结构中的两个字段，使用一种规范的编码方案，确保准确的比较。例如，经过标准化后，`ACE_Time_Value(1, 1000000)` 将与 `ACE_Time_Value(2)` 比较得出相等的结果。请注意，直接对未标准化的类字段进行位比较是无法检测到这种相等性的。

- **ACE_Timer_Queue**：Reactor 的基于定时器的机制对于需要定时器支持的应用程序非常有用。例如，WWW 服务器需要看门狗定时器来释放资源，如果连接的客户端在特定时间间隔内没有发送 HTTP 请求。同样，某些守护进程配置框架（例如 Windows NT 中的服务控制管理器(Service Control Manager)）要求其控制的服务定期报告其当前状态。这些“心跳”消息用于确保服务没有异常终止。

  ![](../asserts/reactor-pattern-figure2.png)

  `ACE_Timer_Queue` 类提供了机制，允许应用程序注册基于时间的对象，这些对象派生自 `ACE_Event_Handler` 基类（在以下项目中描述）。`ACE_Timer_Queue` 确保在应用程序指定的未来时间调用这些对象的 `handle_timeout` 方法。`ACE_Timer_Queue` 类的方法在图 3 中进行了说明，使应用程序能够调度、取消和调用定时器对象。

  应用程序安排一个将在 `delay` 后到期的 `ACE_Event_Handler`。如果它到期，则将 `arg` 作为值传递给事件处理器的 `handle_timeout` 回调方法。如果 `interval` 不等于 `ACE_Time_Value::zero`，它用于自动重新安排事件处理器。`schedule` 方法返回一个句柄，该句柄在定时器队列的内部表中唯一标识此事件处理器。`cancel` 方法可以使用此句柄在事件处理器到期之前删除一个 `ACE_Event_Handler`。如果将非 NULL 的 `arg` 传递给取消，它将设置为应用程序在最初安排定时器时传入的异步完成令牌（Asynchronous Completion Token,ACT）。这使得可以释放动态分配的 ACT，以避免内存泄漏。

  默认情况下，`ACE_Timer_Queue` 实现为包含 `ACE_Time_Value`、`ACE_Event_Handler *` 和 `void *` 成员的元组的链表。这些元组按 `ACE_Time_Value` 字段的“执行时间”升序排序。`ACE_Event_Handler *` 字段是指向定时器对象的指针，该对象计划在 `ACE_Time_Value` 字段指定的时间运行。`void *` 字段是在最初安排定时器对象时提供的参数。当定时器到期时，此参数会自动传递给 `handle_timeout` 方法（在以下项目中描述）。

  链表中的每个 `ACE_Time_Value` 都以“绝对”时间单位存储（例如由 UNIX `gettimeofday` 系统调用生成的单位）。然而，由于类接口中使用了虚拟方法，应用程序可以重新定义 ACE_Timer_Queue 实现，以使用替代数据结构，如 delta-lists[^6] 或 heaps[^7]。delta-lists 以“相对”单位存储时间，表示为与列表前端最早的 `ACE_Time_Value` 的偏移量或“delta”。另一方面，堆使用“部分有序，几乎完整的二叉树”，而不是排序列表，以减少插入或删除条目的平均情况和最坏情况时间复杂度从 O(n) 降低到 O(lg n)。对于某些实时应用程序使用模式，堆表示可能更有效[^7]。

- **ACE_Event_Handler**：这个抽象基类指定了一个可扩展的接口，用于Reactor 类的部分控制和协调自动调度 I/O 和定时器机制。

  ![](../asserts/reactor-pattern-figure3.png)

  `ACE_Event_Handler` 接口中的虚拟方法在图 4 中进行了说明。Reactor 使用应用程序定义的 `ACE_Event_Handler` 基类的子类来实现其自动化的、事件驱动的回调机制。这些子类可以重新定义 `ACE_Event_Handler` 基类中的某些虚拟方法，以执行应用程序定义的处理，以响应各种类型的事件。这些事件包括（1）在一个或多个句柄上的不同类型的（例如，“读取”、“写入”和“异常”）同步 I/O，以及（2）定时器到期。从 `ACE_Event_Handler` 派生的对象通常提供一个 I/O 句柄。例如，以下 `Logging_Acceptor` 类片段封装了由 `SOCK_SAP` 套接字包装器提供的“被动模式”`ACE_SOCK_Acceptor` 工厂。

  ```c++
  class Logging_Acceptor : public ACE_Event_Handler {
  public:
      Logging_Acceptor(ACE_INET_Addr &addr) : acceptor_(addr) { /* ... */ }
      // 双重分派钩子。
      virtual ACE_HANDLE get_handle(void) const {
          return this->acceptor_.get_handle();
      }
      // 工厂，创建并激活一个
      // Logging_Handler
      virtual int handle_input(ACE_HANDLE) {
          ACE_SOCK_Stream peer_handler;
          // Create and activate a Logging_Handler..
          this->acceptor_.accept (peer_handler);
      }
      // ...
  private:
      // 被动模式套接字接受器。
      ACE_SOCK_Acceptor acceptor_;
  };
  
  int main (int argc, char *argv[])
  {
      // 事件复用器
      ACE_Reactor reactor;
      Logging_Acceptor acceptor
      ((ACE_INET_Addr) PORT_NUM);
      reactor.register_handler
      (&acceptor, ACE_Event_Handler::READ_MASK);
      // 循环“forever”接受连接并处理日志记录。
      for (;;)
      reactor.handle_events ();
      /* NOTREACHED */
  }
  ```

在内部，`Reactor::register_handler` 方法通过调用 `acceptor` 对象的 `get_handle` 虚拟方法来检索底层的 I/O 句柄。当调用 `Reactor::handle_events` 方法时，所有注册对象的句柄都会被聚合并传递给 `select`（或 `poll`）。这个操作系统级别的事件复用调用检测到这些句柄上的基于 I/O 的事件的发生。当输入事件发生或输出事件变得可能时，I/O 句柄变得“活跃”。此时，Reactor 通过调用处理事件的方法来通知适当的派生对象。例如，在上面的例子中，当一个连接请求到达时，Reactor 调用 `ACE_Acceptor` 类的 `handle_input` 方法。该方法接受新连接并创建一个 `Logging_Handler`（未显示），它读取客户端发送的所有数据并在标准输出流上显示它。上述描述的 `ACE_Timer_Queue` 类处理基于时间的事件。当此队列管理的定时器到期时，先前安排的 `ACE_Event_Handler` 派生对象的 `handle_timeout` 方法被调用。该方法传递当前时间，以及在最初安排派生对象时传入的 `void *` 参数。

当 `ACE_Event_Handler` 对象中的任何方法返回 -1 时， Reactor 自动调用该对象的 `handle_close` 方法。`handle_close` 方法可用于执行任何用户定义的终止活动（例如，删除对象分配的动态内存，关闭日志文件等）。在此回调函数返回后， Reactor 从其内部表中删除关联的派生类对象。

### 3.2 平台依赖的类组件

`ACE_Reactor` 类为 Reactor 框架提供了中心接口。在 UNIX 平台上，这个类是框架中唯一包含平台依赖代码的部分（`ACE_Time_Value` 类的私有表示在非 UNIX 平台上也可能不同）。

![](../asserts/reactor-pattern-figure6.png)

- **ACE_Reactor**：图 6 展示了 Reactor 类接口中的主要方法，该类封装并扩展了 `select` 和 `poll` 的功能。这些方法可以分为以下几类：

  - **管理方法** - 构造函数和 `open` 方法创建并初始化 `ACE_Reactor`，通过动态分配各种数据结构（在第 3.2.1 节和第 3.2.2 节中描述）。析构函数和 `close` 方法释放这些数据结构。

  - **基于 I/O 的事件处理器方法** - 从 `ACE_Event_Handler` 类的子类派生的实例可以通过 `register_handle` 方法与 Reactor 实例注册。事件处理器也可以通过 `remove_handler` 方法移除。

  - **基于定时器的事件处理器方法** - 传递给 `ACE_Reactor` 的 `schedule_timer` 方法的 `ACE_Time_Value` 参数是相对于当前时间“相对”的。例如，以下代码安排一个对象从 `delay` 秒数开始，每隔 `interval` 秒数打印“hello world”：

    ```c++
    class Hello_World : public ACE_Event_Handler {
    public:
        virtual int handle_timeout(const ACE_Time_Value &tv, const void *arg) {
            ACE_DEBUG((LM_DEBUG, "hello world\n"));
            return 0;
        }
        // ...
    };
    
    int main(int argc, char *argv[]) {
        if (argc != 3) ACE_ERROR_RETURN((LM_ERROR, "usage: %s delay interval\n", argv[0]), -1);
        Reactor reactor;
        Hello_World handler; // 定时器对象。
        ACE_Time_Value delay = ACE_OS::atoi(argv[1]);
        ACE_Time_Value interval = ACE_OS::atoi(argv[2]);
        reactor.schedule_timer(&handler, 0, delay, interval);
    
        for (;;) reactor.handle_events(); /* NOTREACHED */
    }
    ```

    然而，默认的 `ACE_Timer_Queue` 的实现将值存储在“绝对”时间单位中。也就是说，它将计划的时间与当前的日期时间相加。

    由于 `ACE_Reactor` 类的接口由虚拟方法组成，因此通过继承很容易扩展 `ACE_Reactor` 的默认功能。例如，修改 `ACE_Timer_Queue` 实现以使用第 3.1 节中描述的替代表示，不需要对 `ACE_Reactor` 的公共或私有接口进行任何可见的更改。

  - **事件循环方法** - 注册基于 I/O 和/或基于定时器的对象后，应用程序进入一个事件循环，连续调用两个 `Reactor::handle_events` 方法之一。这些方法阻塞一个应用程序指定的时间间隔，等待（1）一个或多个句柄上的同步 I/O 事件的发生和（2）基于定时器的事件。随着事件的发生，`ACE_Reactor` 调度应用程序注册来处理这些事件的对象的适当方法。

接下来的段落描述了基于 `poll` 的 Reactor 和基于 `select` 的 Reactor 之间的主要区别。尽管 `ACE_Reactor` 类的某些方法在不同的操作系统平台上的实现不同，但方法名称和整体功能保持不变。这种一致性源于 `ACE_Reactor` 的设计和实现的模块化，这增强了它的重用性、可移植性和可维护性。

#### 3.2.1 基于 select 的 Reactor 的类组件

![](../asserts/reactor-pattern-figure5.png)

如图 5（1）所示，基于 `select` 的 `ACE_Reactor` 的实现包含三个动态分配的 `ACE_Event_Handler *` 数组。这些数组存储处理读取、写入、异常和/或基于定时器的事件的注册 `ACE_Event_Handler` 对象的指针。`ACE_Handle_Set` 类为底层 `fd_set` 位掩码数据类型提供了一个高效的 C++ 包装器。`fd_set` 将 I/O 句柄名称空间映射到一个紧凑的位向量表示，并提供了几个操作，用于启用、禁用和测试对应于 I/O 句柄的位。一个或多个 `fd_set` 被传递给 `select` 调用。`ACE_Handle_Set` 类通过（1）使用“全字”比较来最小化不必要的位操作和（2）缓存某些值以避免在每次调用时重新计算位偏移量，从而优化了几个常见的 `fd_set` 操作。

#### 3.2.2 基于 poll 的 Reactor 的类组件

`poll` 接口比 `select` 更通用，允许应用程序等待更广泛的事件（例如“优先级带” I/O 事件）。因此，图 5（2）中显示的基于 `poll` 的 Reactor 实现比基于 `select` 的版本要小且不那么复杂。例如，基于 `poll` 的 `ACE_Reactor` 不需要三个 `ACE_Event_Handler *` 数组或 `ACE_Handle_Set` 类。相反，内部动态分配了一个单一的 `ACE_Event_Handler` 指针数组和一个 `pollfd` 结构体数组，用于存储注册的 `ACE_Event_Handler` 派生类对象。

## 4 使用和评估 Reactor

Reactor 框架旨在简化分布式应用程序的开发，特别是网络服务器。为了说明 Reactor 的典型用法，下面的部分将分析分布式日志记录应用程序的设计和实现[^1]。本部分描述了日志记录应用程序中的主要 C++ 类组件，将基于面向对象的 Reactor 解决方案与之前用 C 编写的版本进行了比较，并讨论了 C++ 对 Reactor 框架和分布式日志记录功能的影响。

![](../asserts/reactor-pattern-figure7.png)

图7：分布式日志记录设施中的运行时活动

### 4.1 分布式日志设施概述

这个分布式日志记录设施最初是为一个商业在线事务处理产品设计的。该日志记录设施使用客户端/服务器架构，为通过本地区域和/或广域网络连接的工作站和对称多处理器提供日志记录服务。日志记录设施结合了 Reactor 的事件复用和调度功能，以及 `IPC SAP` 封装库提供的 BSD 套接字和 System V 传输层接口（TLI）的面向对象接口。日志记录提供了一个“仅追加”的存储服务，记录从一个或多个应用程序发送的诊断信息。日志记录的主要单元是 `record`。传入的记录被追加到日志的末尾，并且禁止所有其他类型的写访问。

![../asserts/reactor-pattern-figure8.png]()

图8：日志记录格式

分布式日志记录设施由图 7 所示的以下三个主要组件组成：

- **应用日志接口**：客户端主机上运行的应用程序进程（例如 P1、P2、P3）使用 `Log_Msg` C++ 类生成各种类型的日志记录（例如 `LOG_ERROR` 和 `LOG_DEBUG`）。`Log_Msg::log` 方法提供了一个 `printf` 风格的接口。图 8 描述了应用程序接口和日志守护进程之间交换的记录的优先级级别和数据格式。当被应用程序调用时，日志接口格式化并时间戳这些记录，并将它们写入一个众所周知的命名管道（也称为 FIFO），在那里它们被客户端日志守护进程消费。

- **客户端日志守护进程**：客户端日志守护进程是一个单线程的、迭代的守护进程，运行在参与分布式日志记录服务的每个主机机器上。每个客户端日志守护进程连接到命名管道的读取端，用于从该机器上的应用程序接收日志记录。使用命名管道是因为它们是一种在本地主机上高效的进程间通信（IPC）形式。此外，System V Release 4 UNIX 中命名管道的语义已经扩展，允许“优先级带”消息，可以按“重要性顺序”接收，以及按“到达顺序”（这仍然是默认行为）[^9]。

  客户端日志守护进程和应用程序日志接口的完整设计将在后续的《C++ Report》文章中呈现，该文章介绍了用于多种“本地主机” IPC 机制（如System V Release 4 FIFOs、STREAMpipes、消息队列和 UNIX 域流套接字）的 C++ 包装器。通常情况下，客户端日志守护进程会连续从应用程序按优先级顺序接收日志记录，将多字节记录头字段转换为网络字节顺序，并将记录转发给服务器日志守护进程（通常在远程主机上运行）。

- **服务器日志守护进程**：服务器日志守护进程是一个并发的守护进程，不断收集、重新格式化并向各种外部设备显示传入的日志记录。这些设备可能包括打印机、持久存储库或日志管理控制台。本文的其余部分将重点介绍服务器日志守护进程。此外，本示例中还展示了几个 Reactor 和 `IPC SAP` 机制。

### 4.2 服务器日志守护进程

下面将讨论构建服务器日志守护进程的主要类使用的接口和实现。日志服务器是一个单线程的并发守护进程，运行在一个单独的进程中。并发性是通过让 Reactor 以轮询方式对每个活动客户端进行“时间分片”分配来实现的。具体来说，在每次调用 `Reactor::handle_events` 方法时，从每个 I/O 句柄在此期间变为活动的客户端读取单个日志记录。这些日志记录被写入服务器日志守护进程的标准输出。此输出可以重定向到各种设备，如打印机、持久存储库或日志管理控制台。

除了下面第 4.2.3 节中显示的主要驱动程序外，日志设施架构中还出现了几个其他 C++ 类组件。这些组件在图 9 中使用 Booch 符号[^10]展示了类继承和参数化关系。为了增强重用性和可扩展性，图中显示的组件被设计为解耦应用程序架构的以下方面：

![](../asserts/reactor-pattern-figure9.png)

图9：服务器日志守护进程的类组件

- Reactor 框架组件 - 第 3 节中讨论的 Reactor 框架组件封装了执行 I/O 复用和事件处理器调度的最低级别机制。
- 连接相关机制 - 第 4.2.1 节中讨论的组件代表了一组通用模板，提供了可重用的连接相关机制。具体来说，`ACE_Acceptor` 模板类是一个通用类，旨在标准化和自动化接受来自客户端的网络连接请求的步骤。同样，`ACE_Svc_Handler` 模板类是另一个通用类，旨在向/从连接的客户端发送和/或接收数据。
- 应用程序特定服务 - 第 4.2.2 节中讨论的组件代表了分布式日志记录设施的应用程序特定部分。具体来说，`Logging_Acceptor` 类为 `ACE_Acceptor` 提供了特定的参数化类型，创建了特定于日志应用程序的连接处理实例。同样，`Logging_Handler` 类被实例化以提供从远程客户端接收和处理日志记录所需的应用程序特定功能。

一般来说，通过采用这种高度解耦的面向对象分解，与原始方法相比，开发和维护服务器日志守护进程的工作量显著减少。

[^6]: 
[^7]: 
[^3]: 
[^4]: 
[^1]: 
[^9]: 
[^10]: 