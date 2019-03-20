using System.Collections.Immutable;

namespace _08Collections {
    //存放不重复，多个线程同时访问，很少改变的集合
    //不支持排序，不支持索引查询 时间复杂度为 log(N)
    //ImmutableSortedSet 支持排序，索引查询 时间复杂度为 log(N)
    //遍历不可变Set 同样尽量用 foreach 而不是 for
    public class ImmutableSets {
        public static void ImmutableHashSetAndSortedSet() {
            var sets = ImmutableHashSet<string>.Empty;
            var sotedSets = ImmutableSortedSet<string>.Empty;
        }
    }
}