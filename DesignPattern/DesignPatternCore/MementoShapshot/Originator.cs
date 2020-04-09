using System;

namespace DesignPatternCore.MementoShapshot {
    public class Originator {
        public string State { get; set; }

        // 创建备忘录
        public Memento CreateMemento() {
            return new Memento(State);
        }
        // 恢复
        public void SetMemento(Memento memento) {
            State = memento.State;
        }

        public void Show() {
            Console.WriteLine("State = " + State);
        }
    }
}