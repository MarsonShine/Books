using System.Collections;

namespace chapter_2
{
    public interface IImStack<T> : IEnumerable<T>
    {
        IImStack<T> Push(T value);
        IImStack<T> Pop();
        T Peek();
        bool IsEmpty { get; }
    }

    public class ImStack<T> : IImStack<T>
    {
        private class EmptyStack: IImStack<T>
        {
            public EmptyStack() { }
            public IImStack<T> Push(T item) => new ImStack<T>(item, this);
            public IImStack<T> Pop() => throw new InvalidOperationException("Stack is empty.");
            public T Peek() => throw new InvalidOperationException("Stack is empty.");
            public bool IsEmpty => true;
            public IEnumerator<T> GetEnumerator()
            {
                yield break;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static IImStack<T> Empty { get; } = new EmptyStack();

        private readonly T item;
        private readonly IImStack<T> tail;

        private ImStack(T item, IImStack<T> tail) // #E
        {
            this.item = item;
            this.tail = tail;
        }

        public IImStack<T> Push(T item) => new ImStack<T>(item,
        this);
        public T Peek() => item;
        public IImStack<T> Pop() => tail;
        public bool IsEmpty => false;

        public IEnumerator<T> GetEnumerator()
        {
            for (IImStack<T> s = this; !s.IsEmpty; s = s.Pop())
                yield return s.Peek();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
