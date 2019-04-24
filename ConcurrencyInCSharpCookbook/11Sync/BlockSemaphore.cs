using System;
using System.Threading;
using System.Threading.Tasks;

namespace _11Sync {
    /// <summary>
    /// 阻塞信号
    /// 需要从一个线程发送信号到另一个线程
    /// </summary>
    public class BlockSemaphore {
        private readonly MyClass myClass = new MyClass();

        //最常用的垮线程发送信号是 ManualResetEventSlim 人工重置事件
        //分已标记和未标记状态
        //除了 ManualResetEventSlim，还有AutoResetEvent、CountdownEvent、Barrie
        class MyClass {
            private readonly ManualResetEventSlim _initialized = new ManualResetEventSlim();
            private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
            private int _value;
            public int WaitForInitialization() {
                _initialized.Wait();
                return _value;
            }

            public void InitializeFromAnotherThread() {
                _value = 13;
                _initialized.Set();
            }

            public void WaitForReleaseAutoResetEvent() {
                Console.WriteLine("WaitForReleaseAutoResetEvent：线程正在等待信号量释放...");
                _autoResetEvent.WaitOne();
                Console.WriteLine("WaitForReleaseAutoResetEvent：线程成功占据...");
            }

            public void SetAutoResetEvent() {
                Console.WriteLine("SetAutoResetEvent ：线程coming...");
                _autoResetEvent.Set();
                Thread.Sleep(500);
                Console.WriteLine("SetAutoResetEvent ：线程释放...");
            }
        }

        public void UseAutoResetEvent() {
            Task.Run(() => {
                myClass.WaitForReleaseAutoResetEvent();
                Thread.Sleep(2000);
            });
            Thread.Sleep(3000);
            Task.Run(() => {
                myClass.SetAutoResetEvent();
            });
        }
    }
}