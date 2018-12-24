using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace _12Schedule {
    /// <summary>
    /// 用调度器实现数据流的同步
    /// 需要控制个别代码段在数据流代码中的执行方式
    /// </summary>
    public class TaskScheduleImplementDataflowSync {
        public void Startup() {
            var options = new ExecutionDataflowBlockOptions() {
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            };
            //把数据网格中的每个数据都乘以2
            var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
            //并且把每个项打印出来
            var displayBlock = new ActionBlock<int>(result => Console.WriteLine(result), options);
            multiplyBlock.LinkTo(displayBlock);
            //如果要协调位于数据流网格中不同块的行为，就非常需要指定一个 TaskScheduler。像前面说的 ConcurrentExclusiveSchedulerPair.ExclusiveScheduler 来确保A块和C块不能同时运行，而块B可以随时执行。

        }
    }
}