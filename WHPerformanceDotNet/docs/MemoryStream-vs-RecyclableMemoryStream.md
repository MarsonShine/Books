# 高性能 MemoryStream 组件 —— RecyclableMemoryStream

MemoryStream 有什么问题：

1. 大对象堆（LOH）分配，当数据流很大的时候就会经常发生 LOH 分配。
2. 内存浪费，现在的 MemoryStream 当数据尺寸超过阈值的时候默认就会翻倍，这就会持续导致内存内存增长。
3. 内存数据拷贝，每次 MemoryStream 扩容时，就会把已有的 buffer 全部拷贝到新的缓冲区中，这个在调用方法 `GetBuffer` 就会如此。
4. 内存碎片，由于内存不会重用，在扩容等因素下，会导致内存碎片

RecyclableMemoryStream 的实现细节：

流是实现在一系列的尺寸大小相等的块之上的。随着 `stream.length` 的增长，那些附加的块都是从内存管理器（`RecyclableMemoryStreamManager`）中接收的。这些块都是被池化的，被池化的并不是流对象本身。

一旦大缓冲区被分配到流对象中，那么小块就永远不会被使用。所有的操作都在基于这个大的缓冲区。你可以根据需要，这个大的缓冲区能够被已经池化的更大的缓冲区替换。所有的数据块和大缓冲区都在流中维护，除非你在调度器（`RecyclableMemoryStreamManager`）启用了 `AggressiveBufferReturn`。

使用 `RecyclableMemoryStream` 也很简单：

```c#
private static readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();

var sourceBuffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };        
using (var stream = manager.GetStream())
{
    stream.Write(sourceBuffer, 0, sourceBuffer.Length);
}

// 方式二
using (var stream = manager.GetStream("Program.Main"))
{
    stream.Write(sourceBuffer, 0, sourceBuffer.Length);
}
// 方式三
var stream = manager.GetStream("Program.Main", sourceBuffer, 0, sourceBuffer.Length);
```

