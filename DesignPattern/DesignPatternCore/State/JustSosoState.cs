namespace DesignPatternCore.State {
    using System;
    public class JustSosoState : State {

        public override void Handle(Client client) {
            Console.WriteLine("一般般！");
        }
    }
}