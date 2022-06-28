using System.Diagnostics.CodeAnalysis;
using Unit = System.ValueTuple;
using static MarsonShine.Functional.F;

namespace MarsonShine.Functional
{
    public static partial class F
    {
        public static Option<T> Some<T>(T value) => new Option.Some<T>(value);
        public static Option.None None => Option.None.Default;  // the None value
    }

#pragma warning disable CS0660 // 类型定义运算符 == 或运算符 !=，但不重写 Object.Equals(object o)
#pragma warning disable CS0661 // 类型定义运算符 == 或运算符 !=，但不重写 Object.GetHashCode()
    public struct Option<T> : IEquatable<Option.None>, IEquatable<Option<T>>
#pragma warning restore CS0661 // 类型定义运算符 == 或运算符 !=，但不重写 Object.GetHashCode()
#pragma warning restore CS0660 // 类型定义运算符 == 或运算符 !=，但不重写 Object.Equals(object o)
    {
        readonly T value;
        readonly bool isSome;
        bool isNone => !isSome;

        private Option(T value)
        {
            if (value == null)
                throw new ArgumentNullException();

            this.value = value;
            this.isSome = true;
        }

        public static implicit operator Option<T>(Option.None _) => new Option<T>();
        public static implicit operator Option<T>(Option.Some<T> some) => new Option<T>(some.Value);

        public static implicit operator Option<T>(T value) => value == null ? None : Some(value);
        public TResult Match<TResult>(Func<TResult> None, Func<T, TResult> Some) => isSome ? Some(value) : None();
        public IEnumerable<T> AsEnumerable()
        {
            if (isSome) yield return value;
        }
        public bool Equals(Option<T> other)
         => this.isSome == other.isSome
         && (this.isNone || this.value!.Equals(other.value));

        public bool Equals(Option.None _) => isNone;

        public static bool operator ==(Option<T> @this, Option<T> other) => @this.Equals(other);
        public static bool operator !=(Option<T> @this, Option<T> other) => !(@this == other);

        public override string ToString() => isSome ? $"Some({value})" : "None";
    }

    namespace Option
    {
        public struct None
        {
            internal static readonly None Default = new();
        }
        public struct Some<T>
        {
            internal T Value { get; }

            internal Some(T value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)
                       , "Cannot wrap a null value in a 'Some'; use 'None' instead");
                Value = value;
            }
        }
    }

    public static class OptioExt
    {
        public static Option<R> Map<T, R>(this Option.Some<T> some, Func<T, R> f) => Some(f(some.Value));
        public static Option<R> Map<T, R>(this Option.None _, Func<T, R> f) => None;
        public static Option<R> Map<T, R>(this Option<T> opt, Func<T, R> f) => opt.Match(() => None, (t) => Some(f(t)));
        public static Option<Unit> ForEach<T>(this Option<T> opt, Action<T> action) => Map(opt, action.ToFunc());

        public static Option<R> Bind<T, R>(this Option<T> opt, Func<T, Option<R>> f) => opt.Match(() => None, (t) => f(t));
        public static IEnumerable<R> Bind<T, R>(this Option<T> opt, Func<T, IEnumerable<R>> func) => opt.AsEnumerable().Bind(func);
        public static Option<T> Where<T>(this Option<T> opt, Func<T, bool> predicate) => opt.Match(() => None, (t) => predicate(t) ? opt : None);
    }
}
