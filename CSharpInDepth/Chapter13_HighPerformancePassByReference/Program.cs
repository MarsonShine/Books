using System;

namespace Chapter13_HighPerformancePassByReference {
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
    using System;
    /*
    使用 ref 关键字对变量进行别名
    通过引用和 ref 返回变量
    通过使用 in 关键字有效的参数传递
    通过使用只读 ref 返回来阻止数据变化，只读的 ref 局部变量和只读的结构体声明
    用 in 或 ref 的拓展方法
    类似 ref 的结构以及 Span<T>
    */
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            // 多次通过 ref 参数
            int x = 5;
            // 此时 p1，p2 都是 x 的别名。都是指向同一个内存地址
            IncrementAndDouble(ref x, ref x);
            Console.WriteLine(x);

            // ref 本地变量
            int x2 = 10;
            ref int y = ref x2; // 通过 ref 重新申明一个本地变量
            x2++;
            y++;
            Console.WriteLine(x2);
            // ref 本地变量可以避免值拷贝，内存分配。如果有一个大型可变类型的数组，通过 ref 申明可以避免不必要的复制开销
            var array = new(int x, int y) [10];
            for (int i = 0; i < array.Length; i++) {
                array[i] = (i, i); // 初始化
            }
            for (int i = 0; i < array.Length; i++) {
                ref var element = ref array[i]; // 直接获取地址，将地址的值自增
                element.x++;
                element.y *= 2;
                Console.WriteLine($"值：{array[i].x}");
            }
            // ref 本地变量来给对象的字段别名
            RefLocalField.Start();

            // tuple ref 本地变量
            (int x, int y) tuple1 = (10, 20);
            ref(int a, int b) tuple2 = ref tuple1;
            tuple2.a = 30;
            Console.WriteLine(tuple1.x);

            // ref return
            RefReturn.Start();

            ArrayHelper.Start();

            ReadOnlyArrayView<int>.Start();
        }
        static(int even, int odd) CountEvenAndOdd(IEnumerable<int> values) {
            var result = (even: 0, odd: 0);
            foreach (int value in values) {
                ref int counter = ref(value & 1) == 0 ?
                    ref result.even : ref result.odd;
                counter++;
            }
            return result;
        }
        static void IncrementAndDouble(ref int p1, ref int p2) {
            p1++;
            p2 *= 2;
        }

        /// <summary>
        /// ref local 三种限制
        /// 迭代器块不能包含 ref 本地变量
        /// async 方法不能包含 ref 本地变量
        /// ref 本地变量是不能被异步方法或本地方法捕获
        /// </summary>
        internal class RefLocalField {
            private int value;
            public static void Start() {
                var obj = new RefLocalField();
                // 每个引用对象中的字段的 ref 变量都会引入一个内部指针，指向由垃圾收集器维护的数据结构。
                ref int tmp = ref obj.value;
                tmp = 10; // 分配一个新的值给 ref 本地变量
                Console.WriteLine(obj.value);

                obj = new RefLocalField(); // obj 重新赋值，但是 tmp 不受影响
                Console.WriteLine(tmp); // ref 本地变量可以起到阻止对象被垃圾回收，直到最后一次引用 tmp
                Console.WriteLine(obj.value);
            }
        }

        class MixedVariables {
            private int writableField;
            private readonly int readonlyField;
            public void TryIncrementBoth() {
                ref int x = ref writableField;
                //ref int y = ref readonlyField; // 报错，只读不能被 ref 标记
                x++;
                //y++;
            }
        }
    }
}