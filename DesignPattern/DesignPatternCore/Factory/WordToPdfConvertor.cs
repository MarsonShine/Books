using System;

namespace DesignPatternCore.Factory {
    public class WordToPdfConvertor : IFileConvertor {
        public void Convert(string filePath) {
            Console.WriteLine("word convert pdf success!");
        }
    }
}