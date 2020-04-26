using System;
using System.Collections.Generic;

namespace GCPool {
    public class PoolManager {
        private class Pool {
            public int PooledSize { get; set; }
            public int Count { get { return this.Stack.Count; } }
            public Stack<IPoolableObject> Stack { get; private set; }
            public Pool() {
                this.Stack = new Stack<IPoolableObject>();
            }
        }
        const int MaxSizePerType = 10 * (1 << 10); // 10 MB
        Dictionary<Type, Pool> pools = new Dictionary<Type, Pool>();
        public int TotalCount {
            get {
                int sum = 0;
                foreach (var pool in this.pools.Values) {
                    sum += pool.Count;
                }
                return sum;
            }
        }

        public T GetObject<T>()
        where T : class, IPoolableObject, new() {
            T valueToReturn = null;
            if (pools.TryGetValue(typeof(T), out Pool pool)) {
                if (pool.Stack.Count > 0) {
                    valueToReturn = pool.Stack.Pop() as T;
                }
            }
            if (valueToReturn == null) {
                valueToReturn = new T();
            }
            valueToReturn.SetPoolManager(this);
            return valueToReturn;
        }

        public void ReturnObject<T>(T value)
        where T : class, IPoolableObject, new() {
            if (!pools.TryGetValue(typeof(T), out Pool pool)) {
                pool = new Pool();
                pools[typeof(T)] = pool;
            }
            if (value.Size + pool.PooledSize < MaxSizePerType) {
                pool.PooledSize += value.Size;
                value.Reset();
                pool.Stack.Push(value);
            }
        }
    }
}