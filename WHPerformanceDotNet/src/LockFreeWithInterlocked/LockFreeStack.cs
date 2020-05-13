using System.Threading;

namespace LockFreeWithInterlocked {
    public class LockFreeStack<T> {
        private class Node {
            public T Value;
            public Node Next;
        }

        private Node head;
        public void Push(T value) {
            var newNode = new Node() { Value = value };

            while (true) {
                newNode.Next = this.head;
                if (Interlocked.CompareExchange(ref this.head, newNode, newNode.Next) == newNode.Next) {
                    return;
                }
            }
        }

        public T Pop() {
            while (true) {
                Node node = this.head;
                if (node == null) {
                    return default(T);
                }
                if (Interlocked.CompareExchange(ref this.head, node.Next, node) == node) {
                    return node.Value;
                }
            }
        }
    }
}