## Parallel开发

### Parallel.Invoke

用户：要调用一批方法，并且方法之间大多是独立的

```c#
static void ProcessArray (double[] array) {
    Parallel.Invoke (
        () => ProcessPartialArray (array, 0, array.Length / 2),
        () => ProcessPartialArray (array, array.Length / 2, array.Length)
    );
}

private static void ProcessPartialArray(double[] array, int begin, int end)
{
    //密集型计算
}
```

当无法确定并行调用数量，在运行时才知道具体并行执行的操作等可以用到委托传参

```c#
public static void DoAction20Times(Action action){
  	var times = Enumerable.Repeat(action, 20).ToArray();
  	Parallel.Invoke(times);
}
```

可以在执行期间取消操作

```c#
public static void DoAction20Times(Action action, CancellationToken token){
  	var times = Enumerable.Repeat(action, 20).ToArray();
  	Parallel.Invoke(
		new ParallelOption { CancellationToken = token },
		times
	);
}
```

> 注意：数据小，并且每个逻辑元素（action）运算能力不能过大，执行的时间不能过长， 当高请求情况时，CPU极速上升，发热宕机都是有可能的。

在执行运行时，发现输入一次数据要执行一个方法，这时 `Invoke` 就不合适，而是`ForEach` ，而当输入数据执行之后有输出，那么 `PLINQ` 就合适一些。

### Parallel.Aggregate

 在并行操作结束时，需要聚合结果，累加求和或求平均值

```c#
static int ParallelSumByAggregate(IEnumerable<int> values){
  	return values.AsParallel()
      	.Aggregate(
      	seed: 0,
    	func: (sum, item) =>  item + sum;
    );
}
```

### Task

一个任务处理一个问题（遍历二叉树），在这个任务中又可以分成两个独立的任务（遍历左节点和遍历右节点），这时就可以用父子任务

```c#
static void Parent(){
  	Task.Factory.StartNew(
        action: () => HandleBinaryTree(),
        cancellationToken: CancellationToken.None,
        creationOptions: CreationOptions.None,
        scheduler: TaskScheduler.Default
    );
}
static void HandleBinaryTree(){
  	var t1 = Task.Factory.StartNew(
        action: () => leftNode,
        cancellationToken: CancellationToken.None,
        creationOptions: CreationOptions.AttachedParent,
        scheduler: TaskScheduler.Default
    );
  	var t2 = Task.Factory.StartNew(
        action: () => rightNode,
        cancellationToken: CancellationToken.None,
        creationOptions: CreationOptions.AttachedParent,
        scheduler: TaskScheduler.Default
    );
}
```

这样就能三个任务独立运行， `CreationOptions.AttachedParent` 是把任务附加到父任务上。如果 parent() 函数要等待所有的子任务运行结束后继续执行，那么就可以在 `HandlebinanryTree()` 函数 await 另一个线程即可，或者是阻塞，那么只需要在上述代码最后加上

```c#
await SomeElseTask();
//或者
Task.WaitAll(new Task[2] {t1, t2});
```

`Task.WaitAll` : 等待一批次任务全部完成（存在堵塞）

`Task.WaitAny` : 等待一批次任务中任务一个任务完成退出（阻塞）

`Task.WhenAll` : 等待一批次任务全部完成，并返回新的任务（不堵塞）

`Task.WhenAny` : 等待一批次任务中任意一个任务完成并返回新的任务（不堵塞）

### Plinq

