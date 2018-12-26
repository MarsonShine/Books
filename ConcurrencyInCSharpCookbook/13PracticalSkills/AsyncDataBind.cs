using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace _13PracticalSkills {
    /// <summary>
    /// 异步数据绑定到UI上
    /// 绑定的时候需要同步数据源，例如一个对象的属性是异步获取的，并且要绑定到UI上
    /// </summary>
    public class AsyncDataBind {
        class BindableTask<T> : INotifyPropertyChanged {
            private readonly Task<T> _task;
            public event PropertyChangedEventHandler PropertyChanged;
            BindableTask(Task<T> task) {
                _task = task;
                var _ = WatchTaskAsync();
            }

            private async Task WatchTaskAsync() {
                try {
                    await _task;
                } catch (System.Exception) { }
                OnPropertyChanged("IsNotCompleted...");
                OnPropertyChanged("IsSuccessfullyCompleted...");
                OnPropertyChanged("IsFaulted...");
                OnPropertyChanged("IsResult...");
            }

            protected virtual void OnPropertyChanged(string propertyName) {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public bool IsNotCompleted {
                get { return !_task.IsCompleted; }
            }
            public bool IsSuccessfullyCompleted {
                get {
                    return _task.IsCompletedSuccessfully && _task.Status == TaskStatus.RanToCompletion;
                }
            }
            public bool IsFaulted {
                get { return _task.IsFaulted; }
            }

            public T Result {
                get { return IsSuccessfullyCompleted?_task.Result : default(T); }
            }
        }
    }
}