using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PerFormanceComparison {
    public class AsyncAndSyncComparison {
        public void Start() {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000000; i++) {
                Empty();
            }
            sw.Stop();
            Console.WriteLine("同步方法 Empty 10000000 次调用耗时：" + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
            for (int i = 0; i < 10000000; i++) {
                EmptyAsync();
            }
            sw.Stop();
            Console.WriteLine("同步方法 EmptyAsync 10000000 次调用耗时：" + sw.ElapsedMilliseconds + "ms");
        }

        public async Task EmptyAsync() {
            // await Task.CompletedTask;
        }

        public void Empty() {

        }
    }
}