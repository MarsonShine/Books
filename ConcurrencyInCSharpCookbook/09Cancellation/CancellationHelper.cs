using System;
using System.Threading;
using System.Threading.Tasks;

namespace _09Cancellation {
    public class CancellationHelper {
        /// <summary>
        /// 通过共享的取消变量（CancellationTokenSource.Token）来控制任务是否取消
        /// </summary>
        private CancellationTokenSource _cts;
        public async Task IssueTask() {
            _cts = new CancellationTokenSource();
            try {
                Console.WriteLine("正在执行中...");
                await Task.Delay(TimeSpan.FromSeconds(3), _cts.Token);
                Console.WriteLine("任务完成...");
            } catch (OperationCanceledException) {
                Console.WriteLine("操作取消...");
            } catch (Exception) {
                Console.WriteLine("任务中出现问题");
                throw;
            }
        }

        public void Cancel() {
            if (_cts == null) return;
            _cts.Cancel();
        }
    }
}