using Nito.AsyncEx;

namespace _11Sync {
    /// <summary>
    /// 异步信号，在代码之间发送通知，并且允许异步等待
    /// </summary>
    public class AsyncSemaphore {
        private readonly AsyncManualResetEvent _asyncManualResetEvent = new AsyncManualResetEvent(false);
    }
}