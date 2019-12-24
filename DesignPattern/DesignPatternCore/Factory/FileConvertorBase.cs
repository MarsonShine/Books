using System;

namespace DesignPatternCore.Factory {
    public abstract class FileConvertorBase : IFileConvertor {
        protected abstract void FileConvert(string filePath);
        public void Convert(string filePath) {
            Console.WriteLine("do something before file convert...");
            FileConvert(filePath);
            Console.WriteLine("do something after file convert...");
        }
    }
}