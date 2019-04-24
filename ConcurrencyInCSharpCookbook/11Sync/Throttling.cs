using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace _11Sync {
    /// <summary>
    /// 限流
    /// 因为并发程度太高，生产者速度远大于消费者。会导致大量的数据占据内存，因此需要对入口限流来避免占用太多内存
    /// </summary>
    public class Throttling {
        public IPropagatorBlock<int, int> DataflowMultiplyBy2() {
            var options = new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = 10
            };
            return new TransformBlock<int, int>(data => data * 2, options);
        }

        public IEnumerable<int> ParallelMultiplyBy2(IEnumerable<int> source) {
            return source.AsParallel()
                .WithDegreeOfParallelism(10)
                .Select(p => p * 2);
        }

        public void ParallelMultiplyBy2(IEnumerable<int> source, float degrees) {
            var options = new ParallelOptions {
                MaxDegreeOfParallelism = 10
            };
            Parallel.ForEach(source, options, item => Console.WriteLine(item));
        }

        public async Task<string[]> DownloadUrlsAsync(IEnumerable<string> urls) {
            var httpClient = new HttpClient();
            var semaphore = new SemaphoreSlim(10);
            var tasks = urls.Select(async url => {
                await semaphore.WaitAsync();
                try {
                    return await httpClient.GetStringAsync(url);
                } finally {
                    semaphore.Release();
                }
            }).ToArray();
            return await Task.WhenAll(tasks);
        }
    }
}