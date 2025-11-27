using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace chapter_2
{
    public class HughesList
    {
        public static void SampleCode()
        {
            Console.WriteLine("The Hughes list");
            Func<int, int, int> adder = (x, y) => x + y;
            Console.WriteLine(adder(4, 3));
            Func<int, Func<int, int>> curried = x => y => x + y;
            Func<int, int> add4 = curried(4);
            Console.WriteLine(add4(3));

            var s = ImStack<int>.Empty.Push(2).Push(3).Push(4);
            var hl432 = HList<int>.FromStack(s);
            var hl = hl432.Push(5).Append(1).Concatenate(hl432).Append(0);
            Console.WriteLine(hl.Bracket());
            Console.WriteLine(HList<int>.Reverse(hl.ToStack()).Bracket());
        }
    }

    public struct HList<T> : IEnumerable<T>
    {
        delegate IImStack<T> Concat(IImStack<T> stack);
        private readonly Concat c;
        private HList(Concat c)
        {
            this.c = c;
        }
        private static HList<T> Make(Concat c) => new(c);
        public static HList<T> Empty { get; } = Make(stack => stack);
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
