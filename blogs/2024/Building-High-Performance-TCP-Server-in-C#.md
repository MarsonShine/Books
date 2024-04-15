# 用 C# 构建高性能 TCP 服务器

今年早些时候，当我决定用 C# 构建一个开源 [Telegram 服务器](https://github.com/aykutalparslan/Ferrite?ref=hackernoon.com)时，我需要一个模板来构建服务器的传输层。当我查看 TechEmpower 基准测试时，我感到特别高兴，在该基准测试中，ASP.NET Kestrel 目前以每秒 7,023,107 次响应的成绩在明文基准测试中排名第三。在本篇文章中，我将演示如何构建高性能 TCP 服务器，并借鉴 Kestrel 的一些想法来实现这一目标。

> 所介绍的代码利用了低分配异步编程模式和池化技术，速度极快。

.NET中的 `Socket` 操作由 `SocketAsyncEventArgs` 描述，`SocketAsyncEventArgs` 是一个可重复使用的对象，它提供了一种基于回调的异步模式。首先，需要通过实现 `IValueTaskSource` 接口将该模式封装为异步模式，这样就可以避免为异步路径分配资源。

```c#
//From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketAwaitableEventArgs.cs#L10-L77
public class AwaitableEventArgs : SocketAsyncEventArgs, IValueTaskSource<int>
{
    private ManualResetValueTaskSourceCore<int> _source;

    public AwaitableEventArgs() :
        base(unsafeSuppressExecutionContextFlow: true)
    {
    }

    protected override void OnCompleted(SocketAsyncEventArgs args)
    {
        if (SocketError != SocketError.Success)
        {
            _source.SetException(new SocketException((int)SocketError));
        }

        _source.SetResult(BytesTransferred);
    }

    public int GetResult(short token)
    {
        int result = _source.GetResult(token);
        _source.Reset();
        return result;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _source.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags)
    {
        _source.OnCompleted(continuation, state, token, flags);
    }
}
```

