namespace Demo.Examples._8.Immutables
{
    public sealed class List<T>
    {
        readonly bool isEmpty;
        readonly T head;
        readonly List<T> tail;

        internal List() { isEmpty = true; }

        internal List(T head, List<T> tail)
        {
            this.head = head;
            this.tail = tail;
        }

        public R Match<R>(Func<R> empty, Func<T, List<T>, R> cons) => isEmpty ? empty() : cons(head, tail);
    }

    public static class LinkedList
    {
        public static List<T> List<T>(T h, List<T> t) => new List<T>(h, t);
        public static List<T> List<T>(params T[] items) => items.Reverse().Aggregate(new List<T>(), (acc, t) => List(t, acc));
    }

    public static class ListExt
    {
        public static int Length<T>(this List<T> list) => list.Match(
            empty: () => 0,
            cons: (head, tail) => 1 + tail.Length());
    }
}
