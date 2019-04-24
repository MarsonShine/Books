namespace _11Sync {
    /// <summary>
    /// 阻塞锁
    /// 多个线程需要读写共享资源
    /// </summary>
    public class BlockLock {
        /// <summary>
        /// 这个锁会保护<see>value</see>
        /// </summary>
        /// <returns></returns>
        private readonly object _mutex = new object();
        private int value;

        public void Increment() {
            /// <summary>
            /// lock 满足绝大多数情况，只有极少数情况才会用到其他类型锁
            /// </summary>
            /// <value></value>
            lock(_mutex) {
                value++;
            }
        }
    }
}