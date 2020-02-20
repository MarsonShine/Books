using System;

namespace CSharpGuide.LanguageVersions.Four.Zero {
    public class Example {
        public void Start() {
            dynamic x = "marson shine";
            x = x.Substring(6);
            Console.WriteLine(x);
        }

        public void SomeMethod(dynamic d) { }
        //public void SomeMethod(object o) { } error
    }
}