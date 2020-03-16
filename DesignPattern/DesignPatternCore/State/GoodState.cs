namespace DesignPatternCore.State {
    using System;
    public class GoodState : State {
        public override void Handle(Client client) {
            Console.WriteLine("优良！");
            client.State = new JustSosoState();
        }
    }
}