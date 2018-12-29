using System;
using System.Collections.Concurrent;
using System.Threading;

namespace _13PracticalSkills {
    //程序中有一些状态要传递，不同的线程要访问
    //最好的方法就是把状态变量传递到每个要调用的方法中，这样就必须在每个方法中添加参数
    //或存储在类的成员变量中，或者使用依赖注入来为每个方法提供状态变量
    //这时候就要用 CallContext.LogicalSetData，CallContext.LogicGetData //只在.net framwork存在，.netcore 请用System.Threading.AsyncLock<T>
    //根据http://www.cazzulino.com/callcontext-netstandard-netcore.html 利用 AsyncLocal<T> 与 ConcurrentDictionary 模拟 CallContext
    //.net4.0 ASP.NET 可以使用 HttpContext.Current.Items，效果与 CallContext 一样，效率更高
    public class ParameterStatusInConcurrent {
        public void DoLongOperation() {
            // var opertionID = new Guid();
            // CallContext
        }
    }

    public sealed class CallContext {
        private static readonly ConcurrentDictionary<string, AsyncLocal<object>> states =
            new ConcurrentDictionary<string, AsyncLocal<object>>();

        public static void LogicalSetData(string name, object data) =>
            states.AddOrUpdate(name, _ => new AsyncLocal<object>(), (_, __) => new AsyncLocal<object>()).Value = data;

        public static object GetLogicalData(string name) =>
            states.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : default;
    }
}