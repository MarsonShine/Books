namespace chapter_2
{
    public static class Extensions
    {
        extension<T>(IEnumerable<T> source)
        {
            public string Comma() => string.Join(", ", source);

            public string Bracket() => "[" + source.Comma() + "]";
        }

        extension<T>(IImStack<T> stack)
        {
            public IImStack<T> ReverseOnto(IImStack<T> tail)
            {
                var result = tail;
                for (; !stack.IsEmpty; stack = stack.Pop())
                    result = result.Push(stack.Peek());

                return result;
            }

            public IImStack<T> Reverse() => stack.ReverseOnto(ImStack<T>.Empty);

            /* 
             * 将当前栈(stack)移动到另一个栈(ys)的前面。
             * 例如：stack:[1,2] ys:[3,4]
             * stack.Concatenate(ys) = [1,2,3,4]
             */
            public IImStack<T> Concatenate(IImStack<T> ys) => ys.IsEmpty ? stack : stack.Reverse().ReverseOnto(ys);

            public IImStack<T> Append(T item) => stack.Concatenate(ImStack<T>.Empty.Push(item));
        }

        extension<T>(Covariance.IImStack<T> stack)
        {
            public Covariance.IImStack<T> Push(T item) => Covariance.ImStack<T>.Push(item, stack);
        }
    }
}
