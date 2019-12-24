using System;

namespace DesignPatternCore.Factory {
    public class WpsToPdfConvertor : FileConvertorBase, IPdfConvertor {
        public void ConverToPdf(string filePath) {
            Console.WriteLine("wps convert pdf success!");
        }

        protected override void FileConvert(string filePath) {
            Console.WriteLine($"{nameof(WpsToPdfConvertor)} execute file convert actually.");
            ConverToPdf(filePath);
        }
    }
}