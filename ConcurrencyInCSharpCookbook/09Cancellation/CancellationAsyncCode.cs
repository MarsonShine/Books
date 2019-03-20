using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace _09Cancellation {
    /// <summary>
    /// 取消 async 代码，让 async 支持取消
    /// </summary>
    public class CancellationAsyncCode {
        //方式1 把 CancellationToken 当参数换地下一层 async 代码
        public async Task<int> CancelableMethodAsync(CancellationToken token) {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            return 11;
        }
        //上面的方式模式有一个通用的准则，就是支持取消的任务，那么调用这个任务的方法也要支持取消，要把它传给支持取消的 api

        //取消并行代码，还是跟上述一样，把 CancellationToken 传递到下一个方法。
        //并行方法使用 Parallel Options 实例支持取消。ParallelOptions 实例下的 CancellationToken
        public void SumParallel(IEnumerable<int> source, CancellationToken token) {
            Parallel.ForEach(
                source,
                new ParallelOptions { CancellationToken = token },
                item => item = item * 100);
        }

        //还有一种方法就是直接在 Parallel.ForEach 循环体中监听取消状态，这个不推荐使用，因为在并行代码里监听取消状态显式爆出异常，会把异常信息封装进 AggregateExcetion 中,使用起来会更加麻烦。而把 ParallelOptions 实例中的 CancellationToken 设置之后，Parallel 类会自动处理的很好，确定检查取消状态标记的频率。
        [Obsolete("不推荐使用这一种方法")]
        public void SumParallel_V2(IEnumerable<int> source, CancellationToken token) {
            Parallel.ForEach(
                source,
                new ParallelOptions { CancellationToken = token },
                item => {
                    item = item * 33;
                    token.ThrowIfCancellationRequested();
                });
        }

        //PLinq 本身也支持取消操作
        public IEnumerable<int> Multiple(IEnumerable<int> source, CancellationToken token) {
            return source.AsParallel()
                .WithCancellation(token)
                .Select(p => p * 2);
        }
    }
}