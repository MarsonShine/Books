namespace DesignPatternCore.State {
    public class Client {
        public Client(State state) {
            this.State = state;
        }
        public State State { get; set; }

        public void Handle() {
            this.State.Handle(this);
        }
    }
}