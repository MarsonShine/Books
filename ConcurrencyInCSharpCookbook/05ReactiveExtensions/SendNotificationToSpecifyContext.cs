using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace _05ReactiveExtensions {
    public class SendNotificationToSpecifyContext {
        public static void Startup() {
            Console.WriteLine("Current Thread ID = " + Environment.CurrentManagedThreadId);
            Observable.Interval(TimeSpan.FromSeconds(0.2))
                .Subscribe(x => { Thread.Sleep(TimeSpan.FromSeconds(1)); Console.WriteLine("Interval " + x + " on thread " + Environment.CurrentManagedThreadId); });
        }
    }
}