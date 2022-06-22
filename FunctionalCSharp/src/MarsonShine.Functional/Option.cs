namespace MarsonShine.Functional
{
    using static F;
    public static partial class F {
        public static Option.None None => Option.None.Default;
        public static Option.Some<T> Some<T>(T value) => new Option.Some<T>(value);
    }

    namespace Option
    {
        public struct None
        {
            internal static readonly None Default = new None();
        }

        public struct Some<T>
        {
            internal T Value { get; }
            internal Some(T value) {
                if (value == null)
                    throw new ArgumentNullException();
                Value = value;
            }
        }
    }

        public struct Option<T> {
            readonly bool isSome;
            readonly T value;

            private Option(T value) {
                this.isSome = true;
                this.value = value;
            }
            public static implicit operator Option<T>(Option.None _) => new Option<T>();
            public static implicit operator Option<T>(Option.Some<T> some) => new Option<T>(some.Value);
            public static implicit operator Option<T>(T value) => value == null ? None : Some(value);
            public R Match<R>(Func<R> None, Func<T, R> Some) => isSome ? Some(value) : None();
        }
}