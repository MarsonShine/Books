namespace Program;
public static class ListExtensions
{
    // 增加实例式扩展属性与方法
    extension<T>(List<T> list)
    {
        public bool IsEmpty => list.Count == 0;

        public void AddIfNotNull(T? item)
        {
            if (item is not null) list.Add(item);
        }
    }
    // 增加静态式扩展运算符
    extension<T>(List<T>)
    {
        public static List<T> operator +(List<T> left, List<T> right)
        {
            var result = new List<T>(left.Count + right.Count);
            result.AddRange(left);
            result.AddRange(right);
            return result;
        }
    }
}