# 如何正确使用锁

在提供 `async,await` 关键字之后，我们尤其要注意，在使用的时候千万不要同步等待 Task。这样很有可能会发生死锁，以及吞吐量大的时候会发生浪费线程池的线程以及线程上下文切换导致更大的开销。

所以我们在使用多线程乃至并发的时候，尽量要做到无锁。

我们在于 I/O 线程交互时，发生等待阻塞当前线程。这时就有两种情况

1. 线程会被阻塞为等待状态，不会参与**线程调度**，并运行另一个线程。如果当前所有线程都被占用或者阻塞，那么就会在线程池中新创建一个线程来完成后续任务。
2. 线程遇到某个同步对象，会等待解锁而不停的自旋（Spin）。如果无法及时获取信号量，就会进入第 1 步的状态。

以上两种情况都会对线程池产生一定影响，会创建不必要的线程以及第 2 步会让 CPU 白白自旋浪费资源。所以都是不可取的。

# 如何高效的在 I/O 中使用 Task

以下载资源为例，一般我们的做法都是一次性读取整个文件流加载到内存中。这样会导致程序运行时间长，伸缩性不强。由于等待时间长，浪费的资源也很大。所以我们可以从流中读取多次并加载到内存，直到读取内容结束。、用两层 Task 即可完成任务，外层 Task 表示全部读取的工作，提供给调用者调用。而内层的 Task 用于每次读取剩下的文件流。我们不能在第 1 次返回 Task，这样我们得到的不是读取整个流的 Task，而是用一个 Task 来表示内层的 Task 已经全部读取完成。

这可以用 `TaskCompletionSource<T>` 来达到目的，它可以帮你生成一个用于返回的伪 Task，这会让 Wait 或 ContinueWith 的调用者继续往下执行。

```c#
private static Task<int> AsynchronousRead(string fileName) {
    var chunkSize = 4096; // 设置一次读取的字节数
    var buffer = new byte[chunkSize]; // 缓冲区
    var tcs = new TaskCompletionSource<int>();

    var fileContent = new MemoryStream();
    var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, chunkSize, useAsync : true);
    fileContent.Capacity += chunkSize;

    var task = fileStream.ReadAsync(buffer, 0, buffer.Length);
    task.ContinueWith(readTask => {
        ContinuRead(readTask, fileStream, fileContent, buffer, tcs);
    });

    return tcs.Task;
}

private static void ContinuRead(Task<int> task, FileStream fileStream, MemoryStream fileContent, byte[] buffer, TaskCompletionSource<int> tcs) {
    if (task.IsCompleted) {
        int bytesRead = task.Result;
        fileContent.Write(buffer, 0, bytesRead);
        if (bytesRead > 0) {
            var newTask = fileStream.ReadAsync(buffer, 0, buffer.Length);
            // 这行代码很重要，注意，这并不是递归
            newTask.ContinueWith(readTask => ContinuRead(readTask, fileStream, fileContent, buffer, tcs));
        } else {
            tcs.TrySetResult((int) fileContent.Length);
            fileStream.Dispose();
            fileContent.Dispose();
        }
    }
}
```

这段是《编写高性能 .NET 代码》书中的一段代码，其中有一段非常有意思的是这段代码：

