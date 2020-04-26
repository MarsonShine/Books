using System;

/// <summary>
/// 池化对象，一个比较合理的高质量池化对象
/// 就是继承一个 IDisposable 接口，在 Dispose 方法中将池化对象归还给共享池（Pool）
/// </summary>
namespace GCPool {
    public interface IPoolableObject : IDisposable {
        int Size { get; }
        void Reset();
        void SetPoolManager(PoolManager poolManager);
    }
}