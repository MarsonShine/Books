namespace Chapter13_HighPerformancePassByReference {
    using System;
    /*
    ref readonly 只处理两种场景：
    为了提高效率，可能需要为只读字段设置别名，以避免复制；
    可能需要通过 ref 变量来允许访问只读变量；
    如果您使用 ref readonly 返回调用一个方法或索引器，并希望将结果存储在一个局部变量中，那么它也必须是一个ref readonly 局部变量。
    */
    public class RefReadOnly {
        static readonly int field = DateTime.UtcNow.Second; // 初始化只读字段
        static ref readonly int GetFieldAlias() => ref field; // 返回该字段的只读变量别名
        public static void Start() {
            ref readonly int local = ref GetFieldAlias(); // 通过方法初始化只读的 ref 本地变量
            ref readonly int y = ref field;
            Console.WriteLine(local);
        }
    }

    // 通过 ref readonly 实现零拷贝读
    public class ReadOnlyArrayView<T> {
        private readonly T[] values;
        public ReadOnlyArrayView(T[] values) => this.values = values; // 复制数组引用，而不是值
        public ref readonly T this [int index] => ref values[index]; // 返回数组元素的只读别名

        public static void Start() {
            var array = new int[] { 10, 20, 30 };
            var view = new ReadOnlyArrayView<int>(array);
            ref readonly int element = ref view[0];
            Console.WriteLine(element);
            array[0] = 100;
            Console.WriteLine(element);
        }
    }
}