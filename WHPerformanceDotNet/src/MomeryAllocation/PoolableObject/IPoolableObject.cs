using System;

namespace MomeryAllocation.PoolableObject {
    public interface IPoolableObject : IDisposable {
        int Size { get; }
        void Reset();
        void SetPoolManager(PoolManager poolManager);
    }
}