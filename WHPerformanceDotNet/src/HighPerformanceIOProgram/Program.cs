using System;
using System.IO;
using System.Threading.Tasks;

namespace HighPerformanceIOProgram {
    class Program {
        static void Main(string[] args) {

        }

        private static Task<int> AsynchronousRead(string fileName) {
            var chunkSize = 4096; // 设置一次读取的字节数
            var buffer = new byte[chunkSize]; // 缓冲区
            var tcs = new TaskCompletionSource<int>();

            var fileContent = new MemoryStream();
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, chunkSize, useAsync : true);
            fileContent.Capacity += chunkSize;

            var task = fileStream.ReadAsync(buffer, 0, buffer.Length);
            task.ContinueWith(readTask => {
                ContinuRead(readTask, fileStream, fileContent, buffer, tcs);
            });

            return tcs.Task;
        }

        private static void ContinuRead(Task<int> task, FileStream fileStream, MemoryStream fileContent, byte[] buffer, TaskCompletionSource<int> tcs) {
            if (task.IsCompleted) {
                int bytesRead = task.Result;
                fileContent.Write(buffer, 0, bytesRead);
                if (bytesRead > 0) {
                    var newTask = fileStream.ReadAsync(buffer, 0, buffer.Length);
                    newTask.ContinueWith(readTask => ContinuRead(readTask, fileStream, fileContent, buffer, tcs));
                } else {
                    tcs.TrySetResult((int) fileContent.Length);
                    fileStream.Dispose();
                    fileContent.Dispose();

                }
            }
        }
    }
}