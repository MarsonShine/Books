using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AsyncAwaitable {
    public class ExampleAwaitable : INotifyCompletion {
        private readonly TaskAwaiter m_awaiter;

        public ExampleAwaitable(Task task) {
            if (task == null) throw new ArgumentNullException(nameof(task));
            m_awaiter = task.GetAwaiter();
        }
        public ExampleAwaitable GetAwaiter() { return this; }
        public bool IsCompleted {
            get {
                return m_awaiter.IsCompleted;
            }
        }
        public void OnCompleted(Action continuation) {
            m_awaiter.OnCompleted(continuation);
        }

        public void GetResult() {
            m_awaiter.GetResult();
        }
    }

    // public static class AwaitableExtension {
    //     public static TaskAwaiter GetAwaiter(this ExampleAwaitable notify) {
    //         return notify.GetAwaiter();
    //     }
    // }
}