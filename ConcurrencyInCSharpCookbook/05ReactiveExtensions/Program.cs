using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using System.Timers;

namespace _05ReactiveExtensions {
    class Program {
        static void Main(string[] args) {
            ObservableException();
            Console.WriteLine("Hello World!");
        }

        public static void ObservableException() {
            var client = new WebClient();
            var donwloadedStrings = Observable.FromEventPattern(client, "DownloadStringCompleted");
            donwloadedStrings.Subscribe(
                data => {
                    var eventArgs = (DownloadStringCompletedEventArgs) data.EventArgs;
                    if (eventArgs.Error != null)
                        Trace.WriteLine("OnNext:(Error) " + eventArgs);
                    else
                        Trace.WriteLine("OnNext:" + eventArgs.Result);
                },
                ex => Trace.WriteLine("OnError:" + ex),
                () => Trace.WriteLine("OnCompleted")
            );
            client.DownloadStringAsync(new Uri("http://invalid.example.com/"));
        }

        public static void EncapsulateFromStandardEventHandler() {
            var progress = new Progress<int>();
            var progressReports = Observable.FromEventPattern<int>(
                addHandler: handler => progress.ProgressChanged += handler,
                removeHandler: handler => progress.ProgressChanged -= handler
            );
            progressReports.Subscribe(onNext: data => Trace.WriteLine("OnNext:" + data.EventArgs));
        }

        public static void EncapsulateFromNotStandardEventHandler() {
            var timer = new System.Timers.Timer(interval : 1000) { Enabled = true };
            var ticks = Observable.FromEventPattern<ElapsedEventHandler, ElapsedEventArgs>(
                    handler =>(s, a) => handler(s, a),
                    addHandler : handler => timer.Elapsed += handler,
                    removeHandler : handler => timer.Elapsed -= handler
                );
            ticks.Subscribe(onNext : data => Trace.WriteLine("OnNext:" + data.EventArgs.SignalTime));
        }
        public static void EncapsulateFromNotStandardEventHandlerByReflection() {
            var timer = new System.Timers.Timer(interval : 1000) { Enabled = true };
            var ticks = Observable.FromEventPattern(timer, "Elapsed");
            ticks.Subscribe(data => Trace.WriteLine("OnNext:" + ((ElapsedEventArgs) data.EventArgs).SignalTime));
        }
    }
}