using System;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;

namespace _07Interoperate {
    //TPL 数据流针对异步和并行混合编程
    //Rx 针对响应式编程
    public class UseRxObservableAndDataFlow {
        // 把数据流块当做可观察流的输入
        // 创建缓冲块，ToObservable 转化成可观察流
        // 如果数据流出错并，这个异常信息传递给观察流中，并封装在 AggregateException 中
        public static void UseBufferBlockAndRx() {
            var buffer = new BufferBlock<int>();
            IObservable<int> observable = buffer.AsObservable();
            observable.Subscribe(
                onNext: data => Console.WriteLine(data),
                onError: ex =>
                throw ex,
                    onCompleted: () => Console.WriteLine("Completing..."));

            buffer.Post(12);
        }
        // 将网格转换为一个可观察流，但是如果抛错，观察流中的错误信息会转换成块的错误信息
        public static void UseActionBlockAndRx() {
            IObservable<DateTimeOffset> ticks = Observable.Interval(TimeSpan.FromSeconds(1))
                .Timestamp()
                .Select(x => x.Timestamp)
                .Take(5);

            var display = new ActionBlock<DateTimeOffset>(x => Console.WriteLine(x));
            ticks.Subscribe(display.AsObserver());

            try {
                display.Completion.Wait();
                Console.WriteLine("Done ...");
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }
    }
}