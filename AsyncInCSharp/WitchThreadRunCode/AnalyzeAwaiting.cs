using System;
using System.Net;
using System.Threading.Tasks;

namespace WitchThreadRunCode {
    /// <summary>
    /// await 具体做了什么操作
    /// </summary>
    public class AnalyzeAwaiting {
        private string _uri = "https://www.baidu.com";
        public async Task Button_Click() {
            Task<string> task = GetFaviconAsync(_uri);
            var content = await task;
            var i = 1;
            i++;
            Console.WriteLine(content);
        }

        private async Task<string> GetFaviconAsync(string uri) {
            var task = new WebClient().DownloadDataTaskAsync(uri);
            byte[] bytes = await task;
            return ReadBytes(bytes);
        }

        private string ReadBytes(byte[] bytes) {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}