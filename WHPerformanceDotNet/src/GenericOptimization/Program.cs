using System;
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
                //Create<Node>();
                new Node();
                Console.WriteLine("Node was create successfully");
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
