using System;
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
    }
}