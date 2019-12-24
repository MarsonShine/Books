using System;

namespace DesignPatternCore.Strategy {
    public class WordToPdfConvertor : IFileConvertorStrategy {
        public void Convert(string filePath) {
            Console.WriteLine("strategy:word to pdf success!");
        }
    }
}