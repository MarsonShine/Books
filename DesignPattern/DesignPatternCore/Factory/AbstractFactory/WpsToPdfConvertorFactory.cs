namespace DesignPatternCore.Factory.AbstractFactory {
    public class WpsToPdfConvertorFactory : IConvertorFactory {
        public IFileConvertor Create() => new WpsToPdfConvertor();
    }
}