// refference: https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#asynchronous-programming
// See https://aka.ms/new-console-template for more information
using asynclocal;
using System.Runtime.CompilerServices;

Console.WriteLine("Hello, World!");


//using (var thing = new DisposableThing())
//{
//    DisposableThing.Current = thing;
//    Dispatch();
//    DisposableThing.Current = null;
//}

// 上述示例将始终导致抛出 ObjectDisposedException。尽管 Log 方法在记录值之前会防御性地检查是否为空，但它仍持有已释放的 DisposableThing 的引用。将 AsyncLocal<DisposableThing> 设置为 null 不会影响 Log 内部的代码，这是因为在写入时执行上下文是写时复制（COW）的。这意味着今后读取的所有 DisposableThing.Current 都将为空，但不会影响之前的任何读取

// 每次给 AsyncLocal 设置新值时，执行上下文的哈希代码都是不同的。
DisposableThing.Current = new DisposableThing();
Console.WriteLine("After setting thing " + ExecutionContext.Capture()?.GetHashCode());
// 当我们设置 DisposableThing.Current = null 时，我们是在创建一个新的执行上下文，而不是更改由 Task.Run 捕捉到的执行上下文
DisposableThing.Current = null;
Console.WriteLine("After setting Current to null " + ExecutionContext.Capture()?.GetHashCode());
Console.ReadLine();

// 上面都是 AsyncLocal 持有的对象不可变的情况，如果想让 AsyncLocal 持有的对象可变
// 可让 AsyncLocal 持有一个引用对象，这样虽然每次更改值时，会创建的新的 ExecuteContext，但内部持有的对象是同一个引用
DisposableThing2.Current = new DisposableThing2();

Console.WriteLine("After setting thing " + ExecutionContext.Capture()?.GetHashCode());

DisposableThing2.Current = null;

Console.WriteLine("After setting Current to null " + ExecutionContext.Capture()?.GetHashCode());
// 要注意的是，当我们持有引用对象时，在处理异步、并发的情况下，要注意线程安全的问题

MemoryLeaskDemo.Start();

void Dispatch()
{
    // Task.Run will capture the current execution context (which means async locals are captured in the callback)
    _ = Task.Run(async () =>
    {
        // Delay for a second then log
        await Task.Delay(1000);

        Log();
    });
}

void Log()
{
    try
    {
        // Get the current value and make sure it's not null before reading the value
        var thing = DisposableThing.Current;
        if (thing is not null)
        {
            // 这里尽管校验是否为null，但是还是会报错，因为在这个异步方法中
            // 当前的AsyncLocal持有的对象已经被释放了
            Console.WriteLine($"Logging ambient value {thing.Value}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
internal class DisposableThing : IDisposable
{
    private static readonly AsyncLocal<DisposableThing?> _current = new();

    private bool _disposed;
    public static DisposableThing? Current
    {
        get => _current.Value;
        set
        {
            _current.Value = value;
        }
    }

    public int Value
    {
        get
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            return 1;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

internal class DisposableThing2 : IDisposable
{
    private static readonly AsyncLocal<StrongBox<DisposableThing2?>?> _current = new();

    private bool _disposed;
    public static DisposableThing2? Current
    {
        get => _current.Value?.Value;
        set
        {
            var box = _current.Value;
            if (box is not null)
            {
                // 更改任何被复制的执行上下文中的值
                box.Value = null;
            }
            if (value is not null)
            {
                _current.Value = new StrongBox<DisposableThing2?>(value);
            }
        }
    }

    public int Value
    {
        get
        {
            byte[] s = new byte[] { 1,2,3};
            Console.WriteLine(s);
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            return 1;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}