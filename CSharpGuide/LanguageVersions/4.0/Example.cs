using System;
using System.Diagnostics;

namespace CSharpGuide.LanguageVersions.Four.Zero {
    public class Example {
        public void Start() {
            dynamic x = "marson shine";
            x = x.Substring(6);
            Console.WriteLine(x);

            var f = new Foo();
            Stopwatch sw = new Stopwatch();
            int n = 10000000;
            sw.Start();
            for (int i = 0; i < n; i++) {
                ReflectionMethod(f);
            }
            sw.Stop();
            Console.WriteLine("ReflectionMethod: " + sw.ElapsedMilliseconds + " ms");

            sw.Restart();
            for (int i = 0; i < n; i++) {
                DynamicMethod(f);
            }
            sw.Stop();
            Console.WriteLine("DynamicMethod: " + sw.ElapsedMilliseconds + " ms");
        }

        public void DynamicMethod(Foo f) {
            dynamic d = f;
            d.DoSomething();
        }

        public void ReflectionMethod(Foo f) {
            var m = typeof(Foo).GetMethod("DoSomething");
            m?.Invoke(f, null);
        }

        public void SomeMethod(dynamic d) { }
        //public void SomeMethod(object o) { } error
    }

    public class Foo {
        public void DoSomething() {

        }
    }
}