using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// 线程与锁的应用
/// </summary>
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
        // 多个线程对共享变量的写入操作
        private static void MultiThreadWriteShareObjectWithLocking() {
            object syncObj = new object();
            var masterList = new List<long>();
            const int numTasks = 8;
            Task[] tasks = new Task[numTasks];

            for (int i = 0; i < numTasks; i++) {
                tasks[i] = Task.Run(() => {
                    for (int j = 0; j < 5000000; j++) {
                        lock(syncObj) {
                            masterList.Add(j);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);
        }
        private static void MultiThreadWriteShareObjectWithoutLocking() {
            object syncObj = new object();
            var masterList = new List<long>();
            const int numTasks = 8;
            Task[] tasks = new Task[numTasks];

            for (int i = 0; i < numTasks; i++) {
                tasks[i] = Task.Run(() => {
                    var localList = new List<long>();
                    for (int j = 0; j < 5000000; j++) {
                        localList.Add(j);
                    }
                    lock(syncObj) {
                        masterList.AddRange(localList);
                    }
                });
            }

            Task.WaitAll(tasks);
        }
    }
}