`newTask.ContinueWith(readTask => ContinuRead(readTask, fileStream, fileContent, buffer ,cts))`，很多人第一反应觉得是这是用了递归。其实不是，这是 “递归”。性能要远比真正的递归要高。它的执行复杂度是常数级的，而递归是指数级的。关于伪递归可以详见 [尾递归与Continuation](http://blog.zhaojie.me/2009/03/tail-recursion-and-continuation.html)。

# 同步线程和锁

我们在用多线程，如果想要在多个线程之间共享资源，并且只能在同一时刻访问这个资源时，我们就要用到线程同步了。就是利用一个同步对象来完成的，比如 `Monitor、Semaphore、ManualResetEvent` 等。这些对象就被称为 “锁”，线程中处理同步的过程就是 “加锁”。有一点可以明确的是，加锁绝对不会提高性能，在发生资源竞争时还会降低性能。因为同步锁会阻止其他线程运行，造成 CPU 闲置，增加上下文切换的几率。所以我们在系统要尽量做到 “无锁”。

加入我们在系统有一个 “只读” 变量，那么就可以无锁，所有变量都可以读取。这是 “函数式编程” 的精髓，数据对象就是不变的。但是如果存在多个线程要写入这个变量，那么我们就要想想，这个变量能不能变成每个线程内部的局部变量，这样就能避免线程同步了。这样在每个线程都会各自的变量写入之后，最后再进行一次共享访问即可。

```c#
private static void MultiThreadWriteShareObjectWithoutLocking() {
    object syncObj = new object();
    var masterList = new List<long>();
    const int numTasks = 8;
    Task[] tasks = new Task[numTasks];

    for (int i = 0; i < numTasks; i++) {
        tasks[i] = Task.Run(() => {
            var localList = new List<long>();
            for (int j = 0; j < 5000000; j++) {
                localList.Add(j);
            }
            lock(syncObj) {
                masterList.AddRange(localList);
            }
        });
    }

    Task.WaitAll(tasks);
}
```

# 如何用锁

当你决定用锁的时候，一定得知道各个锁的机制。因为每个锁的性能都是不同的。如果无所谓性能那就直接用 lock。如果要用 lock 之外的同步锁机制，那就应该仔细研究估量。但大体会在下面几点选择：

1. 不做同步
2. 用 Interlocked 方法
3. 用 lock/Monitor 类
4. 用异步锁
5. 其他机制

以上顺序是以性能高低来排序的，但特定的环境条件可能会不一样。取决于采用的方式，比如一次使用多个 Interlocked 方法就不如用一次 lock 语句。

## Lock

```c#
private bool isComplete = false;
private object completeLock = new object();

private void Complete()
{
	lock(completeLock)
  {
    if(isComplete)
    {
      return;
    }
    isComplete = true;
  }
}
```

在这里你用到两个成员变量来判断方法是否已经被执行过，这其实可以通过 `Interlocked.Increment` 来达到同样的目的，并且性能更高

```c#
private int isComplete = 0;

private void Complete()
{
	if(Interlocked.Increment(ref isComplete) == 1)
	{
		...
	}
}
```

Interlocked 附带的方法都是原子操作。并且可以利用它来实现 （无锁）lock-free 的数据结构。以下是用 `Interlocked.CompareExchange` 实现的简单的无锁数据结构

```c#
class FreeLockStack<T> {
  private class Node {
    public T Value;
    public Node Next;
  }
  
  private Node head;
  public void Push (T value) {
    var newNode = new Node() { Value = value };
    while(true) {
      newNode.Next = this.head;
      if (Interlocked.CompareExchanged(ref this.head, newNode, newNode.Next)) {
        return;
      }
    } 
  }
}
```

## Monitor 混合锁

在一般情况下，我们都应该使用 Monitor 锁机制，微软提供一个关键字 lock 来方便快捷的占有资源。以下代码是等价的

```c#
object obj = new object();
bool taken = false;
try {
	Monitor.Enter(obj, ref taken);
} finally {
  if(taken) {
    Monitor.Exit(obj);
  }
}

// 等价于
object obj = new object();
lock(obj) {
  ...
}
```

Monitor 是一种混合锁，在进入等待状态并释放线程之前，会先尝试在循环中自旋一段时间。这样就可以在竞争不激烈或者是竞争时间很短的情况下，可以获得理想的性能。

如果我们在判断有没有获取锁的相关判断，并继而做其他逻辑，Monitor 有个方法 `Monitor.TryEnter` 这个方法会立即返回。

## 异步锁

我们在用上面的锁时，当特别时对象数量很多时，在占有锁的同步情况下，会阻塞线程。所以 .NET 4.5 之后开始增加了一些异步锁。

同步代码详见：[同步锁版本](https://github.com/MarsonShine/Books/blob/master/WHPerformanceDotNet/src/LockFreeWithInterlocked/AsyncLock.cs)

由于在吞吐量大的程序中，在调用 Wait 方法后，线程将会阻塞至心好凉被释放。这会导致资源的浪费，也降低了机器的处理能力，并且很有可能会发生线程上下文切换（内核用户模式切换）。

下面我们来看异步锁的版本：

```c#
static void WriterFuncAsync() {
    semaphore.WaitAsync().ContinueWith(_ => {
        Console.WriteLine("Writer: Obtain");
        for (int i = length; i < array.Length; i++) {
            array[i] = i * 2;
        }
        Console.WriteLine("Writer: Release");
        semaphore.Release();
    }).ContinueWith(_ => WriterFuncAsync()); // 注意，这里不是递归，而是“伪递归”，每次调用都会新开堆栈，之前的方法都会被及时垃圾回收
}
```

## 考虑用其他替代方案

我们在实现一个线程安全的操作时，微软为了避免我们花精力去实现线程安全类，提供给我们一些类：`ConcurrentBag<T> 无需集合；ConcurrentDictionary<TKey, TValue> 键值对，ConcurrentQueue<T> 先进先出队列，ConcurrentStack<T> 后进先出队列`

如果数据大部分都是**只读的**，那么我们可以使用非线程安全的集合。在需要修改集合中的元素时，可以生成一个全新的集合对象，在数据修改完毕之后把原来的引用替换掉即可。

```c#
private volatile Dictionary<string, object> data = new Dictionary<string, object>();
public Dictionary<string, object> Data => data;

private void UpdateData() {
    var newData = new Dictionary<string, object>();
    newData["Foo"] = new { };
    data = newData;
}
```

其中关键字 volatile 关键字是确保 data 所有线程都能正确更新。