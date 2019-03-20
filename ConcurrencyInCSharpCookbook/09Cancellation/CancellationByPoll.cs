using System;
using System.Threading;
using System.Threading.Tasks;

namespace _09Cancellation {
    /// <summary>
    /// 通过轮询取消状态来控制在任务中处于循环逻辑部分
    /// 通过 token.ThrowIfCancellationRequested()
    /// </summary>
    public class CancellationByPoll {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public CancellationByPoll() : this(new CancellationTokenSource()) {

        }

        public CancellationByPoll(CancellationTokenSource cancellationTokenSource) {
            this._cancellationTokenSource = cancellationTokenSource;
        }

        public void Execute() {
            var token = _cancellationTokenSource.Token;
            Task.Run(() => {
                Console.WriteLine("执行中...");
                Thread.Sleep(3000);
                //检查是否取消，如果取消就显示报错
                try {
                    token.ThrowIfCancellationRequested();
                } catch (System.OperationCanceledException) {
                    Console.WriteLine("任务取消...");
                }
            }, token);
        }
        public void Cancel() {
            _cancellationTokenSource.Cancel();
        }
    }
}