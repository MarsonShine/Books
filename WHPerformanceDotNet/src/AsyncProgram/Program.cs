using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncProgram {
    class Program {
        static Stopwatch watch = new Stopwatch();
        static int peddingTasks;
        static void Main(string[] args) {
            const int MaxValue = 100000000;

            watch.Restart();
            int numTasks = Environment.ProcessorCount;
            peddingTasks = numTasks;
            int preThreadCount = MaxValue / numTasks;
            int perThreadLeftover = MaxValue % numTasks;

            var tasks = new Task<long>[numTasks];

            for (int i = 0; i < numTasks; i++) {
                int start = i * preThreadCount;
                int end = (i + 1) * preThreadCount;
                if (i == numTasks - 1) {
                    end += perThreadLeftover;
                }
                tasks[i] = Task<long>.Run(() => {
                    long threadSum = 0;
                    for (int j = 0; j <= end; j++) {
                        threadSum += (long) Math.Sqrt(j);
                    }
                    return threadSum;
                });
                tasks[i].ContinueWith(OnTaskEnd);
            }

            // cancel
            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task task = Task.Run(() => {
                while (true) {
                    // 执行其他逻辑
                    if (token.IsCancellationRequested) {
                        Console.WriteLine("任务取消");
                        return;
                    }
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
            }, token);
            Console.WriteLine("按任意键退出");
            Console.ReadKey();
            tokenSource.Cancel();
            task.Wait();
            Console.WriteLine("任务完成");
        }

        private static void OnTaskEnd(Task<long> task) {
            Console.WriteLine($"Thread sum: {task.Result}");
            if (Interlocked.Decrement(ref peddingTasks) == 0) {
                watch.Stop();
                Console.WriteLine($"Tasks: {watch.Elapsed}");
            }
        }
    }
}