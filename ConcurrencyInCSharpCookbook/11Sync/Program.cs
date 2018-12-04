using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Nito.AsyncEx;

/// <summary>
/// 同步，当在并发情况是，一方在访问数据，另一方企图修改数据时，防止数据不同步，所以要用到同步。
/// </summary>
namespace _11Sync {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            // AsyncContext.Run(() => MeMethodAsync());

            // AsyncContext.Run(async() => {
            //     var ret = await ModifyValueCurrentlyAsync();
            //     Console.WriteLine(ret);
            // });

            BlockSemaphore blockSemaphore = new BlockSemaphore();
            blockSemaphore.UseAutoResetEvent();
            Console.ReadLine();
        }

        private static async Task MeMethodAsync() {
            int val = 10;
            await Task.Delay(TimeSpan.FromSeconds(1));
            val = val + 1;
            await Task.Delay(TimeSpan.FromSeconds(1));
            val = val - 1;
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine(val);
        }
        async static Task ModifyValueAsync(ShareData data) {
            await Task.Delay(TimeSpan.FromSeconds(1));
            data.Value++;
        }
        async static Task<int> ModifyValueCurrentlyAsync() {
            var data = new ShareData();
            // 启动三个并发的修改过程。
            var task1 = ModifyValueAsync(data);
            var task2 = ModifyValueAsync(data);
            var task3 = ModifyValueAsync(data);
            await Task.WhenAll(task1, task2, task3);
            return data.Value;
        }

        async Task<bool> PlayWithStackAsync() {
            var stack = ImmutableStack<int>.Empty;
            var task1 = Task.Run(() => { stack = stack.Push(3); });
            var task2 = Task.Run(() => { stack = stack.Push(5); });
            var task3 = Task.Run(() => { stack = stack.Push(7); });
            await Task.WhenAll(task1, task2, task3);
            return stack.IsEmpty;
        }
        class ShareData {
            public int Value { get; set; }
        }
    }
}