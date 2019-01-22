using System.Threading.Tasks;

namespace ExceptionInAsync {
    /// <summary>
    /// 异常只触发一次
    /// </summary>
    public class FireAndForget {

    }

    public static class TaskExtension {
        public static void ForgetSafety(this Task task) {
            task.ContinueWith(t => new MultipleException());
        }
    }
}