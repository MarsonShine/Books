using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace chapter_6;

internal class Combinations
{
    public static void SampleCode()
    {
        //NextLexiCombination(5, [0, 2, 3]);
        NextLexiCombination(5, [0, 1, 2]);

        Console.WriteLine("Listing 6.15 sample code");
        List<string> foods = ["ham", "jam", "spam", "lamb"];
        Console.WriteLine(foods.Combine([1, 2]).Space());

        Console.WriteLine("Listing 6.16 sample code");
        foreach (var comb in Combinations1(6, 4))
            Console.Write(comb.Concat() + " ");
        Console.WriteLine();


        Console.WriteLine("Listing 6.19 sample code");
        Console.WriteLine(20.Choose(10));


        Console.WriteLine("Listing 6.20 sample code");
        Console.WriteLine(GetCombination(8, 6, 4).Concat());

        Console.WriteLine("Listing 6.21 sample code");
        foreach (var c in Combinations2(6, 4))
            Console.Write($"{c.GetCombNumber(6, 4),2}:{c.Concat()} ");
        Console.WriteLine();

        Console.WriteLine("Listing 6.22 sample code");
        foreach (var c in Combinations1(6, 4))
            Console.Write($"{c.GetCombNumber(),2}:{c.Concat()} ");
        Console.WriteLine();


    }

    /**
     * 从左到右，找一个“还能变大的位置”(这个非常重要)
     * 把它加 1
     * 左边的数全部重置为最小值
     * 这里的意图是“能不能给 c[i] 加 1 还能保持递增”。
     * 如果 i 不是最后一位：c[i] + 1 < c[i+1]
     * 意味着加 1 后仍然 小于右边，递增性不被破坏。
     * 例：[0,2,4]，看 i=1：2+1 < 4 ✅ 可增。
     * 如果 i 是最后一位：理论上应检查 c[i] < n-1（最后一位最大是 n-1）。
     * */
    static bool NextLexiCombination(int n, IList<int> c)
    {
        int k = c.Count; // 组合长度
        // 判断第 i 位还能不能加？
        bool Increasable(int i) =>
            i == k - 1 ? c[i] < k + 1 : c[i] + 1 < c[i + 1];

        // 找最左边第一个能增长的位置 i
        int i = 0;
        while (i < k && !Increasable(i))
            i += 1;
        if (i == k)
            return false;

        // 让 c[i] 加 1，并把左边重置为最小
        // 增长c[i]：让第 i 位变大一点点（“下一个”）
        c[i] += 1;
        // 左侧 0..i-1 全部变成最小值 0,1,2...
        // 目的是让整体尽可能“小”，这样才是“紧挨着的下一个”。
        for (int j = 0; j < i; j += 1)
            c[j] = j;
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="n"></param>
    /// <param name="k"></param>
    /// <returns>返回所有 n 选 k 的组合</returns>
    static IEnumerable<IList<int>> Combinations1(int n, int k)
    {
        var c = Enumerable.Range(0, k).ToArray();
        do
            yield return c.ToList();
        while (NextLexiCombination(n, c));
    }

    /*
     用递归生成组合
     要么不选第 n-1 个元素
     要么选第 n-1 个元素

n = 5, k = 3
不选 4 → C(4,3)
选 4 → C(4,2) + [4]
     */
    static IEnumerable<IDeque<int>> Combinations2(int n, int k)
    {
        if (k > n || k < 0)
            yield break;
        if (k == 0)
        {
            yield return Deque<int>.Empty;
            yield break;
        }
        foreach (var r in Combinations2(n - 1, k))
            yield return r;
        foreach (var r in Combinations2(n - 1, k - 1))
            yield return r.PushRight(n - 1); // 数字放到组合末尾
    }

    /**
     根据编号反推组合
    把组合当作排好序的，给定第 p 个组合，直接算出是哪一个组合
     */
    static IDeque<int> GetCombination(BigInteger p, int n, int k)
    {
        if (k > n || k == 0)
            return Deque<int>.Empty;
        var boundary = (n - 1).Choose(k);
        return p < boundary ?
            GetCombination(p, n - 1, k) :
            GetCombination(p - boundary, n - 1, k - 1).PushRight(n - 1);
    }
}

public static partial class Extensions
{
    extension<T>(IList<T> items)
    {
        public IList<T> Combine(IList<int> combination)
        {
            var result = new T[combination.Count];
            for (int i = 0; i < combination.Count; i += 1)
                result[i] = items[combination[i]];
            return result;
        }
    }

    extension(int n)
    {
        public BigInteger Choose(int k)
        {
            if (k > n) return 0;
            if (k > n / 2)
                k = n - k;
            BigInteger result = 1;
            for (int i = 1; i <= k; i += 1)
                result = result * (n - k + i) / i;  // Careful!
            return result;
        }
    }

    extension(IDeque<int> comb)
    {
        public BigInteger GetCombNumber(int n, int k) => comb.IsEmpty ?
            0 :
            comb.Right() == n - 1 ?
                (n - 1).Choose(k) + comb.PopRight().GetCombNumber(n - 1, k - 1) :
                comb.GetCombNumber(n - 1, k);
    }

    extension(IList<int> comb)
    {
        public BigInteger GetCombNumber()
        {
            BigInteger result = 0;
            int k = comb.Count;
            for (int i = 0; i < k; i += 1)
                result += comb[i].Choose(i + 1);
            return result;
        }
    }
}
