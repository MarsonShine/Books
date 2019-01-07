using System;
using System.Net;
using System.Threading.Tasks;

namespace AsyncPatternInNet {
    public class WriteAsyncMethod {
        private const string domain = "marsonshine.com";
        private async void DumpWebPageAsync(string uri) {
            WebClient client = new WebClient();
            string page = await client.DownloadStringTaskAsync(uri);
            Console.WriteLine(page);
        }
        private async void AddFavicon(string uri) {
            WebClient client = new WebClient();
            byte[] bytes = await client.DownloadDataTaskAsync("http://" + domain + "/favicon.ico");
            // Image imageControl = MakeImageControl(bytes);
            // m_WrapPanel.Children.Add(imageControl);
        }

        private async Task StartMultipleTaskThenAwait() {
            WebClient client = new WebClient();
            Task<string> firstTask = client.DownloadStringTaskAsync("http://oreilly.com");
            Task<string> secondTask = client.DownloadStringTaskAsync("http://someelse.com");
            string firstPage = await firstTask;
            string secondPage = await secondTask;
        }
    }
}