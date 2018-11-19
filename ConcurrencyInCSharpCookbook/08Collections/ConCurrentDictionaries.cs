using System.Collections.Concurrent;

namespace _08Collections {
    /// <summary>
    /// 线程安全字典集合
    /// ConCurrentDictionary,ConCurrentStack,ConCurrentBag,ConCurrentQueue
    /// </summary>
    public class ConCurrentDictionaries {
        public static void Start() {
            var condictionary = new ConcurrentDictionary<int, string>();
            var newValue = condictionary.AddOrUpdate(
                0,
                key => "Zero",
                (key, oldValue) => "Zero");
        }
    }
}