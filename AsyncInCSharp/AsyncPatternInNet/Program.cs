using System;
using System.Net;
using System.Threading.Tasks;

namespace AsyncPatternInNet {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }

        private void DumpWebPage(Uri uri) {
            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += OnDownloadStringCompleted;
            webClient.DownloadStringAsync(uri);
        }

        private void OnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
            Console.WriteLine(e.Result);
        }

        private void LookupHostName() {
            object unrelatedObject = "hello";
            Dns.BeginGetHostAddresses("oreilly.com", OnHostNameResolved, unrelatedObject);
        }

        private void OnHostNameResolved(IAsyncResult ar) {
            object unrelatedObject = ar.AsyncState;
            IPAddress[] addresses = Dns.EndGetHostAddresses(ar);
            //Do something with addresses
        }

        private void LookupHostNameByLambda() {
            // int aUsefulVariable = 3;
            // GetHostAddress("oreilly.com", address => {
            //     //Do something with address and aUsefulVariable
            // });
        }

        private void LookupHostNameByTask() {
            Task<IPAddress[]> iPAddressesPromis = Dns.GetHostAddressesAsync("oreilly.com");
            iPAddressesPromis.ContinueWith(_ => {
                IPAddress[] iPAddresses = iPAddressesPromis.Result;
                //do something with address
            });
        }

        private void LookupHostNames(string[] hostNames) {
            LookupHostNamesHelper(hostNames, 0);
        }

        private void LookupHostNamesHelper(string[] hostNames, int i) {
            Task<IPAddress[]> iPAddressesPromise = Dns.GetHostAddressesAsync(hostNames[i]);
            iPAddressesPromise.ContinueWith(_ => {
                IPAddress[] iPAddresses = iPAddressesPromise.Result;
                //do something with address
                if (i + 1 < hostNames.Length) {
                    LookupHostNamesHelper(hostNames, i + 1);
                }
            });
        }

        private void AddFavicon(string domain) {
            WebClient webclient = new WebClient();
            webclient.DownloadDataCompleted += OnWebClientOnDownloadDataCompleted;
            webclient.DownloadDataAsync(new Uri("http://" + domain + "/favicon.ico"));
        }

        private void OnWebClientOnDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) {
            var bytes = e.Result; //favicon 字节流
            Console.WriteLine(Convert.ToBase64String(bytes));
        }
    }
}