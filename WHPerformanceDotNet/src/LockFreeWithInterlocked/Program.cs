using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LockFreeWithInterlocked
{
    class Program
    {
        // ThreadLocal 主要作用就是让各个线程维持自己的变量
        private readonly static ThreadLocal<Random> threadLocalRand =
            new ThreadLocal<Random>(() =>
            {
                return new Random();
            });

        static void Main(string[] args)
        {
            LockFreeStack<string> lockFree = new LockFreeWithInterlocked.LockFreeStack<string>();
            lockFree.Push("marson shine");
            lockFree.Push("summer zhu");

            string name = lockFree.Pop();
            Console.WriteLine("Hello World!");

            int[] results = new int[100];
            Parallel.For(0, 5000, i =>
            {
                var randomNumber = threadLocalRand.Value.Next(100);
                Interlocked.Increment(ref results[randomNumber]);
            });

            Console.WriteLine(string.Join(' ', results.Select((p, i) => $"results[{i}] = {p}")));
            Console.ReadLine();

        }
    }
}