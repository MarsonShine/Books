using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace _13PracticalSkills {
    /// <summary>
    /// Rx延迟求值
    /// 要在每次被订阅就创建一个新的 observable 对象。例如让每个订阅代表一个不同的 web 服务请求。
    /// 解决方法：Rx 库有一个 Observable.Defer，每次observable 对象被订阅时，就会执行一个委托（创建一个obserable对象的工厂）
    /// </summary>
    public class RxLazyGetValue {
        public void Main() {
            var invokeServerObservable = Observable.Defer(
                () => GetValueAsync().ToObservable()
            );
            //每次订阅observable对象，都会使用defer调用异步方法
            invokeServerObservable.Subscribe(_ => { });
            invokeServerObservable.Subscribe(_ => { });

            Console.ReadLine();
        }

        private async Task<int> GetValueAsync() {
            Console.WriteLine("Calling Server...");
            await Task.Delay(TimeSpan.FromSeconds(3));
            Console.WriteLine("Returning result...");
            return 13;
        }
    }
}