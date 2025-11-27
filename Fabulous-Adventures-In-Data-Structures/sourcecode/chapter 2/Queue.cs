using System.Collections;

namespace chapter_2
{
    public class Queue<T> : IEnumerable<T>
    {
        private IImQueue<T> q = ImQueue<T>.Empty;

        public Queue() { }
        public Queue(IImQueue<T> queue)
        {
            q = queue;
        }

        public void Enqueue(T item)
        {
            q = q.Enqueue(item);
        }

        public T Dequeue()
        {
            T item = Peek();
            q = q.Dequeue();
            return item;
        }

        public T Peek() => q.Peek();

        public bool IsEmpty => q.IsEmpty;

        public IImQueue<T> Freeze() => q;

        public IEnumerator<T> GetEnumerator() => q.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
