using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace valuetask {
    public class FileReadingPooledValueTaskSource : IValueTaskSource<string> {
        /// <summary>
        /// 标记对象，用来表明在 OnCompleted 方法调用时
        /// 这个操作已经完成了
        /// </summary>
        /// <returns></returns>
        private static readonly Action<object> CallbackCompleted = _ => { Debug.Assert(false, "Should not be invoked"); };
        /// <summary>
        /// 表示当一个操作结束时执行的延续任务
        /// </summary>
        private Action<object> ? continuation;
        /// <summary>
        /// 执行成功之后的结果
        /// </summary>
        private string? result;
        /// <summary>
        /// 在操作期间出现的错误
        /// </summary>
        private Exception? exception;
        /// <summary>
        /// 令牌值，传递给 ValueTask 要验证的值，比如值被等待两次，或在用值时已被其他重用等
        /// </summary>
        private short token;
        /// <summary>
        /// 异步机制内部使用的状态
        /// </summary>
        private object? state;
        private ExecutionContext executionContext;
        private object? scheduler;

        public string GetResult(short token) {
            Console.WriteLine("GetResult");
            if (token != this.token)
                ThrowMultipleContinuations();
            var exception = this.exception;
            var result = ResetAndReleaseOperation();
            if (exception != null) {
                throw exception;
            }
            return result!;
        }

        private string? ResetAndReleaseOperation() {
            string? result = this.result;
            this.token++;
            this.result = null;
            this.exception = null;
            this.state = null;
            this.continuation = null;
            // this.pool.Return(this);
            return result;
        }

        private void ThrowMultipleContinuations() {
            throw new InvalidOperationException("Multiple awaiters are not allowed");
        }

        public ValueTaskSourceStatus GetStatus(short token) {
            throw new NotImplementedException();
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) {
            throw new NotImplementedException();
        }
    }
}