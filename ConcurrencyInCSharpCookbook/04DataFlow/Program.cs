using System;
using System.Threading.Tasks.Dataflow;

namespace _04DataFlow
{
    class Program
    {
        static async void Main(string[] args)
        {
            var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
            var substractBlock = new TransformBlock<int, int>(item => item - 2);
            var options = new DataflowLinkOptions { PropagateCompletion = true};
            multiplyBlock.LinkTo(substractBlock, options);
            multiplyBlock.Complete();
            await substractBlock.Completion;

            multiplyBlock.Post(10);
            multiplyBlock.Post(10);
            multiplyBlock.Post(10);
            Console.WriteLine("Hello World!");
        }
    }
}
