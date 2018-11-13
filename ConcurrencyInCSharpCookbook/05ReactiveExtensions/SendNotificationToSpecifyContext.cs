using System;
using System.Diagnostics;
using System.IO;
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
        /// <summary>
        /// ObServe 可以在必要时候离开 UI 线程，转到线程池线程，然后再把结果返回给 UI 线程
        /// Observe 是把通知转移到 Rx 调度器上，然后由调度器决定在那个线程上挂载队列
        /// </summary>
        public static void ObserveOn() {
            var currentContext = SynchronizationContext.Current;
            var watcher = new FileSystemWatcher("D:\\MS\\Project\\dotnet\\Books\\ConcurrencyInCSharpCookbook\\05ReactiveExtensions", "README.md") { EnableRaisingEvents = true };
            var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                    handler => (e, a) => OnRenamed(e, a),
                    addHandler : handler => watcher.Renamed += handler,
                    removeHandler : handler => watcher.Renamed -= handler
                )
                // .ObserveOn(Scheduler.Default) //默认调度器就是线程池线程
                // .Select(b => {
                //     Thread.Sleep(TimeSpan.FromSeconds(10));
                //     return new {
                //         Name = "修改后的文件名=" + b.EventArgs.Name,
                //             OldName = "修改前的文件名=" + b.EventArgs.OldName
                //     };
                // })
                // .ObserveOn(currentContext)
                .Subscribe(x => Console.WriteLine("Result " + x.EventArgs.Name + Environment.NewLine + x.EventArgs.OldName + " on thread " + Environment.CurrentManagedThreadId));
        }

        private static void OnRenamed(object source, RenamedEventArgs e) {
            Console.WriteLine("文件重命名事件处理逻辑{0}  {1}  上个名字 {2} 修改后的文件名 {3}", e.ChangeType, e.FullPath, e.OldName, e.Name);
        }
    }
}