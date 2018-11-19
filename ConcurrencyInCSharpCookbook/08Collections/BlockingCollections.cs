using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace _08Collections {
    /// <summary>
    /// 阻塞集合可用于限流的消费者与生产者线程之间的信息交互
    /// 生产者线程访问阻塞集合，往集合添加数据 Add 
    /// 添加结束后，要 调用 CompleteAdding 来通知消费者线程消费数据
    /// </summary>
    public class BlockingCollections {
        private readonly BlockingCollection<int> blockingCollection = new BlockingCollection<int>();

        public void Consumer() {
            Console.WriteLine("消费者开始消费...");
            foreach (var item in blockingCollection.GetConsumingEnumerable()) {
                Console.WriteLine("消费者：消费了" + item);
            }
        }

        public void Producer() {
            blockingCollection.Add(1);
            blockingCollection.Add(2);
            blockingCollection.Add(3);
            blockingCollection.CompleteAdding(); //调用这个方法标识不能再添加数据
            Console.WriteLine("生产者：生产数据完成...");
        }

        public void Start() {
            Task.Run(() => Consumer());
            Task.WaitAny(Task.Delay(3000));
            Producer();
        }
    }
}