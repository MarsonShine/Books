namespace Chapter13_HighPerformancePassByReference {
    using System.Collections;
    using System;
    /*
    返回类型前加 ref 标识符，表示返回的是一个引用，而不是值。
    一般是通过 ref 本地变量来接受 ref return
    ref return 有效的场景：
    ref 或 or 标记的参数；
    引用类型参数；
    结构变量是 ref 或 out 参数的结构字段；
    数组元素；
    无效的场景：
    在方法中申明的本地变量；
    在方法中申明的结构变量字段；
    */
    public class RefReturn {
        public static void Start() {
            int x = 10;
            ref int y = ref RefReturnType(ref x);
            y++;
            Console.WriteLine(x);

            // 直接操作 ref return
            RefReturnType(ref x) ++;
            Console.WriteLine(x);

            // 更极端的例子
            RefReturnType(ref RefReturnType(ref RefReturnType(ref x))) ++;
            Console.WriteLine(x);
        }

        static ref int RefReturnType(ref int p) {
            return ref p;
        }
    }

    // 通过 ref return 返回数组元素的引用
    public class ArrayHelper {
        private readonly int[] array = new int[10];
        public ref int this [int index] => ref array[index]; // 索引返回数组元素引用
        public static void Start() {
            ArrayHelper holder = new ArrayHelper();
            ref int x = ref holder[0]; // 申明两个 ref 本地变量引用相同的数组元素
            ref int y = ref holder[0];

            x = 20;
            Console.WriteLine(y);
        }
    }
}