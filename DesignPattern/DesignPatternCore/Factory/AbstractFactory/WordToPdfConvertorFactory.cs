namespace DesignPatternCore.Factory.AbstractFactory {
    public class WordToPdfConvertorFactory : IConvertorFactory {
        public IFileConvertor Create() => new WordToPdfConvertor();
    }
}