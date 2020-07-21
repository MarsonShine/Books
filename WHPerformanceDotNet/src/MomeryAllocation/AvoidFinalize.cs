using System;
using System.Collections.Generic;
using System.Text;

namespace MomeryAllocation
{
    /// <summary>
    /// 避免使用终结器，因为会使GC变慢
    /// 需要使用CPU来跟踪对象的生命周期
    /// </summary>
    public class AvoidFinalize : IDisposable
    {
        private bool disposed = false;
        private IntPtr handle;
        private IDisposable managedResource;

        ~AvoidFinalize()    // 析构器，终结器
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) return;
            if (disposing)
            {
                // 在终结器中这样做不安全
                this.managedResource.Dispose();
            }
            // 安全地清理非托管资源
            //UnsafeClose(this.handle);

            // 如果基类是一个 IDisposable 对象，要确保你调用的是 base.Dispose(disposing) 
            this.disposed = true;
        }
    }
}
