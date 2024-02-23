# 第二部分 - 从事件到洞察力

我们生活在一个以惊人的速度产生、存储和分发数据的时代。消费这些数据可能会让人不知所措，就像直接从消防水龙管中喝水一样。我们需要能够识别重要数据，也就是说，我们需要确定什么是相关的，什么是不相关的。我们需要将数据组合起来进行集体处理，以发现可能从任何单独的原始输入中看不出的模式或其他信息。用户、客户和管理人员需要在比以往更多的数据中进行这样的操作，同时仍然提供更高的性能和更有用的输出。

Rx 提供了一些强大的机制，可以从原始数据流中提取有意义的洞察力。这是将信息表示为 `IObservable<T>` 流的主要原因之一。前面的章节展示了如何创建可观察的序列，现在我们将看看如何利用这个已经解锁的能力，使用各种 Rx 方法来处理和转换可观察的序列。

Rx 支持大多数标准的 LINQ 操作符。它还定义了许多额外的操作符。这些操作符大致可以分为以下几个类别，每个后续章节都会涉及其中的一个类别：

- [过滤（Filtering）](operation-filtering.md)
- [转换（Transformation）](operation-transformation.md)
- [聚合（Aggregation）](operation-aggregation.md)
- [分区（Partitioning）](operation-partitioning.md)
- [组合（Combination）](operation-combination.md)