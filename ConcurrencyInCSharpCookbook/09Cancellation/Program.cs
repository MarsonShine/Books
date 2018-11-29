﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace _09Cancellation {
    class Program {
        private static CancellationHelper canceller = new CancellationHelper();
        static void Main(string[] args) {
            Task.Run(() => canceller.IssueTask());
            Thread.Sleep(1000);
            Cancel();

            CancellationByPoll cancellationByPoll = new CancellationByPoll();
            Task.Run(() => cancellationByPoll.Execute());
            Thread.Sleep(2000); //执行计算逻辑
            cancellationByPoll.Cancel();
            // Thread.Sleep(2000);
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
        private static void Cancel() {
            Console.WriteLine("一秒之后取消请求...");
            Thread.Sleep(1000);
            canceller.Cancel();
        }
    }
}