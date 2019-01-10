using System;
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

        private static void HandleException(Task<object> obj) {
            throw new NotImplementedException();
        }
    }
}