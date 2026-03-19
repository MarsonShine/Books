namespace chapter_6
{
    public static partial class Extensions
    {
        extension<T>(IEnumerable<T> source)
        {
            public string Newlines() => string.Join("\n", source);

            public string Concat() => string.Join("", source);

            public string Space() => string.Join(" ", source);
        }
    }
}
