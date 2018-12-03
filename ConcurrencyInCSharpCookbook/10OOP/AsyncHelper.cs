using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace _10OOP {
    /// <summary>
    /// MyClassCompletion 的使用不方便, Dispose是异步的，所以不能用 using，如果改成 using 模式使用方式，会显得更加美观便捷
    /// </summary>
    public class AsyncHelper {
        public static async Task Using<TResource>(Func<TResource> construct, Func<TResource, Task> process)
        where TResource : IAsyncCompletion {
            //创建需要使用的资源
            var resource = construct();
            //使用资源，并捕获所有异常
            Exception exception = null;
            try {
                await process(resource);
            } catch (Exception ex) {
                exception = ex;
            }
            //完成（逻辑上销毁）资源
            resource.Complete();
            await resource.Completion;

            //重新抛出 “process” 产生的异常
            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
        }

        public static async Task<TResult> Using<TResource, TResult>(Func<TResource> construct, Func<TResource, Task<TResult>> process)
        where TResource : IAsyncCompletion {
            var resouce = construct();
            Exception exception = null;
            var result = default(TResult);
            try {
                result = await process(resouce);
            } catch (System.Exception ex) {
                exception = ex;
            }
            //完成（逻辑上销毁）资源
            resouce.Complete();
            try {
                await resouce.Completion;
            } catch {
                //只有当 “process” 没有抛异常时才抛出 “Completion” 异常
                if (exception == null)
                    throw;
            }

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
            return result;
        }

        async Task Test() {
            await AsyncHelper.Using(() => new MyClassCompletion(), async resource => {
                // 使用资源。
                await Task.Delay(TimeSpan.FromSeconds(3));
                return this;
            });
        }
    }
}