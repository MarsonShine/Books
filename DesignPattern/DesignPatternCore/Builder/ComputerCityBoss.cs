namespace DesignPatternCore.Builder {
    public class ComputerCityBoss {
        public IFullComputer TellMeThenReturnComputer(IFullComputerBuilder builder) {
            return builder.Create();
        }
    }
}