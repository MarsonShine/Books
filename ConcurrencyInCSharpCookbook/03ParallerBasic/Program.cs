using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
/// <summary>
/// 并行开发基础
/// </summary>
namespace _03ParallerBasic {
    class Program {
        static void Main (string[] args) {
            Console.WriteLine ("Hello World!");
            ParentTask ();
        }

        static int ParallelSum (IEnumerable<int> values) {
            object mutex = new object ();
            int result = 0;
            Parallel.ForEach (source: values, localInit: () => 0, body: (item, state, localValue) => localValue + item,
                localFinally: (localValue) => {
                    lock (mutex) {
                        result += localValue;
                    }
                }
            );
            return result;
        }

        static int ParallelSumByLinq (IEnumerable<int> values) {
            return values.AsParallel ().Sum ();
        }
        static int ParallelSumByAggregate (IEnumerable<int> values) {
            return values.AsParallel ().Aggregate (
                seed: 0,
                func: (sum, item) => sum + item
            );
        }

        //调用一批方法，这些方法大多都是独立的
        static void ProcessArray (double[] array) {
            Parallel.Invoke (
                () => ProcessPartialArray (array, 0, array.Length / 2),
                () => ProcessPartialArray (array, array.Length / 2, array.Length)
            );
        }

        private static void ProcessPartialArray (double[] array, int begin, int end) {
            //密集型计算
        }
        //无法确定调用的数量，在运行时才确定执行的内容，就需要用到委托
        public static void ProcessArrayByDelegate (Action action) {
            var array = Enumerable.Repeat (action, 20).ToArray ();
            Parallel.Invoke (array);
        }
        //取消任务
        public void DoAction20Times (Action action, CancellationToken token) {
            Action[] actions = Enumerable.Repeat (action, 20).ToArray ();
            Parallel.Invoke (new ParallelOptions { CancellationToken = token }, actions);
        }

        //Task continue taskcreateoption.attachedtoparent
        static void ParentTask () {
            var parent = Task.Factory.StartNew (
                () => {
                    ChildrenTask ();
                    Task.Delay (3000).Wait ();
                    Console.WriteLine ("父线程...");
                },
                cancellationToken : CancellationToken.None,
                creationOptions : TaskCreationOptions.None,
                scheduler : TaskScheduler.Default
            );
            parent.Wait ();
            Console.ReadLine ();
        }
        static void ChildrenTask () {
            var t1 = Task.Factory.StartNew (
                () => {
                    Task.Delay (2000).Wait ();
                    Console.WriteLine ("子线程1...");
                },
                cancellationToken : CancellationToken.None,
                creationOptions : TaskCreationOptions.AttachedToParent,
                scheduler : TaskScheduler.Default
            );
            var t2 = Task.Factory.StartNew (
                () => {
                    Task.Delay (4000).Wait ();
                    Console.WriteLine ("子线程2...");
                },
                CancellationToken.None,
                TaskCreationOptions.AttachedToParent,
                TaskScheduler.Default
            );
            Task.WaitAll (new Task[2] { t1, t2 });
        }
    }
}