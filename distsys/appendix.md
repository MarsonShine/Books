# 拓展阅读与附录

如果你读到这里，非常感谢。

如果你喜欢这本书，请在 [Github](https://github.com/mixu/)（或 [Twitter](https://twitter.com/mikitotakada)）上关注我。我很高兴看到我产生了积极的影响。“创造的价值超过你获得的价值”，诸如此类的话。

非常非常感谢：logpath、alexras、globalcitizen、graue、frankshearar、roryokane、jpfuentes2、eeror、cmeiklejohn、stevenproctor、eos2102 和 steveloughran 的帮助！当然，任何剩下的错误和遗漏都是我的责任！

值得注意的是，我关于最终一致性的章节主要以伯克利为中心；我想改变这一点。我还略过了一个显著的时间用例：一致性快照。此外，还有几个主题我应该详细展开：即明确讨论安全性和活性属性以及更详细地讨论一致性哈希。然而，我要去参加2013年的[Strange Loop](https://thestrangeloop.com/)会议，所以就这样吧。

如果这本书有第6章，它可能会讨论如何利用和处理大量数据。看来最常见的“大数据”计算类型是将大型数据集传递给一个简单的程序。我不确定后续的章节会是什么（也许是高性能计算，考虑到目前的重点是可行性），但我可能会在几年内知道。

关于分布式系统的书籍

1. **Distributed Algorithms (Lynch)**
   这是关于分布式算法最常被推荐的书。我也会推荐它，但有一个警告。它非常全面，但面向研究生读者，所以你会花很多时间阅读同步系统和共享内存算法，然后才会到达对实践者最有趣的内容。

2. **Introduction to Reliable and Secure Distributed Programming (Cachin, Guerraoui & Rodrigues)**
   对于实践者来说，这是一本有趣的书。它简短且充满实际的算法实现。

3. **Replication: Theory and Practice**
   如果你对复制感兴趣，这本书非常棒。关于复制的章节主要基于这本书的有趣部分以及更多的最新阅读材料的综合。

4. **Distributed Systems: An Algorithmic Approach (Ghosh)**
5. **Introduction to Distributed Algorithms (Tel)**
6. **Transactional Information Systems: Theory, Algorithms, and the Practice of Concurrency Control and Recovery (Weikum & Vossen)**
   这本书讲的是传统的事务信息系统，例如本地RDBMS。最后有两章是关于分布式事务的，但这本书的重点是事务处理。

7. **Transaction Processing: Concepts and Techniques by Gray and Reuter**
   一本经典著作。我发现 Weikum & Vossen 更加与时俱进。

## 重要文献

每年，[Edsger W. Dijkstra 分布式计算奖（Edsger W. Dijkstra Prize in Distributed Computing）](https://en.wikipedia.org/wiki/Dijkstra_Prize)都会颁发给有关分布式计算原理的杰出论文。请点击链接查看完整名单，其中包括以下经典论文：

- [分布式系统中的时间、时钟和事件排序](http://research.microsoft.com/users/lamport/pubs/time-clocks.pdf)--莱斯利-兰波特
- [分布式共识不可能有一个错误的过程](http://theory.lcs.mit.edu/tds/papers/Lynch/jacm85.pdf)--费舍尔、林奇、帕特森
- [不可靠的故障探测器和可靠的分布式系统](https://scholar.google.com/scholar?q=Unreliable+Failure+Detectors+for+Reliable+Distributed+Systems)--钱德拉和图埃格

微软学术搜索（Microsoft Academic Search）提供了一份按[引用次数排序的分布式与并行计算领域顶级出版物列表](http://libra.msra.cn/RankList?entitytype=1&topDomainID=2&subDomainID=16&last=0&start=1&end=100)--这份列表可能很有趣，您可以从中略读更多经典作品。

以下是其他一些推荐论文列表：

- [Nancy Lynch 的分布式系统课程推荐阅读列表](http://courses.csail.mit.edu/6.852/08/handouts/handout3.pdf)。
- [NoSQL 夏季论文列表](http://nosqlsummer.org/papers)--与这一热门词汇相关的论文精选列表。
- [关于分布式系统开创性论文的 Quora 问题](https://www.quora.com/What-are-the-seminal-papers-in-distributed-systems-Why)。

### 系统

- [谷歌文件系统](https://research.google.com/archive/gfs.html)--Ghemawat、Gobioff 和 Leung
- [MapReduce：大型集群上的简化数据处理](https://research.google.com/archive/mapreduce.html)--Dean 和 Ghemawat
- [Dynamo：亚马逊的高可用键值存储](https://scholar.google.com/scholar?q=Dynamo%3A+Amazon%27s+Highly+Available+Key-value+Store)--DeCandia 等人
- [Bigtable：结构化数据的分布式存储系统](https://research.google.com/archive/bigtable.html)--Chang 等人
- [松耦合分布式系统的 Chubby Lock 服务](https://research.google.com/archive/chubby.html)--Burrows
- [ZooKeeper：互联网规模系统的无等待协调](http://labs.yahoo.com/publication/zookeeper-wait-free-coordination-for-internet-scale-systems/)--Hunt、Konar、Junqueira、Reed，2010 年