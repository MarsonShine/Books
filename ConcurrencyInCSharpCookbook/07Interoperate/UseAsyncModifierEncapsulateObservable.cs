using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace _07Interoperate {
    public class UseAsyncModifierEncapsulateObservable {
        //Observable.LastAsync 获取最后一个事件
        //await 可以订阅这个事件（内部调用订阅事件流）
        public static async Task<long> UseLastAsync() {
            IObservable<long> observable = Observable.Interval(TimeSpan.FromSeconds(1));
            long lastElement = await observable.LastAsync();
            return lastElement;
        }
        //Observable.ToList 获取所有的事件流
        //await 订阅所有事件
        public static async Task<IList<long>> UseToListInOrderToGetAllEventStreams() {
            IObservable<long> observable = Observable.Interval(TimeSpan.FromSeconds(1));
            IList<long> allEvents = await observable.ToList();
            return allEvents;
        }

        public static async Task<long> UseFirstAsyncToGetNextEventStream() {
            IObservable<long> observable = Observable.Interval(TimeSpan.FromSeconds(1));
            var nextEvent = await observable.FirstAsync();
            return nextEvent;
        }

        public static Task<long> UseToTaskToConvertTaskFromObservable() {
            IObservable<long> observable = Observable.Interval(TimeSpan.FromSeconds(1));
            return observable.ToTask();
        }
    }
}