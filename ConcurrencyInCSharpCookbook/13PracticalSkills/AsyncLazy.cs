using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace _13PracticalSkills {
    public enum AsyncLazyFlags {
        None = 0x0,
        ExecuteOnCallingThread = 0x1,
        RetryOnFailure = 0x2
    }
    //Lazy 对象异步初始化的通用模式
    public class AsyncLazy<TResult> {
        private readonly object _mutex;
        private readonly Func<Task<TResult>> _factory;
        private Lazy<Task<TResult>> _instance;
        public AsyncLazy(Func<Task<TResult>> func, AsyncLazyFlags flag = AsyncLazyFlags.None) {
            if (func == null) throw new ArgumentNullException(nameof(func));
            _factory = func;
            if ((flag & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure)
                _factory = RetryOnFailure(_factory);
            if ((flag & AsyncLazyFlags.ExecuteOnCallingThread) != AsyncLazyFlags.ExecuteOnCallingThread)
                _factory = RunOnThreadPool(_factory);
            _mutex = new object();
            _instance = new Lazy<Task<TResult>>(func);
        }

        private Func<Task<TResult>> RetryOnFailure(Func<Task<TResult>> factory) {
            return async() => {
                try {
                    return await factory().ConfigureAwait(false);
                } catch {
                    lock(_mutex) {
                        _instance = new Lazy<Task<TResult>>(_factory);
                    }
                    throw;
                }
            };
        }

        private Func<Task<TResult>> RunOnThreadPool(Func<Task<TResult>> factory) {
            return () => System.Threading.Tasks.Task.Run(factory);
        }

        public Task<TResult> Task {
            get {
                lock(_mutex)
                return _instance.Value;
            }
        }

        public bool IsStarted {
            get {
                lock(_mutex) {
                    return _instance.IsValueCreated;
                }
            }
        }
        //async await 模式，允许让对象异步等待 await obj
        public TaskAwaiter<TResult> GetAwaiter() {
            return Task.GetAwaiter();
        }

        public ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) {
            return Task.ConfigureAwait(continueOnCapturedContext);
        }

        public void Start() {
            var unused = Task;
        }
    }
}