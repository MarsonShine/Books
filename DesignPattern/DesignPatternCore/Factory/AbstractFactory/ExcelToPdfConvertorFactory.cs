namespace DesignPatternCore.Factory.AbstractFactory {
    public class ExcelToPdfConvertorFactory : IConvertorFactory {
        public IFileConvertor Create() => new ExcelToPdfConvertor();
    }
}