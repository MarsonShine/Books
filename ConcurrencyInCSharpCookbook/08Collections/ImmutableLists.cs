using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace _08Collections {
    /// <summary>
    /// 不可变列表支持索引，不经常修改，可以被多个线程同时访问
    /// 不可变列表不能完全替代 List，在不同的场合使用不同的数据结构
    /// 不可变列表内部是用二叉树实现的，所以时间复杂度很稳定，O(lgnN)
    /// List 集合和索引查找都是O(1)，删除和新增是O(n)
    /// 意味着遍历 ImmutableList 要尽量用 foreach 时间复杂度是 log(N)，而 for 循环则是 N * log(N)
    /// </summary>
    public class ImmutableLists {
        public static void ForeachCompareFor() {
            var lists = new List<string>(10000000);
            for (int i = 0; i < 10000000; i++) {
                lists.Add("name" + i);
            }
            ImmutableList<string> immutableLists = lists.ToImmutableList();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var item in immutableLists) {
                var ret = 1 + item;
            }
            sw.Stop();
            Console.WriteLine("immutablelist foreach time:" + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
            for (int i = 0; i < immutableLists.Count; i++) {
                var ret = 1 + immutableLists[i];
            }
            sw.Stop();
            Console.WriteLine("immutablelist for time:" + sw.ElapsedMilliseconds + "ms");
        }
        //快速构建一个不可变集合
        public static void ImmutableListBuilder() {
            var builder = ImmutableList.CreateBuilder<string>();
        }
    }
}