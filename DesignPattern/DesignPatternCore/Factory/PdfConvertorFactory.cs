using System;

namespace DesignPatternCore.Factory {
    public class PdfConvertorFactory {
        public static IFileConvertor Create(FileType fileType) {
            switch (fileType) {
                case FileType.Word:
                    return new WordToPdfConvertor();
                case FileType.Excel:
                    return new ExcelToPdfConvertor();
                case FileType.PowerPoint:
                    return new PowerPointToPdfConvertor();
                case FileType.Wps:
                    return new WpsToPdfConvertor();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}