using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AwaitWhatActuallyToDo {
    /// <summary>
    /// 什么情况下不能使用 await
    /// </summary>
    public class WhereAwaitCanNotBeUsed {
        private const string URL = "https://www.baidu.com";
        private static readonly object mutex = new object();
        /// <summary>
        /// 不能在catch与finally语句块使用await
        /// </summary>
        public async Task NotUseInCatchOrFinallyBlock() {
            var webClient = new WebClient();
            try {
                var data = await webClient.DownloadDataTaskAsync(URL);
            } catch (Exception e) {
                var data = await webClient.DownloadDataTaskAsync(URL);
            }
        }
        //正确的使用方式
        public async Task CorrectWay() {
            bool failed = false;
            var webClient = new WebClient();
            try {
                var data = await webClient.DownloadDataTaskAsync(URL);
            } catch (Exception e) {
                failed = false;
            }
            if (failed) {
                var data = await webClient.DownloadDataTaskAsync(URL);
            }
        }
        //在lock语句块中不需要await
        public async Task NotUseInLockBlock() {
            lock(mutex) {
                // Prepare for async operation
            }
            await CorrectWay();
            lock(mutex) {
                // Use result of async operation
            }
        }

        public async Task NotUseInLinq() {
            var dataSource = new int[4] { 1, 2, 3, 4 };
            // IEnumerable<int> transformed = from x in dataSource  //linq 查询表达式会被编译器转换成lambda表达式，但是lambda不支持隐式的标记async，所以不能用await
            // where x != 9
            // select x + 2;
            IEnumerable<Task<int>> tasks = dataSource
                .Where(x => x != 9)
                .Select(async x => await DoSomthingAsync(x) + await DoSomthingElseAsync(x));
            IEnumerable<int> transformed = await Task.WhenAll(tasks);
        }

        private async Task<int> DoSomthingElseAsync(int x) {
            return await Task.FromResult(x);
        }

        private async Task<int> DoSomthingAsync(int x) {
            return await Task.FromResult(x);
        }
    }
}