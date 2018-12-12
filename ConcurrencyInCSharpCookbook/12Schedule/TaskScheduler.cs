using System.Threading.Tasks;

namespace _12Schedule {
    /// <summary>
    /// 任务调度器，可以让多个代码段（任务）按照指定的方式运行。例如让所有代码都在 UI 线程中运行，或者只允许某些特定的代码同行运行。
    /// TaskScheduler 对象是让任务在线程池中排队。
    /// </summary>
    public class TaskSchedulers {
        public void GetSpecificContext() {
            //创建一个跟上下文关联的任务调度器，并将任务调度到这个上下文中来。
            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            var schedulerPair = new ConcurrentExclusiveSchedulerPair();
            //ConcurrentScheduler： 确保 ExclusiveScheduler 没有任务执行时，ConcurrentScheduler就可以让多个任务同时执行
            //只有当ConcurrentScheduler 没有任务执行时，ExclusiveScheduler 才执行任务，且每次只执行一次任务
            TaskScheduler concurrent = schedulerPair.ConcurrentScheduler;
            TaskScheduler exclusive = schedulerPair.ExclusiveScheduler;
        }
    }
}