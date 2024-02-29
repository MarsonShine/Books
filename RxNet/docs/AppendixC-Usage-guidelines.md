# 附录 C：使用指南

这是一个快速指南列表，旨在帮助您编写 Rx 查询。

- 返回序列的成员不应返回 null 值。这适用于 `IEnumerable<T`> 和 `IObservable<T>` 序列。请返回空序列。
- 只有在需要提前取消订阅的情况下才取消订阅。
- 始终提供 `OnError` 处理程序。
- 避免使用 `First`、`FirstOrDefault`、`Last`、`LastOrDefault`、`Single`、`SingleOrDefault` 和 `ForEach` 等阻塞操作符，而应使用 `FirstAsync` 等非阻塞操作符。
- 避免在 `IObservable<T>` 和 `IEnumerable<T>` 之间来回切换
- 倾向于使用惰性评估，而不是即时评估。
- 将大型查询分成若干部分。大型查询的关键指标
  - 嵌套
  - 超过 10 行的查询表达式语法
  - 使用 `into` 关键字
- 为可观测变量命名，即避免使用 `query`、`q`、`xs`、`ys`、`subject` 等变量名。
- 避免产生副作用。如果实在无法避免，请不要将副作用埋藏在回调中，因为回调中的操作符是为功能性使用而设计的，如 `Select` 或 `Where`。使用` Do` 操作符时要明确。
- 在可能的情况下，首选 `Observable.Create` 而不是 `Subject` 作为定义新 Rx 源的方法。
- 避免创建自己的 `IObservable<T>` 接口实现。使用 `Observable.Create`（如果确实需要，也可使用 `Subject`）。
- 避免创建自己的 `IObserver<T>` 接口实现。最好使用 `Subscribe` 扩展方法重载。
- 应用程序应定义并发模型。
  - 如果需要安排延迟工作，请使用调度器.
  - `SubscribeOn` 和 `ObserveOn` 操作符应始终位于 `Subscribe` 方法之前。(所以不要夹在其中，例如：`source.SubscribeOn(s).Where(x => x.Foo)`。