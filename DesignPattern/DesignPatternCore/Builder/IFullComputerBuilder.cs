namespace DesignPatternCore.Builder {
    public interface IFullComputerBuilder : IFullComputer {
        IFullComputer Create();
    }
}