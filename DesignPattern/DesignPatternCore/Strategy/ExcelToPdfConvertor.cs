using System;

namespace DesignPatternCore.Strategy {
    public class ExcelToPdfConvertor : IFileConvertorStrategy {
        public void Convert(string filePath) {
            Console.WriteLine("strategy:excel to pdf success!");
        }
    }
}