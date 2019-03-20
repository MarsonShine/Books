using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace _05ReactiveExtensions {
    public class Basic {
        public static void Startup() {
            Observable.Range(1, 5)
                .Subscribe(x => Console.WriteLine(x));

            foreach (var x in Enumerable.Range(1, 5)) {
                Console.WriteLine(x);
            }
        }

        public static void Startup2() {
            Observable.Range(1, 10)
                .Subscribe(
                    onNext: x => Console.WriteLine(x + " 下一个"),
                    onError: e => { throw e; },
                    onCompleted: () => Console.WriteLine("已完成")
                );

            foreach (var item in Enumerable.Range(1, 5)) {
                if (item == 4) throw new ArgumentException("异常");
                Console.WriteLine("遍历中:" + item);
            }
        }

        public static void FileSystemWatcher() {
            var watcher = new FileSystemWatcher("D:\\MS\\Project\\dotnet\\Books\\ConcurrencyInCSharpCookbook\\05ReactiveExtensions", "README.md") { EnableRaisingEvents = true };
            watcher.IncludeSubdirectories = true;
            var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    handler => (e, a) => OnChanged(e, a),
                    addHandler : handler => watcher.Changed += handler,
                    removeHandler : handler => watcher.Changed -= handler
                )
                .Throttle(TimeSpan.FromSeconds(1));
            changed.ForEachAsync(onNext: e => OnChanged(e.Sender, e.EventArgs));

            // var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
            //         handler => (e, a) => OnRenamed(e, a),
            //         addHandler : handler => watcher.Renamed += handler,
            //         removeHandler : handler => watcher.Renamed -= handler
            //     );
            // renamed.ForEachAsync(onNext: e => OnRenamed(e.Sender, e.EventArgs));
            // Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private static void OnChanged(object source, FileSystemEventArgs e) {
            Console.WriteLine("文件改变事件处理逻辑{0}  {1}  {2}", e.ChangeType, e.FullPath, e.Name);
        }

        private static void OnRenamed(object source, RenamedEventArgs e) {
            Console.WriteLine("文件重命名事件处理逻辑{0}  {1}  {2}", e.ChangeType, e.FullPath, e.Name);
        }
    }
}