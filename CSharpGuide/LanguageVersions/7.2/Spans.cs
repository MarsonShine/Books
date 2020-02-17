namespace CSharpGuide.LanguageVersions.Seven.Two {
    /// <summary>
    /// Span<T> 表示一段连续内存的对象，因为是 ref struct 类型，所以该对象只能分配在栈上
    /// 对Span<T>对象进行拆分，copy操作是不会发生对值复制的消耗，而是直接返回原来对象的引用地址
    /// </summary>
    public class Spans {
        public ReadOnlySpan<char> GetLastNameWithSpan(ReadOnlySpan<char> fullName) {
            var lastSpaceIndex = fullName.LastIndexOf(' ');
            return lastSpaceIndex == -1 ?
                ReadOnlySpan<char>.Empty :
                fullName.Slice(lastSpaceIndex + 1);
        }
    }
}