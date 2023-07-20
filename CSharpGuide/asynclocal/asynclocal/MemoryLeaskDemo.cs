using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

/*
 本有一个单例的 NumberCache，用于存储一小时内的数字。我们有一个 ChunkyObject，它在一个字段中存储了 32K 字符串，并且有一个异步本地，因此任何运行中的代码都可以访问当前的 ChunkyObject。该对象本应在 GC 运行时被收集，但我们却通过 CancellationToken.Register 隐式地在 NumberCache 中捕获了 ChunkyObject。

与其说我们只是在缓存数字和 CancellationTokenSource，不如说我们是在隐式捕获并存储所有连接到当前执行上下文的异步本地对象一个小时！
 */
namespace asynclocal
{
    internal class MemoryLeaskDemo
    {
        public static void Start()
        {
            var cache = new NumberCache(TimeSpan.FromHours(1));

            ExecutionContext executionContext = ExecutionContext.Capture()!;
            // 模拟 10000 次并发请求
            Parallel.For(0, 10000, i =>
            {
                // 根据请求恢复初始执行的 ExecutionContext
                ExecutionContext.Restore(executionContext);

                //ChunkyObject.Current = new ChunkyObject();

                //cache.Add(i);

                SafeChunkyObject.Current = new SafeChunkyObject();
                cache.Add(i);
                // 清空大块对象，以便 GC 释放内存
                SafeChunkyObject.Current = null;
            });

            Console.WriteLine("Before GC: " + BytesAsString(GC.GetGCMemoryInfo().HeapSizeBytes));
            Console.ReadLine();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.WriteLine("After GC: " + BytesAsString(GC.GetGCMemoryInfo().HeapSizeBytes));
            // 实际上并没有被 GC
            Console.ReadLine();

            static string BytesAsString(long bytes)
            {
                string[] suffix = { "B", "KB", "MB", "GB", "TB" };
                int i;
                double doubleBytes = 0;

                for (i = 0; bytes / 1024 > 0; i++, bytes /= 1024)
                {
                    doubleBytes = bytes / 1024.0;
                }

                return string.Format("{0:0.00} {1}", doubleBytes, suffix[i]);
            }
        }
    }

    public class NumberCache
    {
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _cache = new();
        private readonly TimeSpan _timeSpan;

        public NumberCache(TimeSpan timeSpan)
        {
            _timeSpan = timeSpan;
        }

        public void Add(int key)
        {
            var cts = _cache.GetOrAdd(key, _ => new CancellationTokenSource());
            // Delete entry on expiration
            cts.Token.Register((_, _) => _cache.TryRemove(key, out _), null);
            //cts.Token.UnsafeRegister((_, _) => _cache.TryRemove(key, out _), null); // 这样就不会捕获 ChunkyObject 了。或者是用 cts.Token.Register，在模拟请求代码段的最后，设置 ChunkyObject.Current = null

            // Start count down
            cts.CancelAfter(_timeSpan);
        }
    }
    class ChunkyObject
    {
        private static readonly AsyncLocal<ChunkyObject?> _current = new();

        // Stores lots of data (but it should be gen0)
        private readonly string _data = new('A', 1024 * 32);

        // 存在内存泄漏问题
        public static ChunkyObject? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        public string Data => _data;
    }

    class SafeChunkyObject
    {
        private static readonly AsyncLocal<StrongBox<SafeChunkyObject?>?> _current = new();

        // Stores lots of data (but it should be gen0)
        private readonly string _data = new('A', 1024 * 32);

        public static SafeChunkyObject? Current
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
                    _current.Value = new StrongBox<SafeChunkyObject?>(value);
                }
            }
        }

        public string Data => _data;
    }

}
