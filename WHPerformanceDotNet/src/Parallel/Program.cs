using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelProgram {
    class Program {
        static void Main(string[] args) {
            long sum = 0;
            Parallel.For(0, 1000, (i) => {
                Interlocked.Add(ref sum, (long) Math.Sqrt(i));
            });

            var urls = new List<string> {
                @"http://www.microsoft.com",
                @"http://www.bing.com",
                @"http://msdn.microsoft.com"
            };
            var results = new ConcurrentDictionary<string, string>();
            var client = new WebClient();
            Parallel.ForEach(urls, url => results[url] = client.DownloadString(url));

            // 需要中断循环执行，可以传对象 ParallelLoopState
            Parallel.ForEach(urls, (url, loopState) => {
                if (url.Contains("bing")) {
                    // 调用 Break 中断当前请求之后的所有请求
                    loopState.Break();
                    // 调用 Stop
                    // loopState.Stop();
                }
            });

            // 并行分区
            Stopwatch watch = new Stopwatch();
            const int MaxValue = 100000000;
            sum = 0;
            // 普通 For 并行循环
            Parallel.For(0, MaxValue, (i) => Interlocked.Add(ref sum, (long) Math.Sqrt(i)));
            watch.Stop();
            Console.WriteLine($"Parallel.For: {watch.Elapsed}");

            // 分区 For 并行循环
            var partitioner = (Partitioner.Create(0, MaxValue));
            watch.Restart();
            sum = 0;
            Parallel.ForEach(partitioner, (range) => {
                long partialSum = 0;
                for (var i = range.Item1; i < range.Item2; i++) {
                    partialSum += (long) Math.Sqrt(i);
                }
                Interlocked.Add(ref sum, partialSum);
            });
            watch.Stop();
            Console.WriteLine($"分区过后的 并寻 For: {watch.Elapsed}");
        }
    }
}