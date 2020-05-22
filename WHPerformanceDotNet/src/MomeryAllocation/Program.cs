using System;
using System.Collections;
using System.Collections.Generic;

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

            var vt = new ValueTypeBox();
            vt.Name = "summer zhu";
            InvokeWithStructClass(vt);
            Console.WriteLine(vt.Name);
            // 值类型当参数传递
            void InvokeWithStructClass(ValueTypeBox box) {
                box.Name = "marson shine";
                Console.WriteLine(box.Name);
            }

            // for vs foreach
            int[] arr = new int[100];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = i;
            }

            int sum = 0;
            foreach (var val in arr) {
                sum += val;
            }

            sum = 0;
            IEnumerable<int> arrEnum = arr;
            foreach (var val in arrEnum) {
                sum += val;
            }
        }
    }
}