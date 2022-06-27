using Demo.NonFunctionals;

namespace Demo.Functionals
{
    using static System.Linq.Enumerable;
    public static class ListFormatter
    {
        public static List<string> Format(List<string> list) {
            var left = list.Select(StringExt.ToSentenceCase);
            var right = Range(1, list.Count);
            var zipped = Enumerable.Zip(left, right, (s, i) => $"{i}. {s}");
            return zipped.ToList();
        }
        // 纯函数更容易也天生适合并行调用
        public static List<string> ParallelFormat(List<string> list) => 
            list.AsParallel()
            .Select(StringExt.ToSentenceCase)
            .Zip(Range(1, list.Count), (s, i) => $"{i}. {s}")
            .ToList();
    }
}