namespace MarsonShine.Functional
{
    using static F;
    using Unit = ValueTuple;
    public static partial class F
    {
        public static Validation<T> Valid<T>(T value) => new(value);

        // create a Validation in the Invalid state
        public static Validation.Invalid Invalid(params Error[] errors) => new(errors);
        public static Validation<R> Invalid<R>(params Error[] errors) => new Validation.Invalid(errors);
        public static Validation.Invalid Invalid(IEnumerable<Error> errors) => new(errors);
        public static Validation<R> Invalid<R>(IEnumerable<Error> errors) => new Validation.Invalid(errors);
    }
    public struct Validation<T>
    {
        internal IEnumerable<Error> Errors { get; }
        internal T? Value { get; }

        public bool IsValid { get; }

        public static Func<T, Validation<T>> Return = t => Valid(t);

        public static Validation<T> Fail(IEnumerable<Error> errors)
           => new(errors);

        public static Validation<T> Fail(params Error[] errors)
           => new(errors.AsEnumerable());

        private Validation(IEnumerable<Error> errors)
        {
            IsValid = false;
            Errors = errors;
            Value = default;
        }

        internal Validation(T right)
        {
            IsValid = true;
            Value = right;
            Errors = Enumerable.Empty<Error>();
        }
        public static implicit operator Validation<T>(Error error)
         => new(new[] { error });
        public static implicit operator Validation<T>(Validation.Invalid left)
           => new(left.Errors);
        public static implicit operator Validation<T>(T right) => Valid(right);
        public TR Match<TR>(Func<IEnumerable<Error>, TR> Invalid, Func<T, TR> Valid)
        => IsValid ? Valid(Value!) : Invalid(this.Errors);

        public Unit Match(Action<IEnumerable<Error>> Invalid, Action<T> Valid)
           => Match(Invalid.ToFunc(), Valid.ToFunc());

    }

    public static class Validation
    {
        public struct Invalid
        {
            internal IEnumerable<Error> Errors;
            public Invalid(IEnumerable<Error> errors) { Errors = errors; }
        }

        public static Validation<RR> Map<R, RR>(this Validation<R> @this, Func<R, RR> f) =>
            @this.IsValid ? Valid(f(@this.Value!)) : Invalid(@this.Errors);

        public static Validation<Unit> ForEach<R>(this Validation<R> @this, Action<R> act) => Map(@this, act.ToFunc());

        public static Validation<T> Do<T>(this Validation<T> validation, Action<T> action)
        {
            validation.ForEach(action);
            return validation;
        }

        public static Validation<R> Bind<T, R>(this Validation<T> val, Func<T, Validation<R>> f) => val.Match(
            Invalid: (err) => Invalid(err),
            Valid: (r) => f(r)
            );
    }
}
