namespace DesignPatternCore.Factory.AbstractFactory {
    /// <summary>
    /// 抽象工厂模式
    /// </summary>
    public interface IConvertorFactory {
        IFileConvertor Create();
    }
}