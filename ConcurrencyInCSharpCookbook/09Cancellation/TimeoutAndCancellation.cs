using System;
using System.Threading;
using System.Threading.Tasks;

namespace _09Cancellation {
    /// <summary>
    /// 超时后停止运行
    /// 监视取消标示，超时时间可以在 CancellationTokenSource 构造函数中传递超时时间
    /// </summary>
    public class TimeoutAndCancellation {
        private CancellationTokenSource _cancellationTokenSource;

        public TimeoutAndCancellation() : this(new CancellationTokenSource()) {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public TimeoutAndCancellation(CancellationTokenSource cancellationTokenSource) {
            this._cancellationTokenSource = cancellationTokenSource;
        }
        /// <summary>
        /// 方式1，通过 CancellationTokenSource 构造函数传递超时时间
        /// </summary>
        /// <returns></returns>
        public async Task IssueTimeoutAsync() {
            await Task.Delay(TimeSpan.FromSeconds(10), _cancellationTokenSource.Token);
        }
        /// <summary>
        /// 如果对已经有的 CancellationTokenSource 进行超时控制，可以对该实例启动一个超时
        /// </summary>
        /// <returns></returns>
        public async Task IssueTimeoutAsync_v2() {
            _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            await DoWork();
        }

        private async Task DoWork() {
            Console.WriteLine("开始执行...");
            await Task.Run(() => {
                Console.WriteLine("执行中...");
                Thread.Sleep(6000);
                Console.WriteLine("超时取消：" + _cancellationTokenSource.IsCancellationRequested + " Token:" + _cancellationTokenSource.Token.IsCancellationRequested.ToString());
            }, _cancellationTokenSource.Token);
        }
    }
}