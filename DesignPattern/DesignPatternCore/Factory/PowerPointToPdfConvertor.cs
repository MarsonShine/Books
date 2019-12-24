using System;

namespace DesignPatternCore.Factory {
    public class PowerPointToPdfConvertor : IFileConvertor {
        public void Convert(string filePath) {
            Console.WriteLine("powerpoint convert pdf success!");
        }
    }
}