namespace chapter_3
{
    static class Extensions
    {
        extension<T>(IDeque<T> deque)
        {
            public IDeque<T> PushRightMany(IEnumerable<T> items) => items.Aggregate(deque, (d, item) => d.PushRight(item));
        }
    }
}
