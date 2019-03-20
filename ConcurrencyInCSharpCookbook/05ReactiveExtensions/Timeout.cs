using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace _05ReactiveExtensions {
    //如果某事件在规定事件内没有作出相应（比如 http 请求超时），app 也要继续做出其他相应
    //timeout：一旦新的事件到达，就重置超时时间。如果超过规定的事件，Timeout 就结束事件流，产生一个 TimeoutException 的 OnError 通知
    public class Timeout {
        public static void UseTimeout() {
            var client = new HttpClient();
            client.GetStringAsync("http://www.baidu.com")
                .ToObservable()
                .Timeout(TimeSpan.FromSeconds(1))
                .Subscribe(
                    x => Console.WriteLine(DateTime.Now.Second + ": Saw " + x.Length),
                    onError : ex => Console.WriteLine(ex.Message));
        }
    }
}