[这里](https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketAwaitableEventArgs.cs?ref=hackernoon.com#L10-L77)是在原始实现的基础上重写的，主要区别在于，为了简单起见，该版本使用了 `ManualResetValueTaskSourceCore`，而原始版本则使用了更薄的 `IValueTaskSource` 接口的自定义实现。


需要的 `Receiver` 和 `Sender` 实现均源自上述 `AwaitableEventArgs`。它们首先为 `SocketAsyncEventArgs` 设置缓冲区，然后调用 Socket 的 `SendAsync` 或 `ReceiveAsync` 方法，如果 I/O 操作未完成，则返回 true，在这种情况下，`Receiver`/`Sender` 可以作为返回 `ValueTask` 的源，因为基类实现了该接口。当 `OnCompleted(SocketAsyncEventArgs args)` 被触发时，等待的 `ValueTask` 要么返回接收/发送的字节，要么抛出异常。但是，如果上述调用返回 false，则表示 I/O 操作同步完成，需要处理同步路径。

```c#
//From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketReceiver.cs
public class Receiver : AwaitableEventArgs
{
   private short _token;
   public ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> memory)
   {
      SetBuffer(memory);
      if (socket.ReceiveAsync(this))
      {
         return new ValueTask<int>(this, _token++);
      }

      var transferred = BytesTransferred;
      var err = SocketError;
      return err == SocketError.Success
         ? new ValueTask<int>(transferred)
         : ValueTask.FromException<int>(new SocketException((int)err));
   }
}
```

`Sender` 还需要处理缓冲区为 `ReadOnlySequence<byte>` 的情况。

```c#
//From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketSender.cs
public class Sender: AwaitableEventArgs
{
    private short _token;
    private List<ArraySegment<byte>>? _buffers;
    public ValueTask<int> SendAsync(Socket socket, in ReadOnlyMemory<byte> data)
    {
        SetBuffer(MemoryMarshal.AsMemory(data));
        if (socket.SendAsync(this))
        {
            return new ValueTask<int>(this, _token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }
    public ValueTask<int> SendAsync(Socket socket, in ReadOnlySequence<byte> data)
    {
        if (data.IsSingleSegment)
        {
            return SendAsync(socket, data.First);
        }
        _buffers ??= new List<ArraySegment<byte>>();
        foreach (var buff in data)
        {
            if (!MemoryMarshal.TryGetArray(buff, out var array))
            {
                throw new InvalidOperationException("Buffer is not backed by an array.");
            }

            _buffers.Add(array);
        }

        BufferList = _buffers;

        if (socket.SendAsync(this))
        {
            return new ValueTask<int>(this, _token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }
    public void Reset()
    {
        if (BufferList != null)
        {
            BufferList = null;

            _buffers?.Clear();
        }
        else
        {
            SetBuffer(null, 0, 0);
        }
    }
}

```

还需要一个轻量级的 `SenderPool` 实现。它由并发队列（`ConcurrentQueue`）支持，并利用 `Interlocked` 来保持更低成本的原子计数。

```c#
//From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketSenderPool.cs
public class SenderPool : IDisposable
{
    private readonly int MaxNumberOfSenders;
    private int _count;
    private readonly ConcurrentQueue<Sender> _senders = new();
    private bool _disposed = false;

    public SenderPool(int maxNumberOfSenders = 128)
    {
        MaxNumberOfSenders = maxNumberOfSenders;
    }

    public Sender Rent()
    {
        if (_senders.TryDequeue(out var sender))
        {
            Interlocked.Decrement(ref _count);
            sender.Reset();
            return sender;
        }

        return new Sender();
    }

    public void Return(Sender sender)
    {
        if (_disposed || _count >= MaxNumberOfSenders)
        {
            sender.Dispose();
        }
        else
        {
            Interlocked.Increment(ref _count);
            _senders.Enqueue(sender);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        while (_senders.TryDequeue(out var sender))
        {
            sender.Dispose();
        }
    }
}

```

现在，事情开始变得有趣起来。编写 TCP 服务器需要对流数据进行缓冲和解析，这是一项复杂的任务，可能会很乏味，而且第一次很难做对。幸运的是，System.IO.Pipelines 正是为解决这一问题而设计的。它降低了代码的复杂性，实现了流数据的高性能解析。我强烈建议阅读本文的读者同时阅读[原文](high-performance-io-in-dotnet.md)，以便更好地理解。下一段代码将利用这个库。

```c#
//From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketConnection.cs
public class Connection : IAsyncDisposable
{
    private const int MinBuffSize = 1024;
    private readonly Socket _socket;
    private readonly Receiver _receiver;
    private Sender? _sender;
    private readonly SenderPool _senderPool;
    private Task? _receiveTask;
    private Task? _sendTask;
    private readonly Pipe _transportPipe;
    private readonly Pipe _applicationPipe;
    private readonly object _shutdownLock = new object();
    private volatile bool _socketDisposed;
    public PipeWriter Output { get;}
    public PipeReader Input { get;}

    public Connection(Socket socket, SenderPool senderPool)
    {
        _socket = socket;
        _receiver = new Receiver();
        _senderPool = senderPool;
        _transportPipe = new Pipe();
        Output = _transportPipe.Writer;
        _applicationPipe = new Pipe();
        Input = _applicationPipe.Reader;
    }

    public void Start()
    {
        try
        {
            _sendTask = SendLoop();
            _receiveTask = ReceiveLoop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task SendLoop()
    {
        try
        {
            while (true)
            {
                var result = await _transportPipe.Reader.ReadAsync();
                if (result.IsCanceled)
                {
                    break;
                }
                var buff = result.Buffer;
                if (!buff.IsEmpty)
                {
                    _sender = _senderPool.Rent();
                    await _sender.SendAsync(_socket, result.Buffer);
                    _senderPool.Return(_sender);
                    _sender = null;
                }
                _transportPipe.Reader.AdvanceTo(buff.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _applicationPipe.Writer.Complete();
            Shutdown();
        }
    }
    private async Task ReceiveLoop()
    {
        try
        {
            while (true)
            {
                var buff = _applicationPipe.Writer.GetMemory(MinBuffSize);
                var bytes = await _receiver.ReceiveAsync(_socket, buff);
                if (bytes == 0)
                {
                    break;
                }
                _applicationPipe.Writer.Advance(bytes);
                var result = await _applicationPipe.Writer.FlushAsync();
                if (result.IsCanceled || result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _applicationPipe.Writer.Complete();
            Shutdown();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _transportPipe.Reader.Complete();
        _applicationPipe.Writer.Complete();
        try
        {
            if (_receiveTask != null)
            {
                await _receiveTask;
            }

            if (_sendTask != null)
            {
                await _sendTask;
            }
        }
        finally
        {
            _receiver.Dispose();
            _sender?.Dispose();
        }
    }
    public void Shutdown()
    {
        lock (_shutdownLock)
        {
            if (_socketDisposed)
            {
                return;
            }
            _socketDisposed = true;
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                _socket.Dispose();
            }
        }
    }
}
```

`Connection` 类有：

- 连接的 `Receiver`
- 对 `Socket` 和 `SenderPool` 的引用
- 一个 `Pipe` 用于传输，另一个管道用于应用程序
- 最后是两个用于发送和接收的循环。

## 我们如何发送/接收数据呢？

传输管道的 `PipeWriter` 以公共的 `Output` 属性暴露，`SendLoop` 在每次发送操作之前从池中获取一个 `Sender`，并在完成后将其返回给池。应用程序管道的 `PipeReader` 以公共的 `Input` 属性暴露，`ReceiveLoop` 从该管道获取一个缓冲区，并将数据接收到其中。然后还有一些清理代码。这又是对原始 `Kestrel` 实现的简化版本。

## TCP Echo

此时，TCP Echo 服务器只需再编写几行代码。下面只是简单地拷贝回接收到的字节：

```c#
var senderPool = new SenderPool(1024);
var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 8989));
listenSocket.Listen(128);
while (true)
{
    var socket = await listenSocket.AcceptAsync();
    var connection = new Connection(socket, senderPool);
    _ = ProcessConnection(connection);
}

static async Task ProcessConnection(Connection connection)
{
    connection.Start();
    while (true)
    {
        var result = await connection.Input.ReadAsync();
        if (result.IsCanceled)
        {
            break;
        }
        var buff = result.Buffer;
        if (!buff.IsEmpty)
        {
            if (buff.IsSingleSegment)
            {
                await connection.Output.WriteAsync(buff.First);
            }
            else
            {
                foreach (var mem in buff)
                {
                    await connection.Output.WriteAsync(mem);
                }
            }
        }
        connection.Input.AdvanceTo(buff.End, buff.End);
        if (result.IsCompleted)
        {
            break;
        }
    }
    connection.Shutdown();
    await connection.DisposeAsync();
}
```

这将作为高性能 TCP 服务器的基础，当然还需要进行更多的测量和优化。为简单起见，上述代码省略了一些更高级的实现细节。下文将概述其中一些细节。

## schedule-me-not

创建 `Pipe` 时，可以通过 `PipeOptions` 为读写器指定 `MemoryPool<byte>` 和 `PipeScheduler`，`Kestrel` 在此使用了自定义的 [PinnedBlockMemoryPool](https://github.com/dotnet/aspnetcore/blob/main/src/Shared/Buffers.MemoryPool/PinnedBlockMemoryPool.cs?ref=hackernoon.com) 以及无锁 [IOQueue](https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/IOQueue.cs?ref=hackernoon.com)。[SocketAwaitableEventArgs](https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketAwaitableEventArgs.cs?ref=hackernoon.com) 实现也支持 `PipeScheduler`，以便对 I/O 发生的位置进行细粒度控制。下面的伪代码试图简化和概述 Kestrel 如何使用调度程序。

```c#
var memoryPool = new PinnedBlockMemoryPool();
var applicationScheduler = PipeScheduler.ThreadPool;
var transportScheduler = new IOQueue();
var socketScheduler = transportScheduler;

var receiver = new SocketReceiver(socketScheduler);

var InputOptions = new PipeOptions(memoryPool, 
        applicationScheduler, transportScheduler, 
        maxReadBufferSize, maxReadBufferSize / 2, 
        useSynchronizationContext: false),

var OutputOptions = new PipeOptions(memoryPool, 
        transportScheduler, applicationScheduler, 
        maxWriteBufferSize, maxWriteBufferSize / 2, 
        useSynchronizationContext: false),

var SocketSenderPool = new SocketSenderPool(PipeScheduler.Inline),
```

从上面也可以看出，`PauseWriterThreshold` 和 `ResumeWriterThreshold` 用于流量控制。当数据处理比简单的数据回传更复杂时，读取器就会分配越来越多的内存。然而，当设置了 `PauseWriterThreshold` 时，如果管道中的数据量超过了设置值，`PipeWriter.FlushAsync` 将不会完成，直到数据量再次小于 `ResumeWriterThreshold`。

## 何去何从？

在我为这篇文章编写代码时，一些初步的基准测试表明，所提交的代码实际上比基本的 [Netty](https://netty.io/?ref=hackernoon.com) Echo 服务器要慢一些。Netty 会达到每秒 115000 次请求，而上面的代码会达到 110000 次。不过，Netty 是一个成熟的框架，所以这并不奇怪。这只能说明，在进行剖析和优化的同时，还需要一些适当的基准测试。


完整源代码可在 [GitHub 代码库](https://github.com/aykutalparslan/high-perfomance-tcp-server/?ref=hackernoon.com)中查看

# 原文链接

[Building a High Performance TCP Server in C# | HackerNoon](https://hackernoon.com/building-a-high-performance-tcp-server-in-c)

