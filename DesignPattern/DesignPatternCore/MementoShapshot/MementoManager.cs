namespace DesignPatternCore.MementoShapshot {
    public class MementoManager {
        private Memento _memento;
        public MementoManager() {

        }

        public void Record(Memento memento) {
            _memento = memento;
        }

        public Memento Memento => _memento;
    }
}