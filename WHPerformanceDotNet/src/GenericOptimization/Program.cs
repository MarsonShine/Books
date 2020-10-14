using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

/// <summary>
/// 泛型的性能优化
/// </summary>
namespace GenericOptimization
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            try
            {
                Stopwatch sw = new Stopwatch();
                const int interation = 10000000;
                sw.Start();
                for (int i = 0; i < interation; i++)
                {
                    new Node();
                }
                sw.Stop();
                Console.WriteLine($"构造函数调用 耗时：{sw.Elapsed}");

                sw.Restart();
                for (int i = 0; i < interation; i++)
                {
                    Create<Node>();
                }
                sw.Stop();
                Console.WriteLine($"泛型约束`:new()`调用 耗时：{sw.Elapsed}");

                sw.Restart();
                for (int i = 0; i < interation; i++)
                {
                    NodeFactory();
                }
                sw.Stop();
                Console.WriteLine($"委托调用 耗时：{sw.Elapsed}");

                sw.Restart();
                for (int i = 0; i < interation; i++)
                {
                    FastActivator.CreateInstance<Node>();
                }
                sw.Stop();
                Console.WriteLine($"表达式树动态调用 耗时：{sw.Elapsed}");

                sw.Restart();
                for (int i = 0; i < interation; i++)
                {
                    FastActivator<Node>.Create();
                }
                sw.Stop();
                Console.WriteLine($"动态Emit调用 耗时：{sw.Elapsed}");
                
                Console.WriteLine("Node was create successfully");
                Console.ReadKey();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Failed to create a node!");
            }
            catch (TargetInvocationException e)
            {
                var edi = ExceptionDispatchInfo.Capture(e.InnerException);
                edi.Throw();
                // Required to avoid compiler error regarding unreachable code
                throw;
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to create a node!");
            }
        }

        public static T Create<T>() where T : new() => new T();

        public static Func<Node> NodeFactory => () => new Node();
    }
}
