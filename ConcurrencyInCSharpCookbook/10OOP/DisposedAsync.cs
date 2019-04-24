using System;
using System.Threading;
using System.Threading.Tasks;

namespace _10OOP {
    /// <summary>
    /// 异步销毁，生成一个 CanncellationTokenResource，然后调用 Disposed
    /// </summary>
    public partial class DisposedAsync : IDisposable {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public async Task<int> GetDataAsync() {
            await Task.Delay(TimeSpan.FromSeconds(2), _cts.Token);
            return 11;
        }

        public void Dispose() {
            _cts.Cancel();
        }

        //这样就能使用 using 代码块
        //但是一般这种销毁异步方式，还要加个判断去检查对象有没有被销毁，所以本身要支持 CancellationToken
        public async Task<int> GetDataAsync(CancellationToken token) {
            using(var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                await Task.Delay(TimeSpan.FromSeconds(2), combinedCts.Token);
                return 11;
            }
        }
    }
}