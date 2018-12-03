using System.Threading;

namespace _11Sync {
    /// <summary>
    /// 阻塞信号
    /// 需要从一个线程发送信号到另一个线程
    /// </summary>
    public class BlockSemaphore {
        //最常用的垮线程发送信号是 ManualResetEventSlim 人工重置事件
        //分已标记和未标记状态
        class MyClass {
            private readonly ManualResetEventSlim _initialized = new ManualResetEventSlim();
            private int _value;
            public int WaitForInitialization() {
                _initialized.Wait();
                return _value;
            }

            public void InitializeFromAnotherThread() {
                _value = 13;
                _initialized.Set();
            }
        }
    }
}