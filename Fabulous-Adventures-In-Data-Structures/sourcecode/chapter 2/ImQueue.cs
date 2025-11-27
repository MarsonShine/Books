using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace chapter_2
{
    static class ImmutableQueue
    {
        public static void SampleCode()
        {
            Console.WriteLine("An immutable queue");
            var q1 = ImQueue<int>.Empty;
            var q2 = q1.Enqueue(10);
            var q3 = q2.Enqueue(20);
            var q4 = q3.Enqueue(30);
            var q5 = q4.Dequeue();
            var q6 = q5.Dequeue();
            var q7 = q6.Dequeue();

            Console.WriteLine(q1.Bracket());
            Console.WriteLine(q2.Bracket());
            Console.WriteLine(q3.Bracket());
            Console.WriteLine(q4.Bracket());
            Console.WriteLine(q5.Bracket());
            Console.WriteLine(q6.Bracket());
            Console.WriteLine(q7.Bracket());


            // []
            // [10]
            // [10, 20]
            // [10, 20, 30]
            // [20, 30]
            // [30]
            // []
        }
    }

    // 不可变队列数据结构和接口
    public interface IImQueue<T> : IEnumerable<T>
    {
        IImQueue<T> Enqueue(T value);
        IImQueue<T> Dequeue();
        T Peek();
        bool IsEmpty { get; }
    }
    public class ImQueue<T> : IImQueue<T>
    {
        public static IImQueue<T> Empty { get; } = new ImQueue<T>(ImStack<T>.Empty, ImStack<T>.Empty);

        private readonly IImStack<T> enqueues;
        private readonly IImStack<T> dequeues;

        private ImQueue(IImStack<T> enqueues, IImStack<T> dequeues)
        {
            this.enqueues = enqueues;
            this.dequeues = dequeues;
        }

        public bool IsEmpty => dequeues.IsEmpty ;

        public IImQueue<T> Dequeue()
        {
            IImStack<T> newdeq = dequeues.Pop();
            if (!newdeq.IsEmpty)
                return new ImQueue<T>(enqueues, newdeq);
            if(enqueues.IsEmpty)
                return Empty;
            return new ImQueue<T>(ImStack<T>.Empty, enqueues.Reverse());
        }

        public IImQueue<T> Enqueue(T value) => IsEmpty ?
            new ImQueue<T>(enqueues, dequeues.Push(value)) :
            new ImQueue<T>(enqueues.Push(value), dequeues);

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in dequeues)
            {
                yield return item;
            }
            foreach (var item in enqueues.Reverse())
            {
                yield return item;
            }
        }

        public T Peek() => dequeues.Peek();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
