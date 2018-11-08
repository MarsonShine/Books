using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;

namespace _05ReactiveExtensions {
    class Program {
        static void Main (string[] args) {
            Console.WriteLine ("Hello World!");
        }

        public static void ObservableException () {
            var client = new WebClient ();
            var donwloadedStrings = Observable.FromEventPattern (client, "DownloadStringCompleted");
            donwloadedStrings.Subscribe (
                data => {
                    var eventArgs = (DownloadStringCompletedEventArgs) data.EventArgs;
                    if (eventArgs.Error != null)
                        Trace.WriteLine ("OnNext:(Error) " + eventArgs);
                    else
                        Trace.WriteLine ("OnNext:" + eventArgs.Result);
                },
                ex => Trace.WriteLine ("OnError:" + ex),
                () => Trace.WriteLine ("OnCompleted")
            );
        }
    }
}