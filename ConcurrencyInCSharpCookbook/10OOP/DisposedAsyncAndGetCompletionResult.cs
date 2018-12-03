using System;
using System.Threading;
using System.Threading.Tasks;

namespace _10OOP {
    //异步完成，获取异步返程的结果
    public partial class DisposedAsync {

    }

    //如果还要知道异步完成的时间，那么就要对这些类采用实现 “异步完成” 的方式
    //异步完成与异步初始化很相似。异步完成的重要部分要封装在一个接口对象中
    /// <summary>
    /// 这个类需要异步完成，并提供完成返回的结果
    /// </summary>
    public interface IAsyncCompletion {
        /// <summary>
        /// 开始本实例的完成过程，类似于“IDisposable.Dispose”
        /// 在调用本方法之后，就不能调用除了 “Completion” 以为的任何成员
        /// </summary>
        void Complete();
        /// <summary>
        /// 取得本实例完成的结果
        /// </summary>
        /// <value></value>
        Task Completion { get; }
    }

    class MyClassCompletion : IAsyncCompletion {
        private readonly TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();
        private Task _completing;
        public Task Completion => _completion.Task;

        public Task Completing { get => _completing; }

        public void Complete() {
            if (_completing != null)
                return;
            _completing = CompletionAsync();
        }

        private async Task CompletionAsync() {
            try {
                await Task.Delay(TimeSpan.FromSeconds(2)); //异步等待任务运行中的结果
            } catch (System.Exception ex) {
                _completion.TrySetException(ex);
            } finally {
                _completion.TrySetResult(null);
            }
        }
    }
}