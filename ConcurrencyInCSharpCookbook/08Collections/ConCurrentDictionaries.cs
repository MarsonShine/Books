using System;
using System.Collections.Concurrent;

namespace _08Collections {
    /// <summary>
    /// 线程安全字典集合
    /// ConCurrentDictionary 虽然功能强大，但是并不是适用所有场合（也不能代替 Dictionary<TKey,TValue>）
    /// 如果多个线程读写这个共享集合，那么使用这个是最合适的。如果不会频繁修改，那么使用 ImmutableDictionay<TKey,TValue>
    /// 这些线程安全的集合都最合适在数据共享的场合（除了 ConCurrentDictionary,线程安全集合还有 ConCurrentStack,ConCurrentBag,ConCurrentQueue）它们一般不单独使用，一般都会用来实现生产者 / 消费者集合
    /// </summary>
    public class ConCurrentDictionaries {
        private readonly ConcurrentDictionary<int, string> m_ceche =
            new ConcurrentDictionary<int, string>();
        public static void Start() {
            var condictionary = new ConcurrentDictionary<int, string>();
            var newValue = condictionary.AddOrUpdate(
                0,
                key => "Zero",
                (key, oldValue) => "Zero");
        }
        /// <summary>
        /// AddOrUpdate 有三个参数，第一个键值
        /// 第二个委托是当键值不存在时触发的
        /// 第三个委托是当键值存在时触发，委托中有两个参数，第一个是键值，第二个是键值对应以前的 oldValue
        /// 返回的新的 value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void DeepInConcurrentDictionary(int key, string value) {
            var ret = m_ceche.AddOrUpdate(key,
                addValueFactory : k => {
                    Console.WriteLine("addValue = " + k);
                    return "Key" + k;
                },
                updateValueFactory: (k, v) => {
                    Console.WriteLine("Update : Key" + k + " Value=" + v);
                    return "Update k=" + k + " v=" + v;
                }
            );
            Console.WriteLine("AddOrUpdate 返回的结果 ret = " + ret);
        }
    }
}