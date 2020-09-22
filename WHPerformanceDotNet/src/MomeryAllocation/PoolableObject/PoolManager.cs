using System;
using System.Collections.Generic;

namespace MomeryAllocation.PoolableObject {
    public class PoolManager {
        private class Pool {
            public int PooledSize { get; set; }
            public int Count { get => this.Stack.Count; }
            public Stack<IPoolableObject> Stack { get; private set; }
            public Pool() {
                this.Stack = new Stack<IPoolableObject>();
            }
        }

        const int MaxSizePerType = 10 * (1 << 10); // 10MB
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
            Pool pool;
            T valueToReturn = null;
            if (pools.TryGetValue(typeof(T), out pool)) {
                if (pool.Stack.Count > 0) {
                    valueToReturn = pool.Stack.Pop() as T;
                }
            }
            if (valueToReturn == null) {
                valueToReturn = new T();
            }
            valueToReturn.SetPoolManager(this);
            pool.PooledSize -= valueToReturn.Size;
            return valueToReturn;
        }
    }
}