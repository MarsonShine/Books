using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/// <summary>
/// doc form https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/weak-references
/// 一个对象被应用程序引用时，垃圾回收器无法收集这个对象。
/// weakreference 允许垃圾回收器对其应用程序引用的对象回收。
/// 弱引用在对象占用大量内存时特别有用，垃圾回收器回收了这些对象，但是能很容易的重新生成这些对象。
/// </summary>
namespace MomeryAllocation.ObjectCache
{
    /// <summary>
    /// <see cref="WeakReference{T}"/> 实现对象缓存，避免内存分配的开销的例子
    ///  内部实现了两种缓存
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class HybirdCache<TKey, TValue>
        where TValue : class
    {
        class ValueContainer<T>
        {
            public T value;
            public long additionTime;
            public long demoteTime;
        }

        private readonly TimeSpan maxAgeBeforeDemotion;
        // Values 生存在这里，直到触发它们的 maxAge 值
        private readonly ConcurrentDictionary<TKey, ValueContainer<TValue>> strongReferences =
            new ConcurrentDictionary<TKey, ValueContainer<TValue>>();
        // Values 在触发 maxAge 值移除到这里
        private readonly ConcurrentDictionary<TKey, WeakReference<ValueContainer<TValue>>> weakReferences =
            new ConcurrentDictionary<TKey, WeakReference<ValueContainer<TValue>>>();

        public int Count => this.strongReferences.Count;
        public int WeakCount => this.weakReferences.Count;

        public HybirdCache(TimeSpan maxAgeBeforeDemotion)
        {
            this.maxAgeBeforeDemotion = maxAgeBeforeDemotion;
        }

        public void Add(TKey key,TValue value)
        {
            RemoveFromWeak(key);
            var container = new ValueContainer<TValue>
            {
                value = value,
                additionTime = Stopwatch.GetTimestamp(),
                demoteTime = 0
            };
            this.strongReferences.AddOrUpdate(key, container, (key, value) => container);
        }

        private void RemoveFromWeak(TKey key)
        {
            weakReferences.TryRemove(key, out _);
        }

        public bool TryGetValue(TKey key,out TValue value)
        {
            value = null;
            if(this.strongReferences.TryGetValue(key,out var container))
            {
                AttemptADemotion(key, container);
                value = container.value;
                return true;
            }
            if(this.weakReferences.TryGetValue(key,out var weakContainer))
            {
                if(weakContainer.TryGetTarget(out var valueContainer))
                {
                    value = valueContainer.value;
                    return true;
                }
                else
                {
                    RemoveFromWeak(key);
                }
            }
            return false;
        }

        private void AttemptADemotion(TKey key, ValueContainer<TValue> container)
        {
            long now = Stopwatch.GetTimestamp();
            var age = CalculateTimeSpan(container.additionTime, now);
            if (age > this.maxAgeBeforeDemotion) {
                Demote(key, container);
            }
        }

        private TimeSpan CalculateTimeSpan(long offsetA, long offsetB)
        {
            long diff = offsetB - offsetA;
            double seconds = (double)diff / Stopwatch.Frequency;
            return TimeSpan.FromSeconds(seconds);
        }

        private void Demote(TKey key, ValueContainer<TValue> container)
        {
            this.strongReferences.TryRemove(key, out ValueContainer<TValue> oldContainer);
            container.demoteTime = Stopwatch.GetTimestamp();
            var weakRef = new WeakReference<ValueContainer<TValue>>(container);
            this.weakReferences.AddOrUpdate(key, weakRef, (k, v) => weakRef);
        }
    }
}
