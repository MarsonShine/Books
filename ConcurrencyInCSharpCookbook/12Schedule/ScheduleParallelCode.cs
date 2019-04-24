using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _12Schedule {
    /// <summary>
    /// 调度并行代码
    /// 控制个别代码段在并行代码中的执行方式
    /// </summary>
    public class ScheduleParallelCode {
        public void LimitTotalParallelDegree(IEnumerable<IEnumerable<int>> collections) {
            var schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxConcurrencyLevel : 10);
            TaskScheduler scheduler = schedulerPair.ConcurrentScheduler;
            ParallelOptions options = new ParallelOptions { TaskScheduler = scheduler };
            //启动一批并行循环，并且限制所有并行循环的总的并行数量。不管数据源的大小
            Parallel.ForEach(collections, options, items => Parallel.ForEach(items, item => {
                Console.WriteLine(item * item);
            }));
        }
    }
}