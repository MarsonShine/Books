using System;
using System.Threading.Tasks;

namespace _07Interoperate {
    public static class UseAsyncModifierEncapsulateAnything {
        public static Task<string> DownloadStringAsync(this IMyAsyncHttpService service, Uri address) {
            var tcs = new TaskCompletionSource<string>();
            service.DownloadString(address, (result, ex) => {
                if (ex != null)
                    tcs.SetException(ex);
                else
                    tcs.SetResult(result);
            });
            return tcs.Task;
        }
    }
    //非标准异步编程模式（AMP，EAP）
    public interface IMyAsyncHttpService {
        void DownloadString(Uri address, Action<string, Exception> callback);
    }
}