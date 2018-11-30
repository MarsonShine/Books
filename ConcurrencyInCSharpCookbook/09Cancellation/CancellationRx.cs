using System;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace _09Cancellation {
    /// <summary>
    /// 取消响应式代码
    /// 可以订阅，取消订阅
    /// </summary>
    public class CancellationRx {
        private const string m_className = nameof(CancellationRx);
        private IDisposable _subscrption;
        //方式1 通过订阅之后实现了 Idisposed 接口，所以取消订阅 只需要释放接口即可
        public void StartSubscribe() {
            _subscrption = Observable.Range(1, 10)
                .Select(i => i * 2)
                .Subscribe(next => {
                    Console.WriteLine("Next...");
                });
        }

        public void Cancel() {
            if (_subscrption != null)
                _subscrption.Dispose();
        }

        //方式2 通过传递 CancellationTokenSource / CancellationToken
        //用响应式操作执行所有对象，最后调用 ToTask 来转换可以 await 的 Task 对象。
        public async Task StartSubscribe(CancellationToken token) {
            IObservable<int> observable = Observable.Range(1, 10);
            int lastElement = await observable.TakeLast(10).ToTask(token);
            //或者
            int last = await observable.ToTask(token);
        }

        //注入取消请求
        public async Task<HttpResponseMessage> GetWithTimeoutAsync(string url, CancellationToken cancellationToken) {
            var client = new HttpClient();
            using(var cts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken)) {
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                var combinedToken = cts.Token;
                return await client.GetAsync(url, combinedToken);
            }
        }

        //取消回调
        public async Task CancellationCallback(CancellationToken token) {
            token.Register(AfterCancelCallback);
            await Task.Run(() => {
                Console.WriteLine(m_className + " 执行中...");
                Thread.Sleep(3000);
            }, cancellationToken : token);
            Console.WriteLine(m_className +
                " 完成...");
        }

        public void Cancel(CancellationTokenSource cts) {
            Console.WriteLine(m_className + " 取消中...");
            Thread.Sleep(1000);
            cts.Cancel();
        }

        private void AfterCancelCallback() {
            Console.WriteLine(m_className + " 任务已取消 回调函数已调用 ");
        }
    }
}