using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace _08Collections {
    /// <summary>
    /// 无论是阻塞队列也好还是阻塞栈、包，它们都是同步的
    /// 如果在一边加载数据，另一边在推送数据。同时还要把数据更新到 UI 上（ UI线程 ）
    /// 这种就要用到异步队列 BufferBlock
    /// </summary>
    public class AsyncQueue {
        private readonly BufferBlock<int> m_asyncQueue = new BufferBlock<int>();

        public async Task Start() {
            await Consumer();
            await Producer();
        }

        private async Task Consumer() {
            //consumer
            while (await m_asyncQueue.OutputAvailableAsync()) {
                Console.WriteLine(await m_asyncQueue.ReceiveAsync());
            }
        }

        private async Task Producer() {
            //producer
            await m_asyncQueue.SendAsync(7);
            await m_asyncQueue.SendAsync(10);
            m_asyncQueue.Complete();
        }
    }
}