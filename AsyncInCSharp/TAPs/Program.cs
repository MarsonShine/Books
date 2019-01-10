using System;
using System.Threading.Tasks;

namespace TAPs {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }

        private Task<bool> GetUserPermission() {
            //创建一个 TaskCompletionSource，让我们可以返回一个任我操控的 Task
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            //新建窗口
            PermissionDialog dialog = new PermissionDialog();
            //当用户完成对话框时，Task 使用 SetResult 来表示完成
            dialog.Closed += delegate { tcs.SetResult(dialog.PermissionGranted); };
            dialog.Show();
            //返回一个“傀儡” Task，它还没有完成。
            return tcs.Task;
        }
    }
}