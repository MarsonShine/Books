using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpGuide
{
    class AsyncLocalViaThreadLocal
    {
        static AsyncLocal<string> _asyncLocalString = new AsyncLocal<string>();
        static ThreadLocal<string> _threadLocalString = new ThreadLocal<string>();

        public static async Task AsyncMethodA()
        {
            _asyncLocalString.Value = "Value 1";
            _threadLocalString.Value = "Value 1";
            var t1 = AsyncMethodB("Value 1");

            _asyncLocalString.Value = "Value 2";
            _threadLocalString.Value = "Value 2";
            var t2 = AsyncMethodB("Value 2");

            await t1;
            await t2;
        }

        public static async Task AsyncMethodB(string expectedValue)
        {
            Console.WriteLine("Entering AsyncMethodB.");
            Console.WriteLine("   Expected '{0}', AsyncLocal value is '{1}', ThreadLocal value is '{2}'",
                              expectedValue, _asyncLocalString.Value, _threadLocalString.Value);
            await Task.Delay(100);
            Console.WriteLine("Exiting AsyncMethodB.");
            Console.WriteLine("   Expected '{0}', got '{1}', ThreadLocal value is '{2}'",
                              expectedValue, _asyncLocalString.Value, _threadLocalString.Value);
        }
    }
}
