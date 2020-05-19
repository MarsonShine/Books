using System;

namespace MomeryAllocation {
    class Program {
        static void Main(string[] args) {
            const int size = 1000 * 1000;
            // var empty = new Empty();
            var before = GC.GetTotalMemory(true);
            var empty = new Empty();
            var after = GC.GetTotalMemory(true);

            var diff = after - before;

            Console.WriteLine("空对象内存大小：" + diff);

            GC.KeepAlive(empty);

            // 数组对象
            var array = new Empty[size];
            before = GC.GetTotalMemory(true);
            for (int i = 0; i < size; i++) {
                array[i] = new Empty();
            }
            after = GC.GetTotalMemory(true);

            diff = after - before;
            Console.WriteLine("数组空对象内存带下：" + diff / size);
            GC.KeepAlive(array);
        }
    }
}