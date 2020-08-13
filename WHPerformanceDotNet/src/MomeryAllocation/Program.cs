using MomeryAllocation.AvoidBoxed;
using MomeryAllocation.MemoryAllocationOfSpan;
using MomeryAllocation.WinDbg;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MomeryAllocation
{
    class Program
    {
        static void Main(string[] args)
        {
            //const int size = 1000 * 1000;
            //// var empty = new Empty();
            //var before = GC.GetTotalMemory(true);
            //var empty = new Empty();
            //var after = GC.GetTotalMemory(true);

            //var diff = after - before;

            //Console.WriteLine("空对象内存大小：" + diff);

            //GC.KeepAlive(empty);

            //// 数组对象
            //var array = new Empty[size];
            //before = GC.GetTotalMemory(true);
            //for (int i = 0; i < size; i++) {
            //    array[i] = new Empty();
            //}
            //after = GC.GetTotalMemory(true);

            //diff = after - before;
            //Console.WriteLine("数组空对象内存带下：" + diff / size);
            //GC.KeepAlive(array);

            //var vt = new ValueTypeBox();
            //vt.Name = "summer zhu";
            //InvokeWithStructClass(vt);
            //Console.WriteLine(vt.Name);
            //// 值类型当参数传递
            //void InvokeWithStructClass(ValueTypeBox box) {
            //    box.Name = "marson shine";
            //    Console.WriteLine(box.Name);
            //}

            //// for vs foreach
            //int[] arr = new int[100];
            //for (int i = 0; i < arr.Length; i++) {
            //    arr[i] = i;
            //}

            //int sum = 0;
            //foreach (var val in arr) {
            //    sum += val;
            //}

            //sum = 0;
            //IEnumerable<int> arrEnum = arr;
            //foreach (var val in arrEnum) {
            //    sum += val;
            //}


            //AvoidMemoryAlloctation.Start();

            // dump 分析
            var ec = new ExampleClass();

            var ecg = new ExampleGenericClass<object>();

            // 通过按值传递返回关键来避免装箱，值拷贝等消耗
            TestMagnitude();

            ZeroMiddleValue(new[] { 1, 2, 3, 4, 5, 6 });
            RefZeroMiddleValue(new[] { 1, 2, 3, 4, 5, 6 });

            var v = new Vector2()
            {
                Location = new Point3d { Name = "l", x = 2, y = 2, z = 2 }
            };
            SetVectorToOrigin(v);
            RefSetVectorToOrigin(v);
        }

        static void TestMagnitude()
        {
            AvoidBoxed.Vector v = new AvoidBoxed.Vector();
            ref int mag = ref v.Magnitude;
            mag = 3;

            int nonRefMag = v.Magnitude;
            mag = 4;

            Console.WriteLine($"mag: {mag}");
            Console.WriteLine($"nonRefMag: {nonRefMag}");
        }

        static void ZeroMiddleValue(int[] arr)
        {
            int midIndex = GetMidIndex(arr);
            arr[midIndex] = 0;
        }

        private static int GetMidIndex(int[] arr)
        {
            return arr.Length / 2;
        }

        static void RefZeroMiddleValue(int[] arr)
        {
            ref int middle = ref GetRefToMiddle(arr);
            middle = 0;
        }

        private static ref int GetRefToMiddle(int[] arr)
        {
            return ref arr[arr.Length / 2];
        }

        private static void SetVectorToOrigin(Vector2 vector)
        {
            Point3d location = vector.Location;
            location.x = 0;
            location.y = 0;
            location.z = 0;
            vector.Location = location;
        }

        private static void RefSetVectorToOrigin(Vector2 vector)
        {
            ref Point3d location = ref vector.RefLocation;
            location.x = 0;
            location.y = 0;
            location.z = 0;
        }
    }
}