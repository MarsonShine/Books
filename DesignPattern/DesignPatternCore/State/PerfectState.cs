namespace DesignPatternCore.State {
    using System;
    public class PerfectState : State {
        public PerfectState() { }

        public override void Handle(Client client) {
            Console.WriteLine("完美！");
            client.State = new GoodState();
        }
    }
}