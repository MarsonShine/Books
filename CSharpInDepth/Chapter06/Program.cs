using System;
using System.Collections.Generic;

namespace Chapter06
{
    class Program
    {
        static void Main(string[] args)
        {
            IteratorWorkflowMain();
        }

        private static void IteratorWorkflowMain()
        {
            IEnumerable<int> iterable = IteratorWorkflow.CreateEnumerable();
            IEnumerator<int> iterator = iterable.GetEnumerator();
            Console.WriteLine("Starting to iterate");
            while (true)
            {
                Console.WriteLine("Calling MoveNext()...");
                bool result = iterator.MoveNext();
                Console.WriteLine("... MoveNext result={0}", result);
                if (!result)
                {
                    break;
                }
                Console.WriteLine("Fetching Current ...");
                Console.WriteLine("... Current result={0}", iterator.Current);
            }
        }
    }
}
