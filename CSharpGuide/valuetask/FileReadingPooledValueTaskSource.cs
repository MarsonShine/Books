using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace valuetask
{
    /// <summary>
    /// IValueTaskSource 具体内部实现，详见 AsyncOperation<TResult>
    /// https://source.dot.net/#System.Threading.Channels/System/Threading/Channels/AsyncOperation.cs,b49b59985a30345e,references
    /// </summary>
    public class FileReadingPooledValueTaskSource : IValueTaskSource<string>
    {
        /// <summary>
        /// 标记对象，用来表明在 OnCompleted 方法调用时
        /// 这个操作已经完成了
        /// </summary>
        /// <returns></returns>
        private static readonly Action<object> CallbackCompleted = _ => { Debug.Assert(false, "Should not be invoked"); };
        /// <summary>
        /// 表示当一个操作结束时执行的延续任务
        /// </summary>
        private Action<object>? continuation;
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
        private ExecutionContext? executionContext;
        private object? scheduler;

        public string GetResult(short token)
        {
            Console.WriteLine("GetResult");
            if (token != this.token)
                ThrowMultipleContinuations();
            var exception = this.exception;
            var result = ResetAndReleaseOperation();
            if (exception != null)
            {
                throw exception;
            }
            return result!;
        }

        private string? ResetAndReleaseOperation()
        {
            string? result = this.result;
            this.token++;
            this.result = null;
            this.exception = null;
            this.state = null;
            this.continuation = null;
            // this.pool.Return(this);
            return result;
        }

        private void ThrowMultipleContinuations()
        {
            throw new InvalidOperationException("Multiple awaiters are not allowed");
        }
        // 这个是状态机调用的方法，已便知道当前操作处于的状态
        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (token != this.token)
                ThrowMultipleContinuations();
            Console.WriteLine("GetStatus:");
            if (result == null)
            {
                Console.WriteLine("pending");
                return ValueTaskSourceStatus.Pending;
            }
            Console.WriteLine("completed: succeeded or faulted");
            return exception != null ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Faulted;
        }
        // 如果被包装的 ValueTask 正在等待，那么底层状态机就会调用它
        // 有两种发生的场景需要处理
        // 1：如果操作还没完成，那么我们存储的延续任务continuation在操作一旦完成就会触发调用
        // 2：如果操作已经完成，这种情况内部的continuation就应该已经设置成CallbackCompleted。如果是这样，那么只需调用延续任务即可
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            Console.WriteLine("." + token);
            if (token != this.token)
                ThrowMultipleContinuations();
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                this.executionContext = ExecutionContext.Capture();
            }
            if((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext? sc = SynchronizationContext.Current;
                if(sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    this.scheduler = sc;
                }
                else
                {
                    TaskScheduler ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        this.scheduler = ts;
                    }
                }
            }
            this.state = state;
            // continuation 必须要在操作完成后执行（如果没有完成，那么就要把continuation赋值为CallbackCompleted）
            var previousContinuation = Interlocked.CompareExchange(ref this.continuation, continuation, null);
            if(previousContinuation != null)
            {
                if (!ReferenceEquals(previousContinuation, CallbackCompleted))
                {
                    ThrowMultipleContinuations();

                    executionContext = null;
                    this.state = null;
                    InvokeContinuation(continuation, state, forceAsync: true);
                }
            }
        }

        private void InvokeContinuation(Action<object?> continuation, object? state, bool forceAsync)
        {
            if (continuation == null)
                return;

            object? scheduler = this.scheduler;
            this.scheduler = null;
            if (scheduler != null)
            {
                if(scheduler is SynchronizationContext sc)
                {
                    sc.Post(s =>
                    {
                        var t = (Tuple<Action<object?>, object?>)s!;
                        t.Item1(t.Item2);
                    }, Tuple.Create(continuation, state));
                }
                else
                {
                    Debug.Assert(scheduler is TaskScheduler, $"Expected TaskScheduler, got {scheduler}");
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, (TaskScheduler)scheduler);
                }
            }
            else if (forceAsync)
            {
                ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
            }
            else
            {
                continuation(state);
            }
        }
    }
}