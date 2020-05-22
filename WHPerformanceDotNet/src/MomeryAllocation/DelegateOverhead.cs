namespace MomeryAllocation {
    /// <summary>
    /// 委托对象开销
    /// </summary>
    public class DelegateOverhead {
        private delegate int MathOp(int x, int y);
        private static int Add(int x, int y) => x + y;
        private static int DoOperation(MathOp op, int x, int y) => op(x, y);

        public static void Start() {
            // 1
            for (int i = 0; i < 10; i++) {
                DoOperation(Add, 1, 2);
            }

            // 2
            MathOp op = Add;
            for (int i = 0; i < 10; i++) {
                DoOperation(op, 1, 2);
            }
        }
    }
}