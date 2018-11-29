using System;
using System.Linq;
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
    }
}