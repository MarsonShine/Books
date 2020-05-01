using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FullGCNotification {
    class Program {
        static void Main(string[] args) {
            const int ArrSize = 1024;
            var arrays = new List<byte[]>();

            GC.RegisterForFullGCNotification(25, 25);

            // 启动一个单独的线程等待接收垃圾回收通知
            Task.Run(() => WaitForGCThread(null));

            Console.WriteLine("Press any key to exist");
            while (!Console.KeyAvailable) {
                try {
                    arrays.Add(new byte[ArrSize]);
                } catch (OutOfMemoryException) {
                    Console.WriteLine("OutOfMemoryException");
                    arrays.Clear();
                }
            }

            GC.CancelFullGCNotification();
        }

        private static void WaitForGCThread(object arg) {
            const int MaxWaitMs = 10000;
            while (true) {
                // 无限制等待还是会让 WaitForFullGCApproach 过载
                GCNotificationStatus status = GC.WaitForFullGCApproach(MaxWaitMs);
                bool didCollect = false;
                switch (status) {
                    case GCNotificationStatus.Succeeded:
                        Console.WriteLine("GC approaching!");
                        Console.WriteLine("-- redirect processing to another machine --");
                        didCollect = true;
                        GC.Collect();
                        break;
                    case GCNotificationStatus.Canceled:
                        Console.WriteLine("GC Notification wa canceled");
                        break;
                    case GCNotificationStatus.Timeout:
                        Console.WriteLine("GC notification timed out");
                        break;
                }

                if (didCollect) {
                    do {
                        status = GC.WaitForFullGCComplete(MaxWaitMs);
                        switch (status) {
                            case GCNotificationStatus.Succeeded:
                                Console.WriteLine("GC Complete");
                                Console.WriteLine("-- accept processing on this machine again --");
                                break;
                            case GCNotificationStatus.Canceled:
                                Console.WriteLine("GC Notification was canceled");
                                break;
                            case GCNotificationStatus.Timeout:
                                Console.WriteLine("GC completion notification timed out");
                                break;
                        }
                        // 这里的循环不一定需要
                        // 但是如果你在进入下一次
                    } while (status == GCNotificationStatus.Timeout);
                }
            }
        }
    }
}