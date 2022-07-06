namespace MarsonShine.Functional
{
    using Unit = ValueTuple;
    public static partial class F
    {
        public static Exceptional<T> Exceptional<T>(T value) => new Exceptional<T>(value);
    }

    public struct Exceptional<T>
    {
        internal Exception? Ex { get; }
        internal T Value { get; }

        public bool Success => Ex == null;
        public bool Exception => Ex != null;

        internal Exceptional(Exception ex)
        {
            Ex = ex ?? throw new ArgumentNullException(nameof(ex));
            Value = default;
        }

        internal Exceptional(T right)
        {
            Value = right;
            Ex = null;
        }

        public static implicit operator Exceptional<T>(T right) => new(right);
        public static implicit operator Exceptional<T>(Exception left) => new(left);

        public TR Match<TR>(Func<Exception, TR> Exception, Func<T, TR> Success) => this.Exception ? Exception(Ex) : Success(Value);
        public Unit Match(Action<Exception> Exception, Action<T> Success) => Match(Exception.ToFunc(), Success.ToFunc());
        public override string ToString() => Match(
            ex => $"Exception({ex.Message})",
            t => $"Success({t})");
    }

    public static class Exceptional
    {
        public static Exceptional<R> Of<R>(Exception left) => new(left);
        public static Exceptional<R> Of<R>(R right) => new(right);

        public static Exceptional<RR> Map<R, RR>(this Exceptional<R> @this
         , Func<R, RR> func) => @this.Success ? func(@this.Value) : new Exceptional<RR>(@this.Ex!);

        public static Exceptional<R> Select<T, R>(this Exceptional<T> @this
         , Func<T, R> map) => @this.Map(map);

        public static Exceptional<RR> SelectMany<T, R, RR>(this Exceptional<T> @this
           , Func<T, Exceptional<R>> bind, Func<T, R, RR> project)
        {
            if (@this.Exception) return new Exceptional<RR>(@this.Ex!);
            var bound = bind(@this.Value);
            return bound.Exception
               ? new Exceptional<RR>(bound.Ex!)
               : project(@this.Value, bound.Value);
        }
    }
}
