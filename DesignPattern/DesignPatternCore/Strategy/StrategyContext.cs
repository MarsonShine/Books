namespace DesignPatternCore.Strategy {
    public class StrategyContext {
        IFileConvertorStrategy _strategy;
        public StrategyContext(IFileConvertorStrategy strategy) {
            _strategy = strategy;
        }

        public void DoWork(string filePath) {
            _strategy.Convert(filePath);
        }
    }
}