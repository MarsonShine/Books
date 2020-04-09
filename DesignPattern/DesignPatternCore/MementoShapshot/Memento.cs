namespace DesignPatternCore.MementoShapshot {
    public class Memento {
        private readonly string state;
        public Memento(string state) {
            this.state = state;
        }

        public string State => state;
    }
}