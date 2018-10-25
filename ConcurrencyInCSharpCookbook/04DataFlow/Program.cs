using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace _04DataFlow {
    class Program {
        static async Task Main (string[] args) {
            var multiplyBlock = new TransformBlock<int, int> (item => {
                    item = item * 2;
                    Console.WriteLine ("multiplyBlock" +
                        item);
                    return item;
                });
            var substractBlock = new TransformBlock<int, int> (item => {
                    item = item - 2;
                    Console.WriteLine ("substractBlock:" +
                        item);
                    return item;
                });
            var options = new DataflowLinkOptions { PropagateCompletion = true };
            multiplyBlock.LinkTo (substractBlock, options);
            multiplyBlock.Complete ();
            await substractBlock.Completion;
            int inputNumber = 10;
            multiplyBlock.Post (inputNumber);
            multiplyBlock.Post (inputNumber);
            multiplyBlock.Post (inputNumber);
            await multiplyBlock.SendAsync (inputNumber);

            // await HandleExceptionInTransformBlock ();
            await HandleExceptionTransformBlockWithPropagateCompletion ();

            BlockParallelSlim (true).ContinueWith (t => Console.WriteLine (t.IsCompleted)).Wait ();
            Console.WriteLine ("Hello World!");
        }

        //处理数据流块发生的错误
        //传递错误信息
        public static async Task HandleExceptionInTransformBlock () {
            try {
                var block = new TransformBlock<int, int> (item => {
                        if (item == 1)
                            throw new InvalidOperationException ("invalid operation");
                        return item * 2;
                    });
                block.Post (1);
                block.Post (2);
                await block.Completion; //等待这个数据链全部完成
            } catch (InvalidOperationException) {
                //catching
                Console.Error.WriteLine ("InvalidOperationException");
            }
        }

        //数据块组成链时，如果传递PropagateCompletion，意味着错误往下一个数据块传递
        //错误信息在AggregateException
        public static async Task HandleExceptionTransformBlockWithPropagateCompletion () {
            try {
                var multiplyBlock = new TransformBlock<int, int> (item => {
                        if (item == 1) {
                            throw new InvalidOperationException ("invalid operation");
                        }
                        return item * 2;
                    });
                var substractBlock = new TransformBlock<int, int> (item => item - 2);
                multiplyBlock.LinkTo (substractBlock,
                    new DataflowLinkOptions { PropagateCompletion = true });
                multiplyBlock.Post (1);
                multiplyBlock.Post (2);
                await substractBlock.Completion;
            } catch (AggregateException ae) {
                Console.WriteLine ("AggregateException 错误次数：" + ae.InnerExceptions.Count);
            }
        }

        //在已经运行的数据块链当中，如果想要更改数据流结构。
        //可以随时对数据流块建立链接和断开链接。数据在网格上的自由传递，不会受此影响。建立或断开链接时，线程是安全的。
        //建立链接时，保留 LinkTo 返回的 IDisposable
        //断开链接时，只需要释放即可
        public static void ReleaseBlock () {
            var multiplyBlock = new TransformBlock<int, int> (item => item * 2);
            var substractBlock = new TransformBlock<int, int> (item => item - 2);
            IDisposable link = multiplyBlock.LinkTo (substractBlock);
            multiplyBlock.Post (1);
            multiplyBlock.Post (2);
            //断开数据流块之间的链接
            link.Dispose ();
        }

        public static async Task BlockParallelSlim (bool isParalllel) {
            var multiplyBlock = new TransformBlock<int, int> (
                    item => {
                        Thread.Sleep (1000 + (100 * item));
                        return item * 2;
                    },
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = isParalllel?ExecutionDataflowBlockOptions.Unbounded : 1 });
            var substractBlock = new TransformBlock<int, int> (
                    item => item - 2);
            multiplyBlock.LinkTo (substractBlock);
            multiplyBlock.Post (10);
            multiplyBlock.Post (11);
            multiplyBlock.Post (12);
            multiplyBlock.Post (13);
            await multiplyBlock.Completion;
        }

        public static IPropagatorBlock<int, int> CreateCustomBlock () {
            var multiplyBlock = new TransformBlock<int, int> (
                    item => item * 2);
            var addBlock = new TransformBlock<int, int> (item => item + 2);
            var divideBlock = new TransformBlock<int, int> (item => item / 2);
            var flowCompletion = new DataflowLinkOptions { PropagateCompletion = true };
            multiplyBlock.LinkTo(addBlock,flowCompletion);
            addBlock.LinkTo(divideBlock,flowCompletion);

            return DataflowBlock.Encapsulate(multiplyBlock,addBlock);
        }
    }
}