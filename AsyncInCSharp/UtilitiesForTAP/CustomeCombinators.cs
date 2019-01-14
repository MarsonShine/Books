using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UtilitiesForTAP {
    public class CustomeCombinators {
        public static async Task<T> WithTimeout<T>(Task<T> task, int timeout) {
            Task delayTask = Task.Delay(timeout);
            Task firstToFinish = await Task.WhenAny(task, delayTask);
            if (firstToFinish == delayTask) {
                //延迟任务限制性，当做超时异常处理
                task.ContinueWith(HandleException);
                throw new TimeoutException();
            }
            return await task;
        }

        private static void HandleException<T>(Task<T> task) {
            if (task.Exception != null)
                Trace.TraceInformation(task.Exception.Message);
        }
    }
}