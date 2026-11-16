using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

// 这些示例按“正常 C# 代码 → 观察运行时行为”的顺序组织，不依赖私有运行时 API。
public static class RuntimeDemos
{
    private static readonly AsyncLocal<string?> RequestId = new();

    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== Runtime Async、JIT、SIMD 和 GC ===");
        Console.WriteLine($"运行时: {RuntimeInformation.FrameworkDescription}; 架构: {RuntimeInformation.ProcessArchitecture}");
        await RuntimeAsyncAsync();
        JitAndBounds();
        VectorAndTail();
        GarbageCollector();
    }

    private static async Task RuntimeAsyncAsync()
    {
        Console.WriteLine("\n[Runtime Async] 同步完成、真正挂起、上下文与异常");
        var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<int> cached = CalculateTotalAsync(cacheHit: true, gate.Task, deliveryFee: 10);
        Console.WriteLine($"缓存命中：调用返回时已完成={cached.IsCompleted}，结果={await cached}");

        RequestId.Value = "request-42";
        Task<int> pending = CalculateTotalAsync(cacheHit: false, gate.Task, deliveryFee: 10);
        Console.WriteLine($"读取未完成：调用返回时已完成={pending.IsCompleted}");
        gate.SetResult(120);
        Console.WriteLine($"恢复后的总价={await pending}；调用方 RequestId={RequestId.Value}");
        RequestId.Value = null;

        var failure = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> failing = CalculateTotalAsync(cacheHit: false, failure.Task, deliveryFee: 10);
        failure.SetException(new FormatException("模拟价格格式错误"));
        try { await failing; }
        catch (FormatException ex) { Console.WriteLine($"调用方通过 await 收到异常：{ex.Message}"); }

        bool hasCompilerStateMachine = typeof(RuntimeDemos)
            .GetMethod(nameof(CalculateTotalAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .IsDefined(typeof(AsyncStateMachineAttribute), inherit: false);
        Console.WriteLine($"示例方法带 AsyncStateMachineAttribute：{hasCompilerStateMachine}");
        Console.WriteLine("此输出用于对照编译产物；性能与中间 Task 分配需要单独测量。");
    }

    private static async Task<int> LoadPriceAsync(bool cacheHit, Task<int> source)
    {
        if (cacheHit) return 120;
        int price = await source.ConfigureAwait(false);
        Console.WriteLine($"恢复后读取 AsyncLocal：{RequestId.Value}");
        return price;
    }

    private static async Task<int> AddDeliveryFeeAsync(bool cacheHit, Task<int> source, int deliveryFee)
    {
        int price = await LoadPriceAsync(cacheHit, source);
        return price + deliveryFee;
    }

    // 防止这一层被内联，方便用反汇编或调试器观察调用链。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> CalculateTotalAsync(bool cacheHit, Task<int> source, int deliveryFee)
    {
        int amount = await AddDeliveryFeeAsync(cacheHit, source, deliveryFee);
        return amount - 20;
    }

    private static void JitAndBounds()
    {
        Console.WriteLine("\n[JIT] 看得懂的代码与边界");
        Console.WriteLine($"空 span 首元素检查：{StartsWith([42, 10], 42)}；空输入：{StartsWith([], 42)}");
        Console.WriteLine($"范围已验证的 checked 转换：{ToByte(80)}；switch：{IsSmall(3)}");
        Console.WriteLine($"接口调用：{ApplyRule(new Discount(), 120)}");
        Console.WriteLine("去虚拟化、边界检查消除和常量折叠需查看 JIT 代码，不能从返回值直接证明。");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool StartsWith(ReadOnlySpan<int> values, int expected) => !values.IsEmpty && values[0] == expected;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte ToByte(uint value) => value <= 80 ? checked((byte)value) : throw new ArgumentOutOfRangeException(nameof(value));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsSmall(int value) => value is 0 or 1 or 2 or 3 or 4;

    private interface IRule { int Apply(int value); }
    private sealed class Discount : IRule { public int Apply(int value) => value - 20; }
    private static int ApplyRule(IRule rule, int value) => rule.Apply(value);

    private static void VectorAndTail()
    {
        Console.WriteLine("\n[SIMD] .NET 11 向量组合与 130 项的尾部处理");
        Vector128<int> sequence = Vector128.CreateGeometricSequence(1, 2);
        var (lower, upper) = Vector128.Zip(Vector128.Create(1, 2, 3, 4), Vector128.Create(10, 20, 30, 40));
        Console.WriteLine($"等比序列：{sequence}；交错后 lower={lower}、upper={upper}");
        var (originalLeft, originalRight) = Vector128.Unzip(lower, upper);
        Vector128<int> reversed = Vector128.Reverse(originalLeft);
        Vector128<int> concatenated = Vector128.ConcatLowerLower(originalLeft, originalRight);
        Console.WriteLine($"拆回：{originalLeft} 和 {originalRight}；反转={reversed}；拼接两个低半部={concatenated}");
        Console.WriteLine($"Vector128<int>.Count={Vector128<int>.Count}；硬件加速={Vector128.IsHardwareAccelerated}");

        int[] left = Enumerable.Range(0, 130).ToArray();
        int[] right = Enumerable.Repeat(10, 130).ToArray();
        int[] destination = new int[left.Length];
        int i = 0;
        int width = Vector128<int>.Count;
        // 只加载完整向量；末尾不足一组的数据留给后面的循环。
        for (; i <= left.Length - width; i += width)
        {
            Vector128<int> a = Vector128.Create(left.AsSpan(i));
            Vector128<int> b = Vector128.Create(right.AsSpan(i));
            (a + b).CopyTo(destination.AsSpan(i));
        }
        int tailStart = i;
        for (; i < left.Length; i++) destination[i] = left[i] + right[i];
        Console.WriteLine($"向量处理 [0, {tailStart})，标量处理 [{tailStart}, {left.Length})；最后两项={destination[^2]}, {destination[^1]}");

        // 新数组不与任何输入重叠，因此重复写入同一结果是安全的。
        int[] overlapResult = AddWithOverlappingTail(left, right);
        Console.WriteLine($"重叠向量收尾从下标 {left.Length - width} 开始；两种结果一致={destination.AsSpan().SequenceEqual(overlapResult)}");
        Console.WriteLine("重叠收尾会重复计算；累加总和、原地加法不能照搬。");
    }

    private static int[] AddWithOverlappingTail(int[] left, int[] right)
    {
        if (left.Length != right.Length) throw new ArgumentException("两个输入长度必须相同");
        int[] output = new int[left.Length];
        int width = Vector128<int>.Count;
        int index = 0;
        for (; index <= left.Length - width; index += width)
        {
            Vector128<int> a = Vector128.Create(left.AsSpan(index));
            Vector128<int> b = Vector128.Create(right.AsSpan(index));
            (a + b).CopyTo(output.AsSpan(index));
        }

        if (index < left.Length && left.Length >= width)
        {
            // 最后一组贴着数组末尾：130 个 int 时读取下标 126、127、128、129。
            int start = left.Length - width;
            Vector128<int> a = Vector128.Create(left.AsSpan(start));
            Vector128<int> b = Vector128.Create(right.AsSpan(start));
            (a + b).CopyTo(output.AsSpan(start));
        }
        else
        {
            // 长度不足一个向量（或刚好没有尾部），无法从 length - width 读取。
            for (; index < left.Length; index++) output[index] = left[index] + right[index];
        }
        return output;
    }

    private static void GarbageCollector()
    {
        Console.WriteLine("\n[GC] 引用存活与收集信息");
        var cache = new Dictionary<string, object>();
        object item = new();
        cache["item"] = item;
        Console.WriteLine($"缓存保留对象：{ReferenceEquals(cache["item"], item)}；当前代数：{GC.GetGeneration(item)}");
        Console.WriteLine($"进程 GC 回收次数：Gen0={GC.CollectionCount(0)}，Gen1={GC.CollectionCount(1)}，Gen2={GC.CollectionCount(2)}");
        Console.WriteLine("分代、写屏障与 dependent handle 是运行时机制；本输出不证明具体优化已发生。");
        GC.KeepAlive(cache);
    }
}
