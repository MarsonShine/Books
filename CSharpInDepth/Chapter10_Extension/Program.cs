using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Chapter10_Extension {
    class Program {
        static void Main (string[] args) {
            WebRequest request = WebRequest.Create ("http://www.baidu.com");
            using (WebResponse response = request.GetResponse ())
            using (Stream responseStream = response.GetResponseStream ())
            using (FileStream output = File.Create ("response.dat")) {
                // StreamUtil.Copy(responseStream,output);
                responseStream.Copy (output);
            }

            Console.WriteLine ("Hello World!");

            Select ();
        }

        static void Select () {
            var collection = Enumerable.Range (0, 10)
                .Where (x => x % 2 != 0)
                .Reverse ()
                .Select (x => new { Original = x, SquareRoot = Math.Sqrt (x) });
            foreach (var element in collection) {
                Console.WriteLine ($"sqrt({element.Original}) = {element.SquareRoot}");
            }
        }

        static void OrderBy () {
            var collection = Enumerable.Range (-5, 11)
                .Select (x => new { Original = x, Square = x * x })
                .OrderBy (x => x.Square)
                .ThenBy (x => x.Original);
            foreach (var element in collection) {
                Console.WriteLine (element);
            }
        }
    }
}