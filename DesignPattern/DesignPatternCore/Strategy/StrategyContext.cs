namespace DesignPatternCore.Strategy {
    public class StrategyContext {
        IFileConvertorStrategy _strategy;
        public StrategyContext(IFileConvertorStrategy strategy) {
            _strategy = strategy;
        }

        public StrategyContext(Factory.FileType fileType) {

        }

        public void DoWork(string filePath) {
            _strategy.Convert(filePath);
        }
    }
}