using System;
using System.Collections.Generic;
using System.Text;

namespace Chapter06
{
    /// <summary>
    /// 1.在调用MoveNext方法之前，CreateEnumerable就不会执行任何代码
    /// 2.在调用MoveNext时就完成了，获取Current的值不会执行任何代码
    /// </summary>
    public class IteratorWorkflow
    {
        static readonly string Padding = new string(' ', 30);
        public static IEnumerable<int> CreateEnumerable()
        {
            Console.WriteLine("{0}Start of CreateEnumberable()", Padding);
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("{0} About to yield {1}", Padding, i);
                yield return i;
                Console.WriteLine("{0} After yield", Padding);
            }
            Console.WriteLine("{0} Yielding final value", Padding);
            yield return -1;
            Console.WriteLine("{0} End of CreateEnumerable()", Padding);
        }
    }
}
