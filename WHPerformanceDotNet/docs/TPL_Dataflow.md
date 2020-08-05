# TPL Dataflow

TPL Dataflow 允许你构造一个数据处理块（Block），并将它们连接到管道或网络中。你可以并行的处理每一个块，并且每个块都可以异步执行。块与块之间彼此通信，块也可以链接到其他源或多个目标。TPL Dataflow 的所有处理都是异步的。

但是要注意，使用 TPL Dataflow 并不能使系统更快，而是使用这种编程风格能更好的让系统达到高性能，让你只用关注于逻辑的处理。

TPL Dataflow 主要使用以下类型

- `BufferBlock<T>` 	FIFO 的消息队列
- `BroadcastBlock<T>`   广播消息，向所有目标发送最新的消息
- `WriteOnceBlock<T>`    类似 `BroadcastBlock`，但是一次只能设置一个值
- `ActionBlock<T>`    执行输入委托，无返回数据
- `TransformBlock<T>`    执行一个可以返回与输入不同类型的委托
- `TransformManyBlock<T>`    类似 `TransformBlock` ，但是一次输入可以输出多个值
- `BatchBlock<T>`    将多个输入转换成单个数组输出

你也可以创建自己的块类型，只需要实现 `ISourceBlock<TOutput>` 或 `ITargetBlock<TInput>` 这两个接口。

> 在 TPL Dataflow 发布之前，其实可以使用 TPL 库来完成上述功能，但是非常复杂，这里面涉及到复杂的连续、同步和协调的系统。实现起来很困难

示例：

```c#
// 创建 Pipeline 处理数据块
ITargetBlock<string> startBlock = CreateTextProcessingPipeline(path, out completionTask);
// 发送数据
startBlock.Post(inputData);
// 等待管道处理完成
completionTask.Wait();

// CreateTextProcessingPipeline 负责组合各个逻辑块
private static ITargetBlock<string> CreateTextProcessingPipeline(string inputPath, out Task completionTask){
	var getFilenames = new TransformManyBlock<string,string>(GetFileNames);	// GetFileNames 为一个委托，下同
    var getFileContents = new TransformBlock<string,string>(GetFileContents);
    var analyzeContents = new TransformBlock<string, Dictionary<string, ulong>>(DoAnalyzeContent);
    var eliminateIgnoredWords = new TransformBlock<Dictionary<string, ulong>, Dictionary<string, ulong>>(DosomeMagic);
    var batch = new BatchBlock<Dictionary<string, ulong>>(fileCount);
    var combineFrequencies = new TransformBlock<Dictionary<string, ulong>[], List<KeyValuePair<string, ulong>>>(DosomeMagic);
    var printTopTen = new ActionBlock<List<KeyValuePair<string, ulong>>>(Printf);
    // 开始连接每个处理块
    getFilenames.LinkTo(getFileContents);
    getFileContents.LinkTo(analyzeContents);
    analyzeContents.LinkTo(eliminateIgnoredWords);
    eliminateIgnoredWords.LinkTo(batch);
    batch.LinkTo(combineFrequencies); 
    combineFrequencies.LinkTo(printTopTen);
    // 返回整个块的开端起始任务
    completionTask = getFilenames.Completion; 
    return getFilenames;
}
```

