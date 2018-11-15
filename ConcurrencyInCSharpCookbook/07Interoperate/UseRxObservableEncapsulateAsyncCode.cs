using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace _07Interoperate {
    //需要把一个异步操作与一个 observable 对象结合。
    public class UseRxObservableEncapsulateAsyncCode {
        //立即启动异步事件，不会等待订阅
        public static void UseToObservable() {
            var client = new HttpClient();
            IObservable<HttpResponseMessage> response = client.GetAsync("http://www.baidu.com")
                .ToObservable();
        }
        //立即启动异步事件，不会等待订阅
        public static void UseStartAsync() {
            var client = new HttpClient();
            IObservable<HttpResponseMessage> response = Observable.StartAsync(token => {
                Console.WriteLine("StartAsync 立即调用");
                return client.GetAsync("http://tms.yhglobal.com/yhweb/api/order/OrderReceiptImageHandler.ashx?custOrdNo=DB09122200", token);
            });
        }
        //等待订阅事件完成之后，启动全新的异步操作
        public static IObservable<HttpResponseMessage> UseFromAsync() {
            var client = new HttpClient();
            IObservable<HttpResponseMessage> response = Observable.FromAsync(token => {
                Console.WriteLine("FromAsync 需订阅后立即调用");
                return client.GetAsync("http://tms.yhglobal.com/yhweb/api/order/OrderReceiptImageHandler.ashx?custOrdNo=DB09122200", token);
            });
            return response;
        }
        //在源事件流中，每到达一个事件，就启动一个异步操作
        //下面的代码是 每达到一个事件，就发出一个请求
        public static async Task UseSelectMany() {
            IObservable<string> urls = Task.FromResult("http://tms.yhglobal.com/yhweb/api/order/OrderReceiptImageHandler.ashx?custOrdNo=DB09122200").ToObservable();
            var client = new HttpClient();
            IObservable<HttpResponseMessage> response = urls.SelectMany((url, token) => client.GetAsync(url, token));
            var ret = await (await response).Content.ReadAsStringAsync();
            Console.WriteLine(ret);
        }
    }

    public class MyObserver : IObserver<string>, IDisposable {
        public void Dispose() {
            Console.WriteLine("Disposing");
        }

        public void OnCompleted() {
            Console.WriteLine("Completed...");
        }

        public void OnError(Exception error) {
            Console.WriteLine("Exception:" + error.Message);
        }

        public void OnNext(string value) {
            Console.WriteLine("value = " + value);
        }
    }
}