using System;
using System.Threading;
using System.Threading.Tasks;

namespace UtilitiesForTAP {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }

        public static Task Delay(int millis) {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Timer timer = new Timer(_ => tcs.SetResult(null), null, millis, Timeout.Infinite);
            tcs.Task.ContinueWith(delegate { timer.Dispose(); });
            return tcs.Task;
        }
    }
}