namespace GCPool {
    public class MyPoolObject : IPoolableObject {
        private PoolManager poolManager;
        public byte[] Data { get; set; }
        public int UsableLength { get; set; }
        public int Size => Data != null?Data.Length : 0;

        public void Dispose() {
            this.poolManager.ReturnObject(this);
        }

        public void Reset() {
            UsableLength = 0;
        }

        public void SetPoolManager(PoolManager poolManager) {
            this.poolManager = poolManager;
        }
    }
}