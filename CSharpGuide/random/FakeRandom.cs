using System;

namespace CSharpGuide.random {
    public class FakeRandom {
        public void Invoke() {
            var rd = new Random();
            var i = rd.Next();
            Console.Write($" {i} ");
        }
    }
}