namespace chapter_3
{
    public interface IDeque<T> : IEnumerable<T>
    {
        IDeque<T> PushLeft(T item);
        IDeque<T> PushRight(T item);
        IDeque<T> PopLeft();
        IDeque<T> PopRight();
        T Left();
        T Right();
        bool IsEmpty();
        IDeque<T> Concatenate(IDeque<T> items);
    }
}
