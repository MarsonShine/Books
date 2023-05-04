using System.Diagnostics;

namespace EventSourceDemo
{
    public class DiagnosticSourceQuickStart
    {
        class HTTPClient
        {
            private static DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
            public byte[] SendWebRequest(string url)
            {
                if (httpLogger.IsEnabled("RequestStart"))
                {
                    httpLogger.Write("RequestStart", new { Url = url });
                }
                byte[] reply = Array.Empty<byte>();
                return reply;
            }
        }
        class Observer<T> : IObserver<T>
        {
            private Action<T> _onNext;
            private Action _onCompleted;
            public Observer(Action<T> onNext, Action onCompleted)
            {
                _onNext = onNext ?? new Action<T>(_ => { });
                _onCompleted = onCompleted ?? new Action(() => { });
            }
            public void OnCompleted()
            {
                _onCompleted();
            }
            public void OnError(Exception error)
            {
                Console.WriteLine("OnError");
            }
            public void OnNext(T value)
            {
                _onNext(value);
            }
        }
        class MyListner
        {
            IDisposable networkSubscribtion;
            IDisposable listenerSubscribtion;
            private readonly object allListeners = new object();
            public void Listening()
            {
                void whenHeard(KeyValuePair<string, object> data)
                {
                    Console.WriteLine($"Data received: {data.Key}: {data.Value}");
                }
                void onNewListner(DiagnosticListener data)
                {
                    Console.WriteLine($"New Listner discoverd: {data.Name}");
                    // 订阅指定的 Listener
                    if (data.Name == "System.Net.Http")
                    {
                        lock (allListeners)
                        {
                            networkSubscribtion?.Dispose();
                            IObserver<KeyValuePair<string, object>> observer = new Observer<KeyValuePair<string, object>>(whenHeard, null!);
                            networkSubscribtion = data.Subscribe(observer!);
                        }
                    }
                }
                // 订阅发现的所有的 Listener
                IObserver<DiagnosticListener> observer = new Observer<DiagnosticListener>(onNewListner, null!);
                // 当一个 listener 被创建后，调用委托的 onNext 函数。
                listenerSubscribtion = DiagnosticListener.AllListeners.Subscribe(observer!);
            }
            // 通常情况下，你让 listenerSubscription 订阅永远处于活动状态。然而，当你不再希望你的回调被调用时，你可以调用 listenerSubscription.Dispose() 来取消你对 IObservable 的订阅。
        }

        public static void Start()
        {
            var listner = new MyListner();
            listner.Listening();
            var client = new HTTPClient();
            client.SendWebRequest("https://www.baidu.com");
        }
    }
}
