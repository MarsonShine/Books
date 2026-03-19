using System.Numerics;

namespace chapter_6
{
    public class Permutations
    {
        /**
         * 获取“字典序”排列。就像查字典一样，按从小到大的顺序给排列编号，输入一个编号 p，算出对应的排列长什么样。
         */
        public static IEnumerable<int> GetLexiPermutation(int n, BigInteger p)
        {
            var remaining = new List<int>(Enumerable.Range(0, n));
            var result = new List<int>(n);
            var f = n.Factorial(); // 计算 n! (n的阶乘)，即总共有多少种排列
            for (int cur = n; cur > 0; cur -= 1)
            {
                f /= cur; // 计算 (n-1)!, (n-2)! ... 也就是每一组的大小
                var r = (int)(p / f); // 计算当前应该选备选列表里的第几个数字
                p %= f; // 更新 p，去掉已经确定的部分，准备计算下一位
                result.Add(remaining[r]);
                remaining.RemoveAt(r);
            }
            return result;
        }

        /**
         * 基于 Fisher-Yates 洗牌算法逻辑的排列生成。
         * 根据一个特定的数字 p 来决定“怎么洗”，从而生成一个确定的排列。+
         */
        static IList<int> GetFYPermutation(int n, BigInteger p)
        {
            var result = new List<int>(Enumerable.Range(0, n));
            var f = n.Factorial();
            for (int cur = n; cur > 0; cur -= 1)
            {
                f /= cur;
                var r = (int)(p / f);
                p %= f;
                result.Swap(n - cur, n - cur + r);
            }
            return result;
        }

        static IEnumerable<IEnumerable<int>> Changes(int n)
        {
            if (n == 0) // 递归终止条件
            {
                yield return [];
                yield break;
            }
            bool fromLeft = false; // 控制插入方向：一会儿从左往右插，一会儿从右往左
            // 递归：先获取 n-1 个元素的所有排列
            foreach (var perm in Changes(n - 1))
            {
                // 在当前的每一个排列中，把数字 n-1 插进去
                // fromLeft 决定了是从索引 0 遍历到 n，还是反过来
                for (int row = 0; row < n; row += 1)
                    yield return perm.InsertAt(fromLeft ? row : n - row - 1, n - 1);
                fromLeft = !fromLeft; // 下一次循环反转方向（蛇形填充）
            }
        }

        /**
         * Changes 的直接计算版本，不需要遍历前 n-1 个就能直接算出第 p 个排列
         */
        static IEnumerable<int> GetChange(int n, BigInteger p)
        {
            if (n == 0)
                return [];
            BigInteger column = p / n; // 算出 p 属于 n-1 排列中的哪一组
            IEnumerable<int> perm = GetChange(n - 1, column); // 递归去算 n-1 的那个排列基底
            bool fromLeft = column % 2 != 0; // 根据组号的奇偶性，判断当前是“从左往右”还是“从右往左”
            int row = (int)(p % n); // 算出具体插在哪个位置
            return perm.InsertAt(fromLeft ? row : n - row - 1, n - 1);
        }

        /**
         * 迭代式 Johnson-Trotter 算法 
         * 引入了“方向”和“活动整数(Mobile Integer)”的概念
         */
        static IEnumerable<IList<int>> EvensChanges(int n)
        {
            var perm = Enumerable.Range(0, n).ToArray();
            // Right == +1, left == -1
            // dirs 数组记录每个数字的移动方向：-1 代表向左，+1 代表向右
            // 初始所有数字都想向左移动
            var dirs = Enumerable.Repeat(-1, n).ToArray();

            // 局部函数：判断索引 i 处的数字是否是“活动的”
            // 活动的意思是：它箭头指向的那个邻居比它小
            bool IsMobile(int i)
            {
                int j = i + dirs[perm[i]]; // j 是它想去的位置
                // 检查 j 是否越界，且当前数字是否比目标位置的数字大
                return 0 <= j && j < n && perm[j] < perm[i];
            }

            // 局部函数：找到所有活动数字中最大的那个
            int? MaxMobile()
            {
                int max = -1;
                int? maxIndex = null;
                for (int i = 0; i < perm.Length; i += 1)
                {
                    if (IsMobile(i) && perm[i] > max)
                    {
                        maxIndex = i;
                        max = perm[i];
                    }
                }
                return maxIndex;
            }

            while (true)
            {
                yield return perm.ToList(); // 返回当前排列
                // 1. 找最大的活动整数
                int? maxIndex = MaxMobile();
                if (maxIndex == null) // 如果找不到活动整数，说明所有排列生成完毕
                    yield break;
                int mi = maxIndex.Value;
                int m = perm[mi];
                // 2. 交换：把这个最大的活动整数向它指向的方向移动一步
                perm.Swap(mi, mi + dirs[m]);
                // 3. 更新方向：所有比刚才移动的那个数(m)大的数，方向都要反转
                for (int k = m + 1; k < n; k += 1)
                    dirs[k] = -dirs[k];
            }
        }
    }

    public static partial class Extensions
    {
        extension<T>(IList<T> items)
        {
            public IList<T> Permute(IList<int> permutation)
            {
                if (items.Count != permutation.Count)
                    throw new InvalidOperationException();
                var result = new T[items.Count];
                for (int i = 0; i < items.Count; i += 1)
                    result[i] = items[permutation[i]];
                return result;
            }

            public void Swap(int x, int y) => (items[x], items[y]) = (items[y], items[x]);
        }

        extension<T>(IEnumerable<T> items)
        {
            public IEnumerable<T> InsertAt(int index, T insert)
            {
                int current = 0;
                foreach (T item in items)
                {
                    if (current == index)
                        yield return insert;
                    yield return item;
                    current += 1;
                }
                if (current == index)
                    yield return insert;
            }
        }

        extension(int n)
        {
            public BigInteger Factorial()
            {
                BigInteger result = 1;
                for (int i = 2; i <= n; i += 1)
                    result *= i;
                return result;
            }
        }
    }
}
