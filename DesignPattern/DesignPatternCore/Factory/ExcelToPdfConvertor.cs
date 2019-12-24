using System;

namespace DesignPatternCore.Factory {
    public class ExcelToPdfConvertor : IFileConvertor {
        public void Convert(string filePath) {
            Console.WriteLine("excel convert to pdf success!");
        }
    }
}