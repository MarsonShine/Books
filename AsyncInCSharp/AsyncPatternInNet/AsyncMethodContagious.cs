using System.Net;
using System.Threading.Tasks;

namespace AsyncPatternInNet {
    /// <summary>
    /// 异步方法具有传染性
    /// </summary>
    public class AsyncMethodContagious {
        private async Task<int> GetPageSizeAsync(string url) {
            WebClient webClient = new WebClient();
            string page = await webClient.DownloadStringTaskAsync(url);
            return page.Length;
        }

        private async Task<string> FindLargestWebPage(string[] urls) {
            string largest = null;
            int largestSize = 0;
            foreach (string url in urls) {
                int size = await GetPageSizeAsync(url);
                if (size > largestSize) {
                    size = largestSize;
                    largest = url;
                }
            }
            return largest;
        }
    }
}