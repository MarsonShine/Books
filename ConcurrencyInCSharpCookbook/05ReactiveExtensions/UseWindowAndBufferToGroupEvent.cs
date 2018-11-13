using System;
using System.Reactive.Linq;

namespace _05ReactiveExtensions {
    //用 Window 和 Buffer 对事件进行分组
    //Window：在逻辑上对事件进行分组，到达一个事件就传递一个事件 返回类型是 IObservable<IObservable<T>>（由若干个事件流组成的事件流）
    //Buffer：会缓冲到达的事件，直到接收完一组事件，一次性传递出去 返回类型是 IObservable<IList<T>>（由若干个集合组成的事件流）
    //Windw,Buffer 还可以用来限流（throttling）。参数 skip 和 timeShift 能创建重合的组，还可以跳过组之间的元素。
    public class UseWindowAndBufferToGroupEvent {
        //每秒创建一个 OnNext 通知，然后每 2 个通知做一次缓冲
        //Buffer 等待所有事件，然后把所有事件作为一个集合发布。
        public static void NotificationLoopUseBuffer() {
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Buffer(2)
                .Subscribe(x => Console.WriteLine(DateTime.Now.Second + ": Got" + x[0] + " and " + x[1]));
        }
        //Window 同样的方法分组，但是它是每个事件一到达就发布。
        public static void NotificationLoopUseWindow() {
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Window(2)
                .Subscribe(group => {
                    Console.WriteLine(DateTime.Now.Second + ": Starting new group");
                    group.Subscribe(
                        x => Console.WriteLine(DateTime.Now.Second + ": Saw " + x),
                        () => Console.WriteLine(DateTime.Now.Second + ": Ending group")
                    );
                });
        }
    }
}