# 事务

事务可以处理很多复杂的场景，以及各种位置的错误情况（网络错误、进程崩溃、断电、磁盘已满、并发竞争等）会导致数据不一致的情况。

在这些错误模式下，我们也有相应的数据隔离级别：读-提交、快照隔离（或可重复读）与可串行化。这些隔离级别可以处理下面这些**边界条件**：

- 脏读：客户端读到了其他客户端尚未提交的写入。**读-提交**以及更强的隔离级别可以防止脏读。

  > 读-提交隔离通常是采用**行级锁**，当事务想要修改某个对象（行或文档）时，它首先必须先获取该对象的锁；然后一直持久锁知道事务提交完成或终止，而另一个事务尝试更新同一个对象则需要等待前面的事务提交完成或事务中止。但是由于这里面的性能体验不好，毕竟读数据期间如果非常长的话，会导致等待的事务越积越多。很多数据库采取的解决方式是：**对每个对象在更新时会维护其旧值和当前持锁事务要设置的新值两个版本。在事务提交之前，所有其他读操作都读取旧值；只有在持锁事务提交成功之后才会切换到最新值。**

- 脏写：客户端覆盖了另一个客户端尚未提交的写入。几乎所有数据库实现都可以防止脏写。读-提交隔离级别可以防止脏写，一般做法是**延迟第二个写请求**，直到前面的事务完成提交或中止。

- 读倾斜（不可重复读）：**客户在不同的时间点看到了不同值，出现了其中一个覆盖另一个的写入，但又没包含对方最新值的情况，最终导致了部分修改数据发生丢失**。通常采用**多版本并发控制（MVCC）**来实现快照隔离

- 更新丢失：**两个客户端同时执行读-修改-写入的操作序列，出现了其中一个覆盖了另一个的写入，但又没包含对方最新值的情况**，最终导致了部分修改数据发生丢失。通常快照隔离的实现可以防止这种事情的发生，另一种情况也是需要手动去锁定查询结果（SELECT FOR UPDATE）

- 写倾斜：事务首先查询数据，根据返回的结果作出对应的决定，然后操作数据库。当事务提交时，支持这种决定的先决条件已经被更改导致执行操作的条件无法成立。只有串行化的隔离才能解决这种问题。

- 幻读：事务读取了某些符合条件的查询结果，通过另一个客户端执行写入，改变了先前的查询结果。快照隔离可以防止简单的幻读，但写倾斜情况则需要特殊处理，例如采用**区间范围锁**

可串行化的隔离要求最严格，如果一个事务执行的时间非常短，并且单个 CPU 核可以满足事务的吞吐要求，那么直接使用可串行化的隔离是一个非常合适的方案。

- 两阶段加锁：
- 可串行化的快照隔离（SSI）：它是乐观锁的一种，允许多个事务并发执行而不互相阻塞；仅当事务尝试提交时，才检查可能的冲突，如果发现违背了串行化，则这些事务就会被终止。

## 多版本并发控制（MVCC）

### 快照级别隔离

其总的实现方式是，每个事务都从数据库的一致性快照中读取数据，事务一开始所看到的是最近提交的数据，即使随后数据会被其他事务所更改，但保证每个事务都只看到该特定时间点的旧数据。

实现方式通常是采用**写锁**来防止脏写，也就是说正在进行写操作的事务会阻塞同一对象上的其他事务操作。**但是读操作是不需要加锁的**。所以从性能上看，这与读锁不同，写锁是不会阻塞读操作的。

还有一种更加通用的解决方案，那就是多版本并发控制：考虑到多个正在进行的事务可能会在不同的时间查询数据，所以**数据库保留了对象多个不同的提交版本**。

通过前面的学习的读提交隔离级别要求，也是可以通过新旧两个版本的值来避免脏读，其实也可以用 MVCC 来实现读-提交隔离。在读-提交隔离下，只保留对象的两个版本即可：一个已提交的旧版本和一个未提交的新版本。

## 两阶段加锁（two-phase locking，2PL）

两阶段加锁类似读-提交隔离实现方法，通过加锁来防止脏写。但是灵活度更高，多个事务可以同时读取同一个对象，但只要涉及到任何写操作，则必须加锁以独占形式操作。

两阶段加锁最大的问题就是性能问题，多个事务只要一旦涉及到写的并发，就会导致彼此互斥，等待独占锁的事务提交完成或中止。所以其事务的吞吐能力和查询相应时间要比其他隔离级别要少。

这里面优化措施就是**谓词锁**和**索引区间锁**

- 谓词锁，事务 A 在根据特定条件查询对象，而此时另一个事务 B 执行提交并更改了事务 A 的查询条件，此时 A 的查询结果就不满足条件了，此时就可以通过谓词锁来避免这种情况。**谓词锁不是特定对象的，而是作用于满足某些搜索条件的所有查询对象**。但是谓词的限制很多，性能不高，同样的条件下，只有持有谓词锁的事务才能继续执行，其他事务需要等待。并且如果此时事务 A 想要插入，更新或删除对象，必须首先要检查所有的旧值和新值是否与现在的谓词锁匹配（检验冲突），然后必须等待持有锁的人提交。由于性能表现不佳，此时索引区间锁就为此提供了解决方案
- 索引区间锁：谓词锁是保护精准的对象查询条件，而索引区间锁则是在谓词锁的基础之上扩大的锁的范围。例如一个谓词锁保护的查询条件是：房间 123，时间段是中午至下午 1 点，则一种方式是通过扩大时间段来简化，即：保护 123 房间的所有时间段；或者另一种是扩大房间：保护中午至下午 1 点的所有房间。这样任何与谓词锁冲突的操作肯定也和这样的区间锁冲突。

## 可串行化快照隔离（SSI）

可串行化快照隔离是一种乐观并发控制。如果可能发生冲突，事务会继续执行而不是中止，寄希望于接下来相安无事；而当事务提交时（只有可串行化的事务被允许提交），数据库才会检查是否确实发生冲突，如果冲突则中止事务并接下来重试。

虽然此种方式性能表现客观，但是要注意，其性能表现是于当提交时刻检查出来的确发生冲突的比例有直接关系的。例如一个运行很长时间的事务，读取和写入大量数据，因而产生的冲突并中止的概率就会增大。所以在使用 SSI 时要求读-写事务尽量要简短。
