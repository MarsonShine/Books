using System;
using System.Threading;
using System.Threading.Tasks;

namespace _11Sync {
    /// <summary>
    /// 异步锁，多个代码段要安全的访问共享数据，并且这些代码段是异步可等待的
    /// </summary>
    public class AsyncLock {
        //这个锁保护value
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private int _value;

        public async Task DelayAndIncrementAsync() {
            await _mutex.WaitAsync();
            try {
                var oldValue = _value;
                await Task.Delay(TimeSpan.FromSeconds(oldValue));
                _value = oldValue + 1;
            } finally {
                _mutex.Release();
            }
        }
    }
}