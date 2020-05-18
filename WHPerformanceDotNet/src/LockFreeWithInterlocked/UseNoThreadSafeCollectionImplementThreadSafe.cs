using System.Collections.Generic;

namespace LockFreeWithInterlocked {
    /// <summary>
    /// 用一般非线程安全集合替代哪些线程安全的集合
    /// 来达到高性能的目的
    /// </summary>
    public class UseNoThreadSafeCollectionImplementThreadSafe {
        private volatile Dictionary<string, object> data = new Dictionary<string, object>();

        public Dictionary<string, object> Data => data;

        private void UpdateData() {
            var newData = new Dictionary<string, object>();
            newData["Foo"] = new { };
            data = newData;
        }
    }
}