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
            int aUsefulVariable = 3;
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
    }
}