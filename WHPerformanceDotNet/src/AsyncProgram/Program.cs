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